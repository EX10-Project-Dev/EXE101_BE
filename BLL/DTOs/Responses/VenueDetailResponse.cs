using System;
using System.Collections.Generic;

namespace BLL.DTOs.Responses;

public sealed class VenueDetailResponse
{
    public Guid VenueId { get; init; }
    public string? SourceId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string CategoryCode { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Address { get; init; } = string.Empty;
    public VenueLocation Location { get; init; } = new();
    public string? GoogleMapsUrl { get; init; }
    public string? GooglePlaceId { get; init; }
    public string? WebsiteUrl { get; init; }
    public string? MenuUrl { get; init; }
    public string? PhoneNumber { get; init; }
    public decimal? PriceMin { get; init; }
    public decimal? PriceMax { get; init; }
    public string? PriceBasis { get; init; }
    public string? PriceNote { get; init; }
    public string CurrencyCode { get; init; } = "VND";
    public bool IsOpenNow { get; init; }
    public double? RatingAverage { get; init; }
    public int ReviewCount { get; init; }
    public IReadOnlyList<VenuePhotoDto> Photos { get; init; } = [];
    public IReadOnlyList<VenueOpeningHourDto> OpeningHours { get; init; } = [];
    public IReadOnlyList<string> Amenities { get; init; } = [];
    public IReadOnlyList<string> AtmosphereTags { get; init; } = [];
    public IReadOnlyList<string> PurposeTags { get; init; } = [];
    public IReadOnlyDictionary<string, string?> SourceData { get; init; } = new Dictionary<string, string?>();
}

public sealed class VenuePhotoDto
{
    public Guid Id { get; init; }
    public string ImageUrl { get; init; } = string.Empty;
    public string PhotoType { get; init; } = string.Empty;
    public string? Caption { get; init; }
    public int SortOrder { get; init; }
}

public sealed class VenueOpeningHourDto
{
    public string DayOfWeek { get; init; } = string.Empty;
    public string? OpenTime { get; init; }
    public string? CloseTime { get; init; }
    public bool IsClosed { get; init; }
    public string? Note { get; init; }
}
