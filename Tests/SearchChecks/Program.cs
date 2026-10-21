using System.Text.Json;
using API.Data;
using BLL.DTOs.Common;
using BLL.DTOs.Requests;
using BLL.Interfaces;
using BLL.Services;
using DAL.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();

var services = new ServiceCollection();
services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
services.AddLogging();
services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?> { ["Search:IncludePendingVenues"] = "true" })
    .Build());
services.AddScoped<IPromptAnalysisService, LocalPromptAnalysisService>();
services.AddScoped<IVenueSearchService, VenueSearchService>();
await using var provider = services.BuildServiceProvider();

await using (var scope = provider.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();
}

var seedPath = Path.Combine(AppContext.BaseDirectory, "Data", "venue-seed.json");
await VenueCatalogSeeder.SeedAsync(provider, seedPath);

async Task<BLL.DTOs.Responses.VenueSearchResponse> SearchResponse(string prompt, params string[] categoryCodes)
{
    await using var scope = provider.CreateAsyncScope();
    return await scope.ServiceProvider.GetRequiredService<IVenueSearchService>()
        .SearchAsync(new VenueSearchRequest { Prompt = prompt, CategoryCodes = categoryCodes, PageSize = 100 });
}

async Task<string[]> Search(string prompt, params string[] categoryCodes) =>
    (await SearchResponse(prompt, categoryCodes)).Results.Select(item => item.Venue.Name).ToArray();

static void Contains(string[] names, string expected, string query)
{
    if (!names.Contains(expected, StringComparer.Ordinal))
        throw new Exception($"Search '{query}' did not return '{expected}'. Results: {string.Join(", ", names)}");
}

static void Excludes(string[] names, string unexpected, string query)
{
    if (names.Contains(unexpected, StringComparer.Ordinal))
        throw new Exception($"Search '{query}' incorrectly returned '{unexpected}'.");
}

var rice = await Search("cơm");
Contains(rice, "Cơm Tấm Làng", "cơm");
Contains(rice, "Cơm Niêu Bà Chang", "cơm");
Contains(rice, "Vị Nhà - Tinh hoa ẩm thực 3 miền", "cơm");
Excludes(rice, "Phở Tình Tây", "cơm");

var beefHotpot = await Search("lẩu bò");
Contains(beefHotpot, "Chow Long - Tiệm Lẩu Bò Triều Châu", "lẩu bò");
Excludes(beefHotpot, "DaHú hotpot Vincom Mega Mall", "lẩu bò");
Excludes(beefHotpot, "Mala Panda Thủ Đức", "lẩu bò");
Contains(await Search("tìm quán lẩu bò gần đây"), "Chow Long - Tiệm Lẩu Bò Triều Châu", "tìm quán lẩu bò gần đây");

Contains(await Search("phở"), "Phở Tình Tây", "phở");
Contains(await Search("bánh mì"), "Root's (Cà phê - Bánh mì - Trái cây)", "bánh mì");
Contains(await Search("kem"), "Kem Dẻo Tràng Tiền", "kem");
Contains(await Search("trà sữa"), "Trà sữa TRẠM - Milk Tea & Coffee", "trà sữa");
Contains(await Search("hải sản"), "Hải Sản Út Linh 2", "hải sản");
Excludes(await Search("cơm", "CAFE"), "Cơm Tấm Làng", "cơm + explicit CAFE filter");

var longPrompt = "Tìm một quán cơm có ổ điện, máy lạnh, giá tầm 50k gần đây";
var longPromptCriteria = PromptAnalysisService.AnalyzeWithRuleBasedNlp(longPrompt);
if (!longPromptCriteria.InferredAmenityCodes.Contains("POWER_OUTLET") ||
    !longPromptCriteria.InferredAmenityCodes.Contains("AIR_CONDITIONING") ||
    longPromptCriteria.InferredBudgetMax != 50000m)
    throw new Exception("The long prompt did not extract outlet, air conditioning and 50k budget.");
var longPromptResult = await SearchResponse(longPrompt);
Contains(longPromptResult.Results.Select(item => item.Venue.Name).ToArray(), "Cơm Tấm Làng", longPrompt);
if (longPromptResult.Results.Any(item =>
        !item.Venue.AmenityCodes.Contains("POWER_OUTLET") ||
        !item.Venue.AmenityCodes.Contains("AIR_CONDITIONING") ||
        item.Venue.PriceMin > 50000m))
    throw new Exception("The long prompt returned a venue outside its amenity or price filters.");
if (!longPromptResult.Warnings.Any(warning => warning.Contains("Chưa có vị trí", StringComparison.Ordinal)))
    throw new Exception("The missing location warning was not returned.");

var amenityPrompt = "Tôi cần một quán có ổ điện, máy lạnh, giá khoảng 50k";
var amenityResult = await SearchResponse(amenityPrompt);
if (amenityResult.TotalCount == 0 || amenityResult.Results.Any(item =>
        !item.Venue.AmenityCodes.Contains("POWER_OUTLET") ||
        !item.Venue.AmenityCodes.Contains("AIR_CONDITIONING") ||
        item.Venue.PriceMin > 50000m))
    throw new Exception("The amenity and budget prompt returned no valid results.");

using var seed = JsonDocument.Parse(await File.ReadAllTextAsync(seedPath));
var exactNames = seed.RootElement.EnumerateArray()
    .Select(record => record.GetProperty("name").GetString()!)
    .ToArray();
foreach (var name in exactNames)
    Contains(await Search(name), name, name);

Console.WriteLine($"Search checks passed: dish/category cases and all {exactNames.Length} venue names.");

sealed class LocalPromptAnalysisService : IPromptAnalysisService
{
    public Task<ParsedSearchCriteria> AnalyzePromptAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var parsed = PromptAnalysisService.AnalyzeWithRuleBasedNlp(prompt);
        if (prompt is "cơm" or "lẩu bò")
        {
            // Simulate a model assigning a narrow category to a cross-category food term.
            parsed = new ParsedSearchCriteria
            {
                RawPrompt = prompt,
                InferredCategoryCode = prompt == "cơm" ? "VIETNAMESE_RESTAURANT" : "MALA_HOTPOT_FAST_FOOD"
            };
        }

        return Task.FromResult(parsed);
    }
}
