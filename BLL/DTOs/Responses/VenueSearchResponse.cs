using System;
using System.Collections.Generic;

namespace BLL.DTOs.Responses;

public sealed class VenueSearchResponse
{
    public Guid SearchHistoryId { get; init; }
    public string Status { get; init; } = "NO_MATCH";
    public string Message { get; init; } = string.Empty;
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public bool HasMore { get; init; }
    public IReadOnlyList<VenueSearchResult> Results { get; init; } = [];
}

public sealed class VenueSearchResult
{
    public VenueSummary Venue { get; init; } = new();
    public double? DistanceKm { get; init; }
    public double MatchScore { get; init; }
    public string? RecommendationReason { get; init; }
    public IReadOnlyList<string> MatchReasons { get; init; } = [];
}

public sealed class VenueSummary
{
    public Guid VenueId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string? ShortDescription { get; init; }
    public IReadOnlyList<string> ImageUrls { get; init; } = [];
    public string Address { get; init; } = string.Empty;
    public VenueLocation Location { get; init; } = new();
    public decimal? PriceMin { get; init; }
    public decimal? PriceMax { get; init; }
    public string CurrencyCode { get; init; } = "VND";
    public bool IsOpenNow { get; init; }
    public double? RatingAverage { get; init; }
    public int ReviewCount { get; init; }
    public IReadOnlyList<string> AmenityCodes { get; init; } = [];
}

public sealed class VenueLocation
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
}
