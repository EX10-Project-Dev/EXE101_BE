using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BLL.DTOs.Common;
using BLL.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BLL.Services;

/// <summary>
/// Hybrid prompt analysis service: extracts structured search criteria from Vietnamese natural language prompts
/// using either an external LLM (Gemini/OpenAI) or a high-performance offline Rule-based NLP intent parser.
/// </summary>
public sealed class PromptAnalysisService : IPromptAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PromptAnalysisService> _logger;

    public PromptAnalysisService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<PromptAnalysisService> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _configuration = configuration;
        _logger = logger;
    }

    private static readonly string[] CandidateModels = new[]
    {
        "gemini-2.5-flash",
        "gemini-1.5-flash",
        "gemini-flash-latest",
        "gemini-2.5-pro",
        "gemini-3.8-flash",
        "gemini-3.5-flash"
    };

    public async Task<ParsedSearchCriteria> AnalyzePromptAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return new ParsedSearchCriteria();
        }

        var trimmedPrompt = prompt.Trim();
        var validKeys = GetConfiguredApiKeys();

        // 1. Try Gemini with multi-key rotation and candidate models fallback (similar to LoverApp)
        if (validKeys.Count > 0)
        {
            try
            {
                var llmResult = await TryAnalyzeWithGeminiRotationAsync(trimmedPrompt, validKeys, cancellationToken);
                if (llmResult is not null)
                {
                    return llmResult;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini prompt extraction failed across all candidate keys and models. Falling back to rule-based NLP intent parser.");
            }
        }

        // 2. High-performance Rule-based & Regex NLP parser (Offline Ground-Truth)
        return AnalyzeWithRuleBasedNlp(trimmedPrompt);
    }

    private List<string> GetConfiguredApiKeys()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);

        // Check array sections
        var array1 = _configuration.GetSection("GeminiApiKeys").Get<string[]>();
        if (array1 is not null)
        {
            foreach (var k in array1)
            {
                if (IsValidKey(k)) keys.Add(k!.Trim());
            }
        }

        var array2 = _configuration.GetSection("AI:GeminiApiKeys").Get<string[]>();
        if (array2 is not null)
        {
            foreach (var k in array2)
            {
                if (IsValidKey(k)) keys.Add(k!.Trim());
            }
        }

        // Check single key configs
        var singleKeys = new[]
        {
            _configuration["GeminiApiKey"],
            _configuration["AI:GeminiApiKey"],
            _configuration["GEMINI_API_KEY"]
        };

        foreach (var k in singleKeys)
        {
            if (IsValidKey(k)) keys.Add(k!.Trim());
        }

        return keys.ToList();
    }

    private static bool IsValidKey(string? key)
    {
        return !string.IsNullOrWhiteSpace(key)
            && !key.StartsWith("YOUR_")
            && !key.StartsWith("API_KEY_THU_")
            && !key.Contains("DÁN_KEY");
    }

    private async Task<ParsedSearchCriteria?> TryAnalyzeWithGeminiRotationAsync(
        string prompt,
        List<string> validKeys,
        CancellationToken cancellationToken)
    {
        var random = new Random();

        var systemPrompt = @"You are a Vietnamese NLU assistant for the Find&Go venue recommendation app.
Analyze the user's natural language request into a strictly structured JSON object.
Allowed categories: CAFE, RESTAURANT, EATERY, BAKERY_DESSERT, TEA_DRINKS, TEA_COFFEE, VIETNAMESE_RESTAURANT, ICE_CREAM, MALA_HOTPOT_FAST_FOOD, OTHER.
Allowed purposes: STUDY, WORK, DATING, GATHERING, DINING.
Allowed atmospheres: QUIET, COZY, LIVELY, CHILL, OUTDOOR, ROOFTOP, MODERN.
Allowed amenity codes: WIFI, POWER_OUTLET, AIR_CONDITIONING, PARKING, MOTORBIKE_PARKING, CAR_PARKING, GROUP_SEATING.

Respond ONLY with valid JSON in this schema:
{
  ""inferredCategoryCode"": string or null,
  ""inferredPurpose"": string or null,
  ""inferredAtmosphereTags"": [string],
  ""inferredAmenityCodes"": [string],
  ""inferredBudgetMin"": number or null,
  ""inferredBudgetMax"": number or null,
  ""inferredMaxDistanceKm"": number or null,
  ""inferredGroupSize"": number or null,
  ""summaryExplanation"": string
}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = $"{systemPrompt}\n\nUser Prompt: \"{prompt}\"" }
                    }
                }
            },
            generationConfig = new
            {
                response_mime_type = "application/json"
            }
        };

        foreach (var modelName in CandidateModels)
        {
            // Shuffle keys to distribute quota and rotate between keys
            var shuffledKeys = validKeys.OrderBy(_ => random.Next()).ToList();

            foreach (var key in shuffledKeys)
            {
                var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={key}";

                try
                {
                    var response = await _httpClient.PostAsJsonAsync(endpoint, requestBody, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        var jsonDoc = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken);
                        if (jsonDoc is null) continue;

                        if (!jsonDoc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                            continue;

                        var textContent = candidates[0]
                            .GetProperty("content")
                            .GetProperty("parts")[0]
                            .GetProperty("text")
                            .GetString();

                        if (string.IsNullOrWhiteSpace(textContent)) continue;

                        textContent = textContent.Replace("```json", "").Replace("```", "").Trim();

                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var rawResult = JsonSerializer.Deserialize<GeminiParsedResponse>(textContent, options);
                        if (rawResult is null) continue;

                        _logger.LogInformation("Gemini prompt extraction succeeded using model {Model}", modelName);

                        return new ParsedSearchCriteria
                        {
                            RawPrompt = prompt,
                            InferredCategoryCode = rawResult.InferredCategoryCode,
                            InferredPurpose = rawResult.InferredPurpose,
                            InferredAtmosphereTags = rawResult.InferredAtmosphereTags ?? [],
                            InferredAmenityCodes = rawResult.InferredAmenityCodes ?? [],
                            InferredBudgetMin = rawResult.InferredBudgetMin,
                            InferredBudgetMax = rawResult.InferredBudgetMax,
                            InferredMaxDistanceKm = rawResult.InferredMaxDistanceKm,
                            InferredGroupSize = rawResult.InferredGroupSize,
                            ExtractedByAi = true,
                            SummaryExplanation = rawResult.SummaryExplanation ?? $"Trích xuất tiêu chí bằng Gemini AI ({modelName})"
                        };
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                             response.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                             response.StatusCode == System.Net.HttpStatusCode.InternalServerError ||
                             (int)response.StatusCode == 429)
                    {
                        // 429 Rate limit / Quota exhausted or 503 Overload -> rotate to next key
                        _logger.LogWarning("Gemini API key returned {StatusCode} on model {Model}. Rotating to next API key...", response.StatusCode, modelName);
                        await Task.Delay(400, cancellationToken);
                        continue;
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        // Model not available or retired -> break key loop, try next candidate model
                        _logger.LogInformation("Model {Model} not found or retired (404). Switching to next candidate model...", modelName);
                        break;
                    }
                    else
                    {
                        var errContent = await response.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogWarning("Gemini API call failed with status {StatusCode}: {Error}. Rotating to next key...", response.StatusCode, errContent);
                        continue;
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Exception calling Gemini API with model {Model}. Trying next key...", modelName);
                    continue;
                }
            }
        }

        return null;
    }

    public static ParsedSearchCriteria AnalyzeWithRuleBasedNlp(string prompt)
    {
        var lower = prompt.ToLowerInvariant();

        // 1. Category Inference
        string? categoryCode = null;
        if (Regex.IsMatch(lower, @"(cà phê|cafe|coffee|caphe)"))
            categoryCode = "CAFE";
        else if (Regex.IsMatch(lower, @"(trà sữa|tra sua|trà trái cây|milk tea)"))
            categoryCode = "TEA_DRINKS";
        else if (Regex.IsMatch(lower, @"(lẩu|mala|hotpot)"))
            categoryCode = "MALA_HOTPOT_FAST_FOOD";
        else if (Regex.IsMatch(lower, @"(bánh|tráng miệng|dessert)"))
            categoryCode = "BAKERY_DESSERT";
        else if (Regex.IsMatch(lower, @"(kem|ice cream)"))
            categoryCode = "ICE_CREAM";
        else if (Regex.IsMatch(lower, @"(cơm|phở|bún|bánh mì|quán ăn|nhà hàng)"))
            categoryCode = "VIETNAMESE_RESTAURANT";

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
        if (Regex.IsMatch(lower, @"(ổ cắm|o cam|cắm sạc|sạc pin|power)"))
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
        var maxBudgetMatch = Regex.Match(lower, @"(dưới|tối đa|<|không quá|tầm)\s*(\d+[\.,]?\d*)\s*(k|nghìn|ngàn|vnd|đ)?");
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

    private sealed class GeminiParsedResponse
    {
        public string? InferredCategoryCode { get; set; }
        public string? InferredPurpose { get; set; }
        public List<string>? InferredAtmosphereTags { get; set; }
        public List<string>? InferredAmenityCodes { get; set; }
        public decimal? InferredBudgetMin { get; set; }
        public decimal? InferredBudgetMax { get; set; }
        public double? InferredMaxDistanceKm { get; set; }
        public int? InferredGroupSize { get; set; }
        public string? SummaryExplanation { get; set; }
    }
}
