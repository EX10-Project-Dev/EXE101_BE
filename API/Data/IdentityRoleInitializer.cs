using API.Services;
using DAL.Constants;
using DAL.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public static class IdentityRoleInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        await using var scope = services.CreateAsyncScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var accountRoles = scope.ServiceProvider.GetRequiredService<AccountRoleService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(IdentityRoleInitializer));

        foreach (var roleName in AppRoles.All)
        {
            if (await roles.RoleExistsAsync(roleName)) continue;
            var result = await roles.CreateAsync(new IdentityRole(roleName));
            if (!result.Succeeded && !await roles.RoleExistsAsync(roleName))
                throw new InvalidOperationException("Could not initialize account roles.");
        }

        var db = scope.ServiceProvider.GetRequiredService<DAL.Data.AppDbContext>();
        var unassigned = await users.Users.Where(u => !db.UserRoles.Any(r => r.UserId == u.Id))
            .ToListAsync();
        foreach (var user in unassigned)
            await accountRoles.EnsureCustomerWhenUnassignedAsync(user);

        // Explicit allowlist for initial provisioning. Remove it after setup so later role changes persist.
        // Never choose an account by its name or by who registers first.
        var emails = (configuration.GetSection("RoleSetup:AdminEmails").Get<string[]>() ?? [])
            .Append(configuration["RoleSetup:AdminEmail"] ?? string.Empty)
            .Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);
        var administration = scope.ServiceProvider.GetRequiredService<AccountAdministrationService>();
        foreach (var email in emails)
        {
            var admin = await users.FindByEmailAsync(email);
            if (admin is null || !admin.IsActive || !admin.EmailConfirmed)
            {
                logger.LogWarning("Admin bootstrap skipped for {AdminEmail}: account must exist, be active and have a verified email.", email);
                continue;
            }
            await administration.SetRoleAsync(admin.Id, admin.Id, AppRoles.Admin);
        }
    }
}
