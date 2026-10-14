using System.Runtime.InteropServices;
using System.Text.Json;
using BLL.DTOs.Requests;
using BLL.DTOs.Responses;
using BLL.Interfaces;
using DAL.Data;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BLL.Services;

/// <summary>Searches venues that are open according to the schedules currently stored in the catalog.</summary>
public sealed class VenueSearchService : IVenueSearchService
{
    private readonly AppDbContext _db;
    private readonly bool _includePendingVenues;

    public VenueSearchService(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _includePendingVenues = configuration.GetValue<bool>("Search:IncludePendingVenues");
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
            .Where(x => !x.IsDeleted);

        if (!_includePendingVenues)
        {
            query = query.Where(x => x.Status == VenueStatus.Published);
        }

        var venues = await query.ToListAsync(cancellationToken);
        var categoryCodes = request.CategoryCodes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var candidates = new List<(Venue Venue, double? DistanceKm)>();

        foreach (var venue in venues)
        {
            if (!IsOpenAt(venue, now))
                continue;

            if (categoryCodes.Count > 0 && !categoryCodes.Contains(venue.Category.Code))
                continue;

            if (request.BudgetMin.HasValue && (!venue.PriceMax.HasValue || venue.PriceMax < request.BudgetMin))
                continue;

            if (request.BudgetMax.HasValue && (!venue.PriceMin.HasValue || venue.PriceMin > request.BudgetMax))
                continue;

            double? distanceKm = null;
            if (request.Origin is not null)
            {
                distanceKm = CalculateDistanceKm(
                    request.Origin.Latitude,
                    request.Origin.Longitude,
                    (double)venue.Latitude,
                    (double)venue.Longitude);
            }

            if (request.MaxDistanceKm.HasValue && (!distanceKm.HasValue || distanceKm > request.MaxDistanceKm.Value))
                continue;

            candidates.Add((venue, distanceKm));
        }

        var ordered = request.Origin is null
            ? candidates.OrderBy(x => x.Venue.SourceId).ThenBy(x => x.Venue.Name).ToList()
            : candidates.OrderBy(x => x.DistanceKm).ThenBy(x => x.Venue.Name).ToList();

        var totalCount = ordered.Count;
        var pageItems = ordered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new VenueSearchResult
            {
                Venue = new VenueSummary
                {
                    VenueId = x.Venue.Id,
                    Name = x.Venue.Name,
                    CategoryName = x.Venue.Category.Name,
                    ShortDescription = x.Venue.Description,
                    ImageUrls = x.Venue.Photos.OrderBy(photo => photo.SortOrder).Select(photo => photo.ImageUrl).ToList(),
                    Address = x.Venue.Address,
                    Location = new VenueLocation
                    {
                        Latitude = (double)x.Venue.Latitude,
                        Longitude = (double)x.Venue.Longitude
                    },
                    DistanceKm = x.DistanceKm.HasValue ? Math.Round(x.DistanceKm.Value, 2) : null,
                    GoogleMapsUrl = BuildGoogleMapsUrl(x.Venue),
                    PriceMin = x.Venue.PriceMin,
                    PriceMax = x.Venue.PriceMax,
                    PriceNote = x.Venue.PriceNote,
                    SourceData = DeserializeSourceData(x.Venue.SourceDataJson),
                    CurrencyCode = x.Venue.CurrencyCode,
                    IsOpenNow = true,
                    RatingAverage = null,
                    ReviewCount = 0
                },
                MatchReasons = BuildReasons(x.DistanceKm, request)
            })
            .ToList();

        var warnings = BuildWarnings(request);
        return new VenueSearchResponse
        {
            SearchHistoryId = Guid.NewGuid(),
            Status = totalCount > 0 ? "MATCHED" : "NO_MATCH",
            Message = totalCount > 0
                ? "Tìm thấy địa điểm đang mở theo lịch hiện có trong hệ thống."
                : "Không tìm thấy địa điểm đang mở phù hợp với các điều kiện đã kiểm tra.",
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            HasMore = request.Page * request.PageSize < totalCount,
            Results = pageItems,
            Warnings = warnings
        };
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

    private static string BuildGoogleMapsUrl(Venue venue)
    {
        var query = Uri.EscapeDataString($"{venue.Name}, {venue.Address}");
        return $"https://www.google.com/maps/search/?api=1&query={query}";
    }

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
    private static IReadOnlyList<string> BuildReasons(double? distanceKm, VenueSearchRequest request)
    {
        var reasons = new List<string> { "Đang mở theo lịch hoạt động được ghi trong dữ liệu nhóm; lịch vẫn cần xác minh." };
        if (distanceKm.HasValue)
            reasons.Add($"Cách vị trí bắt đầu khoảng {Math.Round(distanceKm.Value, 2):0.##} km.");
        if (request.CategoryCodes.Count > 0)
            reasons.Add("Thuộc danh mục bạn đã chọn.");
        return reasons;
    }

    private static IReadOnlyList<string> BuildWarnings(VenueSearchRequest request)
    {
        var warnings = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Prompt))
            warnings.Add("Prompt chưa được phân tích bằng AI; kết quả dựa trên các bộ lọc có cấu trúc.");
        if (!string.IsNullOrWhiteSpace(request.Purpose))
            warnings.Add("Mục đích chưa được đối chiếu vì thẻ mục đích chưa được nạp vào database.");
        if (request.AtmosphereTags.Count > 0)
            warnings.Add("Không gian chưa được đối chiếu vì thẻ không gian chưa được nạp vào database.");
        if (request.AmenityCodes.Count > 0)
            warnings.Add("Tiện ích chưa được đối chiếu vì dữ liệu tiện ích chưa được nạp vào database.");
        if (request.GroupSize.HasValue)
            warnings.Add("Quy mô nhóm chưa được đối chiếu vì dữ liệu sức chứa chưa được nạp vào database.");
        if (request.BudgetMin.HasValue || request.BudgetMax.HasValue)
            warnings.Add("Giá lấy từ dữ liệu nhóm; một số mức giá còn ghi chú chưa xác minh hoặc chưa rõ nguồn, hãy xem priceNote của từng quán.");
        return warnings;
    }
}
