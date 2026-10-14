namespace BLL.DTOs.Common;

public sealed class ParsedSearchCriteria
{
    public string? RawPrompt { get; init; }
    public string? InferredCategoryCode { get; init; }
    public string? InferredPurpose { get; init; }
    public IReadOnlyList<string> InferredAtmosphereTags { get; init; } = [];
    public IReadOnlyList<string> InferredAmenityCodes { get; init; } = [];
    public decimal? InferredBudgetMin { get; init; }
    public decimal? InferredBudgetMax { get; init; }
    public double? InferredMaxDistanceKm { get; init; }
    public int? InferredGroupSize { get; init; }
    public bool ExtractedByAi { get; init; }
    public string? SummaryExplanation { get; init; }
}
