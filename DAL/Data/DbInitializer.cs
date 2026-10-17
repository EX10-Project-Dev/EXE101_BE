using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DAL.Data;
using DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace DAL.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // 1. Seed Categories if empty
        if (!await context.VenueCategories.AnyAsync())
        {
            var categories = new List<VenueCategory>
            {
                new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111101"), Code = "CAFE", Name = "Cà phê", IsActive = true },
                new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111102"), Code = "RESTAURANT", Name = "Nhà hàng", IsActive = true },
                new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111103"), Code = "EATERY", Name = "Quán ăn", IsActive = true },
                new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111104"), Code = "TEA_BEVERAGE", Name = "Trà sữa / trà", IsActive = true },
                new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111105"), Code = "DESSERT", Name = "Tiệm bánh / tráng miệng", IsActive = true },
                new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111106"), Code = "ICE_CREAM", Name = "Cửa hàng kem", IsActive = true },
                new() { Id = Guid.Parse("11111111-1111-1111-1111-111111111107"), Code = "OTHER", Name = "Khác", IsActive = true }
            };
            await context.VenueCategories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }

        // 2. Seed Amenities if empty
        if (!await context.Amenities.AnyAsync())
        {
            var amenities = new List<Amenity>
            {
                new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222201"), Code = "WIFI", Name = "Wi-Fi miễn phí", IsActive = true },
                new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222202"), Code = "POWER_OUTLET", Name = "Ổ cắm điện", IsActive = true },
                new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222203"), Code = "AIR_CONDITIONING", Name = "Máy lạnh / Điều hòa", IsActive = true },
                new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222204"), Code = "MOTORBIKE_PARKING", Name = "Chỗ để xe máy", IsActive = true },
                new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222205"), Code = "CAR_PARKING", Name = "Chỗ để ô tô", IsActive = true },
                new() { Id = Guid.Parse("22222222-2222-2222-2222-222222222206"), Code = "GROUP_SEATING", Name = "Phù hợp nhóm 4+", IsActive = true }
            };
            await context.Amenities.AddRangeAsync(amenities);
            await context.SaveChangesAsync();
        }

        // 3. Seed Tags if empty
        if (!await context.VenueTags.AnyAsync())
        {
            var tags = new List<VenueTag>
            {
                new() { Id = Guid.Parse("33333333-3333-3333-3333-333333333301"), Code = "STUDY_WORK", Name = "Học bài / Làm việc", TagType = VenueTagType.Purpose },
                new() { Id = Guid.Parse("33333333-3333-3333-3333-333333333302"), Code = "EAT_DRINK", Name = "Ăn uống", TagType = VenueTagType.Purpose },
                new() { Id = Guid.Parse("33333333-3333-3333-3333-333333333303"), Code = "HANGOUT", Name = "Tụ tập bạn bè", TagType = VenueTagType.Purpose },
                new() { Id = Guid.Parse("33333333-3333-3333-3333-333333333304"), Code = "DATE", Name = "Hẹn hò", TagType = VenueTagType.Purpose },
                new() { Id = Guid.Parse("33333333-3333-3333-3333-333333333305"), Code = "GROUP_GATHERING", Name = "Họp nhóm", TagType = VenueTagType.Purpose },
                new() { Id = Guid.Parse("33333333-3333-3333-3333-333333333306"), Code = "QUIET", Name = "Yên tĩnh", TagType = VenueTagType.Atmosphere },
                new() { Id = Guid.Parse("33333333-3333-3333-3333-333333333307"), Code = "COZY", Name = "Ấm cúng", TagType = VenueTagType.Atmosphere },
                new() { Id = Guid.Parse("33333333-3333-3333-3333-333333333308"), Code = "SOCIAL", Name = "Náo nhiệt", TagType = VenueTagType.Atmosphere },
                new() { Id = Guid.Parse("33333333-3333-3333-3333-333333333309"), Code = "MODERATE", Name = "Vừa phải", TagType = VenueTagType.Atmosphere }
            };
            await context.VenueTags.AddRangeAsync(tags);
            await context.SaveChangesAsync();
        }

        // 4. Seed Venues if empty
        if (!await context.Venues.AnyAsync())
        {
            var categories = await context.VenueCategories.ToDictionaryAsync(c => c.Code, c => c.Id);
            var amenities = await context.Amenities.ToDictionaryAsync(a => a.Code, a => a.Id);
            var tags = await context.VenueTags.ToDictionaryAsync(t => t.Code, t => t.Id);

            var venueList = new List<Venue>();

            // [VEN-001] Chow Long - Tiệm Lẩu Bò Triều Châu
            var v1 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444401"),
                Name = @"Chow Long - Tiệm Lẩu Bò Triều Châu",
                CategoryId = categories["RESTAURANT"],
                Description = @"lẩu bò; ăn tối; nhóm bạn",
                Address = @"29 Nguyễn Văn Tăng, Phường Long Thạnh Mỹ, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.848273m,
                Longitude = 106.815297m,
                PriceMin = 100000m,
                PriceMax = 200000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=Chow%20Long%20-%20Ti%E1%BB%87m%20L%E1%BA%A9u%20B%C3%B2%20Tri%E1%BB%81u%20Ch%C3%A2u",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/Chow%20Long%20-%20Ti%E1%BB%87m%20L%E1%BA%A9u%20B%C3%B2%20Tri%E1%BB%81u%20Ch%C3%A2u",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v1.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v1.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("16:30:00"),
                    CloseTime = TimeOnly.Parse("23:59:00"),
                    IsClosed = false
                });
            }
            v1.Amenities.Add(new VenueAmenityLink { VenueId = v1.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v1.Amenities.Add(new VenueAmenityLink { VenueId = v1.Id, AmenityId = amenities["POWER_OUTLET"], Availability = AmenityAvailability.Available });
            v1.Amenities.Add(new VenueAmenityLink { VenueId = v1.Id, AmenityId = amenities["AIR_CONDITIONING"], Availability = AmenityAvailability.Available });
            v1.Amenities.Add(new VenueAmenityLink { VenueId = v1.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v1.Amenities.Add(new VenueAmenityLink { VenueId = v1.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v1.Amenities.Add(new VenueAmenityLink { VenueId = v1.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v1.Tags.Add(new VenueTagLink { VenueId = v1.Id, VenueTagId = tags["EAT_DRINK"] });
            v1.Tags.Add(new VenueTagLink { VenueId = v1.Id, VenueTagId = tags["SOCIAL"] });
            venueList.Add(v1);

            // [VEN-002] Cơm Tấm Làng
            var v2 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444402"),
                Name = @"Cơm Tấm Làng",
                CategoryId = categories["EATERY"],
                Description = @"ăn trưa; ăn tối; cơm phần",
                Address = @"163 Nguyễn Văn Tăng, Phường Long Thạnh Mỹ, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.843847m,
                Longitude = 106.818607m,
                PriceMin = 35000m,
                PriceMax = 60000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=C%C6%A1m%20T%E1%BA%A5m%20L%C3%A0ng",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/C%C6%A1m%20T%E1%BA%A5m%20L%C3%A0ng",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v2.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v2.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("06:00:00"),
                    CloseTime = TimeOnly.Parse("21:00:00"),
                    IsClosed = false
                });
            }
            v2.Amenities.Add(new VenueAmenityLink { VenueId = v2.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v2.Amenities.Add(new VenueAmenityLink { VenueId = v2.Id, AmenityId = amenities["POWER_OUTLET"], Availability = AmenityAvailability.Available });
            v2.Amenities.Add(new VenueAmenityLink { VenueId = v2.Id, AmenityId = amenities["AIR_CONDITIONING"], Availability = AmenityAvailability.Available });
            v2.Amenities.Add(new VenueAmenityLink { VenueId = v2.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v2.Amenities.Add(new VenueAmenityLink { VenueId = v2.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v2.Amenities.Add(new VenueAmenityLink { VenueId = v2.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v2.Tags.Add(new VenueTagLink { VenueId = v2.Id, VenueTagId = tags["EAT_DRINK"] });
            v2.Tags.Add(new VenueTagLink { VenueId = v2.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v2);

            // [VEN-003] Hải Sản Út Linh 2
            var v3 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444403"),
                Name = @"Hải Sản Út Linh 2",
                CategoryId = categories["RESTAURANT"],
                Description = @"hải sản; ăn nhóm",
                Address = @"57 Lã Xuân Oai, Phường Tăng Nhơn Phú A, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.843149m,
                Longitude = 106.789587m,
                PriceMin = 100000m,
                PriceMax = 200000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=H%E1%BA%A3i%20S%E1%BA%A3n%20%C3%9At%20Linh%202",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/H%E1%BA%A3i%20S%E1%BA%A3n%20%C3%9At%20Linh%202",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v3.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v3.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("08:00:00"),
                    CloseTime = TimeOnly.Parse("23:00:00"),
                    IsClosed = false
                });
            }
            v3.Amenities.Add(new VenueAmenityLink { VenueId = v3.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v3.Amenities.Add(new VenueAmenityLink { VenueId = v3.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v3.Amenities.Add(new VenueAmenityLink { VenueId = v3.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v3.Tags.Add(new VenueTagLink { VenueId = v3.Id, VenueTagId = tags["EAT_DRINK"] });
            v3.Tags.Add(new VenueTagLink { VenueId = v3.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v3);

            // [VEN-004] The Rice Z
            var v4 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444404"),
                Name = @"The Rice Z",
                CategoryId = categories["RESTAURANT"],
                Description = @"ăn chính; cơm/món Á; gặp bạn",
                Address = @"305 Võ Văn Hát, Phường Long Trường, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.815746m,
                Longitude = 106.808539m,
                PriceMin = 100000m,
                PriceMax = 200000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=The%20Rice%20Z",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/The%20Rice%20Z",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v4.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v4.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("10:00:00"),
                    CloseTime = TimeOnly.Parse("21:00:00"),
                    IsClosed = false
                });
            }
            v4.Amenities.Add(new VenueAmenityLink { VenueId = v4.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v4.Amenities.Add(new VenueAmenityLink { VenueId = v4.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v4.Amenities.Add(new VenueAmenityLink { VenueId = v4.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v4.Tags.Add(new VenueTagLink { VenueId = v4.Id, VenueTagId = tags["EAT_DRINK"] });
            v4.Tags.Add(new VenueTagLink { VenueId = v4.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v4);

            // [VEN-005] Kem Dẻo Tràng Tiền
            var v5 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444405"),
                Name = @"Kem Dẻo Tràng Tiền",
                CategoryId = categories["DESSERT"],
                Description = @"ăn vặt; tráng miệng; gặp bạn",
                Address = @"4 Đường số 17, Phường Linh Xuân, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.860408m,
                Longitude = 106.789986m,
                PriceMin = 30000m,
                PriceMax = 65000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=Kem%20D%E1%BA%BBo%20Tr%C3%A0ng%20Ti%E1%BB%81n",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/Kem%20D%E1%BA%BBo%20Tr%C3%A0ng%20Ti%E1%BB%81n",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v5.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v5.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("09:00:00"),
                    CloseTime = TimeOnly.Parse("22:30:00"),
                    IsClosed = false
                });
            }
            v5.Amenities.Add(new VenueAmenityLink { VenueId = v5.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v5.Amenities.Add(new VenueAmenityLink { VenueId = v5.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v5.Amenities.Add(new VenueAmenityLink { VenueId = v5.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v5.Tags.Add(new VenueTagLink { VenueId = v5.Id, VenueTagId = tags["EAT_DRINK"] });
            v5.Tags.Add(new VenueTagLink { VenueId = v5.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v5);

            // [VEN-006] SoGood Milktea & More
            var v6 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444406"),
                Name = @"SoGood Milktea & More",
                CategoryId = categories["TEA_BEVERAGE"],
                Description = @"trà sữa; ăn nhẹ; gặp bạn",
                Address = @"135/3 Đường 14, Phường Tăng Nhơn Phú B, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.838913m,
                Longitude = 106.781441m,
                PriceMin = 30000m,
                PriceMax = 65000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=SoGood%20Milktea%20%26amp%3B%20More",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/SoGood%20Milktea%20%26amp%3B%20More",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v6.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v6.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("10:00:00"),
                    CloseTime = TimeOnly.Parse("22:00:00"),
                    IsClosed = false
                });
            }
            v6.Amenities.Add(new VenueAmenityLink { VenueId = v6.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v6.Amenities.Add(new VenueAmenityLink { VenueId = v6.Id, AmenityId = amenities["AIR_CONDITIONING"], Availability = AmenityAvailability.Available });
            v6.Amenities.Add(new VenueAmenityLink { VenueId = v6.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v6.Amenities.Add(new VenueAmenityLink { VenueId = v6.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v6.Tags.Add(new VenueTagLink { VenueId = v6.Id, VenueTagId = tags["HANGOUT"] });
            v6.Tags.Add(new VenueTagLink { VenueId = v6.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v6);

            // [VEN-007] Phở Tình Tây
            var v7 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444407"),
                Name = @"Phở Tình Tây",
                CategoryId = categories["EATERY"],
                Description = @"ăn sáng; ăn chính; phở",
                Address = @"Số 7 Đường 5B, Phường Long Bình, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.848726m,
                Longitude = 106.837143m,
                PriceMin = 35000m,
                PriceMax = 60000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=Ph%E1%BB%9F%20T%C3%ACnh%20T%C3%A2y",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/Ph%E1%BB%9F%20T%C3%ACnh%20T%C3%A2y",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v7.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v7.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("06:00:00"),
                    CloseTime = TimeOnly.Parse("14:30:00"),
                    IsClosed = false
                });
            }
            v7.Amenities.Add(new VenueAmenityLink { VenueId = v7.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v7.Amenities.Add(new VenueAmenityLink { VenueId = v7.Id, AmenityId = amenities["POWER_OUTLET"], Availability = AmenityAvailability.Available });
            v7.Amenities.Add(new VenueAmenityLink { VenueId = v7.Id, AmenityId = amenities["AIR_CONDITIONING"], Availability = AmenityAvailability.Available });
            v7.Amenities.Add(new VenueAmenityLink { VenueId = v7.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v7.Amenities.Add(new VenueAmenityLink { VenueId = v7.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v7.Amenities.Add(new VenueAmenityLink { VenueId = v7.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v7.Tags.Add(new VenueTagLink { VenueId = v7.Id, VenueTagId = tags["EAT_DRINK"] });
            v7.Tags.Add(new VenueTagLink { VenueId = v7.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v7);

            // [VEN-008] Trà sữa TRẠM - Milk Tea & Coffee
            var v8 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444408"),
                Name = @"Trà sữa TRẠM - Milk Tea & Coffee",
                CategoryId = categories["TEA_BEVERAGE"],
                Description = @"trà sữa; cà phê; gặp bạn",
                Address = @"16 Đình Phong Phú, Phường Tăng Nhơn Phú B, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.843969m,
                Longitude = 106.781072m,
                PriceMin = 30000m,
                PriceMax = 65000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=Tr%C3%A0%20s%E1%BB%AFa%20TR%E1%BA%A0M%20-%20Milk%20Tea%20%26amp%3B%20Coffee",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/Tr%C3%A0%20s%E1%BB%AFa%20TR%E1%BA%A0M%20-%20Milk%20Tea%20%26amp%3B%20Coffee",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v8.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v8.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("06:30:00"),
                    CloseTime = TimeOnly.Parse("23:00:00"),
                    IsClosed = false
                });
            }
            v8.Amenities.Add(new VenueAmenityLink { VenueId = v8.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v8.Amenities.Add(new VenueAmenityLink { VenueId = v8.Id, AmenityId = amenities["AIR_CONDITIONING"], Availability = AmenityAvailability.Available });
            v8.Amenities.Add(new VenueAmenityLink { VenueId = v8.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v8.Amenities.Add(new VenueAmenityLink { VenueId = v8.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v8.Tags.Add(new VenueTagLink { VenueId = v8.Id, VenueTagId = tags["HANGOUT"] });
            v8.Tags.Add(new VenueTagLink { VenueId = v8.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v8);

            // [VEN-009] Le Pierro’s Bakery & Coffee Shop
            var v9 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444409"),
                Name = @"Le Pierro’s Bakery & Coffee Shop",
                CategoryId = categories["CAFE"],
                Description = @"bánh; cà phê; ăn nhẹ",
                Address = @"Tòa S7.03 The Origami, 03 Nguyễn Xiển, Vinhomes Grand Park, Phường Long Bình, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.846748m,
                Longitude = 106.838909m,
                PriceMin = 30000m,
                PriceMax = 65000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=Le%20Pierro%E2%80%99s%20Bakery%20%26amp%3B%20Coffee%20Shop",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/Le%20Pierro%E2%80%99s%20Bakery%20%26amp%3B%20Coffee%20Shop",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v9.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v9.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("07:30:00"),
                    CloseTime = TimeOnly.Parse("20:00:00"),
                    IsClosed = false
                });
            }
            v9.Amenities.Add(new VenueAmenityLink { VenueId = v9.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v9.Amenities.Add(new VenueAmenityLink { VenueId = v9.Id, AmenityId = amenities["POWER_OUTLET"], Availability = AmenityAvailability.Available });
            v9.Amenities.Add(new VenueAmenityLink { VenueId = v9.Id, AmenityId = amenities["AIR_CONDITIONING"], Availability = AmenityAvailability.Available });
            v9.Amenities.Add(new VenueAmenityLink { VenueId = v9.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v9.Amenities.Add(new VenueAmenityLink { VenueId = v9.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v9.Amenities.Add(new VenueAmenityLink { VenueId = v9.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v9.Tags.Add(new VenueTagLink { VenueId = v9.Id, VenueTagId = tags["STUDY_WORK"] });
            v9.Tags.Add(new VenueTagLink { VenueId = v9.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v9);

            // [VEN-010] Thủy Vân Ký S605 Vinhomes Grand Park
            var v10 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444410"),
                Name = @"Thủy Vân Ký S605 Vinhomes Grand Park",
                CategoryId = categories["EATERY"],
                Description = @"mì; ăn sáng; ăn chính",
                Address = @"Shophouse 01S06, Tòa S6.05 The Origami, Vinhomes Grand Park, Phường Long Bình, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.843707m,
                Longitude = 106.839053m,
                PriceMin = 35000m,
                PriceMax = 60000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=Th%E1%BB%A7y%20V%C3%A2n%20K%C3%BD%20S605%20Vinhomes%20Grand%20Park",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/Th%E1%BB%A7y%20V%C3%A2n%20K%C3%BD%20S605%20Vinhomes%20Grand%20Park",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v10.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v10.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("07:00:00"),
                    CloseTime = TimeOnly.Parse("21:00:00"),
                    IsClosed = false
                });
            }
            v10.Amenities.Add(new VenueAmenityLink { VenueId = v10.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v10.Amenities.Add(new VenueAmenityLink { VenueId = v10.Id, AmenityId = amenities["POWER_OUTLET"], Availability = AmenityAvailability.Available });
            v10.Amenities.Add(new VenueAmenityLink { VenueId = v10.Id, AmenityId = amenities["AIR_CONDITIONING"], Availability = AmenityAvailability.Available });
            v10.Amenities.Add(new VenueAmenityLink { VenueId = v10.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v10.Amenities.Add(new VenueAmenityLink { VenueId = v10.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v10.Amenities.Add(new VenueAmenityLink { VenueId = v10.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v10.Tags.Add(new VenueTagLink { VenueId = v10.Id, VenueTagId = tags["EAT_DRINK"] });
            v10.Tags.Add(new VenueTagLink { VenueId = v10.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v10);

            // [VEN-011] Nhà hàng WASABI
            var v11 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444411"),
                Name = @"Nhà hàng WASABI",
                CategoryId = categories["RESTAURANT"],
                Description = @"món Nhật; ăn chính; nhóm bạn",
                Address = @"Tòa S10.07 The Origami, Vinhomes Grand Park, Phường Long Bình, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.845128m,
                Longitude = 106.839668m,
                PriceMin = 100000m,
                PriceMax = 600000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=Nh%C3%A0%20h%C3%A0ng%20WASABI",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/Nh%C3%A0%20h%C3%A0ng%20WASABI",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v11.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v11.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("10:00:00"),
                    CloseTime = TimeOnly.Parse("14:00:00"),
                    IsClosed = false
                });
            }
            v11.Amenities.Add(new VenueAmenityLink { VenueId = v11.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v11.Amenities.Add(new VenueAmenityLink { VenueId = v11.Id, AmenityId = amenities["POWER_OUTLET"], Availability = AmenityAvailability.Available });
            v11.Amenities.Add(new VenueAmenityLink { VenueId = v11.Id, AmenityId = amenities["AIR_CONDITIONING"], Availability = AmenityAvailability.Available });
            v11.Amenities.Add(new VenueAmenityLink { VenueId = v11.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v11.Amenities.Add(new VenueAmenityLink { VenueId = v11.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v11.Amenities.Add(new VenueAmenityLink { VenueId = v11.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v11.Tags.Add(new VenueTagLink { VenueId = v11.Id, VenueTagId = tags["EAT_DRINK"] });
            v11.Tags.Add(new VenueTagLink { VenueId = v11.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v11);

            // [VEN-012] Thai Market Restaurant - Vincom Lê Văn Việt
            var v12 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444412"),
                Name = @"Thai Market Restaurant - Vincom Lê Văn Việt",
                CategoryId = categories["RESTAURANT"],
                Description = @"món Thái; ăn chính; nhóm bạn",
                Address = @"Tầng 4 Vincom Plaza, 50 Lê Văn Việt, Phường Hiệp Phú, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.845592m,
                Longitude = 106.779065m,
                PriceMin = 200000m,
                PriceMax = 300000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=Thai%20Market%20Restaurant%20-%20Vincom%20L%C3%AA%20V%C4%83n%20Vi%E1%BB%87t",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/Thai%20Market%20Restaurant%20-%20Vincom%20L%C3%AA%20V%C4%83n%20Vi%E1%BB%87t",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v12.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v12.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("10:30:00"),
                    CloseTime = TimeOnly.Parse("21:30:00"),
                    IsClosed = false
                });
            }
            v12.Amenities.Add(new VenueAmenityLink { VenueId = v12.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v12.Amenities.Add(new VenueAmenityLink { VenueId = v12.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v12.Amenities.Add(new VenueAmenityLink { VenueId = v12.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v12.Tags.Add(new VenueTagLink { VenueId = v12.Id, VenueTagId = tags["EAT_DRINK"] });
            v12.Tags.Add(new VenueTagLink { VenueId = v12.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v12);

            // [VEN-013] TIỆM PHỞ GIA TRUYỀN 1979
            var v13 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444413"),
                Name = @"TIỆM PHỞ GIA TRUYỀN 1979",
                CategoryId = categories["EATERY"],
                Description = @"ăn sáng; phở; ăn nhanh",
                Address = @"14 Đường D9, Vinhomes Grand Park, Phường Long Bình, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.839261m,
                Longitude = 106.841481m,
                PriceMin = 35000m,
                PriceMax = 60000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=TI%E1%BB%86M%20PH%E1%BB%9E%20GIA%20TRUY%E1%BB%80N%201979",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/TI%E1%BB%86M%20PH%E1%BB%9E%20GIA%20TRUY%E1%BB%80N%201979",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v13.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v13.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("06:00:00"),
                    CloseTime = TimeOnly.Parse("13:00:00"),
                    IsClosed = false
                });
            }
            v13.Amenities.Add(new VenueAmenityLink { VenueId = v13.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v13.Amenities.Add(new VenueAmenityLink { VenueId = v13.Id, AmenityId = amenities["POWER_OUTLET"], Availability = AmenityAvailability.Available });
            v13.Amenities.Add(new VenueAmenityLink { VenueId = v13.Id, AmenityId = amenities["AIR_CONDITIONING"], Availability = AmenityAvailability.Available });
            v13.Amenities.Add(new VenueAmenityLink { VenueId = v13.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v13.Amenities.Add(new VenueAmenityLink { VenueId = v13.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v13.Amenities.Add(new VenueAmenityLink { VenueId = v13.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v13.Tags.Add(new VenueTagLink { VenueId = v13.Id, VenueTagId = tags["EAT_DRINK"] });
            v13.Tags.Add(new VenueTagLink { VenueId = v13.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v13);

            // [VEN-014] Vintage Taste Deli Cafe
            var v14 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444414"),
                Name = @"Vintage Taste Deli Cafe",
                CategoryId = categories["CAFE"],
                Description = @"cà phê; gặp bạn; ăn nhẹ",
                Address = @"28 Đường D9, Vinhomes Grand Park, Phường Long Bình, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.839829m,
                Longitude = 106.841784m,
                PriceMin = 30000m,
                PriceMax = 65000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=Vintage%20Taste%20Deli%20Cafe",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/Vintage%20Taste%20Deli%20Cafe",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v14.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v14.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("06:30:00"),
                    CloseTime = TimeOnly.Parse("20:00:00"),
                    IsClosed = false
                });
            }
            v14.Amenities.Add(new VenueAmenityLink { VenueId = v14.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v14.Amenities.Add(new VenueAmenityLink { VenueId = v14.Id, AmenityId = amenities["POWER_OUTLET"], Availability = AmenityAvailability.Available });
            v14.Amenities.Add(new VenueAmenityLink { VenueId = v14.Id, AmenityId = amenities["AIR_CONDITIONING"], Availability = AmenityAvailability.Available });
            v14.Amenities.Add(new VenueAmenityLink { VenueId = v14.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v14.Amenities.Add(new VenueAmenityLink { VenueId = v14.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v14.Amenities.Add(new VenueAmenityLink { VenueId = v14.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v14.Tags.Add(new VenueTagLink { VenueId = v14.Id, VenueTagId = tags["STUDY_WORK"] });
            v14.Tags.Add(new VenueTagLink { VenueId = v14.Id, VenueTagId = tags["QUIET"] });
            venueList.Add(v14);

            // [VEN-015] CHAGEE Vinhomes Grand Park
            var v15 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444415"),
                Name = @"CHAGEE Vinhomes Grand Park",
                CategoryId = categories["TEA_BEVERAGE"],
                Description = @"trà; đồ uống; gặp bạn",
                Address = @"Tòa S5.01, 88 Phước Thiện, Vinhomes Grand Park, Phường Long Bình, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.843065m,
                Longitude = 106.841819m,
                PriceMin = 30000m,
                PriceMax = 65000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=CHAGEE%20Vinhomes%20Grand%20Park",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/CHAGEE%20Vinhomes%20Grand%20Park",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v15.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v15.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("09:00:00"),
                    CloseTime = TimeOnly.Parse("22:00:00"),
                    IsClosed = false
                });
            }
            v15.Amenities.Add(new VenueAmenityLink { VenueId = v15.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v15.Amenities.Add(new VenueAmenityLink { VenueId = v15.Id, AmenityId = amenities["POWER_OUTLET"], Availability = AmenityAvailability.Available });
            v15.Amenities.Add(new VenueAmenityLink { VenueId = v15.Id, AmenityId = amenities["AIR_CONDITIONING"], Availability = AmenityAvailability.Available });
            v15.Amenities.Add(new VenueAmenityLink { VenueId = v15.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v15.Amenities.Add(new VenueAmenityLink { VenueId = v15.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v15.Amenities.Add(new VenueAmenityLink { VenueId = v15.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v15.Tags.Add(new VenueTagLink { VenueId = v15.Id, VenueTagId = tags["HANGOUT"] });
            v15.Tags.Add(new VenueTagLink { VenueId = v15.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v15);

            // [VEN-016] Quán ăn Mê Món
            var v16 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444416"),
                Name = @"Quán ăn Mê Món",
                CategoryId = categories["RESTAURANT"],
                Description = @"món Việt; ăn chính; nhóm bạn",
                Address = @"Shophouse 01S11, Tòa GH5 Glory Heights, Vinhomes Grand Park, Phường Long Bình, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.844821m,
                Longitude = 106.842291m,
                PriceMin = 150000m,
                PriceMax = 250000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=Qu%C3%A1n%20%C4%83n%20M%C3%AA%20M%C3%B3n",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/Qu%C3%A1n%20%C4%83n%20M%C3%AA%20M%C3%B3n",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v16.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v16.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("10:00:00"),
                    CloseTime = TimeOnly.Parse("15:00:00"),
                    IsClosed = false
                });
            }
            v16.Amenities.Add(new VenueAmenityLink { VenueId = v16.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v16.Amenities.Add(new VenueAmenityLink { VenueId = v16.Id, AmenityId = amenities["POWER_OUTLET"], Availability = AmenityAvailability.Available });
            v16.Amenities.Add(new VenueAmenityLink { VenueId = v16.Id, AmenityId = amenities["AIR_CONDITIONING"], Availability = AmenityAvailability.Available });
            v16.Amenities.Add(new VenueAmenityLink { VenueId = v16.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v16.Amenities.Add(new VenueAmenityLink { VenueId = v16.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v16.Amenities.Add(new VenueAmenityLink { VenueId = v16.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v16.Tags.Add(new VenueTagLink { VenueId = v16.Id, VenueTagId = tags["EAT_DRINK"] });
            v16.Tags.Add(new VenueTagLink { VenueId = v16.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v16);

            // [VEN-017] DaHú hotpot Vincom Mega Mall
            var v17 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444417"),
                Name = @"DaHú hotpot Vincom Mega Mall",
                CategoryId = categories["RESTAURANT"],
                Description = @"lẩu; ăn nhóm; ăn tối",
                Address = @"Tầng 4 Vincom Mega Mall Grand Park, 88 Phước Thiện, Phường Long Bình, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.843742m,
                Longitude = 106.842502m,
                PriceMin = 150000m,
                PriceMax = 250000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=DaH%C3%BA%20hotpot%20Vincom%20Mega%20Mall",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/DaH%C3%BA%20hotpot%20Vincom%20Mega%20Mall",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v17.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v17.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("10:00:00"),
                    CloseTime = TimeOnly.Parse("22:00:00"),
                    IsClosed = false
                });
            }
            v17.Amenities.Add(new VenueAmenityLink { VenueId = v17.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v17.Amenities.Add(new VenueAmenityLink { VenueId = v17.Id, AmenityId = amenities["POWER_OUTLET"], Availability = AmenityAvailability.Available });
            v17.Amenities.Add(new VenueAmenityLink { VenueId = v17.Id, AmenityId = amenities["AIR_CONDITIONING"], Availability = AmenityAvailability.Available });
            v17.Amenities.Add(new VenueAmenityLink { VenueId = v17.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v17.Amenities.Add(new VenueAmenityLink { VenueId = v17.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v17.Amenities.Add(new VenueAmenityLink { VenueId = v17.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v17.Tags.Add(new VenueTagLink { VenueId = v17.Id, VenueTagId = tags["EAT_DRINK"] });
            v17.Tags.Add(new VenueTagLink { VenueId = v17.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v17);

            // [VEN-018] Vị Nhà - Tinh hoa ẩm thực 3 miền
            var v18 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444418"),
                Name = @"Vị Nhà - Tinh hoa ẩm thực 3 miền",
                CategoryId = categories["RESTAURANT"],
                Description = @"món Việt; cơm; ăn gia đình",
                Address = @"Đường T10, Phân khu The Manhattan, Vinhomes Grand Park, Phường Long Bình, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.838485m,
                Longitude = 106.843125m,
                PriceMin = 150000m,
                PriceMax = 250000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=V%E1%BB%8B%20Nh%C3%A0%20-%20Tinh%20hoa%20%E1%BA%A9m%20th%E1%BB%B1c%203%20mi%E1%BB%81n",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/V%E1%BB%8B%20Nh%C3%A0%20-%20Tinh%20hoa%20%E1%BA%A9m%20th%E1%BB%B1c%203%20mi%E1%BB%81n",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v18.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v18.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("06:00:00"),
                    CloseTime = TimeOnly.Parse("21:00:00"),
                    IsClosed = false
                });
            }
            v18.Amenities.Add(new VenueAmenityLink { VenueId = v18.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v18.Amenities.Add(new VenueAmenityLink { VenueId = v18.Id, AmenityId = amenities["POWER_OUTLET"], Availability = AmenityAvailability.Available });
            v18.Amenities.Add(new VenueAmenityLink { VenueId = v18.Id, AmenityId = amenities["AIR_CONDITIONING"], Availability = AmenityAvailability.Available });
            v18.Amenities.Add(new VenueAmenityLink { VenueId = v18.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v18.Amenities.Add(new VenueAmenityLink { VenueId = v18.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v18.Amenities.Add(new VenueAmenityLink { VenueId = v18.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v18.Tags.Add(new VenueTagLink { VenueId = v18.Id, VenueTagId = tags["EAT_DRINK"] });
            v18.Tags.Add(new VenueTagLink { VenueId = v18.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v18);

            // [VEN-019] Cơm Niêu Bà Chang
            var v19 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444419"),
                Name = @"Cơm Niêu Bà Chang",
                CategoryId = categories["RESTAURANT"],
                Description = @"cơm; ăn chính; ăn nhóm",
                Address = @"47 Đường T2, Phân khu The Manhattan, Vinhomes Grand Park, Phường Long Bình, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.840833m,
                Longitude = 106.842554m,
                PriceMin = 150000m,
                PriceMax = 250000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=C%C6%A1m%20Ni%C3%AAu%20B%C3%A0%20Chang",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/C%C6%A1m%20Ni%C3%AAu%20B%C3%A0%20Chang",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v19.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v19.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("08:00:00"),
                    CloseTime = TimeOnly.Parse("22:00:00"),
                    IsClosed = false
                });
            }
            v19.Amenities.Add(new VenueAmenityLink { VenueId = v19.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v19.Amenities.Add(new VenueAmenityLink { VenueId = v19.Id, AmenityId = amenities["POWER_OUTLET"], Availability = AmenityAvailability.Available });
            v19.Amenities.Add(new VenueAmenityLink { VenueId = v19.Id, AmenityId = amenities["AIR_CONDITIONING"], Availability = AmenityAvailability.Available });
            v19.Amenities.Add(new VenueAmenityLink { VenueId = v19.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v19.Amenities.Add(new VenueAmenityLink { VenueId = v19.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v19.Amenities.Add(new VenueAmenityLink { VenueId = v19.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v19.Tags.Add(new VenueTagLink { VenueId = v19.Id, VenueTagId = tags["EAT_DRINK"] });
            v19.Tags.Add(new VenueTagLink { VenueId = v19.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v19);

            // [VEN-020] New Zealand Ice Cream - Megamall Grand Park
            var v20 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444420"),
                Name = @"New Zealand Ice Cream - Megamall Grand Park",
                CategoryId = categories["ICE_CREAM"],
                Description = @"kem; tráng miệng; ăn vặt",
                Address = @"Tầng 3 Vincom Mega Mall Grand Park, 88 Phước Thiện, Phường Long Bình, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.843139m,
                Longitude = 106.843146m,
                PriceMin = 30000m,
                PriceMax = 65000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=New%20Zealand%20Ice%20Cream%20-%20Megamall%20Grand%20Park",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/New%20Zealand%20Ice%20Cream%20-%20Megamall%20Grand%20Park",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v20.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v20.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("09:00:00"),
                    CloseTime = TimeOnly.Parse("22:00:00"),
                    IsClosed = false
                });
            }
            v20.Amenities.Add(new VenueAmenityLink { VenueId = v20.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v20.Amenities.Add(new VenueAmenityLink { VenueId = v20.Id, AmenityId = amenities["POWER_OUTLET"], Availability = AmenityAvailability.Available });
            v20.Amenities.Add(new VenueAmenityLink { VenueId = v20.Id, AmenityId = amenities["AIR_CONDITIONING"], Availability = AmenityAvailability.Available });
            v20.Amenities.Add(new VenueAmenityLink { VenueId = v20.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v20.Amenities.Add(new VenueAmenityLink { VenueId = v20.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v20.Amenities.Add(new VenueAmenityLink { VenueId = v20.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v20.Tags.Add(new VenueTagLink { VenueId = v20.Id, VenueTagId = tags["EAT_DRINK"] });
            v20.Tags.Add(new VenueTagLink { VenueId = v20.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v20);

            // [VEN-021] Khải Phong Dimsum - Mì tươi kéo sợi - Dessert
            var v21 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444421"),
                Name = @"Khải Phong Dimsum - Mì tươi kéo sợi - Dessert",
                CategoryId = categories["RESTAURANT"],
                Description = @"dimsum; mì; ăn sáng; tráng miệng",
                Address = @"Căn T10-27 The Manhattan, Vinhomes Grand Park, Phường Long Bình, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.839154m,
                Longitude = 106.843538m,
                PriceMin = 150000m,
                PriceMax = 250000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=Kh%E1%BA%A3i%20Phong%20Dimsum%20-%20M%C3%AC%20t%C6%B0%C6%A1i%20k%C3%A9o%20s%E1%BB%A3i%20-%20Dessert",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/Kh%E1%BA%A3i%20Phong%20Dimsum%20-%20M%C3%AC%20t%C6%B0%C6%A1i%20k%C3%A9o%20s%E1%BB%A3i%20-%20Dessert",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v21.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v21.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("06:30:00"),
                    CloseTime = TimeOnly.Parse("14:00:00"),
                    IsClosed = false
                });
            }
            v21.Amenities.Add(new VenueAmenityLink { VenueId = v21.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v21.Amenities.Add(new VenueAmenityLink { VenueId = v21.Id, AmenityId = amenities["POWER_OUTLET"], Availability = AmenityAvailability.Available });
            v21.Amenities.Add(new VenueAmenityLink { VenueId = v21.Id, AmenityId = amenities["AIR_CONDITIONING"], Availability = AmenityAvailability.Available });
            v21.Amenities.Add(new VenueAmenityLink { VenueId = v21.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v21.Amenities.Add(new VenueAmenityLink { VenueId = v21.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v21.Amenities.Add(new VenueAmenityLink { VenueId = v21.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v21.Tags.Add(new VenueTagLink { VenueId = v21.Id, VenueTagId = tags["EAT_DRINK"] });
            v21.Tags.Add(new VenueTagLink { VenueId = v21.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v21);

            // [VEN-022] LIÊN HẢI PHÒNG
            var v22 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444422"),
                Name = @"LIÊN HẢI PHÒNG",
                CategoryId = categories["EATERY"],
                Description = @"ăn sáng; món Hải Phòng; ăn nhanh",
                Address = @"02 Đường T12 The Manhattan, Vinhomes Grand Park, Phường Long Bình, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.837898m,
                Longitude = 106.843933m,
                PriceMin = 35000m,
                PriceMax = 60000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=LI%C3%8AN%20H%E1%BA%A2I%20PH%C3%92NG",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/LI%C3%8AN%20H%E1%BA%A2I%20PH%C3%92NG",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v22.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v22.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("06:00:00"),
                    CloseTime = TimeOnly.Parse("20:00:00"),
                    IsClosed = false
                });
            }
            v22.Amenities.Add(new VenueAmenityLink { VenueId = v22.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v22.Amenities.Add(new VenueAmenityLink { VenueId = v22.Id, AmenityId = amenities["POWER_OUTLET"], Availability = AmenityAvailability.Available });
            v22.Amenities.Add(new VenueAmenityLink { VenueId = v22.Id, AmenityId = amenities["AIR_CONDITIONING"], Availability = AmenityAvailability.Available });
            v22.Amenities.Add(new VenueAmenityLink { VenueId = v22.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v22.Amenities.Add(new VenueAmenityLink { VenueId = v22.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v22.Amenities.Add(new VenueAmenityLink { VenueId = v22.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v22.Tags.Add(new VenueTagLink { VenueId = v22.Id, VenueTagId = tags["EAT_DRINK"] });
            v22.Tags.Add(new VenueTagLink { VenueId = v22.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v22);

            // [VEN-023] Lâm Ký Mì Gia
            var v23 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444423"),
                Name = @"Lâm Ký Mì Gia",
                CategoryId = categories["RESTAURANT"],
                Description = @"mì; ăn chính; ăn nhanh",
                Address = @"Căn T6-06 The Manhattan, Vinhomes Grand Park, Phường Long Bình, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.838145m,
                Longitude = 106.844401m,
                PriceMin = 150000m,
                PriceMax = 250000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=L%C3%A2m%20K%C3%BD%20M%C3%AC%20Gia",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/L%C3%A2m%20K%C3%BD%20M%C3%AC%20Gia",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v23.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v23.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("06:30:00"),
                    CloseTime = TimeOnly.Parse("20:30:00"),
                    IsClosed = false
                });
            }
            v23.Amenities.Add(new VenueAmenityLink { VenueId = v23.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v23.Amenities.Add(new VenueAmenityLink { VenueId = v23.Id, AmenityId = amenities["POWER_OUTLET"], Availability = AmenityAvailability.Available });
            v23.Amenities.Add(new VenueAmenityLink { VenueId = v23.Id, AmenityId = amenities["AIR_CONDITIONING"], Availability = AmenityAvailability.Available });
            v23.Amenities.Add(new VenueAmenityLink { VenueId = v23.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v23.Amenities.Add(new VenueAmenityLink { VenueId = v23.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v23.Amenities.Add(new VenueAmenityLink { VenueId = v23.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v23.Tags.Add(new VenueTagLink { VenueId = v23.Id, VenueTagId = tags["EAT_DRINK"] });
            v23.Tags.Add(new VenueTagLink { VenueId = v23.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v23);

            // [VEN-024] Link Ốc Q9
            var v24 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444424"),
                Name = @"Link Ốc Q9",
                CategoryId = categories["RESTAURANT"],
                Description = @"ốc; hải sản; ăn tối; nhóm bạn",
                Address = @"51 Phan Chu Trinh, Phường Hiệp Phú, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.842052m,
                Longitude = 106.774144m,
                PriceMin = 100000m,
                PriceMax = 200000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=Link%20%E1%BB%90c%20Q9",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/Link%20%E1%BB%90c%20Q9",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v24.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v24.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("16:00:00"),
                    CloseTime = TimeOnly.Parse("23:30:00"),
                    IsClosed = false
                });
            }
            v24.Amenities.Add(new VenueAmenityLink { VenueId = v24.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v24.Amenities.Add(new VenueAmenityLink { VenueId = v24.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v24.Amenities.Add(new VenueAmenityLink { VenueId = v24.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v24.Amenities.Add(new VenueAmenityLink { VenueId = v24.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v24.Tags.Add(new VenueTagLink { VenueId = v24.Id, VenueTagId = tags["EAT_DRINK"] });
            v24.Tags.Add(new VenueTagLink { VenueId = v24.Id, VenueTagId = tags["SOCIAL"] });
            venueList.Add(v24);

            // [VEN-025] Cafe Khanh
            var v25 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444425"),
                Name = @"Cafe Khanh",
                CategoryId = categories["CAFE"],
                Description = @"cà phê; gặp bạn; đồ uống",
                Address = @"147/24 Tăng Nhơn Phú, Phường Tăng Nhơn Phú B, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.833765m,
                Longitude = 106.774193m,
                PriceMin = 30000m,
                PriceMax = 65000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=Cafe%20Khanh",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/Cafe%20Khanh",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v25.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v25.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("06:30:00"),
                    CloseTime = TimeOnly.Parse("19:30:00"),
                    IsClosed = false
                });
            }
            v25.Amenities.Add(new VenueAmenityLink { VenueId = v25.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v25.Amenities.Add(new VenueAmenityLink { VenueId = v25.Id, AmenityId = amenities["POWER_OUTLET"], Availability = AmenityAvailability.Available });
            v25.Amenities.Add(new VenueAmenityLink { VenueId = v25.Id, AmenityId = amenities["AIR_CONDITIONING"], Availability = AmenityAvailability.Available });
            v25.Amenities.Add(new VenueAmenityLink { VenueId = v25.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v25.Amenities.Add(new VenueAmenityLink { VenueId = v25.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v25.Amenities.Add(new VenueAmenityLink { VenueId = v25.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v25.Tags.Add(new VenueTagLink { VenueId = v25.Id, VenueTagId = tags["STUDY_WORK"] });
            v25.Tags.Add(new VenueTagLink { VenueId = v25.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v25);

            // [VEN-026] Hanmaeum BBQ
            var v26 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444426"),
                Name = @"Hanmaeum BBQ",
                CategoryId = categories["RESTAURANT"],
                Description = @"BBQ; ăn tối; nhóm bạn",
                Address = @"311 Phước Thiện, Phường Long Bình, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.845993m,
                Longitude = 106.847215m,
                PriceMin = 200000m,
                PriceMax = 700000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=Hanmaeum%20BBQ",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/Hanmaeum%20BBQ",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v26.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v26.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("11:00:00"),
                    CloseTime = TimeOnly.Parse("02:00:00"),
                    IsClosed = false
                });
            }
            v26.Amenities.Add(new VenueAmenityLink { VenueId = v26.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v26.Amenities.Add(new VenueAmenityLink { VenueId = v26.Id, AmenityId = amenities["POWER_OUTLET"], Availability = AmenityAvailability.Available });
            v26.Amenities.Add(new VenueAmenityLink { VenueId = v26.Id, AmenityId = amenities["AIR_CONDITIONING"], Availability = AmenityAvailability.Available });
            v26.Amenities.Add(new VenueAmenityLink { VenueId = v26.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v26.Amenities.Add(new VenueAmenityLink { VenueId = v26.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v26.Amenities.Add(new VenueAmenityLink { VenueId = v26.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v26.Tags.Add(new VenueTagLink { VenueId = v26.Id, VenueTagId = tags["EAT_DRINK"] });
            v26.Tags.Add(new VenueTagLink { VenueId = v26.Id, VenueTagId = tags["SOCIAL"] });
            venueList.Add(v26);

            // [VEN-027] Monstera Roastery - Specialty coffee and plants
            var v27 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444427"),
                Name = @"Monstera Roastery - Specialty coffee and plants",
                CategoryId = categories["CAFE"],
                Description = @"cà phê; gặp bạn; trải nghiệm không gian",
                Address = @"6 Chu Mạnh Trinh, Phường Bình Thọ, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.847656m,
                Longitude = 106.769213m,
                PriceMin = 30000m,
                PriceMax = 65000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=Monstera%20Roastery%20-%20Specialty%20coffee%20and%20plants",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/Monstera%20Roastery%20-%20Specialty%20coffee%20and%20plants",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v27.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v27.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("07:00:00"),
                    CloseTime = TimeOnly.Parse("22:00:00"),
                    IsClosed = false
                });
            }
            v27.Amenities.Add(new VenueAmenityLink { VenueId = v27.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v27.Amenities.Add(new VenueAmenityLink { VenueId = v27.Id, AmenityId = amenities["POWER_OUTLET"], Availability = AmenityAvailability.Available });
            v27.Amenities.Add(new VenueAmenityLink { VenueId = v27.Id, AmenityId = amenities["AIR_CONDITIONING"], Availability = AmenityAvailability.Available });
            v27.Amenities.Add(new VenueAmenityLink { VenueId = v27.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v27.Amenities.Add(new VenueAmenityLink { VenueId = v27.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v27.Amenities.Add(new VenueAmenityLink { VenueId = v27.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v27.Tags.Add(new VenueTagLink { VenueId = v27.Id, VenueTagId = tags["STUDY_WORK"] });
            v27.Tags.Add(new VenueTagLink { VenueId = v27.Id, VenueTagId = tags["QUIET"] });
            venueList.Add(v27);

            // [VEN-028] Root's (Cà phê - Bánh mì - Trái cây)
            var v28 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444428"),
                Name = @"Root's (Cà phê - Bánh mì - Trái cây)",
                CategoryId = categories["OTHER"],
                Description = @"cà phê; bánh mì; bữa sáng; mang đi",
                Address = @"276 Võ Văn Ngân, Phường Bình Thọ, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.849534m,
                Longitude = 106.769286m,
                PriceMin = 35000m,
                PriceMax = 60000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=Root's%20(C%C3%A0%20ph%C3%AA%20-%20B%C3%A1nh%20m%C3%AC%20-%20Tr%C3%A1i%20c%C3%A2y)",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/Root's%20(C%C3%A0%20ph%C3%AA%20-%20B%C3%A1nh%20m%C3%AC%20-%20Tr%C3%A1i%20c%C3%A2y)",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v28.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v28.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("06:30:00"),
                    CloseTime = TimeOnly.Parse("18:30:00"),
                    IsClosed = false
                });
            }
            v28.Amenities.Add(new VenueAmenityLink { VenueId = v28.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v28.Amenities.Add(new VenueAmenityLink { VenueId = v28.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v28.Amenities.Add(new VenueAmenityLink { VenueId = v28.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v28.Amenities.Add(new VenueAmenityLink { VenueId = v28.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v28.Tags.Add(new VenueTagLink { VenueId = v28.Id, VenueTagId = tags["EAT_DRINK"] });
            v28.Tags.Add(new VenueTagLink { VenueId = v28.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v28);

            // [VEN-029] CHANGE COFFEE & TEA HOUSE
            var v29 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444429"),
                Name = @"CHANGE COFFEE & TEA HOUSE",
                CategoryId = categories["TEA_BEVERAGE"],
                Description = @"cà phê; trà; gặp bạn",
                Address = @"38 Đường 297, Phường Phước Long B, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.824942m,
                Longitude = 106.771771m,
                PriceMin = 30000m,
                PriceMax = 65000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=CHANGE%20COFFEE%20%26amp%3B%20TEA%20HOUSE",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/CHANGE%20COFFEE%20%26amp%3B%20TEA%20HOUSE",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v29.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v29.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("07:00:00"),
                    CloseTime = TimeOnly.Parse("23:00:00"),
                    IsClosed = false
                });
            }
            v29.Amenities.Add(new VenueAmenityLink { VenueId = v29.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v29.Amenities.Add(new VenueAmenityLink { VenueId = v29.Id, AmenityId = amenities["AIR_CONDITIONING"], Availability = AmenityAvailability.Available });
            v29.Amenities.Add(new VenueAmenityLink { VenueId = v29.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v29.Amenities.Add(new VenueAmenityLink { VenueId = v29.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v29.Amenities.Add(new VenueAmenityLink { VenueId = v29.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v29.Tags.Add(new VenueTagLink { VenueId = v29.Id, VenueTagId = tags["HANGOUT"] });
            v29.Tags.Add(new VenueTagLink { VenueId = v29.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v29);

            // [VEN-030] ROSIE BAKERY
            var v30 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444430"),
                Name = @"ROSIE BAKERY",
                CategoryId = categories["DESSERT"],
                Description = @"bánh; tráng miệng; mua mang đi",
                Address = @"15 Hòa Bình, Phường Bình Thọ, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.843278m,
                Longitude = 106.766985m,
                PriceMin = 30000m,
                PriceMax = 65000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=ROSIE%20BAKERY",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/ROSIE%20BAKERY",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v30.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v30.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("09:00:00"),
                    CloseTime = TimeOnly.Parse("21:30:00"),
                    IsClosed = false
                });
            }
            v30.Amenities.Add(new VenueAmenityLink { VenueId = v30.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v30.Amenities.Add(new VenueAmenityLink { VenueId = v30.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v30.Amenities.Add(new VenueAmenityLink { VenueId = v30.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v30.Amenities.Add(new VenueAmenityLink { VenueId = v30.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v30.Tags.Add(new VenueTagLink { VenueId = v30.Id, VenueTagId = tags["EAT_DRINK"] });
            v30.Tags.Add(new VenueTagLink { VenueId = v30.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v30);

            // [VEN-031] KA3 Nướng
            var v31 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444431"),
                Name = @"KA3 Nướng",
                CategoryId = categories["RESTAURANT"],
                Description = @"nướng; ăn tối; nhóm bạn",
                Address = @"12/4 Đường số 9, Khu phố 3, Phường Linh Xuân, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.857021m,
                Longitude = 106.769795m,
                PriceMin = 100000m,
                PriceMax = 200000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=KA3%20N%C6%B0%E1%BB%9Bng",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/KA3%20N%C6%B0%E1%BB%9Bng",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v31.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v31.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("16:00:00"),
                    CloseTime = TimeOnly.Parse("00:30:00"),
                    IsClosed = false
                });
            }
            v31.Amenities.Add(new VenueAmenityLink { VenueId = v31.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v31.Amenities.Add(new VenueAmenityLink { VenueId = v31.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v31.Amenities.Add(new VenueAmenityLink { VenueId = v31.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v31.Amenities.Add(new VenueAmenityLink { VenueId = v31.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v31.Tags.Add(new VenueTagLink { VenueId = v31.Id, VenueTagId = tags["EAT_DRINK"] });
            v31.Tags.Add(new VenueTagLink { VenueId = v31.Id, VenueTagId = tags["SOCIAL"] });
            venueList.Add(v31);

            // [VEN-032] Cafe Thanh
            var v32 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444432"),
                Name = @"Cafe Thanh",
                CategoryId = categories["CAFE"],
                Description = @"cà phê; gặp bạn; đồ uống",
                Address = @"15A Hòa Bình, Phường Bình Thọ, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.843169m,
                Longitude = 106.766891m,
                PriceMin = 30000m,
                PriceMax = 65000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=Cafe%20Thanh",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/Cafe%20Thanh",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v32.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v32.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("07:00:00"),
                    CloseTime = TimeOnly.Parse("23:00:00"),
                    IsClosed = false
                });
            }
            v32.Amenities.Add(new VenueAmenityLink { VenueId = v32.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v32.Amenities.Add(new VenueAmenityLink { VenueId = v32.Id, AmenityId = amenities["POWER_OUTLET"], Availability = AmenityAvailability.Available });
            v32.Amenities.Add(new VenueAmenityLink { VenueId = v32.Id, AmenityId = amenities["AIR_CONDITIONING"], Availability = AmenityAvailability.Available });
            v32.Amenities.Add(new VenueAmenityLink { VenueId = v32.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v32.Amenities.Add(new VenueAmenityLink { VenueId = v32.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v32.Amenities.Add(new VenueAmenityLink { VenueId = v32.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v32.Tags.Add(new VenueTagLink { VenueId = v32.Id, VenueTagId = tags["STUDY_WORK"] });
            v32.Tags.Add(new VenueTagLink { VenueId = v32.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v32);

            // [VEN-033] Lò Há Cảo Tâm Ký
            var v33 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444433"),
                Name = @"Lò Há Cảo Tâm Ký",
                CategoryId = categories["RESTAURANT"],
                Description = @"há cảo; món Hoa; ăn chính",
                Address = @"189 Hoàng Diệu 2, Phường Linh Chiểu, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.854362m,
                Longitude = 106.769507m,
                PriceMin = 100000m,
                PriceMax = 200000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=L%C3%B2%20H%C3%A1%20C%E1%BA%A3o%20T%C3%A2m%20K%C3%BD",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/L%C3%B2%20H%C3%A1%20C%E1%BA%A3o%20T%C3%A2m%20K%C3%BD",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v33.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v33.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("07:00:00"),
                    CloseTime = TimeOnly.Parse("22:00:00"),
                    IsClosed = false
                });
            }
            v33.Amenities.Add(new VenueAmenityLink { VenueId = v33.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v33.Amenities.Add(new VenueAmenityLink { VenueId = v33.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v33.Amenities.Add(new VenueAmenityLink { VenueId = v33.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v33.Amenities.Add(new VenueAmenityLink { VenueId = v33.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v33.Tags.Add(new VenueTagLink { VenueId = v33.Id, VenueTagId = tags["EAT_DRINK"] });
            v33.Tags.Add(new VenueTagLink { VenueId = v33.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v33);

            // [VEN-034] Mala Panda Thủ Đức
            var v34 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444434"),
                Name = @"Mala Panda Thủ Đức",
                CategoryId = categories["RESTAURANT"],
                Description = @"món cay; ăn nhanh; ăn nhóm",
                Address = @"133B Hoàng Diệu 2, Phường Linh Chiểu, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.85554m,
                Longitude = 106.767285m,
                PriceMin = 100000m,
                PriceMax = 200000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=Mala%20Panda%20Th%E1%BB%A7%20%C4%90%E1%BB%A9c",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/Mala%20Panda%20Th%E1%BB%A7%20%C4%90%E1%BB%A9c",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v34.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v34.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("10:00:00"),
                    CloseTime = TimeOnly.Parse("22:00:00"),
                    IsClosed = false
                });
            }
            v34.Amenities.Add(new VenueAmenityLink { VenueId = v34.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v34.Amenities.Add(new VenueAmenityLink { VenueId = v34.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v34.Amenities.Add(new VenueAmenityLink { VenueId = v34.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v34.Amenities.Add(new VenueAmenityLink { VenueId = v34.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v34.Tags.Add(new VenueTagLink { VenueId = v34.Id, VenueTagId = tags["EAT_DRINK"] });
            v34.Tags.Add(new VenueTagLink { VenueId = v34.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v34);

            // [VEN-035] VIÊN Thủ Đức - Rice . Coffee & More
            var v35 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444435"),
                Name = @"VIÊN Thủ Đức - Rice . Coffee & More",
                CategoryId = categories["EATERY"],
                Description = @"cơm; cà phê; ăn chính; gặp bạn",
                Address = @"68A Tô Vĩnh Diện, Phường Linh Chiểu, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.852391m,
                Longitude = 106.767064m,
                PriceMin = 35000m,
                PriceMax = 60000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=VI%C3%8AN%20Th%E1%BB%A7%20%C4%90%E1%BB%A9c%20-%20Rice%20.%20Coffee%20%26amp%3B%20More",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/VI%C3%8AN%20Th%E1%BB%A7%20%C4%90%E1%BB%A9c%20-%20Rice%20.%20Coffee%20%26amp%3B%20More",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v35.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v35.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("10:30:00"),
                    CloseTime = TimeOnly.Parse("21:30:00"),
                    IsClosed = false
                });
            }
            v35.Amenities.Add(new VenueAmenityLink { VenueId = v35.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v35.Amenities.Add(new VenueAmenityLink { VenueId = v35.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v35.Amenities.Add(new VenueAmenityLink { VenueId = v35.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v35.Amenities.Add(new VenueAmenityLink { VenueId = v35.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v35.Tags.Add(new VenueTagLink { VenueId = v35.Id, VenueTagId = tags["EAT_DRINK"] });
            v35.Tags.Add(new VenueTagLink { VenueId = v35.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v35);

            // [VEN-036] BIUBIU Café - Study & Workspace
            var v36 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444436"),
                Name = @"BIUBIU Café - Study & Workspace",
                CategoryId = categories["CAFE"],
                Description = @"cà phê; học tập/làm việc (cần xác minh ổ cắm và chỗ ngồi)",
                Address = @"32 Đường số 7, Khu phố 14, Phường Linh Xuân, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.858423m,
                Longitude = 106.767203m,
                PriceMin = 30000m,
                PriceMax = 65000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=BIUBIU%20Caf%C3%A9%20-%20Study%20%26amp%3B%20Workspace",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/BIUBIU%20Caf%C3%A9%20-%20Study%20%26amp%3B%20Workspace",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v36.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v36.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("08:00:00"),
                    CloseTime = TimeOnly.Parse("23:59:00"),
                    IsClosed = false
                });
            }
            v36.Amenities.Add(new VenueAmenityLink { VenueId = v36.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v36.Amenities.Add(new VenueAmenityLink { VenueId = v36.Id, AmenityId = amenities["POWER_OUTLET"], Availability = AmenityAvailability.Available });
            v36.Amenities.Add(new VenueAmenityLink { VenueId = v36.Id, AmenityId = amenities["AIR_CONDITIONING"], Availability = AmenityAvailability.Available });
            v36.Amenities.Add(new VenueAmenityLink { VenueId = v36.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v36.Amenities.Add(new VenueAmenityLink { VenueId = v36.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v36.Amenities.Add(new VenueAmenityLink { VenueId = v36.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v36.Tags.Add(new VenueTagLink { VenueId = v36.Id, VenueTagId = tags["STUDY_WORK"] });
            v36.Tags.Add(new VenueTagLink { VenueId = v36.Id, VenueTagId = tags["QUIET"] });
            venueList.Add(v36);

            // [VEN-037] Juice 197 - Nước ép, sinh tố
            var v37 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444437"),
                Name = @"Juice 197 - Nước ép, sinh tố",
                CategoryId = categories["TEA_BEVERAGE"],
                Description = @"nước ép; sinh tố; đồ uống",
                Address = @"197 Đường 410, Phường Phước Long B, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.815453m,
                Longitude = 106.765429m,
                PriceMin = 30000m,
                PriceMax = 65000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=Juice%20197%20-%20N%C6%B0%E1%BB%9Bc%20%C3%A9p%2C%20sinh%20t%E1%BB%91",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/Juice%20197%20-%20N%C6%B0%E1%BB%9Bc%20%C3%A9p%2C%20sinh%20t%E1%BB%91",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v37.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v37.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("07:00:00"),
                    CloseTime = TimeOnly.Parse("21:00:00"),
                    IsClosed = false
                });
            }
            v37.Amenities.Add(new VenueAmenityLink { VenueId = v37.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v37.Amenities.Add(new VenueAmenityLink { VenueId = v37.Id, AmenityId = amenities["AIR_CONDITIONING"], Availability = AmenityAvailability.Available });
            v37.Amenities.Add(new VenueAmenityLink { VenueId = v37.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v37.Amenities.Add(new VenueAmenityLink { VenueId = v37.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v37.Amenities.Add(new VenueAmenityLink { VenueId = v37.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v37.Tags.Add(new VenueTagLink { VenueId = v37.Id, VenueTagId = tags["HANGOUT"] });
            v37.Tags.Add(new VenueTagLink { VenueId = v37.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v37);

            // [VEN-038] BBQ Tường Phú
            var v38 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444438"),
                Name = @"BBQ Tường Phú",
                CategoryId = categories["RESTAURANT"],
                Description = @"nướng; ăn nhóm; ăn tối",
                Address = @"1-32 Đường Hồ Thiên Nga, Vinhomes Grand Park, Phường Long Bình, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.83702m,
                Longitude = 106.841401m,
                PriceMin = 100000m,
                PriceMax = 200000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.facebook.com/search/top?q=BBQ%20T%C6%B0%E1%BB%9Dng%20Ph%C3%BA",
                MenuUrl = @"https://shopeefood.vn/tim-kiem/BBQ%20T%C6%B0%E1%BB%9Dng%20Ph%C3%BA",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v38.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v38.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("09:00:00"),
                    CloseTime = TimeOnly.Parse("23:59:00"),
                    IsClosed = false
                });
            }
            v38.Amenities.Add(new VenueAmenityLink { VenueId = v38.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v38.Amenities.Add(new VenueAmenityLink { VenueId = v38.Id, AmenityId = amenities["POWER_OUTLET"], Availability = AmenityAvailability.Available });
            v38.Amenities.Add(new VenueAmenityLink { VenueId = v38.Id, AmenityId = amenities["AIR_CONDITIONING"], Availability = AmenityAvailability.Available });
            v38.Amenities.Add(new VenueAmenityLink { VenueId = v38.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v38.Amenities.Add(new VenueAmenityLink { VenueId = v38.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v38.Amenities.Add(new VenueAmenityLink { VenueId = v38.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v38.Tags.Add(new VenueTagLink { VenueId = v38.Id, VenueTagId = tags["EAT_DRINK"] });
            v38.Tags.Add(new VenueTagLink { VenueId = v38.Id, VenueTagId = tags["SOCIAL"] });
            venueList.Add(v38);

            // [VEN-039] Highlands Coffee - Nguyễn Văn Tăng
            var v39 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444439"),
                Name = @"Highlands Coffee - Nguyễn Văn Tăng",
                CategoryId = categories["CAFE"],
                Description = @"cà phê; học bài; làm việc; gặp gỡ bạn bè; máy lạnh",
                Address = @"327 Nguyễn Văn Tăng, Phường Long Thạnh Mỹ, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.841425m,
                Longitude = 106.823677m,
                PriceMin = 29000m,
                PriceMax = 69000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://www.highlandscoffee.com.vn",
                MenuUrl = @"https://shopeefood.vn/ho-chi-minh/highlands-coffee-nguyen-van-tang",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v39.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v39.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("08:00:00"),
                    CloseTime = TimeOnly.Parse("22:00:00"),
                    IsClosed = false
                });
            }
            v39.Photos.Add(new VenuePhoto { Id = Guid.NewGuid(), VenueId = v39.Id, ImageUrl = @"https://down-vn.img.susercontent.com/vn-11134513-81ztc-mtxi1an3ztoo51", PhotoType = VenuePhotoType.Exterior, SortOrder = 1, Source = @"ShopeeFood & Foody / Bản quyền thuộc Highlands Coffee" });
            v39.Photos.Add(new VenuePhoto { Id = Guid.NewGuid(), VenueId = v39.Id, ImageUrl = @"https://www.foody.vn/ho-chi-minh/highlands-coffee-nguyen-van-tang", PhotoType = VenuePhotoType.Interior, SortOrder = 2, Source = @"ShopeeFood & Foody / Bản quyền thuộc Highlands Coffee" });
            v39.Photos.Add(new VenuePhoto { Id = Guid.NewGuid(), VenueId = v39.Id, ImageUrl = @"https://shopeefood.vn/ho-chi-minh/highlands-coffee-nguyen-van-tang", PhotoType = VenuePhotoType.Menu, SortOrder = 3, Source = @"ShopeeFood & Foody / Bản quyền thuộc Highlands Coffee" });
            v39.Amenities.Add(new VenueAmenityLink { VenueId = v39.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v39.Amenities.Add(new VenueAmenityLink { VenueId = v39.Id, AmenityId = amenities["POWER_OUTLET"], Availability = AmenityAvailability.Available });
            v39.Amenities.Add(new VenueAmenityLink { VenueId = v39.Id, AmenityId = amenities["AIR_CONDITIONING"], Availability = AmenityAvailability.Available });
            v39.Amenities.Add(new VenueAmenityLink { VenueId = v39.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v39.Amenities.Add(new VenueAmenityLink { VenueId = v39.Id, AmenityId = amenities["CAR_PARKING"], Availability = AmenityAvailability.Available });
            v39.Amenities.Add(new VenueAmenityLink { VenueId = v39.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v39.Tags.Add(new VenueTagLink { VenueId = v39.Id, VenueTagId = tags["STUDY_WORK"] });
            v39.Tags.Add(new VenueTagLink { VenueId = v39.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v39);

            // [VEN-040] Trà Sữa MayCha - Nguyễn Văn Tăng
            var v40 = new Venue
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444440"),
                Name = @"Trà Sữa MayCha - Nguyễn Văn Tăng",
                CategoryId = categories["TEA_BEVERAGE"],
                Description = @"trà sữa; trà trái cây; sinh viên; học nhóm; ăn vặt",
                Address = @"398 Nguyễn Văn Tăng, Phường Long Thạnh Mỹ, TP. Thủ Đức, TP. Hồ Chí Minh",
                Latitude = 10.840931m,
                Longitude = 106.826376m,
                PriceMin = 25000m,
                PriceMax = 55000m,
                PriceBasis = PriceBasis.PerPerson,
                CurrencyCode = "VND",
                WebsiteUrl = @"https://maycha.com.vn",
                MenuUrl = @"https://shopeefood.vn/ho-chi-minh/tra-sua-maycha-398-nguyen-van-tang",
                Status = VenueStatus.Published
            };
            foreach (DayOfWeek day in Enum.GetValues<DayOfWeek>())
            {
                v40.OpeningHours.Add(new VenueOpeningHour
                {
                    Id = Guid.NewGuid(),
                    VenueId = v40.Id,
                    DayOfWeek = day,
                    OpenTime = TimeOnly.Parse("08:00:00"),
                    CloseTime = TimeOnly.Parse("22:00:00"),
                    IsClosed = false
                });
            }
            v40.Photos.Add(new VenuePhoto { Id = Guid.NewGuid(), VenueId = v40.Id, ImageUrl = @"https://down-vn.img.susercontent.com/vn-11134513-81ztc-mtxlrgnlbo5e66", PhotoType = VenuePhotoType.Exterior, SortOrder = 1, Source = @"ShopeeFood & Foody / Bản quyền thuộc MayCha" });
            v40.Photos.Add(new VenuePhoto { Id = Guid.NewGuid(), VenueId = v40.Id, ImageUrl = @"https://www.foody.vn/ho-chi-minh/tra-sua-maycha-398-nguyen-van-tang", PhotoType = VenuePhotoType.Interior, SortOrder = 2, Source = @"ShopeeFood & Foody / Bản quyền thuộc MayCha" });
            v40.Photos.Add(new VenuePhoto { Id = Guid.NewGuid(), VenueId = v40.Id, ImageUrl = @"https://shopeefood.vn/ho-chi-minh/tra-sua-maycha-398-nguyen-van-tang", PhotoType = VenuePhotoType.Menu, SortOrder = 3, Source = @"ShopeeFood & Foody / Bản quyền thuộc MayCha" });
            v40.Amenities.Add(new VenueAmenityLink { VenueId = v40.Id, AmenityId = amenities["WIFI"], Availability = AmenityAvailability.Available });
            v40.Amenities.Add(new VenueAmenityLink { VenueId = v40.Id, AmenityId = amenities["POWER_OUTLET"], Availability = AmenityAvailability.Available });
            v40.Amenities.Add(new VenueAmenityLink { VenueId = v40.Id, AmenityId = amenities["AIR_CONDITIONING"], Availability = AmenityAvailability.Available });
            v40.Amenities.Add(new VenueAmenityLink { VenueId = v40.Id, AmenityId = amenities["MOTORBIKE_PARKING"], Availability = AmenityAvailability.Available });
            v40.Amenities.Add(new VenueAmenityLink { VenueId = v40.Id, AmenityId = amenities["GROUP_SEATING"], Availability = AmenityAvailability.Available });
            v40.Tags.Add(new VenueTagLink { VenueId = v40.Id, VenueTagId = tags["HANGOUT"] });
            v40.Tags.Add(new VenueTagLink { VenueId = v40.Id, VenueTagId = tags["MODERATE"] });
            venueList.Add(v40);

            await context.Venues.AddRangeAsync(venueList);
            await context.SaveChangesAsync();
        }
    }
}
