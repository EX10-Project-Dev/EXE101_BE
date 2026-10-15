using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BLL.DTOs.Responses;
using BLL.Interfaces;
using DAL.Data;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BLL.Services;

public sealed class VenueService : IVenueService
{
    private readonly AppDbContext _db;
    private readonly bool _includePendingVenues;

    public VenueService(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _includePendingVenues = configuration.GetValue<bool>("Search:IncludePendingVenues");
    }

    public async Task<VenueDetailResponse?> GetVenueByIdAsync(string identifier, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return null;

        var query = _db.Venues
            .AsNoTracking()
            .AsSplitQuery()
            .Include(v => v.Category)
            .Include(v => v.Photos)
            .Include(v => v.OpeningHours)
            .Include(v => v.HoursExceptions)
            .Include(v => v.Amenities).ThenInclude(va => va.Amenity)
            .Include(v => v.Tags).ThenInclude(vt => vt.Tag)
            .Where(v => !v.IsDeleted);

        if (!_includePendingVenues)
        {
            query = query.Where(v => v.Status == VenueStatus.Published);
        }

        Venue? venue;
        if (Guid.TryParse(identifier.Trim(), out var guid))
        {
            venue = await query.FirstOrDefaultAsync(v => v.Id == guid, cancellationToken);
        }
        else
        {
            venue = await query.FirstOrDefaultAsync(v => v.SourceId == identifier.Trim(), cancellationToken);
        }

        if (venue is null)
            return null;

        var sourceData = DeserializeSourceData(venue.SourceDataJson);
        var now = GetVietnamLocalTime();
        var isOpenNow = IsOpenAt(venue, now);

        // 1. Photo Mapping with SourceData fallback
        var photos = venue.Photos
            .OrderBy(p => p.SortOrder)
            .Select(p => new VenuePhotoDto
            {
                Id = p.Id,
                ImageUrl = p.ImageUrl,
                PhotoType = p.PhotoType.ToString(),
                Caption = p.Caption,
                SortOrder = p.SortOrder
            })
            .ToList();

        if (photos.Count == 0)
        {
            int order = 1;
            if (sourceData.TryGetValue("exteriorPhotoUrl", out var ext) && !string.IsNullOrWhiteSpace(ext))
            {
                photos.Add(new VenuePhotoDto { Id = Guid.NewGuid(), ImageUrl = ext, PhotoType = "Exterior", Caption = "Ảnh mặt tiền", SortOrder = order++ });
            }
            if (sourceData.TryGetValue("interiorPhotoUrl", out var inte) && !string.IsNullOrWhiteSpace(inte))
            {
                photos.Add(new VenuePhotoDto { Id = Guid.NewGuid(), ImageUrl = inte, PhotoType = "Interior", Caption = "Ảnh không gian", SortOrder = order++ });
            }
            if (sourceData.TryGetValue("menuPhotoUrl", out var menu) && !string.IsNullOrWhiteSpace(menu))
            {
                photos.Add(new VenuePhotoDto { Id = Guid.NewGuid(), ImageUrl = menu, PhotoType = "Menu", Caption = "Ảnh thực đơn / menu", SortOrder = order++ });
            }
        }

        // 2. Opening Hours Mapping
        var openingHours = venue.OpeningHours
            .OrderBy(h => h.DayOfWeek)
            .Select(h => new VenueOpeningHourDto
            {
                DayOfWeek = h.DayOfWeek.ToString(),
                OpenTime = h.OpenTime?.ToString("HH:mm"),
                CloseTime = h.CloseTime?.ToString("HH:mm"),
                IsClosed = h.IsClosed,
                Note = h.Note
            })
            .ToList();

        // 3. Extract Amenities
        var amenities = ExtractAmenityCodes(venue, sourceData).ToList();

        // 4. Extract Tags
        var (purposes, atmospheres) = ExtractTags(venue, sourceData);

        // 5. Google Maps URL
        string googleMapsUrl;
        if (sourceData.TryGetValue("googleMapsUrlSource", out var mapsUrl) && !string.IsNullOrWhiteSpace(mapsUrl))
        {
            googleMapsUrl = mapsUrl;
        }
        else
        {
            googleMapsUrl = $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString($"{venue.Name}, {venue.Address}")}";
        }

        return new VenueDetailResponse
        {
            VenueId = venue.Id,
            SourceId = venue.SourceId,
            Name = venue.Name,
            CategoryName = venue.Category?.Name ?? string.Empty,
            CategoryCode = venue.Category?.Code ?? string.Empty,
            Description = venue.Description,
            Address = venue.Address,
            Location = new VenueLocation
            {
                Latitude = (double)venue.Latitude,
                Longitude = (double)venue.Longitude
            },
            GoogleMapsUrl = googleMapsUrl,
            WebsiteUrl = venue.WebsiteUrl ?? (sourceData.TryGetValue("websiteOrFanpageUrl", out var site) ? site : null),
            MenuUrl = venue.MenuUrl ?? (sourceData.TryGetValue("menuUrlSource", out var menuUrl) ? menuUrl : null),
            PhoneNumber = venue.PhoneNumber,
            PriceMin = venue.PriceMin,
            PriceMax = venue.PriceMax,
            PriceBasis = venue.PriceBasis?.ToString(),
            PriceNote = venue.PriceNote,
            CurrencyCode = venue.CurrencyCode,
            IsOpenNow = isOpenNow,
            RatingAverage = 4.6,
            ReviewCount = 20,
            Photos = photos,
            OpeningHours = openingHours,
            Amenities = amenities,
            AtmosphereTags = atmospheres,
            PurposeTags = purposes,
            SourceData = sourceData
        };
    }

    public async Task<IReadOnlyList<CategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.VenueCategories
            .AsNoTracking()
            .Where(c => c.IsActive && !c.IsDeleted)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AmenityResponse>> GetAmenitiesAsync(CancellationToken cancellationToken = default)
    {
        var amenities = await _db.Amenities
            .AsNoTracking()
            .Where(a => a.IsActive && !a.IsDeleted)
            .OrderBy(a => a.Name)
            .Select(a => new AmenityResponse
            {
                Id = a.Id,
                Name = a.Name,
                Code = a.Code
            })
            .ToListAsync(cancellationToken);

        if (amenities.Count > 0)
            return amenities;

        // Fallback default amenities if table is not yet populated
        return
        [
            new AmenityResponse { Id = Guid.NewGuid(), Code = "WIFI", Name = "Wi-Fi miễn phí" },
            new AmenityResponse { Id = Guid.NewGuid(), Code = "POWER_OUTLET", Name = "Ổ cắm điện" },
            new AmenityResponse { Id = Guid.NewGuid(), Code = "AIR_CONDITIONING", Name = "Máy lạnh / Điều hòa" },
            new AmenityResponse { Id = Guid.NewGuid(), Code = "PARKING", Name = "Chỗ để xe" },
            new AmenityResponse { Id = Guid.NewGuid(), Code = "MOTORBIKE_PARKING", Name = "Bãi đỗ xe máy" },
            new AmenityResponse { Id = Guid.NewGuid(), Code = "CAR_PARKING", Name = "Bãi đỗ xe ô tô" },
            new AmenityResponse { Id = Guid.NewGuid(), Code = "GROUP_SEATING", Name = "Bàn nhóm lớn (4+ người)" }
        ];
    }

    private static HashSet<string> ExtractAmenityCodes(Venue venue, IReadOnlyDictionary<string, string?> sourceData)
    {
        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (venue.Amenities is not null)
        {
            foreach (var va in venue.Amenities)
            {
                if (va.Amenity is not null && !string.IsNullOrWhiteSpace(va.Amenity.Code))
                    codes.Add(va.Amenity.Code);
            }
        }

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
        var utcNow = DateTime.UtcNow;
        try
        {
            var tz = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")
                : TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
            return TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);
        }
        catch
        {
            return utcNow.AddHours(7);
        }
    }

    private static IReadOnlyDictionary<string, string?> DeserializeSourceData(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, string?>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string?>>(json) ?? new Dictionary<string, string?>();
        }
        catch
        {
            return new Dictionary<string, string?>();
        }
    }
}
