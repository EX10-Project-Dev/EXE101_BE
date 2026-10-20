using BLL.Mappings;
using BLL.Interfaces;
using BLL.Services;
using API.Data;
using DAL.Data;
using DAL.Entities;
using DAL.IRepositories;
using DAL.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using API.Security;
using API.Services;
using API.Swagger;

var builder = WebApplication.CreateBuilder(args);

// Keep development logs in the console; writing to Windows Event Log requires elevated access.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// 1. Add Controllers
builder.Services.AddControllers();

// 2. Add Database Context (SQL Server)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (!string.IsNullOrEmpty(connectionString))
    {
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
            sqlOptions.CommandTimeout(90);
        });
    }
});

// 3. Add Identity
builder.Services.AddDataProtection();
builder.Services.AddIdentityCore<AppUser>(opt =>
{
    opt.Password.RequireNonAlphanumeric = false;
    opt.Password.RequiredLength = 8;
    opt.User.RequireUniqueEmail = true;
    opt.SignIn.RequireConfirmedEmail = true;
    opt.Lockout.MaxFailedAccessAttempts = 5;
    opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
    options.TokenLifespan = TimeSpan.FromHours(3));

builder.Services.AddFindGoAuthentication(builder.Configuration);
builder.Services.AddScoped<AccountRoleService>();
builder.Services.AddScoped<AccountAdministrationService>();

// 4. Register Dependency Injection for DAL (UnitOfWork & Generic Repository)
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// 5. Register AutoMapper
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<AutoMapperProfile>();
});

// 6. Register BLL services
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<IAppEmailSender, MailjetAppEmailSender>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddScoped<IPromptAnalysisService, PromptAnalysisService>();
builder.Services.AddScoped<IVenueSearchService, VenueSearchService>();
builder.Services.AddScoped<IVenueService, VenueService>();

// 7. Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 8. Add Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EXE101 API",
        Version = "v1"
    });
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Dán access token nhận được khi đăng nhập."
    });
    option.OperationFilter<AuthorizeOperationFilter>();
});

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await using var migrationScope = app.Services.CreateAsyncScope();
    await migrationScope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

await IdentityRoleInitializer.InitializeAsync(app.Services, app.Configuration);

if (app.Configuration.GetValue<bool>("SeedData:ImportVenueCatalogOnStartup"))
{
    await using (var schemaScope = app.Services.CreateAsyncScope())
    {
        var schemaDb = schemaScope.ServiceProvider.GetRequiredService<AppDbContext>();
        await schemaDb.Database.ExecuteSqlRawAsync("IF COL_LENGTH('dbo.Venues', 'SourceDataJson') IS NULL ALTER TABLE [dbo].[Venues] ADD [SourceDataJson] nvarchar(max) NULL; IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = '20261014143633_AddVenueSourceData') INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('20261014143633_AddVenueSourceData', '10.0.0');");
    }

    var seededVenueCount = await VenueCatalogSeeder.SeedAsync(
        app.Services,
        Path.Combine(AppContext.BaseDirectory, "Data", "venue-seed.json"));
    Console.WriteLine($"Đã nạp {seededVenueCount} địa điểm từ bộ dữ liệu mẫu.");
}

// Configure the HTTP request pipeline
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "EXE101 API v1");
    c.RoutePrefix = "swagger";
});

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();

app.Run();
