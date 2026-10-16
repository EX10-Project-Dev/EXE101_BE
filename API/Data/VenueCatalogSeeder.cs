using System.Text.Json;
using DAL.Data;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace API.Data;

public static class VenueCatalogSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<int> SeedAsync(IServiceProvider services, string seedFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(seedFilePath))
        {
            throw new FileNotFoundException("Không tìm thấy tệp dữ liệu seed địa điểm.", seedFilePath);
        }

        var records = JsonSerializer.Deserialize<List<VenueSeedRecord>>(
            await File.ReadAllTextAsync(seedFilePath, cancellationToken), JsonOptions)
            ?? throw new InvalidDataException("Tệp seed địa điểm không có dữ liệu hợp lệ.");

        if (records.Count != 40 || records.Select(x => x.SourceId).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 40)
        {
            throw new InvalidDataException("Tệp seed cần có đúng 40 mã địa điểm duy nhất.");
        }

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        // Clean up any legacy invalid Google Maps search links stored in VenuePhotos table
        await db.Database.ExecuteSqlRawAsync(
            "IF OBJECT_ID('dbo.VenuePhotos', 'U') IS NOT NULL DELETE FROM [dbo].[VenuePhotos] WHERE [ImageUrl] LIKE '%google.com/maps%' OR [ImageUrl] LIKE '%maps/search%' OR [ImageUrl] LIKE '%google. com%'",
            cancellationToken);

        var categoryNames = records.Select(x => x.SourceCategory).Distinct().ToArray();
        var categoryCodes = categoryNames.ToDictionary(x => x, GetCategoryCode);
        var existingCategories = await db.VenueCategories
            .Where(x => categoryCodes.Values.Contains(x.Code))
            .ToDictionaryAsync(x => x.Code, cancellationToken);

        foreach (var categoryName in categoryNames)
        {
            var code = categoryCodes[categoryName];
            if (!existingCategories.TryGetValue(code, out var category))
            {
                category = new VenueCategory { Name = categoryName, Code = code, IsActive = true };
                db.VenueCategories.Add(category);
                existingCategories.Add(code, category);
            }
            else
            {
                category.Name = categoryName;
                category.IsActive = true;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var standardAmenities = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["WIFI"] = "Wi-Fi miễn phí",
            ["POWER_OUTLET"] = "Ổ cắm điện",
            ["AIR_CONDITIONING"] = "Máy lạnh / Điều hòa",
            ["PARKING"] = "Chỗ để xe",
            ["MOTORBIKE_PARKING"] = "Bãi đỗ xe máy",
            ["CAR_PARKING"] = "Bãi đỗ xe ô tô",
            ["GROUP_SEATING"] = "Chỗ ngồi nhóm đông (4+ người)"
        };

        var existingAmenities = await db.Amenities
            .Where(x => standardAmenities.Keys.Contains(x.Code))
            .ToDictionaryAsync(x => x.Code, cancellationToken);

        foreach (var (code, name) in standardAmenities)
        {
            if (!existingAmenities.TryGetValue(code, out var amenity))
            {
                amenity = new Amenity { Name = name, Code = code, IsActive = true };
                db.Amenities.Add(amenity);
                existingAmenities.Add(code, amenity);
            }
            else
            {
                amenity.Name = name;
                amenity.IsActive = true;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var sourceIds = records.Select(x => x.SourceId).ToArray();
        var existingVenues = await db.Venues
            .Where(x => x.SourceId != null && sourceIds.Contains(x.SourceId))
            .ToDictionaryAsync(x => x.SourceId!, cancellationToken);

        foreach (var record in records)
        {
            var isNew = false;
            if (!existingVenues.TryGetValue(record.SourceId, out var venue))
            {
                venue = new Venue { SourceId = record.SourceId, Status = VenueStatus.Pending };
                db.Venues.Add(venue);
                existingVenues.Add(record.SourceId, venue);
                isNew = true;
            }

            var changed = isNew
                || venue.Name != record.Name
                || venue.Address != record.Address
                || venue.Latitude != record.Latitude
                || venue.Longitude != record.Longitude
                || venue.CategoryId != existingCategories[categoryCodes[record.SourceCategory]].Id
                || venue.PriceMin != record.PriceMin
                || venue.PriceMax != record.PriceMax
                || venue.PriceNote != record.PriceNote
                || venue.SourceDataJson != JsonSerializer.Serialize(record.SourceData, JsonOptions)
                || venue.PriceBasis != (record.PriceMin.HasValue || record.PriceMax.HasValue ? PriceBasis.PerPerson : null)
                || venue.CurrencyCode != "VND";

            venue.Name = record.Name;
            venue.Address = record.Address;
            venue.Latitude = record.Latitude;
            venue.Longitude = record.Longitude;
            venue.CategoryId = existingCategories[categoryCodes[record.SourceCategory]].Id;
            venue.PriceMin = record.PriceMin;
            venue.PriceMax = record.PriceMax;
            venue.PriceNote = record.PriceNote;
            venue.SourceDataJson = JsonSerializer.Serialize(record.SourceData, JsonOptions);
            venue.PriceBasis = record.PriceMin.HasValue || record.PriceMax.HasValue ? PriceBasis.PerPerson : null;
            venue.CurrencyCode = "VND";
            if (changed && !isNew)
            {
                venue.UpdatedAt = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var venueIds = existingVenues.Values.Select(x => x.Id).ToArray();
        var existingHours = await db.VenueOpeningHours
            .Where(x => venueIds.Contains(x.VenueId))
            .ToListAsync(cancellationToken);
        var seededHourKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var record in records)
        {
            var venue = existingVenues[record.SourceId];
            foreach (var period in record.OpeningHours)
            {
                var day = Enum.Parse<DayOfWeek>(period.DayOfWeek, ignoreCase: true);
                var open = TimeOnly.Parse(period.Open, CultureInfo.InvariantCulture);
                var close = TimeOnly.Parse(period.Close, CultureInfo.InvariantCulture);
                var key = GetHourKey(venue.Id, day, open, close);
                seededHourKeys.Add(key);
                var existing = existingHours.FirstOrDefault(x => GetHourKey(x.VenueId, x.DayOfWeek, x.OpenTime, x.CloseTime) == key);

                if (existing is null)
                {
                    db.VenueOpeningHours.Add(new VenueOpeningHour
                    {
                        VenueId = venue.Id,
                        DayOfWeek = day,
                        OpenTime = open,
                        CloseTime = close,
                        IsClosed = false,
                        Note = period.Note
                    });
                }
                else if (existing.Note != period.Note)
                {
                    existing.Note = period.Note;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        db.VenueOpeningHours.RemoveRange(existingHours.Where(x => !seededHourKeys.Contains(GetHourKey(x.VenueId, x.DayOfWeek, x.OpenTime, x.CloseTime))));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await db.Venues.CountAsync(x => x.SourceId != null && sourceIds.Contains(x.SourceId), cancellationToken);
    }

    private static string GetCategoryCode(string sourceCategory) => sourceCategory switch
    {
        "Nhà hàng" => "RESTAURANT",
        "Quán ăn" => "EATERY",
        "Tiệm bánh / tráng miệng" => "BAKERY_DESSERT",
        "Trà sữa / trà" => "TEA_DRINKS",
        "Trà sữa / cà phê" => "TEA_COFFEE",
        "Cà phê" => "CAFE",
        "Nhà hàng Việt Nam" => "VIETNAMESE_RESTAURANT",
        "Cửa hàng kem" => "ICE_CREAM",
        "Nhà hàng lẩu mala / ăn nhanh" => "MALA_HOTPOT_FAST_FOOD",
        "Khác" => "OTHER",
        _ => throw new InvalidDataException($"Chưa có mã danh mục cho loại địa điểm '{sourceCategory}'.")
    };

    private static string GetHourKey(Guid venueId, DayOfWeek day, TimeOnly? open, TimeOnly? close)
        => $"{venueId:N}|{day}|{open}|{close}";

    private sealed class VenueSeedRecord
    {
        public string SourceId { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string SourceCategory { get; init; } = string.Empty;
        public string Address { get; init; } = string.Empty;
        public decimal Latitude { get; init; }
        public decimal Longitude { get; init; }
        public decimal? PriceMin { get; init; }
        public decimal? PriceMax { get; init; }
        public string? PriceNote { get; init; }
        public Dictionary<string, string?> SourceData { get; init; } = new(StringComparer.OrdinalIgnoreCase);
        public List<OpeningHourSeedRecord> OpeningHours { get; init; } = [];
    }

    private sealed class OpeningHourSeedRecord
    {
        public string DayOfWeek { get; init; } = string.Empty;
        public string Open { get; init; } = string.Empty;
        public string Close { get; init; } = string.Empty;
        public string? Note { get; init; }
    }
}
