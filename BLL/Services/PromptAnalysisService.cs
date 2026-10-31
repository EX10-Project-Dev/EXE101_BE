using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using BLL.DTOs.Common;

namespace BLL.Services;

/// <summary>Offline Vietnamese keyword parser used when AI extraction is unavailable.</summary>
public static class PromptAnalysisService
{
    public static ParsedSearchCriteria AnalyzeWithRuleBasedNlp(string prompt)
    {
        var lower = prompt.ToLowerInvariant();

        // 1. Category Inference
        string? categoryCode = null;
        if (Regex.IsMatch(lower, @"(cà phê|cafe|coffee|caphe)"))
            categoryCode = "CAFE";
        else if (Regex.IsMatch(lower, @"(trà sữa|tra sua|trà trái cây|milk tea)"))
            categoryCode = "TEA_DRINKS";
        else if (Regex.IsMatch(lower, @"mala"))
            categoryCode = "MALA_HOTPOT_FAST_FOOD";
        else if (Regex.IsMatch(lower, @"(tráng miệng|dessert|bakery)"))
            categoryCode = "BAKERY_DESSERT";
        else if (Regex.IsMatch(lower, @"(kem|ice cream)"))
            categoryCode = "ICE_CREAM";
        else if (Regex.IsMatch(lower, @"quán ăn"))
            categoryCode = "EATERY";
        else if (Regex.IsMatch(lower, @"nhà hàng"))
            categoryCode = "RESTAURANT";

        // 2. Purpose Inference
        string? purpose = null;
        if (Regex.IsMatch(lower, @"(học|học bài|học nhóm|chạy deadline|study)"))
            purpose = "STUDY";
        else if (Regex.IsMatch(lower, @"(làm việc|họp|work)"))
            purpose = "WORK";
        else if (Regex.IsMatch(lower, @"(hẹn hò|date|dating|người yêu|lãng mạn)"))
            purpose = "DATING";
        else if (Regex.IsMatch(lower, @"(tụ tập|nhóm bạn|gặp gỡ|chém gió|gathering)"))
            purpose = "GATHERING";
        else if (Regex.IsMatch(lower, @"(ăn tối|ăn trưa|ăn uống|dinner)"))
            purpose = "DINING";

        // 3. Atmosphere Inference
        var atmospheres = new List<string>();
        if (Regex.IsMatch(lower, @"(yên tĩnh|quiet|tĩnh lặng|ít ồn|không ồn)"))
            atmospheres.Add("QUIET");
        if (Regex.IsMatch(lower, @"(ấm cúng|cozy|ấm áp)"))
            atmospheres.Add("COZY");
        if (Regex.IsMatch(lower, @"(chill|thoải mái|thư giãn)"))
            atmospheres.Add("CHILL");
        if (Regex.IsMatch(lower, @"(náo nhiệt|sôi động|vui vẻ|lively)"))
            atmospheres.Add("LIVELY");
        if (Regex.IsMatch(lower, @"(rooftop|tầng thượng|ngoài trời|outdoor|view đẹp|thoáng)"))
            atmospheres.Add("OUTDOOR");

        // 4. Amenities Inference
        var amenities = new List<string>();
        if (Regex.IsMatch(lower, @"(wifi|wi-fi|mạng|internet)"))
            amenities.Add("WIFI");
        if (Regex.IsMatch(lower, @"(ổ cắm|ổ điện|o cam|o dien|cắm sạc|sạc pin|power)"))
            amenities.Add("POWER_OUTLET");
        if (Regex.IsMatch(lower, @"(máy lạnh|điều hòa|may lanh|ac|air condition)"))
            amenities.Add("AIR_CONDITIONING");
        if (Regex.IsMatch(lower, @"(chỗ để xe|gửi xe|giữ xe|xe máy|parking)"))
        {
            amenities.Add("PARKING");
            amenities.Add("MOTORBIKE_PARKING");
        }
        if (Regex.IsMatch(lower, @"(ô tô|xe hơi|car parking)"))
            amenities.Add("CAR_PARKING");
        if (Regex.IsMatch(lower, @"(nhóm đông|bàn lớn|nhiều chỗ ngồi|group seating)"))
            amenities.Add("GROUP_SEATING");

        // 5. Group Size Inference
        int? groupSize = null;
        var groupMatch = Regex.Match(lower, @"(\d+)\s*(người|bạn|thành viên|mem|pax|đứa)");
        if (groupMatch.Success && int.TryParse(groupMatch.Groups[1].Value, out var parsedSize))
        {
            groupSize = parsedSize;
            if (groupSize >= 4 && !amenities.Contains("GROUP_SEATING"))
            {
                amenities.Add("GROUP_SEATING");
            }
        }
        else if (Regex.IsMatch(lower, @"(1 mình|một mình|cá nhân)"))
        {
            groupSize = 1;
        }

        // 6. Budget Inference
        decimal? budgetMin = null;
        decimal? budgetMax = null;

        // Match "dưới 50k", "< 70k", "tối đa 60.000", "dưới 50 ngàn"
        var maxBudgetMatch = Regex.Match(lower, @"(dưới|tối đa|<|không quá|tầm|khoảng)\s*(\d+[\.,]?\d*)\s*(k|nghìn|ngàn|vnd|đ)?");
        if (maxBudgetMatch.Success)
        {
            var numStr = maxBudgetMatch.Groups[2].Value.Replace(".", "").Replace(",", ".");
            if (decimal.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var num))
            {
                var unit = maxBudgetMatch.Groups[3].Value;
                if (unit == "k" || unit == "nghìn" || unit == "ngàn" || num < 1000)
                {
                    budgetMax = num * 1000m;
                }
                else
                {
                    budgetMax = num;
                }
            }
        }
        else if (Regex.IsMatch(lower, @"(giá rẻ|sinh viên|hạt dẻ|bình dân)"))
        {
            budgetMax = 45000m; // Default student budget threshold
        }

        // 7. Distance Inference
        double? maxDistanceKm = null;
        var distMatch = Regex.Match(lower, @"(dưới|<|gần|trong vòng)\s*(\d+[\.,]?\d*)\s*(km|cây|kilomet)");
        if (distMatch.Success)
        {
            var numStr = distMatch.Groups[2].Value.Replace(",", ".");
            if (double.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            {
                maxDistanceKm = d;
            }
        }
        else if (Regex.IsMatch(lower, @"(gần đây|gần trường|quanh đây|đi bộ)"))
        {
            maxDistanceKm = 2.0; // 2km radius
        }

        return new ParsedSearchCriteria
        {
            RawPrompt = prompt,
            InferredCategoryCode = categoryCode,
            InferredPurpose = purpose,
            InferredAtmosphereTags = atmospheres,
            InferredAmenityCodes = amenities,
            InferredBudgetMin = budgetMin,
            InferredBudgetMax = budgetMax,
            InferredMaxDistanceKm = maxDistanceKm,
            InferredGroupSize = groupSize,
            ExtractedByAi = false,
            SummaryExplanation = "Phân tích tự động bằng bộ NLP từ khóa tiếng Việt thông minh"
        };
    }

}
