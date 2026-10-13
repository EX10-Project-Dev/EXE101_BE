using System.ComponentModel.DataAnnotations;

namespace BLL.DTOs.Requests;

/// <summary>Optional criteria for finding open venues. An empty body is valid.</summary>
public sealed class VenueSearchRequest : IValidatableObject
{
    [MaxLength(1000)]
    public string? Prompt { get; init; }

    public string? Purpose { get; init; }

    [Range(0, 100_000_000)]
    public decimal? BudgetMin { get; init; }

    [Range(0, 100_000_000)]
    public decimal? BudgetMax { get; init; }

    [Range(0.1, 500)]
    public double? MaxDistanceKm { get; init; }

    [Range(1, 1000)]
    public int? GroupSize { get; init; }

    public IReadOnlyList<string> CategoryCodes { get; init; } = [];
    public IReadOnlyList<string> AtmosphereTags { get; init; } = [];
    public IReadOnlyList<string> AmenityCodes { get; init; } = [];
    public SearchOrigin? Origin { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 50)]
    public int PageSize { get; init; } = 10;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (BudgetMin.HasValue && BudgetMax.HasValue && BudgetMin > BudgetMax)
            yield return new ValidationResult("budgetMin must be less than or equal to budgetMax.", [nameof(BudgetMin), nameof(BudgetMax)]);

        if (MaxDistanceKm.HasValue && Origin is null)
            yield return new ValidationResult("origin is required when maxDistanceKm is provided.", [nameof(Origin)]);
    }
}

public sealed class SearchOrigin
{
    [Range(-90, 90)]
    public double Latitude { get; init; }

    [Range(-180, 180)]
    public double Longitude { get; init; }
}
