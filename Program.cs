using FataleCore.Data;
using FataleCore.Models;
using FataleCore.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// 1. Logging & Config
Console.WriteLine($"[STARTUP] Environment: {builder.Environment.EnvironmentName}");
Console.WriteLine($"[STARTUP] PORT (env): {Environment.GetEnvironmentVariable("PORT") ?? "N/A"}");

// 1.1 App Base & Path Configuration
var appBase = builder.Environment.IsProduction() ? "/data" : Directory.GetCurrentDirectory();
Console.WriteLine($"[STARTUP] Using appBase: {appBase}");

// 1.2 DB Configuration
var dbPath = builder.Configuration.GetConnectionString("Default");
var railwayDbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrWhiteSpace(railwayDbUrl))
{
    try 
    {
        if (railwayDbUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) || 
            railwayDbUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(railwayDbUrl);
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 5432;
            var database = uri.AbsolutePath.Trim('/');
            var user = "";
            var password = "";

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                var parts = uri.UserInfo.Split(':');
                user = parts[0];
                if (parts.Length > 1) password = parts[1];
            }

            var sslMode = host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase) ? "Disable" : "Prefer";
            
            dbPath = $"Host={host};Port={port};Database={database};Username={user};Password={password};SslMode={sslMode};Trust Server Certificate=true;Maximum Pool Size=50;";
            Console.WriteLine($"[STARTUP] Parsed DATABASE_URL: Host={host}, Port={port}, Database={database}, User={user}, SslMode={sslMode}");
        }
        else 
        {
            // If it's already a connection string format
            dbPath = railwayDbUrl;
            Console.WriteLine("[STARTUP] Using DATABASE_URL as raw connection string (no re-parsing).");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[STARTUP] ERROR parsing DATABASE_URL: {ex.Message}");
    }
}

if (string.IsNullOrWhiteSpace(dbPath))
{
    var sqliteFile = Path.Combine(appBase, "fatale_core.db");
    dbPath = $"Data Source={sqliteFile}";
    Console.WriteLine($"[STARTUP] Using SQLite fallback at: {sqliteFile}");
}
Console.WriteLine($"[STARTUP] Database Connection established using path/connection string.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (dbPath.Contains("Host=") || dbPath.Contains("Server="))
    {
        Console.WriteLine("[STARTUP] CONFIGURED: PostgreSQL Provider [Npgsql]");
        options.UseNpgsql(dbPath);
    }
    else
    {
        Console.WriteLine("[STARTUP] CONFIGURED: SQLite Provider [Sqlite]");
        options.UseSqlite(dbPath);
    }
});

// 1.5 Service Registration
builder.Services.AddMemoryCache();
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
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase; 
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true; 
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseDeveloperExceptionPage();

app.Use(async (context, next) => 
{
    Console.WriteLine($"[REQUEST] {context.Request.Method} {context.Request.Path}");
    await next.Invoke();
});

// DATABASE INIT
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    Console.WriteLine("[DATABASE] Applying migrations...");
    try 
    {
        db.Database.Migrate();
        Console.WriteLine("[DATABASE] OK: Migrations applied.");
        
        var systemArtist = db.Artists.FirstOrDefault(a => a.Name == "The Archive");
        if (systemArtist == null)
        {
            systemArtist = new Artist { Name = "The Archive", Bio = "System content aggregator.", ImageUrl = "" };
            db.Artists.Add(systemArtist);
            db.SaveChanges();
            Console.WriteLine("[DATABASE] OK: Created 'The Archive' artist.");
        }
    } 
    catch (Exception ex) 
    {
        Console.WriteLine("[DATABASE] ERROR during migration: " + ex.Message);
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

// In production (Railway), use the persistent volume at /data (already set in appBase)
Console.WriteLine($"[STARTUP] Final Path Mapping OK.");
Console.WriteLine($"[STARTUP] Environment: {builder.Environment.EnvironmentName}");

var uploadsPath = Path.Combine(appBase, "uploads");
if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);
Console.WriteLine($"[STARTUP] Serving /uploads from: {uploadsPath}");
app.UseStaticFiles(new StaticFileOptions { FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath), RequestPath = "/uploads" });

var cachePath = Path.Combine(appBase, "Cache");
if (!Directory.Exists(cachePath)) Directory.CreateDirectory(cachePath);
Console.WriteLine($"[STARTUP] Serving /cache from: {cachePath}");
app.UseStaticFiles(new StaticFileOptions { FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(cachePath), RequestPath = "/cache" });

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "FATALE_CORE_ONLINE_V2_PATCHED_DB_SYNC");
app.MapGet("/api/version", () => "VERSION_2026_04_16_POSTGRES_SYNC_V1");
app.MapGet("/api/ping", () => "PONG_VERSION_2_PATCHED");
app.MapGet("/api/debug-db", async (ApplicationDbContext db) => {
    var count = await db.Communities.CountAsync();
    var provider = db.Database.ProviderName;
    return Results.Ok(new { communityCount = count, databaseProvider = provider, timestamp = DateTime.UtcNow });
});

// Temporary endpoint to migrate 116MB of legacy media
app.MapPost("/api/migrate-media", async (HttpRequest request) =>
{
    var secret = request.Headers["X-Migration-Secret"].ToString();
    if (secret != "fatale-rescue-2026") return Results.Unauthorized();

    var appBase = app.Environment.IsProduction() ? "/data" : Directory.GetCurrentDirectory();
    var destPath = Path.Combine(appBase, "uploads");

    if (!request.HasFormContentType) return Results.BadRequest("Expected form content");
    var form = await request.ReadFormAsync();
    var file = form.Files.FirstOrDefault();
    if (file == null) return Results.BadRequest("No file provided");

    var tempZip = Path.GetTempFileName();
    using (var stream = new FileStream(tempZip, FileMode.Create))
        await file.CopyToAsync(stream);

    System.IO.Compression.ZipFile.ExtractToDirectory(tempZip, appBase, overwriteFiles: true);
    File.Delete(tempZip);

    return Results.Ok($"Successfully extracted media to {destPath}");
});

// Temporary endpoint to clean up old broken IMG_ posts for mel00test
app.MapGet("/api/cleanup-mel00", async (ApplicationDbContext db) => {
    var userIds = await db.Users.Where(u => u.Username.Contains("mel00")).Select(u => u.Id).ToListAsync();
    var badPosts = await db.StudioContents
        .Where(s => userIds.Contains(s.UserId) && (s.Title.Contains("IMG_") || s.Title.Contains("blob") || s.Url.Contains("blob")))
        .ToListAsync();
    db.StudioContents.RemoveRange(badPosts);
    await db.SaveChangesAsync();
    return Results.Ok(new { message = $"Deleted {badPosts.Count} broken posts for mel00", deleted = badPosts.Select(b => b.Title) });
});

app.MapControllers();
app.MapHub<RadioHub>("/hubs/radio");

Console.WriteLine("[STARTUP] Application ready.");
app.Run();
