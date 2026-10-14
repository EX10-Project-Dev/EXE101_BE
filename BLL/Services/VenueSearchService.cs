using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BLL.DTOs.Requests;
using BLL.DTOs.Responses;
using BLL.Interfaces;
using DAL.Entities;
using DAL.IRepositories;

namespace BLL.Services;

public sealed class VenueSearchService : IVenueSearchService
{
    private readonly IUnitOfWork _unitOfWork;

    public VenueSearchService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<VenueSearchResponse> SearchAsync(VenueSearchRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Fetch published venues with full details (Category, Amenities, Tags, Photos, OpeningHours)
        var allVenues = await _unitOfWork.Venues.GetPublishedVenuesWithDetailsAsync(cancellationToken);

        if (allVenues.Count == 0)
        {
            return new VenueSearchResponse
            {
                SearchHistoryId = Guid.NewGuid(),
                Status = "NO_MATCH",
                Message = "Hiện tại chưa có dữ liệu địa điểm trong hệ thống.",
                TotalCount = 0,
                Page = request.Page,
                PageSize = request.PageSize,
                HasMore = false,
                Results = []
            };
        }

        // 2. Normalize search criteria
        var origin = request.Origin;
        var hasOrigin = origin is not null;
        var userLat = origin?.Latitude ?? 0;
        var userLng = origin?.Longitude ?? 0;

        var categoryCodes = request.CategoryCodes
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim().ToUpperInvariant())
            .ToHashSet();

        var atmosphereTags = request.AtmosphereTags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim().ToUpperInvariant())
            .ToHashSet();

        var amenityCodes = request.AmenityCodes
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => a.Trim().ToUpperInvariant())
            .ToHashSet();

        var purpose = request.Purpose?.Trim().ToUpperInvariant();
        var prompt = request.Prompt?.Trim().ToLowerInvariant();

        // 3. Evaluate and filter venues
        var scoredList = new List<VenueSearchResult>();

        foreach (var v in allVenues)
        {
            // Calculate distance
            double? distanceKm = null;
            if (hasOrigin)
            {
                distanceKm = CalculateHaversineDistance(userLat, userLng, (double)v.Latitude, (double)v.Longitude);
            }

            // Hard Filter: Distance
            if (request.MaxDistanceKm.HasValue && distanceKm.HasValue && distanceKm.Value > request.MaxDistanceKm.Value)
            {
                continue;
            }

            // Hard Filter: Budget
            if (request.BudgetMin.HasValue && v.PriceMax.HasValue && v.PriceMax.Value < request.BudgetMin.Value)
            {
                continue;
            }
            if (request.BudgetMax.HasValue && v.PriceMin.HasValue && v.PriceMin.Value > request.BudgetMax.Value)
            {
                continue;
            }

            // Hard Filter: Category
            if (categoryCodes.Count > 0 && (v.Category is null || !categoryCodes.Contains(v.Category.Code.ToUpperInvariant())))
            {
                continue;
            }

            // Available amenities for this venue
            var venueAvailableAmenities = v.Amenities
                .Where(a => a.Availability == AmenityAvailability.Available)
                .Select(a => a.Amenity.Code.ToUpperInvariant())
                .ToHashSet();

            // Hard Filter: Required Amenities
            if (amenityCodes.Count > 0 && !amenityCodes.IsSubsetOf(venueAvailableAmenities))
            {
                continue;
            }

            // Check if open now (VN time UTC+7)
            var nowVn = DateTime.UtcNow.AddHours(7);
            var todayDayOfWeek = nowVn.DayOfWeek;
            var currentTime = TimeOnly.FromDateTime(nowVn);
            var isOpenNow = false;

            var todayHour = v.OpeningHours.FirstOrDefault(h => h.DayOfWeek == todayDayOfWeek);
            if (todayHour is not null && !todayHour.IsClosed && todayHour.OpenTime.HasValue && todayHour.CloseTime.HasValue)
            {
                isOpenNow = currentTime >= todayHour.OpenTime.Value && currentTime <= todayHour.CloseTime.Value;
            }
            else if (v.OpeningHours.Count == 0)
            {
                isOpenNow = true;
            }

            // 4. Scoring & Ranking Logic
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
            var hasPurposeMatch = !string.IsNullOrEmpty(purpose) && v.Tags.Any(t =>
                t.Tag.TagType == VenueTagType.Purpose && t.Tag.Code.Equals(purpose, StringComparison.OrdinalIgnoreCase));
            if (hasPurposeMatch) score += 0.15;

            // Atmosphere bonus (+0.15)
            var hasAtmosphereMatch = atmosphereTags.Count > 0 && v.Tags.Any(t =>
                t.Tag.TagType == VenueTagType.Atmosphere && atmosphereTags.Contains(t.Tag.Code.ToUpperInvariant()));
            if (hasAtmosphereMatch) score += 0.15;

            // Budget fit bonus (+0.10)
            if (request.BudgetMin.HasValue || request.BudgetMax.HasValue)
            {
                score += 0.10;
            }

            // Group size bonus
            if (request.GroupSize.HasValue && request.GroupSize.Value >= 4 && venueAvailableAmenities.Contains("GROUP_SEATING"))
            {
                score += 0.05;
            }

            // Prompt match bonus
            if (!string.IsNullOrWhiteSpace(prompt))
            {
                var combinedText = $"{v.Name} {v.Description} {v.Category?.Name}".ToLowerInvariant();
                if (combinedText.Contains(prompt))
                {
                    score += 0.15;
                }
            }

            var finalScore = Math.Min(1.0, Math.Round(score, 2));

            // 5. Generate Match Reasons
            var reasons = new List<string>();
            if (distanceKm.HasValue)
            {
                reasons.Add($"Cách bạn {distanceKm.Value:F1} km");
            }
            if (v.PriceMin.HasValue && v.PriceMax.HasValue)
            {
                reasons.Add($"Mức giá {v.PriceMin.Value:N0} - {v.PriceMax.Value:N0} VND phù hợp");
            }
            if (hasPurposeMatch)
            {
                var pTagName = v.Tags.FirstOrDefault(t => t.Tag.TagType == VenueTagType.Purpose)?.Tag.Name ?? "học tập / làm việc";
                reasons.Add($"Phù hợp {pTagName}");
            }
            if (hasAtmosphereMatch)
            {
                var aTagName = v.Tags.FirstOrDefault(t => t.Tag.TagType == VenueTagType.Atmosphere)?.Tag.Name ?? "thoải mái";
                reasons.Add($"Không gian {aTagName}");
            }
            if (venueAvailableAmenities.Contains("WIFI") && venueAvailableAmenities.Contains("AIR_CONDITIONING"))
            {
                reasons.Add("Có Wi-Fi và máy lạnh");
            }
            else if (venueAvailableAmenities.Contains("WIFI"))
            {
                reasons.Add("Có Wi-Fi miễn phí");
            }

            var summaryReason = reasons.Count > 0
                ? string.Join(", ", reasons) + "."
                : "Địa điểm được gợi ý dựa trên đánh giá và mức độ phổ biến.";

            // 6. Map to DTO
            var imageUrls = v.Photos
                .OrderBy(p => p.SortOrder)
                .Select(p => p.ImageUrl)
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .ToList();

            var summary = new VenueSummary
            {
                VenueId = v.Id,
                Name = v.Name,
                CategoryName = v.Category?.Name ?? "F&B",
                ShortDescription = v.Description,
                ImageUrls = imageUrls,
                Address = v.Address,
                Location = new VenueLocation
                {
                    Latitude = (double)v.Latitude,
                    Longitude = (double)v.Longitude
                },
                PriceMin = v.PriceMin,
                PriceMax = v.PriceMax,
                CurrencyCode = v.CurrencyCode,
                IsOpenNow = isOpenNow,
                RatingAverage = 4.6,
                ReviewCount = 20,
                AmenityCodes = venueAvailableAmenities.ToList()
            };

            scoredList.Add(new VenueSearchResult
            {
                Venue = summary,
                DistanceKm = distanceKm,
                MatchScore = finalScore,
                RecommendationReason = summaryReason,
                MatchReasons = reasons
            });
        }

        // 7. Sort by MatchScore DESC, then Distance ASC
        var sortedResults = scoredList
            .OrderByDescending(r => r.MatchScore)
            .ThenBy(r => r.DistanceKm ?? double.MaxValue)
            .ToList();

        var totalCount = sortedResults.Count;

        if (totalCount == 0)
        {
            return new VenueSearchResponse
            {
                SearchHistoryId = Guid.NewGuid(),
                Status = "NO_MATCH",
                Message = "Không tìm thấy địa điểm phù hợp với tiêu chí hiện tại. Bạn có thể thử nới rộng khoảng cách hoặc khoảng giá.",
                TotalCount = 0,
                Page = request.Page,
                PageSize = request.PageSize,
                HasMore = false,
                Results = []
            };
        }

        // 8. Pagination
        var pageSize = Math.Max(1, request.PageSize);
        var page = Math.Max(1, request.Page);
        var pagedResults = sortedResults
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var hasMore = (page * pageSize) < totalCount;

        return new VenueSearchResponse
        {
            SearchHistoryId = Guid.NewGuid(),
            Status = "MATCHED",
            Message = $"Tìm thấy {totalCount} địa điểm phù hợp nhất với nhu cầu của bạn.",
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            HasMore = hasMore,
            Results = pagedResults
        };
    }

    private static double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return Math.Round(earthRadiusKm * c, 2);
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
