using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public sealed class RegisterRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string FullName { get; init; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(100)]
    public string Password { get; init; } = string.Empty;
}

public sealed class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public sealed class EmailTokenRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;
}

public sealed class ConfirmEmailRequest
{
    [Required]
    public string UserId { get; init; } = string.Empty;

    [Required]
    public string Token { get; init; } = string.Empty;
}

public sealed class ResetPasswordRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Token { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string NewPassword { get; init; } = string.Empty;
}

public sealed class GoogleLoginRequest
{
    [Required]
    public string IdToken { get; init; } = string.Empty;
}
