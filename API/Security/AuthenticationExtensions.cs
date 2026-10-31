using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DAL.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace API.Security;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddFindGoAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection("Jwt");
        var signingKey = jwt["SigningKey"];
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt["Issuer"] ?? "FINDGO",
                ValidateAudience = true,
                ValidAudience = jwt["Audience"] ?? "FINDGO.Client",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(string.IsNullOrWhiteSpace(signingKey)
                    ? RandomNumberGenerator.GetBytes(32) : Encoding.UTF8.GetBytes(signingKey)),
                ValidateLifetime = true,
                RoleClaimType = ClaimTypes.Role,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var users = context.HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>();
                    var id = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                    var stamp = context.Principal?.FindFirstValue("sst");
                    var user = id is null ? null : await users.FindByIdAsync(id);
                    if (user is null || !user.IsActive || !user.EmailConfirmed || stamp is null ||
                        !string.Equals(stamp, await users.GetSecurityStampAsync(user), StringComparison.Ordinal))
                    {
                        context.Fail("The access token has been revoked.");
                        return;
                    }

                    // Also invalidate stale permissions after a role change made directly in the DB.
                    var tokenRoles = context.Principal!.FindAll(ClaimTypes.Role).Select(c => c.Value)
                        .ToHashSet(StringComparer.Ordinal);
                    if (tokenRoles.Count == 0 || !tokenRoles.SetEquals(await users.GetRolesAsync(user)))
                        context.Fail("Account permissions have changed. Sign in again.");
                }
            };
        });
        services.AddAuthorization();
        return services;
    }
}
