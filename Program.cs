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
    Console.WriteLine("[DATABASE] Initializing schema...");

    // 2. Ensure Schema Consistency (Manual Patching for legacy/custom tables)
    // Legacy Transactions table (if not managed by EF)
    try {
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS Transactions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId INTEGER NOT NULL,
                Type TEXT NOT NULL,
                Amount INTEGER NOT NULL,
                Description TEXT,
                Timestamp TEXT NOT NULL,
                RelatedUserId INTEGER,
                TrackId INTEGER
            );
        ");
        Console.WriteLine("[DATABASE] OK: 'Transactions' table verified.");
    } catch { }

    // Idempotent column additions for other tables (Safety measures)
    string[] patches = {
        "ALTER TABLE Tracks ADD COLUMN Source TEXT;",
        "ALTER TABLE Tracks ADD COLUMN IsPinned INTEGER DEFAULT 0;",
        "ALTER TABLE Tracks ADD COLUMN IsPosted INTEGER DEFAULT 0;",
        "ALTER TABLE Tracks ADD COLUMN CreatedAt TEXT;",
        "ALTER TABLE Artists ADD COLUMN IsLive INTEGER DEFAULT 0;",
        "ALTER TABLE Artists ADD COLUMN FeaturedTrackId INTEGER;",
        "ALTER TABLE Users ADD COLUMN Biography TEXT DEFAULT '';",
        "ALTER TABLE Users ADD COLUMN ProfilePictureUrl TEXT DEFAULT '';",
        "ALTER TABLE Users ADD COLUMN BannerUrl TEXT;",
        "ALTER TABLE Users ADD COLUMN ThemeColor TEXT DEFAULT '#ff006e';",
        "ALTER TABLE Users ADD COLUMN TextColor TEXT DEFAULT '#ffffff';",
        "ALTER TABLE Users ADD COLUMN BackgroundColor TEXT DEFAULT '#000000';",
        "ALTER TABLE Users ADD COLUMN IsGlass INTEGER DEFAULT 0;",
        "ALTER TABLE Users ADD COLUMN WallpaperVideoUrl TEXT;",
        "ALTER TABLE Playlists ADD COLUMN IsPinned INTEGER DEFAULT 0;",
        "ALTER TABLE Playlists ADD COLUMN IsPosted INTEGER DEFAULT 0;",
        "UPDATE Users SET Biography = '' WHERE Biography IS NULL;",
        "UPDATE Users SET ProfilePictureUrl = '' WHERE ProfilePictureUrl IS NULL;",
        "ALTER TABLE StudioContents ADD COLUMN IsPinned INTEGER DEFAULT 0;",
        @"CREATE TABLE IF NOT EXISTS StudioContents (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            UserId INTEGER NOT NULL,
            Title TEXT NOT NULL,
            Description TEXT,
            Url TEXT NOT NULL,
            Type TEXT NOT NULL,
            CreatedAt TEXT NOT NULL,
            IsPosted INTEGER DEFAULT 0,
            IsPinned INTEGER DEFAULT 0
        );",
        @"CREATE TABLE IF NOT EXISTS FeedInteractions (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            UserId INTEGER NOT NULL,
            ItemType TEXT NOT NULL,
            ItemId INTEGER NOT NULL,
            InteractionType TEXT NOT NULL,
            Content TEXT,
            CreatedAt TEXT NOT NULL
        );",
        @"CREATE TABLE IF NOT EXISTS UserListeningEvents (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            UserId INTEGER NOT NULL,
            TrackType TEXT NOT NULL,
            TrackId TEXT NOT NULL,
            TrackTitle TEXT NOT NULL,
            Tags TEXT,
            ListenedAt TEXT NOT NULL,
            DurationSeconds INTEGER NOT NULL,
            Source TEXT NOT NULL
        );",
        @"CREATE TABLE IF NOT EXISTS TrackFingerprints (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            TrackType TEXT NOT NULL,
            TrackId TEXT NOT NULL,
            Tags TEXT,
            ViewTier INTEGER NOT NULL,
            ChannelType TEXT NOT NULL,
            EnrichedAt TEXT NOT NULL,
            PlayCount INTEGER NOT NULL
        );",
        "ALTER TABLE Stations ADD COLUMN Description TEXT;",
        "ALTER TABLE Stations ADD COLUMN IsChatEnabled INTEGER NOT NULL DEFAULT 1;",
        "ALTER TABLE Stations ADD COLUMN IsQueueEnabled INTEGER NOT NULL DEFAULT 1;",
        @"CREATE TABLE IF NOT EXISTS CommunityMessages (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            CommunityId INTEGER NOT NULL,
            UserId INTEGER NOT NULL,
            Content TEXT NOT NULL,
            SentAt TEXT NOT NULL
        );"
    };

    foreach (var patch in patches) {
        try { db.Database.ExecuteSqlRaw(patch); } catch {  }
    }

    // Ensure no NULL values for CreatedAt (Safety for Feed sorting)
    try {
        db.Database.ExecuteSqlRaw("UPDATE Tracks SET CreatedAt = CURRENT_TIMESTAMP WHERE CreatedAt IS NULL;");
        db.Database.ExecuteSqlRaw("UPDATE StudioContents SET CreatedAt = CURRENT_TIMESTAMP WHERE CreatedAt IS NULL;");
    } catch { }


    Console.WriteLine("[DATABASE] Schema check complete.");

    // 3. Run Standard Migrations (Core Schema Source of Truth)
    try {
        db.Database.Migrate();
        Console.WriteLine("[DATABASE] OK: Migrations applied.");
    } catch (Exception ex) {
        Console.WriteLine("[DATABASE] MIGRATION_NOTICE: " + ex.Message);
    }

    // 4. Ensure System Data
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

    // 5. Cleanup
    try {
        db.Database.ExecuteSqlRaw("DELETE FROM UserLikes WHERE YoutubeTrackId IS NOT NULL OR TrackId IS NULL;");
        Console.WriteLine("[DATABASE] OK: Purged legacy signal interference from 'UserLikes'.");
    } catch { }
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

app.MapGet("/", () => "FATALE_CORE_ONLINE_V1");
app.MapGet("/api/ping", () => "PONG_VERSION_1");
app.MapControllers();
app.MapHub<RadioHub>("/hubs/radio");

Console.WriteLine("[STARTUP] Application configured. Starting host...");
app.Run();
