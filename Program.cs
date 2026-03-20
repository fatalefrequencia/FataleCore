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

// 1.1 DB Configuration
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
    dbPath = "Host=localhost;Port=5432;Database=fatale_core;Username=postgres;Password=password;";
    Console.WriteLine("[STARTUP] Falling back to default localhost connection string.");
}
Console.WriteLine($"[STARTUP] Database Path: {dbPath}");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(dbPath));

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
