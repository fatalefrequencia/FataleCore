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
            .SetIsOriginAllowed(_ => true) 
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()); 
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
        options.JsonSerializerOptions.PropertyNamingPolicy = null; 
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true; 
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ASEGURAR BASE DE DATOS
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    Console.WriteLine("[DATABASE] Initializing initialization strategy...");

    // A. Manually create Transactions if EF missed it (Common point of failure)
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

    // B. Run standard migrations first
    try {
        db.Database.Migrate();
        Console.WriteLine("[DATABASE] OK: Standard migrations applied.");
    } catch (Exception ex) {
        Console.WriteLine("[DATABASE] MIGRATION_NOTICE (Skipping/Continuing): " + ex.Message);
    }

    // C. Post-Migration "Self-Healing" Patches (For tables that were already created but missing columns)
    string[] schemaPatches = {
        // Artists table fixes
        "ALTER TABLE Artists ADD COLUMN FeaturedTrackId INTEGER;",
        "ALTER TABLE Artists ADD COLUMN IsLive INTEGER DEFAULT 0;",
        "ALTER TABLE Artists ADD COLUMN CreditsBalance INTEGER DEFAULT 0;",
        "ALTER TABLE Artists ADD COLUMN MapX INTEGER;",
        "ALTER TABLE Artists ADD COLUMN MapY INTEGER;",
        "ALTER TABLE Artists ADD COLUMN SectorId INTEGER;",
        "ALTER TABLE Artists ADD COLUMN UserId INTEGER;",
        
        // Users table fixes
        "ALTER TABLE Users ADD COLUMN CommunityId INTEGER;",
        "ALTER TABLE Users ADD COLUMN Biography TEXT DEFAULT '';",
        "ALTER TABLE Users ADD COLUMN ProfilePictureUrl TEXT DEFAULT '';",
        "ALTER TABLE Users ADD COLUMN BannerUrl TEXT;",
        "ALTER TABLE Users ADD COLUMN ThemeColor TEXT DEFAULT '#ff006e';",
        "ALTER TABLE Users ADD COLUMN TextColor TEXT DEFAULT '#ffffff';",
        "ALTER TABLE Users ADD COLUMN BackgroundColor TEXT DEFAULT '#000000';",
        "ALTER TABLE Users ADD COLUMN IsGlass INTEGER DEFAULT 0;",
        "ALTER TABLE Users ADD COLUMN WallpaperVideoUrl TEXT;",
        "ALTER TABLE Users ADD COLUMN CreditsBalance INTEGER DEFAULT 0;",

        // Tracks table fixes
        "ALTER TABLE Tracks ADD COLUMN Source TEXT;",
        "ALTER TABLE Tracks ADD COLUMN IsPinned INTEGER DEFAULT 0;",
        "ALTER TABLE Tracks ADD COLUMN IsPosted INTEGER DEFAULT 0;",
        "ALTER TABLE Tracks ADD COLUMN CreatedAt TEXT;",

        // Stations table fixes
        "ALTER TABLE Stations ADD COLUMN Description TEXT;",
        "ALTER TABLE Stations ADD COLUMN IsChatEnabled INTEGER DEFAULT 1;",
        "ALTER TABLE Stations ADD COLUMN IsQueueEnabled INTEGER DEFAULT 1;",
        "ALTER TABLE Stations ADD COLUMN ArtistId INTEGER DEFAULT 0;",
        "ALTER TABLE Stations ADD COLUMN CurrentTrackId INTEGER;",
        "ALTER TABLE Stations ADD COLUMN IsLive INTEGER DEFAULT 0;",

        // Playlists table fixes
        "ALTER TABLE Playlists ADD COLUMN IsPinned INTEGER DEFAULT 0;",
        "ALTER TABLE Playlists ADD COLUMN IsPosted INTEGER DEFAULT 0;"
    };

    foreach (var patch in schemaPatches) {
        try { 
            db.Database.ExecuteSqlRaw(patch); 
            Console.WriteLine($"[DATABASE] Patch Applied: {patch.Split(' ')[2]}");
        } catch (Exception ex) {
            // Ignore if column already exists
            if (!ex.Message.Contains("duplicate") && !ex.Message.Contains("already exists"))
            {
                // Unhandled error might be useful?
            }
        }
    }

    // D. Data integrity fixes
    try {
        db.Database.ExecuteSqlRaw("UPDATE Tracks SET CreatedAt = CURRENT_TIMESTAMP WHERE CreatedAt IS NULL;");
        db.Database.ExecuteSqlRaw("UPDATE Users SET Biography = '' WHERE Biography IS NULL;");
        db.Database.ExecuteSqlRaw("UPDATE Users SET ProfilePictureUrl = '' WHERE ProfilePictureUrl IS NULL;");
    } catch { }

    // E. Ensure System Content
    try {
        var systemArtist = db.Artists.FirstOrDefault(a => a.Name == "The Archive");
        if (systemArtist == null)
        {
            systemArtist = new Artist { Name = "The Archive", Bio = "System content aggregator.", ImageUrl = "" };
            db.Artists.Add(systemArtist);
            db.SaveChanges();
            Console.WriteLine("[DATABASE] OK: Created 'The Archive' artist.");
        }
    } catch { }

    Console.WriteLine("[DATABASE] Initialization sequence complete.");
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

var appBase = app.Environment.IsProduction() ? "/app" : Directory.GetCurrentDirectory();

var uploadsPath = Path.Combine(appBase, "uploads");
if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions { FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath), RequestPath = "/uploads" });

var cachePath = Path.Combine(appBase, "Cache");
if (!Directory.Exists(cachePath)) Directory.CreateDirectory(cachePath);
app.UseStaticFiles(new StaticFileOptions { FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(cachePath), RequestPath = "/cache" });

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "FATALE_CORE_ONLINE_V2_PATCHED");
app.MapGet("/api/ping", () => "PONG_VERSION_2");
app.MapControllers();
app.MapHub<RadioHub>("/hubs/radio");

Console.WriteLine("[STARTUP] Application ready.");
app.Run();
