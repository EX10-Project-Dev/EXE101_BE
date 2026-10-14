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
        options.UseSqlServer(connectionString);
    }
});

// 3. Add Identity
builder.Services.AddDataProtection();
builder.Services.AddIdentityCore<AppUser>(opt =>
{
    opt.Password.RequireNonAlphanumeric = false;
    opt.User.RequireUniqueEmail = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

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
builder.Services.AddScoped<IPromptAnalysisService, PromptAnalysisService>();
builder.Services.AddScoped<IVenueSearchService, VenueSearchService>();

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
});

var app = builder.Build();

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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EXE101 API v1");
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();

app.Run();
