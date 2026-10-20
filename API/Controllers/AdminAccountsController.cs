using System.Security.Claims;
using API.DTOs;
using API.Services;
using DAL.Constants;
using DAL.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[Route("api/v1/admin")]
[ApiController]
[Authorize(Roles = AppRoles.Admin)]
public sealed class AdminAccountsController(AccountAdministrationService accounts, AppDbContext db) : ControllerBase
{
    [HttpGet("users")]
    public async Task<IActionResult> Users([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100 || page > int.MaxValue / pageSize) return BadRequest();
        var users = await db.Users.AsNoTracking().OrderBy(u => u.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => new { userId = u.Id, u.Email, u.FullName, u.IsActive, u.EmailConfirmed,
                roles = db.UserRoles.Where(ur => ur.UserId == u.Id)
                    .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name).ToArray() })
            .ToListAsync(cancellationToken);
        return Ok(new { page, pageSize, totalCount = await db.Users.CountAsync(cancellationToken), users });
    }

    [HttpPut("users/{userId}/role")]
    public Task<IActionResult> SetRole(string userId, SetAccountRoleRequest request) => Handle(async () =>
    {
        await accounts.SetRoleAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, userId, request.Role);
        return Ok(new { message = "Đã cập nhật quyền. Tài khoản cần đăng nhập lại.", userId, roles = new[] { request.Role } });
    });

    [HttpPost("merchants")]
    public Task<IActionResult> CreateMerchant(ProvisionMerchantRequest request, CancellationToken cancellationToken) => Handle(async () =>
    {
        var userId = await accounts.ProvisionMerchantAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created,
            new { userId, roles = new[] { AppRoles.Merchant }, message = "Đã cấp tài khoản merchant và gửi email đặt mật khẩu." });
    });

    [HttpPost("merchants/{userId}/resend-invitation")]
    public Task<IActionResult> ResendInvitation(string userId, CancellationToken cancellationToken) => Handle(async () =>
    {
        await accounts.SendInvitationAsync(userId, cancellationToken);
        return Ok(new { message = "Đã gửi lại email kích hoạt merchant." });
    });

    [HttpPut("merchants/{userId}/venues")]
    public Task<IActionResult> AssignVenues(string userId, AssignMerchantVenuesRequest request) => Handle(async () =>
    {
        await accounts.AssignVenuesAsync(userId, request.VenueIds);
        return Ok(new { message = "Đã cập nhật danh sách quán được quản lý." });
    });

    private static async Task<IActionResult> Handle(Func<Task<IActionResult>> action)
    {
        try { return await action(); }
        catch (AccountOperationException exception)
        {
            return new ObjectResult(new { message = exception.Message }) { StatusCode = exception.StatusCode };
        }
    }
}
