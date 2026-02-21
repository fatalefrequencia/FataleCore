using FataleCore.Data;
using FataleCore.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// 1. DB Configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=fatale_core.db"));

// 1.5 Service Registration
builder.Services.AddScoped<FataleCore.Services.ISubscriptionService, FataleCore.Services.SubscriptionService>();

// 2. CORS Configuration (Explicit)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
            .AllowAnyOrigin()
            .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS") // Explicit methods
            .AllowAnyHeader());
});

// 3. JWT Authentication Configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetSection("AppSettings:Token").Value ?? "super secret key that is long enough to be secure 1234567890")),
            ValidateIssuer = false,
            ValidateAudience = false
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
        "ALTER TABLE Users ADD COLUMN BannerUrl TEXT;",
        "ALTER TABLE Users ADD COLUMN ThemeColor TEXT DEFAULT '#ff006e';",
        "ALTER TABLE Users ADD COLUMN TextColor TEXT DEFAULT '#ffffff';",
        "ALTER TABLE Users ADD COLUMN BackgroundColor TEXT DEFAULT '#000000';",
        "ALTER TABLE Users ADD COLUMN IsGlass INTEGER DEFAULT 0;",
        "ALTER TABLE Users ADD COLUMN WallpaperVideoUrl TEXT;",
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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// IMPORTANT: Use CORS before Auth
app.UseCors("AllowAll");

var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

var cachePath = Path.Combine(Directory.GetCurrentDirectory(), "Cache");
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

app.MapControllers();

app.Run();
