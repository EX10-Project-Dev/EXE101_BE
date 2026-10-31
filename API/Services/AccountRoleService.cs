using DAL.Constants;
using DAL.Entities;
using Microsoft.AspNetCore.Identity;

namespace API.Services;

public sealed class AccountRoleService(UserManager<AppUser> users)
{
    public async Task<IList<string>> EnsureCustomerWhenUnassignedAsync(AppUser user)
    {
        var roles = await users.GetRolesAsync(user);
        if (roles.Count != 0) return roles;

        var result = await users.AddToRoleAsync(user, AppRoles.Customer);
        // Another request may have assigned a role in the meantime.
        if (!result.Succeeded && (await users.GetRolesAsync(user)).Count == 0)
            throw new InvalidOperationException("Could not assign the default account role.");
        return await users.GetRolesAsync(user);
    }
}
