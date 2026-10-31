using System.Net;
using System.Text;
using System.Text.Json;
using BLL.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

internal static class AiFallbackChecks
{
    private static readonly string CriteriaJson = JsonSerializer.Serialize(new
    {
        inferredCategoryCode = (string?)null,
        inferredPurpose = (string?)null,
        inferredAtmosphereTags = Array.Empty<string>(),
        inferredAmenityCodes = new[] { "POWER_OUTLET" },
        inferredBudgetMin = (decimal?)null,
        inferredBudgetMax = (decimal?)50000,
        inferredMaxDistanceKm = (double?)null,
        inferredGroupSize = (int?)null,
        summaryExplanation = "Cần ổ điện, giá tối đa 50k"
    });

    public static async Task RunAsync()
    {
        const string firstKey = "AIza-test-key-first-0001";
        const string secondKey = "AQ.test-key-second-0002";
        var calls = new List<string>();
        var handler = new StubHandler(request =>
        {
            if (request.RequestUri!.Host == "api.openai.com")
            {
                calls.Add("OpenAI");
                return new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            }

            var key = request.Headers.GetValues("x-goog-api-key").Single();
            calls.Add(key);
            return key == firstKey
                ? new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                : JsonResponse(new { candidates = new[] { new { content = new { parts = new[] { new { text = CriteriaJson } } } } } });
        });
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["OpenAI:ApiKey"] = "test-openai-key",
            ["GeminiApiKeys:0"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(firstKey)),
            ["AI:GeminiApiKeys:0"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(firstKey)),
            ["GeminiApiKeys:1"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(secondKey))
        }).Build();
        var service = new OpenAiPromptAnalysisService(new StubClientFactory(handler), config,
            NullLogger<OpenAiPromptAnalysisService>.Instance);
        var parsed = await service.AnalyzePromptAsync("quán có ổ điện giá khoảng 50k");
        if (!parsed.ExtractedByAi || parsed.InferredBudgetMax != 50000 ||
            !parsed.InferredAmenityCodes.Contains("POWER_OUTLET") ||
            !calls.SequenceEqual(new[] { "OpenAI", firstKey, secondKey }))
            throw new Exception("OpenAI-to-Gemini fallback or Gemini key rotation failed.");

        var fallbackHandler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        var fallbackConfig = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Gemini:ApiKeys:0"] = firstKey,
            ["Gemini:ApiKeys:1"] = secondKey
        }).Build();
        var fallbackService = new OpenAiPromptAnalysisService(new StubClientFactory(fallbackHandler), fallbackConfig,
            NullLogger<OpenAiPromptAnalysisService>.Instance);
        var fallback = await fallbackService.AnalyzePromptAsync("quán có ổ điện, máy lạnh, giá khoảng 50k");
        if (fallback.ExtractedByAi || !fallback.InferredAmenityCodes.Contains("POWER_OUTLET") ||
            fallback.InferredBudgetMax != 50000 || fallbackHandler.CallCount != 2)
            throw new Exception("Local parser fallback after all Gemini keys failed.");

        var geminiCalls = 0;
        var openAiHandler = new StubHandler(request =>
        {
            if (request.RequestUri!.Host != "api.openai.com") geminiCalls++;
            return JsonResponse(new
            {
                status = "completed",
                output = new[] { new { type = "message", content = new[] { new { type = "output_text", text = CriteriaJson } } } }
            });
        });
        var openAiService = new OpenAiPromptAnalysisService(new StubClientFactory(openAiHandler), config,
            NullLogger<OpenAiPromptAnalysisService>.Instance);
        if (!(await openAiService.AnalyzePromptAsync("quán có ổ điện")).ExtractedByAi || geminiCalls != 0)
            throw new Exception("OpenAI should be used before Gemini when it succeeds.");
    }

    private static HttpResponseMessage JsonResponse(object body) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
    };

    private sealed class StubClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(respond(request));
        }
    }
}
