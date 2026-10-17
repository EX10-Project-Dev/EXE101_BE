using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using API.DTOs;
using API.Services;
using DAL.Entities;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace API.Controllers;

public sealed class AuthController(
    UserManager<AppUser> userManager,
    IAppEmailSender emailSender,
    IConfiguration configuration,
    ILogger<AuthController> logger) : BaseApiController
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (!emailSender.IsConfigured || string.IsNullOrWhiteSpace(configuration["ClientApp:BaseUrl"]))
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Email chưa được cấu hình. Hãy cấu hình Email:ApiKey, Email:SecretKey, Email:From và ClientApp:BaseUrl trước khi đăng ký." });

        var email = request.Email.Trim();
        if (await userManager.FindByEmailAsync(email) is not null)
            return Conflict(new { message = "Email này đã được sử dụng." });

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            FullName = request.FullName.Trim(),
            IsActive = true
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return BadRequest(new { message = "Không thể tạo tài khoản.", errors = createResult.Errors.Select(x => x.Description) });

        try
        {
            await SendEmailConfirmationAsync(user, cancellationToken);
        }
        catch
        {
            await userManager.DeleteAsync(user);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Không gửi được email xác minh. Hãy kiểm tra cấu hình email rồi thử lại." });
        }

        return StatusCode(StatusCodes.Status201Created,
            new { message = "Tạo tài khoản thành công. Hãy kiểm tra email để xác minh trước khi đăng nhập." });
    }

    [HttpGet("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmailLink([FromQuery] string? userId, [FromQuery] string? token)
    {
        Response.Headers.CacheControl = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        Response.Headers["Content-Security-Policy"] = "default-src 'none'; style-src 'unsafe-inline'; frame-ancestors 'none'; base-uri 'none'";

        var success = false;
        if (!string.IsNullOrWhiteSpace(userId) && !string.IsNullOrWhiteSpace(token))
        {
            var result = await ConfirmEmail(new ConfirmEmailRequest { UserId = userId, Token = token });
            success = result is OkObjectResult;
        }

        var title = success ? "Email đã được xác minh" : "Không thể xác minh email";
        var message = success
            ? "Bạn có thể quay lại FIND&amp;GO để đăng nhập."
            : "Liên kết không hợp lệ hoặc đã hết hạn. Vui lòng quay lại ứng dụng và yêu cầu gửi lại email xác minh.";
        var loginLink = "";
        if (Uri.TryCreate(configuration["ClientApp:BaseUrl"], UriKind.Absolute, out var clientUrl)
            && (clientUrl.Scheme == "https" || clientUrl.Scheme == "http"))
        {
            var loginUrl = System.Net.WebUtility.HtmlEncode(clientUrl.AbsoluteUri.TrimEnd('/') + "/login");
            loginLink = $"<p><a href=\"{loginUrl}\">Về trang đăng nhập</a></p>";
        }

        return new ContentResult
        {
            StatusCode = success ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest,
            ContentType = "text/html; charset=utf-8",
            Content = $"<!doctype html><html lang=\"vi\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><title>{title} | FIND&amp;GO</title><style>body{{font-family:system-ui,sans-serif;margin:64px auto;padding:24px;max-width:600px;line-height:1.6}}a{{color:#145c39}}</style></head><body><main><h1>{title}</h1><p>{message}</p>{loginLink}</main></body></html>"
        };
    }

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailRequest request)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null)
            return BadRequest(new { message = "Liên kết xác minh không hợp lệ hoặc đã hết hạn." });

        string decodedToken;
        try { decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token)); }
        catch (FormatException) { return BadRequest(new { message = "Liên kết xác minh không hợp lệ hoặc đã hết hạn." }); }

        var result = await userManager.ConfirmEmailAsync(user, decodedToken);
        return result.Succeeded
            ? Ok(new { message = "Email đã được xác minh. Bạn có thể đăng nhập." })
            : BadRequest(new { message = "Liên kết xác minh không hợp lệ hoặc đã hết hạn." });
    }

    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendConfirmation(EmailTokenRequest request, CancellationToken cancellationToken)
    {
        if (!emailSender.IsConfigured || string.IsNullOrWhiteSpace(configuration["ClientApp:BaseUrl"]))
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Dịch vụ email chưa được cấu hình." });

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is not null && !user.EmailConfirmed)
        {
            try { await SendEmailConfirmationAsync(user, cancellationToken); }
            catch (Exception exception) { logger.LogError(exception, "Confirmation email could not be sent."); }
        }

        return Ok(new { message = "Nếu tài khoản cần xác minh, email hướng dẫn sẽ được gửi." });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (!HasJwtSigningKey())
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "JWT chưa được cấu hình trên máy chủ." });

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive || !await userManager.IsEmailConfirmedAsync(user))
            return Unauthorized(new { message = "Email hoặc mật khẩu không đúng, hoặc email chưa được xác minh." });

        if (await userManager.IsLockedOutAsync(user))
            return StatusCode(StatusCodes.Status423Locked, new { message = "Tài khoản tạm khóa do đăng nhập sai nhiều lần." });

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            return Unauthorized(new { message = "Email hoặc mật khẩu không đúng." });
        }

        await userManager.ResetAccessFailedCountAsync(user);
        return Ok(await CreateAuthResponseAsync(user));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();

        // The access token is stateless; rotating the security stamp revokes all tokens issued to this account.
        var result = await userManager.UpdateSecurityStampAsync(user);
        if (!result.Succeeded)
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Không thể đăng xuất lúc này." });

        return Ok(new { message = "Đã đăng xuất." });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(EmailTokenRequest request, CancellationToken cancellationToken)
    {
        var genericMessage = "Nếu email thuộc về tài khoản đã xác minh, hướng dẫn đặt lại mật khẩu sẽ được gửi.";
        if (!emailSender.IsConfigured || string.IsNullOrWhiteSpace(configuration["ClientApp:BaseUrl"]))
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Dịch vụ email chưa được cấu hình." });

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !await userManager.IsEmailConfirmedAsync(user)) return Ok(new { message = genericMessage });

        try
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var url = BuildClientUrl("auth/reset-password", new Dictionary<string, string?>
            {
                ["email"] = user.Email,
                ["token"] = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token))
            });
            var safeUrl = System.Net.WebUtility.HtmlEncode(url);
            await emailSender.SendAsync(user.Email!, "Đặt lại mật khẩu FIND&GO",
                $"<p>Nhấn vào liên kết để đặt lại mật khẩu:</p><p><a href=\"{safeUrl}\">Đặt lại mật khẩu</a></p><p>Nếu bạn không yêu cầu thao tác này, hãy bỏ qua email.</p>", cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Password reset email could not be sent.");
            return Ok(new { message = genericMessage });
        }

        return Ok(new { message = genericMessage });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null) return BadRequest(new { message = "Yêu cầu đặt lại mật khẩu không hợp lệ hoặc đã hết hạn." });

        string decodedToken;
        try { decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token)); }
        catch (FormatException) { return BadRequest(new { message = "Yêu cầu đặt lại mật khẩu không hợp lệ hoặc đã hết hạn." }); }

        var result = await userManager.ResetPasswordAsync(user, decodedToken, request.NewPassword);
        return result.Succeeded
            ? Ok(new { message = "Mật khẩu đã được cập nhật." })
            : BadRequest(new { message = "Không thể đặt lại mật khẩu.", errors = result.Errors.Select(x => x.Description) });
    }

    [HttpPost("google")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleLogin(GoogleLoginRequest request)
    {
        var clientIds = configuration.GetSection("Google:ClientIds").Get<string[]>() ?? [];
        clientIds = clientIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).ToArray();
        if (clientIds.Length == 0)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Google Login chưa được cấu hình." });
        if (!HasJwtSigningKey())
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "JWT chưa được cấu hình trên máy chủ." });

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken,
                new GoogleJsonWebSignature.ValidationSettings { Audience = clientIds });
        }
        catch (InvalidJwtException)
        {
            return Unauthorized(new { message = "Google ID token không hợp lệ hoặc đã hết hạn." });
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Google ID token validation failed.");
            return Unauthorized(new { message = "Không xác minh được tài khoản Google." });
        }

        if (string.IsNullOrWhiteSpace(payload.Email) || !payload.EmailVerified || string.IsNullOrWhiteSpace(payload.Subject))
            return Unauthorized(new { message = "Tài khoản Google cần có email đã xác minh." });

        const string provider = "Google";
        var user = await userManager.FindByLoginAsync(provider, payload.Subject);
        if (user is null)
        {
            user = await userManager.FindByEmailAsync(payload.Email);
            if (user is null)
            {
                user = new AppUser
                {
                    UserName = payload.Email,
                    Email = payload.Email,
                    EmailConfirmed = true,
                    FullName = payload.Name,
                    IsActive = true
                };
                var createResult = await userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                    return Conflict(new { message = "Không thể tạo tài khoản từ Google.", errors = createResult.Errors.Select(x => x.Description) });
            }
            else if (!user.EmailConfirmed)
            {
                user.EmailConfirmed = true;
                var confirmResult = await userManager.UpdateAsync(user);
                if (!confirmResult.Succeeded)
                    return Conflict(new { message = "Không thể liên kết tài khoản Google." });
            }

            var linkResult = await userManager.AddLoginAsync(user, new UserLoginInfo(provider, payload.Subject, provider));
            if (!linkResult.Succeeded && !linkResult.Errors.Any(x => x.Code == "LoginAlreadyAssociated"))
                return Conflict(new { message = "Không thể liên kết tài khoản Google." });
        }

        if (!user.IsActive) return StatusCode(StatusCodes.Status403Forbidden, new { message = "Tài khoản đã bị vô hiệu hóa." });
        return Ok(await CreateAuthResponseAsync(user));
    }

    private async Task SendEmailConfirmationAsync(AppUser user, CancellationToken cancellationToken)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        // Configure the public API URL when running behind a proxy or sending links to another device.
        var apiBaseUrl = configuration["Api:PublicBaseUrl"];
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            apiBaseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        var url = QueryHelpers.AddQueryString($"{apiBaseUrl.TrimEnd('/')}/api/v1/auth/confirm-email", new Dictionary<string, string?>
        {
            ["userId"] = user.Id,
            ["token"] = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token))
        });
        var safeUrl = System.Net.WebUtility.HtmlEncode(url);
        await emailSender.SendAsync(user.Email!, "Xác minh email FIND&GO",
            $"<p>Nhấn vào liên kết để xác minh email:</p><p><a href=\"{safeUrl}\">Xác minh email</a></p>", cancellationToken);
    }

    private string BuildClientUrl(string path, IDictionary<string, string?> query)
    {
        var baseUrl = configuration["ClientApp:BaseUrl"]!.TrimEnd('/');
        return QueryHelpers.AddQueryString($"{baseUrl}/{path}", query);
    }

    private bool HasJwtSigningKey()
    {
        var key = configuration["Jwt:SigningKey"];
        return !string.IsNullOrWhiteSpace(key) && Encoding.UTF8.GetByteCount(key) >= 32;
    }

    private async Task<object> CreateAuthResponseAsync(AppUser user)
    {
        var jwt = configuration.GetSection("Jwt");
        var signingKey = jwt["SigningKey"];
        if (string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32)
            throw new InvalidOperationException("Jwt:SigningKey must be configured with at least 32 bytes.");

        var issuer = jwt["Issuer"] ?? "FINDGO";
        var audience = jwt["Audience"] ?? "FINDGO.Client";
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(int.TryParse(jwt["AccessTokenMinutes"], out var minutes) ? Math.Clamp(minutes, 5, 1440) : 60);
        var securityStamp = await userManager.GetSecurityStampAsync(user);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.FullName ?? user.UserName ?? user.Email ?? user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("sst", securityStamp)
        };
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, claims, now, expires, credentials);
        return new
        {
            accessToken = new JwtSecurityTokenHandler().WriteToken(token),
            tokenType = "Bearer",
            expiresAtUtc = expires,
            user = new { userId = user.Id, email = user.Email, fullName = user.FullName }
        };
    }
}
