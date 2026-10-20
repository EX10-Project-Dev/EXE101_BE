using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using API.Controllers;
using API.Data;
using API.Security;
using API.Services;
using DAL.Constants;
using DAL.Data;
using DAL.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

// Real HTTP/JWT/Identity checks against an isolated in-memory database and a recording email sender.
// This host never loads the API's appsettings or connects to the project's SQL Server/Mailjet accounts.
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    EnvironmentName = "Testing", ContentRootPath = AppContext.BaseDirectory
});
builder.Configuration.Sources.Clear();
builder.Configuration.AddInMemoryCollection();
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Configuration["Jwt:SigningKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
builder.Configuration["ClientApp:BaseUrl"] = "https://frontend.example.test";
builder.Configuration["Api:PublicBaseUrl"] = "https://api.example.test";
builder.Configuration["RoleSetup:AdminEmail"] = "admin@example.test";
builder.Configuration["RoleSetup:AdminEmails:0"] = "backup@example.test";
builder.Configuration["RoleSetup:AdminEmails:1"] = "pending@example.test";
builder.Configuration["RoleSetup:AdminEmails:2"] = "missing@example.test";
await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
builder.Services.AddIdentityCore<AppUser>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = true;
}).AddRoles<IdentityRole>().AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();
builder.Services.Configure<DataProtectionTokenProviderOptions>(o => o.TokenLifespan = TimeSpan.FromHours(3));
builder.Services.AddFindGoAuthentication(builder.Configuration);
builder.Services.AddScoped<AccountRoleService>();
builder.Services.AddScoped<AccountAdministrationService>();
var mail = new RecordingEmail();
builder.Services.AddSingleton<IAppEmailSender>(mail);
builder.Services.AddControllers().AddApplicationPart(typeof(AuthController).Assembly);
await using var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

const string password = "CheckPass123!";
string adminId;
Guid venueId, otherVenueId;
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var admin = new AppUser { UserName = "admin@example.test", Email = "admin@example.test", EmailConfirmed = true };
    Check((await users.CreateAsync(admin, password)).Succeeded, "create test admin");
    adminId = admin.Id;
    var backup = new AppUser { UserName = "backup@example.test", Email = "backup@example.test", EmailConfirmed = true };
    Check((await users.CreateAsync(backup, password)).Succeeded, "create second admin");
    var pending = new AppUser { UserName = "pending@example.test", Email = "pending@example.test", EmailConfirmed = false };
    Check((await users.CreateAsync(pending, password)).Succeeded, "create unverified configured account");
    var legacy = new AppUser { UserName = "legacy@example.test", Email = "legacy@example.test", EmailConfirmed = true };
    Check((await users.CreateAsync(legacy, password)).Succeeded, "create legacy account");
    var category = new VenueCategory { Name = "Cafe", Code = "CAFE" };
    var venue = new Venue { Name = "Own cafe", Address = "Test address", Category = category, Status = VenueStatus.Published };
    var otherVenue = new Venue { Name = "Other cafe", Address = "Other address", Category = category, Status = VenueStatus.Published };
    venueId = venue.Id;
    otherVenueId = otherVenue.Id;
    db.AddRange(venue, otherVenue);
    await db.SaveChangesAsync();
}
await IdentityRoleInitializer.InitializeAsync(app.Services, builder.Configuration);
await IdentityRoleInitializer.InitializeAsync(app.Services, builder.Configuration); // Idempotent startup.
await app.StartAsync();
using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };

try
{
    var adminToken = await Login("admin@example.test", AppRoles.Admin);
    await Login("backup@example.test", AppRoles.Admin);
    await using (var scope = app.Services.CreateAsyncScope())
    {
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        Check(!await users.IsInRoleAsync((await users.FindByEmailAsync("pending@example.test"))!, AppRoles.Admin), "unverified email must not bootstrap admin");
        Check(await users.FindByEmailAsync("missing@example.test") is null, "bootstrap must not create missing accounts");
    }
    var legacyToken = await Login("legacy@example.test", AppRoles.Customer);
    await Expect(HttpMethod.Get, "/api/v1/admin/users", null, null, HttpStatusCode.Unauthorized);
    await Expect(HttpMethod.Get, "/api/v1/admin/users", null, legacyToken, HttpStatusCode.Forbidden);
    await Expect(HttpMethod.Get, "/api/v1/merchant/venues", null, legacyToken, HttpStatusCode.Forbidden);
    Pass("Startup roles, verified admin bootstrap, backfill, anonymous 401 and customer 403");

    await Expect(HttpMethod.Post, "/api/v1/auth/register", new
    {
        fullName = "New customer", email = "new@example.test", password, role = "admin"
    }, null, HttpStatusCode.Created);
    await Expect(HttpMethod.Post, "/api/v1/auth/login", new { email = "new@example.test", password }, null, HttpStatusCode.Unauthorized);
    var confirmation = ExtractLink(mail.LastHtml);
    await Expect(HttpMethod.Get, confirmation.PathAndQuery, null, null, HttpStatusCode.OK);
    var customerToken = await Login("new@example.test", AppRoles.Customer);
    var me = await Expect(HttpMethod.Get, "/api/v1/auth/me", null, customerToken, HttpStatusCode.OK);
    var customerId = (await Json(me))["userId"]!.GetValue<string>();
    Pass("Public registration cannot select admin; confirmation and customer login work");

    await Expect(HttpMethod.Post, "/api/v1/admin/merchants", new
    {
        fullName = "Wrong owner", email = "wrong@example.test", venueIds = new[] { Guid.NewGuid() }
    }, adminToken, HttpStatusCode.NotFound);
    mail.Fail = true;
    await Expect(HttpMethod.Post, "/api/v1/admin/merchants", new
    {
        fullName = "Merchant", email = "merchant@example.test", venueIds = new[] { venueId }
    }, adminToken, HttpStatusCode.ServiceUnavailable);
    string merchantId;
    await using (var scope = app.Services.CreateAsyncScope())
    {
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var merchant = await users.FindByEmailAsync("merchant@example.test");
        Check(merchant is not null && !merchant.EmailConfirmed, "retain pending invitation after provider failure");
        merchantId = merchant!.Id;
        Check((await scope.ServiceProvider.GetRequiredService<AccountRoleService>()
            .EnsureCustomerWhenUnassignedAsync(merchant)).SequenceEqual([AppRoles.Merchant]), "preserve merchant role");
        Check(await users.FindByEmailAsync("wrong@example.test") is null, "invalid venue must not create account");
    }
    mail.Fail = false;
    await Expect(HttpMethod.Post, $"/api/v1/admin/merchants/{merchantId}/resend-invitation", null, adminToken, HttpStatusCode.OK);
    var query = QueryHelpers.ParseQuery(ExtractLink(mail.LastHtml).Query);
    var activation = new { userId = merchantId, token = query["token"].ToString(), password };
    await Expect(HttpMethod.Post, "/api/v1/auth/activate-merchant", activation with { token = "broken" }, null, HttpStatusCode.BadRequest);
    await Expect(HttpMethod.Post, "/api/v1/auth/activate-merchant", activation, null, HttpStatusCode.OK);
    await Expect(HttpMethod.Post, "/api/v1/auth/activate-merchant", activation, null, HttpStatusCode.BadRequest);
    var merchantToken = await Login("merchant@example.test", AppRoles.Merchant);
    Pass("Merchant provisioning, email failure recovery, activation and one-use invitation");

    await Expect(HttpMethod.Get, "/api/v1/admin/users", null, merchantToken, HttpStatusCode.Forbidden);
    var listing = await Json(await Expect(HttpMethod.Get, "/api/v1/merchant/venues", null, merchantToken, HttpStatusCode.OK));
    Check(listing.AsArray().Count == 1, "merchant can only list own venue");
    var edit = new { name = "Updated cafe", address = "New address", priceMin = 20000, priceMax = 50000,
        merchantUserId = customerId, status = "Published" };
    await Expect(HttpMethod.Put, $"/api/v1/merchant/venues/{otherVenueId}", edit, merchantToken, HttpStatusCode.NotFound);
    await Expect(HttpMethod.Put, $"/api/v1/merchant/venues/{venueId}", edit, customerToken, HttpStatusCode.Forbidden);
    await Expect(HttpMethod.Put, $"/api/v1/merchant/venues/{venueId}", edit, merchantToken, HttpStatusCode.OK);
    await Expect(HttpMethod.Put, $"/api/v1/merchant/venues/{venueId}", edit with { priceMax = 100 }, merchantToken, HttpStatusCode.BadRequest);
    var seedPath = Path.Combine(AppContext.BaseDirectory, "Data", "venue-seed.json");
    var firstSourceId = JsonNode.Parse(await File.ReadAllTextAsync(seedPath))![0]!["sourceId"]!.GetValue<string>();
    await using (var scope = app.Services.CreateAsyncScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Venues.Where(v => v.Id == venueId).ExecuteUpdateAsync(s => s.SetProperty(v => v.SourceId, firstSourceId));
    }
    await VenueCatalogSeeder.SeedAsync(app.Services, seedPath);
    await using (var scope = app.Services.CreateAsyncScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var owned = await db.Venues.SingleAsync(v => v.Id == venueId);
        Check(owned.Name == edit.name && owned.MerchantUserId == merchantId, "seed must preserve merchant edits and ownership");
    }
    await Expect(HttpMethod.Put, $"/api/v1/admin/merchants/{merchantId}/venues", new { venueIds = new[] { otherVenueId } }, adminToken, HttpStatusCode.OK);
    await Expect(HttpMethod.Put, $"/api/v1/merchant/venues/{venueId}", edit, merchantToken, HttpStatusCode.NotFound);
    Pass("Ownership checks, reassignment, seed preservation, role restrictions and input validation");

    await Expect(HttpMethod.Put, $"/api/v1/admin/users/{adminId}/role", new { role = "customer" }, adminToken, HttpStatusCode.Conflict);
    await Expect(HttpMethod.Put, $"/api/v1/admin/users/{customerId}/role", new { role = "owner" }, adminToken, HttpStatusCode.BadRequest);
    await Expect(HttpMethod.Put, $"/api/v1/admin/users/{customerId}/role", new { role = "admin" }, customerToken, HttpStatusCode.Forbidden);
    await Expect(HttpMethod.Put, $"/api/v1/admin/users/{customerId}/role", new { role = "admin" }, adminToken, HttpStatusCode.OK);
    await Expect(HttpMethod.Get, "/api/v1/auth/me", null, customerToken, HttpStatusCode.Unauthorized);
    var promotedToken = await Login("new@example.test", AppRoles.Admin);
    await Expect(HttpMethod.Put, $"/api/v1/admin/users/{merchantId}/role", new { role = "customer" }, adminToken, HttpStatusCode.OK);
    await Expect(HttpMethod.Get, "/api/v1/merchant/venues", null, merchantToken, HttpStatusCode.Unauthorized);
    await Login("merchant@example.test", AppRoles.Customer);
    Pass("Admin-only role changes, self-demotion prevention and old-token revocation");

    // Simulate a role change made directly in the DB without updating the security stamp.
    await using (var scope = app.Services.CreateAsyncScope())
    {
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = (await users.FindByIdAsync(customerId))!;
        Check((await users.RemoveFromRoleAsync(user, AppRoles.Admin)).Succeeded, "direct role removal");
        Check((await users.AddToRoleAsync(user, AppRoles.Customer)).Succeeded, "direct role assignment");
    }
    await Expect(HttpMethod.Get, "/api/v1/admin/users", null, promotedToken, HttpStatusCode.Unauthorized);
    await Expect(HttpMethod.Post, "/api/v1/auth/logout", null, legacyToken, HttpStatusCode.OK);
    await Expect(HttpMethod.Get, "/api/v1/auth/me", null, legacyToken, HttpStatusCode.Unauthorized);
    Pass("Direct-DB role changes and logout invalidate prior tokens");
    Console.WriteLine("All role-flow checks passed. No production database or email provider was used.");
}
finally { await app.StopAsync(); }

async Task<string> Login(string email, string role)
{
    var json = await Json(await Expect(HttpMethod.Post, "/api/v1/auth/login", new { email, password }, null, HttpStatusCode.OK));
    Check(json["user"]!["roles"]!.AsArray().Select(n => n!.GetValue<string>()).SequenceEqual([role]), "login role");
    return json["accessToken"]!.GetValue<string>();
}

async Task<HttpResponseMessage> Expect(HttpMethod method, string path, object? body, string? token, HttpStatusCode expected)
{
    using var request = new HttpRequestMessage(method, path);
    if (body is not null) request.Content = JsonContent.Create(body);
    if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    var response = await client.SendAsync(request);
    Check(response.StatusCode == expected, $"{method} {path.Split('?')[0]}: expected {(int)expected}, got {(int)response.StatusCode}");
    return response;
}

static async Task<JsonNode> Json(HttpResponseMessage response) => JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
static Uri ExtractLink(string html) => new(WebUtility.HtmlDecode(Regex.Match(html, "href=\"([^\"]+)\"").Groups[1].Value));
static void Check(bool condition, string message) { if (!condition) throw new Exception("FAILED: " + message); }
static void Pass(string message) => Console.WriteLine("PASS: " + message);

sealed class RecordingEmail : IAppEmailSender
{
    public bool IsConfigured => true;
    public bool Fail { get; set; }
    public string LastHtml { get; private set; } = string.Empty;
    public Task SendAsync(string recipient, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (Fail) throw new HttpRequestException("Simulated provider failure.");
        LastHtml = htmlBody;
        return Task.CompletedTask;
    }
}
