using System.ComponentModel.DataAnnotations;

namespace API.DTOs;

public sealed class SetAccountRoleRequest
{
    [Required, RegularExpression("^(customer|merchant|admin)$")]
    public string Role { get; init; } = string.Empty;
}

public sealed class ProvisionMerchantRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string FullName { get; init; } = string.Empty;
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; init; } = string.Empty;
    [Required, MinLength(1), MaxLength(100)]
    public Guid[] VenueIds { get; init; } = [];
}

public sealed class AssignMerchantVenuesRequest
{
    [Required, MaxLength(100)]
    public Guid[] VenueIds { get; init; } = [];
}

public sealed class ActivateMerchantRequest
{
    [Required]
    public string UserId { get; init; } = string.Empty;
    [Required]
    public string Token { get; init; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;
}

public sealed class UpdateMerchantVenueRequest : IValidatableObject
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; init; } = string.Empty;
    [StringLength(2000)]
    public string? Description { get; init; }
    [Required, StringLength(500, MinimumLength = 1)]
    public string Address { get; init; } = string.Empty;
    [Range(typeof(decimal), "0", "9999999999.99")]
    public decimal? PriceMin { get; init; }
    [Range(typeof(decimal), "0", "9999999999.99")]
    public decimal? PriceMax { get; init; }
    [StringLength(1000)]
    public string? PriceNote { get; init; }
    [StringLength(30)]
    public string? PhoneNumber { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Address))
            yield return new ValidationResult("Name and address cannot be blank.", [nameof(Name), nameof(Address)]);
        if (PriceMin.HasValue && PriceMax.HasValue && PriceMin > PriceMax)
            yield return new ValidationResult("PriceMin cannot exceed PriceMax.", [nameof(PriceMin), nameof(PriceMax)]);
    }
}
