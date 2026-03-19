using FataleCore.Data;
using FataleCore.Models;
using FataleCore.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// 1. Logging & Config
Console.WriteLine($"[STARTUP] Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"[STARTUP] PORT (env): {Environment.GetEnvironmentVariable("PORT") ?? "N/A"}");

// 1.1 DB Configuration
// In production (Railway), DB lives on the persistent volume at /app/data.
// Locally it falls back to fatale_core.db in the working directory.
var dbPath = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(dbPath))
{
    dbPath = Environment.GetEnvironmentVariable("DATABASE_PATH")
             ?? (builder.Environment.IsProduction()
                    ? "Data Source=/app/data/fatale_core.db"
                    : "Data Source=fatale_core.db");
}
Console.WriteLine($"[STARTUP] Database Path: {dbPath}");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(dbPath));

// 1.5 Service Registration
builder.Services.AddScoped<FataleCore.Services.ISubscriptionService, FataleCore.Services.SubscriptionService>();
builder.Services.AddScoped<FataleCore.Services.Intelligence.IIntelligenceService, FataleCore.Services.Intelligence.IntelligenceService>();
builder.Services.AddHostedService<FataleCore.Services.Intelligence.MeditationWorkerService>();

// 2. CORS Configuration (Explicit)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy
            .SetIsOriginAllowed(_ => true) // Echoes the origin, works better with AllowCredentials
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()); // Required for SignalR
});

// 3. SignalR Registration
builder.Services.AddSignalR();

// 4. JWT Authentication Configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                string.IsNullOrWhiteSpace(builder.Configuration.GetSection("AppSettings:Token").Value)
                    ? "super secret key that is long enough to be secure 1234567890"
                    : builder.Configuration.GetSection("AppSettings:Token").Value!)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/radio"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // Output: PascalCase
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true; // Input: Allow camelCase
    });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ASEGURAR BASE DE DATOS
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    Console.WriteLine("[DATABASE] Running migrations...");

    // 1. Run Standard Migrations (Core Schema Source of Truth)
    try {
        db.Database.Migrate();
        Console.WriteLine("[DATABASE] OK: Migrations applied successfully.");
    } catch (Exception ex) {
        Console.WriteLine("[DATABASE] MIGRATION_NOTICE: " + ex.Message);
        
        // Final fallback: try to ensure system data even if migration had a minor conflict
    }

    // 2. Ensure System Data
    try {
        var systemArtist = db.Artists.FirstOrDefault(a => a.Name == "The Archive");
        if (systemArtist == null)
        {
            systemArtist = new Artist { Name = "The Archive", Bio = "System content aggregator.", ImageUrl = "" };
            db.Artists.Add(systemArtist);
            db.SaveChanges();
            Console.WriteLine("[DATABASE] OK: Created system-level service artist: 'The Archive'.");
        }

        var systemAlbum = db.Albums.FirstOrDefault(a => a.Title == "YouTube Signals");
        if (systemAlbum == null)
        {
            systemAlbum = new Album { 
                Title = "YouTube Signals", 
                ArtistId = systemArtist.Id, 
                ReleaseDate = DateTime.UtcNow, 
                CoverImageUrl = "" 
            };
            db.Albums.Add(systemAlbum);
            db.SaveChanges();
            Console.WriteLine("[DATABASE] OK: Created system-level archive album: 'YouTube Signals'.");
        }
    } catch (Exception ex) {
        Console.WriteLine("[DATABASE] ERROR in system data: " + ex.Message);
    }
}

// Configure the HTTP request pipeline.
// Always show Swagger — useful for the shared dev server.
app.UseSwagger();
app.UseSwaggerUI();

// Railway handles TLS termination at the proxy level, so HTTPS redirection
// is intentionally removed to avoid redirect loops inside the container.
// app.UseHttpsRedirection();

// IMPORTANT: Use CORS before Auth
app.UseCors("AllowAll");

// In production (Docker/Railway), the app runs from /app so we use that as the base.
// In development, Directory.GetCurrentDirectory() points to the project root.
var appBase = app.Environment.IsProduction() ? "/app" : Directory.GetCurrentDirectory();

var uploadsPath = Path.Combine(appBase, "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

var cachePath = Path.Combine(appBase, "Cache");
if (!Directory.Exists(cachePath))
{
    Directory.CreateDirectory(cachePath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(cachePath),
    RequestPath = "/cache"
});

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "FATALE_CORE_ONLINE_V1_MIGRATED");
app.MapGet("/api/ping", () => "PONG_VERSION_1");
app.MapControllers();
app.MapHub<RadioHub>("/hubs/radio");

Console.WriteLine("[STARTUP] Application configured. Starting host...");
app.Run();
