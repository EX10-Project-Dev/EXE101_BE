using System.Runtime.InteropServices;
using System.Text.Json;
using BLL.DTOs.Common;
using BLL.DTOs.Requests;
using BLL.DTOs.Responses;
using BLL.Interfaces;
using DAL.Data;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BLL.Services;

/// <summary>
/// Searches and recommends venues with multi-criteria filtering, distance calculation,
/// amenity parsing, and intelligent scoring.
/// </summary>
public sealed class VenueSearchService : IVenueSearchService
{
    private readonly AppDbContext _db;
    private readonly bool _includePendingVenues;
    private readonly IPromptAnalysisService _promptAnalysisService;

    public VenueSearchService(
        AppDbContext db,
        IConfiguration configuration,
        IPromptAnalysisService promptAnalysisService)
    {
        _db = db;
        _includePendingVenues = configuration.GetValue<bool>("Search:IncludePendingVenues");
        _promptAnalysisService = promptAnalysisService;
    }

    public async Task<VenueSearchResponse> SearchAsync(VenueSearchRequest request, CancellationToken cancellationToken = default)
    {
        var now = GetVietnamLocalTime();

        var query = _db.Venues
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Category)
            .Include(x => x.Photos)
            .Include(x => x.OpeningHours)
            .Include(x => x.HoursExceptions)
            .Include(x => x.Amenities).ThenInclude(va => va.Amenity)
            .Include(x => x.Tags).ThenInclude(vt => vt.Tag)
            .Where(x => !x.IsDeleted);

        if (!_includePendingVenues)
        {
            query = query.Where(x => x.Status == VenueStatus.Published);
        }

        var venues = await query.ToListAsync(cancellationToken);

        // 0. Parse Prompt with AI / Rule-based NLP if provided
        ParsedSearchCriteria? parsedCriteria = null;
        if (!string.IsNullOrWhiteSpace(request.Prompt))
        {
            parsedCriteria = await _promptAnalysisService.AnalyzePromptAsync(request.Prompt, cancellationToken);
        }

        // Pre-process filters with prompt fallback
        var categoryCodes = request.CategoryCodes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (categoryCodes.Count == 0 && !string.IsNullOrWhiteSpace(parsedCriteria?.InferredCategoryCode))
        {
            categoryCodes.Add(parsedCriteria.InferredCategoryCode);
        }

        var requestedAmenities = request.AmenityCodes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToUpperInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (parsedCriteria is not null)
        {
            foreach (var am in parsedCriteria.InferredAmenityCodes)
            {
                requestedAmenities.Add(am);
            }
        }

        var requestedAtmospheres = request.AtmosphereTags
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToUpperInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (parsedCriteria is not null)
        {
            foreach (var at in parsedCriteria.InferredAtmosphereTags)
            {
                requestedAtmospheres.Add(at);
            }
        }

        var prompt = request.Prompt?.Trim().ToLowerInvariant();
        var purpose = request.Purpose?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(purpose) && !string.IsNullOrEmpty(parsedCriteria?.InferredPurpose))
        {
            purpose = parsedCriteria.InferredPurpose.ToUpperInvariant();
        }

        var budgetMin = request.BudgetMin ?? parsedCriteria?.InferredBudgetMin;
        var budgetMax = request.BudgetMax ?? parsedCriteria?.InferredBudgetMax;
        var maxDistanceKm = request.MaxDistanceKm ?? parsedCriteria?.InferredMaxDistanceKm;
        var groupSize = request.GroupSize ?? parsedCriteria?.InferredGroupSize;

        var scoredResults = new List<VenueSearchResult>();

        foreach (var venue in venues)
        {
            // 1. Check open now (per Thai's schedule & exception logic)
            var isOpen = IsOpenAt(venue, now);
            if (request.OpenNowOnly && !isOpen)
                continue;

            // 2. Category hard filter
            if (categoryCodes.Count > 0 && !categoryCodes.Contains(venue.Category?.Code ?? string.Empty))
                continue;

            // 3. Budget hard filter
            if (budgetMin.HasValue && venue.PriceMax.HasValue && venue.PriceMax < budgetMin.Value)
                continue;

            if (budgetMax.HasValue && venue.PriceMin.HasValue && venue.PriceMin > budgetMax.Value)
                continue;

            // 4. Distance calculation & hard filter
            double? distanceKm = null;
            if (request.Origin is not null)
            {
                distanceKm = CalculateDistanceKm(
                    request.Origin.Latitude,
                    request.Origin.Longitude,
                    (double)venue.Latitude,
                    (double)venue.Longitude);
            }

            if (maxDistanceKm.HasValue && (!distanceKm.HasValue || distanceKm.Value > maxDistanceKm.Value))
                continue;

            // 5. Amenities & tags parsing (from relational tables or SourceDataJson)
            var sourceData = DeserializeSourceData(venue.SourceDataJson);
            var venueAmenities = ExtractAmenityCodes(venue, sourceData);
            var (venuePurposes, venueAtmospheres) = ExtractTags(venue, sourceData);

            // Amenities hard filter: must have all requested amenities
            if (requestedAmenities.Count > 0 && !requestedAmenities.IsSubsetOf(venueAmenities))
                continue;

            // 6. Recommendation Scoring (0.0 -> 1.0)
            var score = 0.50; // Base score

            // Distance score bonus (up to +0.25)
            if (distanceKm.HasValue)
            {
                if (distanceKm.Value <= 1.0) score += 0.25;
                else if (distanceKm.Value <= 3.0) score += 0.20;
                else if (distanceKm.Value <= 5.0) score += 0.15;
                else if (distanceKm.Value <= 10.0) score += 0.08;
                else score += 0.02;
            }

            // Purpose bonus (+0.15)
            var hasPurposeMatch = !string.IsNullOrEmpty(purpose) &&
                venuePurposes.Any(p => p.Equals(purpose, StringComparison.OrdinalIgnoreCase));
            if (hasPurposeMatch) score += 0.15;

            // Atmosphere bonus (+0.15)
            var hasAtmosphereMatch = requestedAtmospheres.Count > 0 &&
                venueAtmospheres.Any(a => requestedAtmospheres.Contains(a));
            if (hasAtmosphereMatch) score += 0.15;

            // Budget fit bonus (+0.10)
            if (budgetMin.HasValue || budgetMax.HasValue)
            {
                score += 0.10;
            }

            // Group size bonus (+0.05)
            if (groupSize.HasValue && groupSize.Value >= 4 && venueAmenities.Contains("GROUP_SEATING"))
            {
                score += 0.05;
            }

            // Prompt keyword bonus (+0.15)
            if (!string.IsNullOrWhiteSpace(prompt))
            {
                var combinedText = $"{venue.Name} {venue.Description} {venue.Category?.Name}".ToLowerInvariant();
                if (combinedText.Contains(prompt))
                {
                    score += 0.15;
                }
            }

            var finalScore = Math.Min(1.0, Math.Round(score, 2));

            // 7. Generate reasons
            var reasons = new List<string>();
            if (distanceKm.HasValue)
            {
                reasons.Add($"Cách vị trí bắt đầu {Math.Round(distanceKm.Value, 1):0.#} km");
            }
            if (venue.PriceMin.HasValue && venue.PriceMax.HasValue)
            {
                reasons.Add($"Mức giá {venue.PriceMin.Value:N0} - {venue.PriceMax.Value:N0} VND phù hợp");
            }
            else if (!string.IsNullOrWhiteSpace(venue.PriceNote))
            {
                reasons.Add($"Giá: {venue.PriceNote}");
            }

            if (hasPurposeMatch)
            {
                reasons.Add($"Phù hợp mục đích {purpose}");
            }
            if (hasAtmosphereMatch)
            {
                reasons.Add("Không gian phù hợp nhu cầu");
            }
            if (venueAmenities.Contains("WIFI") && venueAmenities.Contains("AIR_CONDITIONING"))
            {
                reasons.Add("Có Wi-Fi và máy lạnh");
            }
            else if (venueAmenities.Contains("WIFI"))
            {
                reasons.Add("Có Wi-Fi miễn phí");
            }

            if (reasons.Count == 0)
            {
                reasons.Add(isOpen ? "Đang mở theo lịch hoạt động hiện tại." : "Địa điểm phù hợp với tìm kiếm của bạn.");
            }

            var summaryReason = string.Join(", ", reasons) + ".";

            // 8. Image URLs
            var imageUrls = venue.Photos
                .Where(p => VenueMediaHelper.IsValidImageUrl(p.ImageUrl))
                .OrderBy(p => p.SortOrder)
                .Select(p => VenueMediaHelper.CleanImageUrl(p.ImageUrl))
                .ToList();

            if (imageUrls.Count == 0)
            {
                if (sourceData.TryGetValue("exteriorPhotoUrl", out var ext) && VenueMediaHelper.IsValidImageUrl(ext))
                    imageUrls.Add(VenueMediaHelper.CleanImageUrl(ext));
                if (sourceData.TryGetValue("interiorPhotoUrl", out var inte) && VenueMediaHelper.IsValidImageUrl(inte))
                    imageUrls.Add(VenueMediaHelper.CleanImageUrl(inte));
                if (sourceData.TryGetValue("menuPhotoUrl", out var menu) && VenueMediaHelper.IsValidImageUrl(menu))
                    imageUrls.Add(VenueMediaHelper.CleanImageUrl(menu));
            }

            if (imageUrls.Count == 0)
            {
                var defaultImg = VenueMediaHelper.GetDefaultImageUrl(venue.Category?.Code);
                imageUrls.Add(defaultImg);
            }

            var summary = new VenueSummary
            {
                VenueId = venue.Id,
                Name = venue.Name,
                CategoryName = venue.Category?.Name ?? string.Empty,
                ShortDescription = venue.Description,
                ImageUrls = imageUrls,
                Address = venue.Address,
                Location = new VenueLocation
                {
                    Latitude = (double)venue.Latitude,
                    Longitude = (double)venue.Longitude
                },
                DistanceKm = distanceKm.HasValue ? Math.Round(distanceKm.Value, 2) : null,
                GoogleMapsUrl = VenueMediaHelper.BuildGoogleMapsUrl(venue, sourceData),
                GooglePlaceId = venue.GooglePlaceId,
                PriceMin = venue.PriceMin,
                PriceMax = venue.PriceMax,
                PriceNote = venue.PriceNote,
                SourceData = sourceData,
                CurrencyCode = venue.CurrencyCode,
                IsOpenNow = isOpen,
                RatingAverage = 4.6,
                ReviewCount = 20,
                AmenityCodes = venueAmenities.ToList()
            };

            scoredResults.Add(new VenueSearchResult
            {
                Venue = summary,
                DistanceKm = summary.DistanceKm,
                MatchScore = finalScore,
                RecommendationReason = summaryReason,
                MatchReasons = reasons
            });
        }

        // 9. Sort by MatchScore DESC, then Distance ASC
        var ordered = request.Origin is null
            ? scoredResults.OrderByDescending(x => x.MatchScore).ThenBy(x => x.Venue.Name).ToList()
            : scoredResults.OrderByDescending(x => x.MatchScore).ThenBy(x => x.DistanceKm ?? double.MaxValue).ToList();

        var totalCount = ordered.Count;
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Max(1, request.PageSize);
        var pageItems = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var warnings = BuildWarnings(request, parsedCriteria);

        return new VenueSearchResponse
        {
            SearchHistoryId = Guid.NewGuid(),
            Status = totalCount > 0 ? "MATCHED" : "NO_MATCH",
            Message = totalCount > 0
                ? $"Tìm thấy {totalCount} địa điểm phù hợp nhất với nhu cầu của bạn."
                : "Không tìm thấy địa điểm đang mở phù hợp với các điều kiện đã chọn.",
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            HasMore = (page * pageSize) < totalCount,
            Results = pageItems,
            Warnings = warnings
        };
    }

    private static HashSet<string> ExtractAmenityCodes(Venue venue, IReadOnlyDictionary<string, string?> sourceData)
    {
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Relational table
        if (venue.Amenities is not null)
        {
            foreach (var va in venue.Amenities)
            {
                if (va.Amenity is not null && !string.IsNullOrWhiteSpace(va.Amenity.Code))
                    codes.Add(va.Amenity.Code);
            }
        }

        // SourceData fallback
        if (IsPositive(sourceData, "wifi")) codes.Add("WIFI");
        if (IsPositive(sourceData, "powerOutlets")) codes.Add("POWER_OUTLET");
        if (IsPositive(sourceData, "airConditioning")) codes.Add("AIR_CONDITIONING");
        if (IsPositive(sourceData, "motorbikeParking"))
        {
            codes.Add("PARKING");
            codes.Add("MOTORBIKE_PARKING");
        }
        if (IsPositive(sourceData, "carParking")) codes.Add("CAR_PARKING");
        if (IsPositive(sourceData, "suitableForGroupsOf4Plus")) codes.Add("GROUP_SEATING");

        return codes;
    }

    private static (List<string> Purposes, List<string> Atmospheres) ExtractTags(Venue venue, IReadOnlyDictionary<string, string?> sourceData)
    {
        var purposes = new List<string>();
        var atmospheres = new List<string>();

        if (venue.Tags is not null)
        {
            foreach (var vt in venue.Tags)
            {
                if (vt.Tag is null) continue;
                if (vt.Tag.TagType == VenueTagType.Purpose)
                    purposes.Add(vt.Tag.Code);
                else if (vt.Tag.TagType == VenueTagType.Atmosphere)
                    atmospheres.Add(vt.Tag.Code);
            }
        }

        if (sourceData.TryGetValue("tagsAndPurpose", out var tp) && !string.IsNullOrWhiteSpace(tp))
        {
            var rawTags = tp.Split([';', ',', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var t in rawTags)
            {
                var lower = t.ToLowerInvariant();
                if (lower.Contains("học") || lower.Contains("study") || lower.Contains("làm việc") || lower.Contains("work"))
                {
                    purposes.Add("STUDY");
                    purposes.Add("WORK");
                }
                if (lower.Contains("hẹn hò") || lower.Contains("date") || lower.Contains("dating"))
                    purposes.Add("DATING");
                if (lower.Contains("nhóm") || lower.Contains("họp") || lower.Contains("gathering") || lower.Contains("tụ tập"))
                    purposes.Add("GATHERING");
                if (lower.Contains("ăn tối") || lower.Contains("dinner") || lower.Contains("lẩu") || lower.Contains("nướng"))
                    purposes.Add("DINING");
            }
        }

        if (sourceData.TryGetValue("observedNoise", out var noise) && !string.IsNullOrWhiteSpace(noise))
        {
            var lower = noise.ToLowerInvariant();
            if (lower.Contains("yên tĩnh") || lower.Contains("quiet"))
                atmospheres.Add("QUIET");
            if (lower.Contains("ấm cúng") || lower.Contains("cozy"))
                atmospheres.Add("COZY");
            if (lower.Contains("náo nhiệt") || lower.Contains("lively"))
                atmospheres.Add("LIVELY");
        }

        return (purposes.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                atmospheres.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
    }

    private static bool IsPositive(IReadOnlyDictionary<string, string?> dict, string key)
    {
        if (dict.TryGetValue(key, out var val) && val is not null)
        {
            var trimmed = val.Trim().ToLowerInvariant();
            return trimmed == "có" || trimmed == "co" || trimmed == "yes" || trimmed == "true";
        }
        return false;
    }

    private static bool IsOpenAt(Venue venue, DateTime now)
    {
        var today = DateOnly.FromDateTime(now);
        var todayException = venue.HoursExceptions.FirstOrDefault(x => x.Date == today);
        if (todayException is not null)
        {
            if (todayException.IsClosed)
                return false;
            if (IsWithinPeriod(TimeOnly.FromDateTime(now), todayException.OpenTime, todayException.CloseTime))
                return true;
        }
        else
        {
            var todayHours = venue.OpeningHours.Where(x => x.DayOfWeek == now.DayOfWeek);
            if (todayHours.Any(x => !x.IsClosed && IsWithinPeriod(TimeOnly.FromDateTime(now), x.OpenTime, x.CloseTime)))
                return true;

            var yesterday = now.AddDays(-1);
            var yesterdayHours = venue.OpeningHours.Where(x => x.DayOfWeek == yesterday.DayOfWeek);
            if (yesterdayHours.Any(x => !x.IsClosed
                && x.OpenTime.HasValue && x.CloseTime.HasValue
                && x.CloseTime < x.OpenTime
                && TimeOnly.FromDateTime(now) < x.CloseTime.Value))
                return true;
        }

        return false;
    }

    private static bool IsWithinPeriod(TimeOnly now, TimeOnly? open, TimeOnly? close)
    {
        if (!open.HasValue || !close.HasValue || open == close)
            return false;

        return open < close
            ? now >= open.Value && now < close.Value
            : now >= open.Value;
    }

    private static DateTime GetVietnamLocalTime()
    {
        var timeZoneId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "SE Asia Standard Time"
            : "Asia/Ho_Chi_Minh";
        var zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone).DateTime;
    }

    private static double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0088;
        var latitudeDelta = ToRadians(lat2 - lat1);
        var longitudeDelta = ToRadians(lon2 - lon1);
        var a = Math.Pow(Math.Sin(latitudeDelta / 2), 2)
            + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Pow(Math.Sin(longitudeDelta / 2), 2);
        return earthRadiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;

    private static IReadOnlyDictionary<string, string?> DeserializeSourceData(string? sourceDataJson)
    {
        if (string.IsNullOrWhiteSpace(sourceDataJson))
            return new Dictionary<string, string?>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string?>>(sourceDataJson)
                ?? new Dictionary<string, string?>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, string?>();
        }
    }

    private static IReadOnlyList<string> BuildWarnings(VenueSearchRequest request, ParsedSearchCriteria? parsed)
    {
        var warnings = new List<string>();
        if (parsed is not null && !string.IsNullOrWhiteSpace(parsed.RawPrompt))
        {
            warnings.Add(parsed.ExtractedByAi
                ? $"AI đã trích xuất tiêu chí từ prompt: {parsed.SummaryExplanation}"
                : $"Bộ NLP đã trích xuất tiêu chí từ prompt: {parsed.SummaryExplanation}");
        }
        return warnings;
    }
}
