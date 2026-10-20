using System.Data;
using System.Text;
using API.DTOs;
using DAL.Constants;
using DAL.Data;
using DAL.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public sealed class AccountOperationException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class AccountAdministrationService(
    AppDbContext db, UserManager<AppUser> users, IAppEmailSender email,
    IConfiguration configuration, ILogger<AccountAdministrationService> logger)
{
    public Task SetRoleAsync(string actorId, string userId, string role) => InTransaction(async () =>
    {
        if (!AppRoles.All.Contains(role, StringComparer.Ordinal))
            throw Error(400, "Role không hợp lệ.");
        var user = await users.FindByIdAsync(userId) ?? throw Error(404, "Không tìm thấy tài khoản.");
        var oldRoles = await users.GetRolesAsync(user);
        if (oldRoles.Count == 1 && oldRoles[0] == role) return true;
        if (oldRoles.Contains(AppRoles.Admin) && role != AppRoles.Admin)
        {
            if (actorId == userId) throw Error(409, "Bạn không thể tự hạ quyền admin của mình.");
            if ((await users.GetUsersInRoleAsync(AppRoles.Admin)).Count <= 1)
                throw Error(409, "Không thể xóa quyền của admin cuối cùng.");
        }
        if (role != AppRoles.Customer && (!user.IsActive || !user.EmailConfirmed))
            throw Error(400, "Tài khoản phải hoạt động và đã xác minh email trước khi được nâng quyền.");

        Require(await users.RemoveFromRolesAsync(user, oldRoles));
        Require(await users.AddToRoleAsync(user, role));
        Require(await users.UpdateSecurityStampAsync(user));
        if (role != AppRoles.Merchant)
            await db.Venues.Where(v => v.MerchantUserId == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.MerchantUserId, (string?)null));
        return true;
    });

    public async Task<string> ProvisionMerchantAsync(ProvisionMerchantRequest request, CancellationToken cancellationToken)
    {
        if (!email.IsConfigured || !HasClientUrl()) throw Error(503, "Chưa cấu hình email hoặc URL frontend.");
        var id = await InTransaction(async () =>
        {
            if (await users.FindByEmailAsync(request.Email.Trim()) is not null)
                throw Error(409, "Email đã tồn tại. Hãy cấp role merchant cho tài khoản đó rồi gán quán.");
            var venues = await LoadAssignableVenues(request.VenueIds, null);
            var user = new AppUser { UserName = request.Email.Trim(), Email = request.Email.Trim(),
                FullName = request.FullName.Trim(), IsActive = true, EmailConfirmed = false };
            Require(await users.CreateAsync(user));
            Require(await users.AddToRoleAsync(user, AppRoles.Merchant));
            foreach (var venue in venues) venue.MerchantUserId = user.Id;
            await db.SaveChangesAsync(cancellationToken);
            return user.Id;
        });
        // Keep the allocated account if the provider fails, so an admin can resend the invitation.
        await SendInvitationAsync(id, cancellationToken);
        return id;
    }

    public Task AssignVenuesAsync(string userId, Guid[] venueIds) => InTransaction(async () =>
    {
        var user = await users.FindByIdAsync(userId) ?? throw Error(404, "Không tìm thấy tài khoản.");
        if (!user.IsActive || !await users.IsInRoleAsync(user, AppRoles.Merchant))
            throw Error(400, "Tài khoản phải có role merchant và đang hoạt động.");
        var requested = await LoadAssignableVenues(venueIds, userId);
        var assigned = await db.Venues.Where(v => v.MerchantUserId == userId).ToListAsync();
        foreach (var venue in assigned) venue.MerchantUserId = null;
        foreach (var venue in requested) venue.MerchantUserId = userId;
        await db.SaveChangesAsync();
        return true;
    });

    public async Task SendInvitationAsync(string userId, CancellationToken cancellationToken)
    {
        if (!email.IsConfigured || !HasClientUrl()) throw Error(503, "Chưa cấu hình email hoặc URL frontend.");
        var user = await users.FindByIdAsync(userId) ?? throw Error(404, "Không tìm thấy tài khoản.");
        if (!user.IsActive || !await users.IsInRoleAsync(user, AppRoles.Merchant) || user.EmailConfirmed || await users.HasPasswordAsync(user))
            throw Error(400, "Tài khoản không còn ở trạng thái chờ kích hoạt merchant.");
        var token = await users.GeneratePasswordResetTokenAsync(user);
        var url = QueryHelpers.AddQueryString(configuration["ClientApp:BaseUrl"]!.TrimEnd('/') + "/auth/activate-merchant",
            new Dictionary<string, string?> { ["userId"] = user.Id,
                ["token"] = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token)) });
        var safeUrl = System.Net.WebUtility.HtmlEncode(url);
        try
        {
            await email.SendAsync(user.Email!, "Kích hoạt tài khoản đối tác FIND&GO",
                $"<p>Bạn được cấp tài khoản merchant trên FIND&amp;GO.</p><p><a href=\"{safeUrl}\">Xác minh email và đặt mật khẩu</a></p><p>Liên kết có hiệu lực trong 3 giờ. Nếu không yêu cầu tài khoản này, bạn có thể bỏ qua email.</p>", cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogError("Merchant invitation delivery failed for account {UserId}.", userId);
            throw Error(503, "Tài khoản merchant đã được lưu nhưng chưa gửi được email. Admin có thể gửi lại lời mời.");
        }
    }

    public Task ActivateMerchantAsync(ActivateMerchantRequest request) => InTransaction(async () =>
    {
        var user = await users.FindByIdAsync(request.UserId);
        const string invalid = "Liên kết kích hoạt không hợp lệ, đã dùng hoặc đã hết hạn.";
        if (user is null || !user.IsActive || user.EmailConfirmed || await users.HasPasswordAsync(user) ||
            !await users.IsInRoleAsync(user, AppRoles.Merchant)) throw Error(400, invalid);
        string token;
        try { token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token)); }
        catch (FormatException) { throw Error(400, invalid); }
        if (!await users.VerifyUserTokenAsync(user, users.Options.Tokens.PasswordResetTokenProvider,
                UserManager<AppUser>.ResetPasswordTokenPurpose, token)) throw Error(400, invalid);
        var result = await users.ResetPasswordAsync(user, token, request.Password);
        if (!result.Succeeded) throw Error(400, "Không đặt được mật khẩu. Mật khẩu cần ít nhất 8 ký tự, gồm chữ hoa, chữ thường và số.");
        user.EmailConfirmed = true;
        Require(await users.UpdateAsync(user));
        Require(await users.UpdateSecurityStampAsync(user));
        return true;
    });

    private bool HasClientUrl() => Uri.TryCreate(configuration["ClientApp:BaseUrl"], UriKind.Absolute, out var url)
        && (url.Scheme == "https" || url.Scheme == "http");

    private async Task<List<Venue>> LoadAssignableVenues(Guid[] venueIds, string? ownerId)
    {
        var ids = venueIds.Distinct().ToArray();
        var venues = await db.Venues.Where(v => ids.Contains(v.Id) && !v.IsDeleted).ToListAsync();
        if (venues.Count != ids.Length) throw Error(404, "Có quán không tồn tại hoặc đã bị xóa.");
        if (venues.Any(v => v.MerchantUserId is not null && v.MerchantUserId != ownerId))
            throw Error(409, "Có quán đã được giao cho merchant khác. Cần gỡ liên kết cũ trước.");
        return venues;
    }

    private async Task<T> InTransaction<T>(Func<Task<T>> action)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var result = await action();
            await transaction.CommitAsync();
            return result;
        });
    }

    private static AccountOperationException Error(int status, string message) => new(status, message);
    private static void Require(IdentityResult result)
    {
        if (!result.Succeeded) throw Error(409, "Không thể cập nhật tài khoản. Hãy tải lại dữ liệu và thử lại.");
    }
}
