using FataleCore.Data;
using FataleCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FataleCore.DTOs;
using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace FataleCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public UsersController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
        {
            var users = await _context.Users.ToListAsync();
            return users.Select(u => u.ToDto()).ToList();
        }

        private static string GetSectorColor(int sectorId) => sectorId switch
        {
            0 => "#ff006e",
            1 => "#00ffff",
            2 => "#ff0000",
            3 => "#aaff00",
            4 => "#bf00ff",
            _ => "#ff006e"
        };

        // GET: api/Users/search?query=...
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<UserDto>>> SearchUsers([FromQuery] string? query)
        {
            if (string.IsNullOrWhiteSpace(query)) 
            {
                var usersDefault = await _context.Users.Take(10).ToListAsync();
                return usersDefault.Select(u => u.ToDto()).ToList();
            }
            
            var results = await _context.Users
                .Where(u => u.Username.ToLower().Contains(query.ToLower()))
                .Take(20)
                .ToListAsync();
                
            return Ok(results.Select(r => r.ToDto()));
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile([FromHeader(Name = "UserId")] int? userId)
        {
            if (userId == null || userId <= 0) 
            {
                 return Unauthorized(new { message = "IDENTITY_SYNC_FAIL: UserId header missing or invalid in request.", receivedId = userId });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound("User not found");

            var artist = await _context.Artists.FirstOrDefaultAsync(a => a.UserId == userId);
            return Ok(user.ToDto(artist));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var artist = await _context.Artists.FirstOrDefaultAsync(a => a.UserId == id);
            return Ok(user.ToDto(artist));
        }

        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileDto dto, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid User ID");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound("User not found");

            // Update text fields if provided
            if (!string.IsNullOrEmpty(dto.Username)) user.Username = dto.Username;
            if (dto.Biography != null) user.Biography = dto.Biography;
            if (dto.ResidentSectorId.HasValue) user.ResidentSectorId = dto.ResidentSectorId.Value;
            
            // Handle Artist-specific fields
            var artist = await _context.Artists.FirstOrDefaultAsync(a => a.UserId == userId);
            if (artist == null)
            {
                // Create minimal artist profile if missing
                artist = new Artist 
                { 
                    UserId = userId,
                    Name = user.Username ?? "Unknown",
                    Bio = user.Biography ?? "",
                    ImageUrl = user.ProfilePictureUrl ?? "",
                    SectorId = user.ResidentSectorId
                };
                _context.Artists.Add(artist);
            }

            if (dto.ResidentSectorId.HasValue) artist.SectorId = dto.ResidentSectorId.Value;

            if (dto.IsLive.HasValue) artist.IsLive = dto.IsLive.Value;
            
            // Handle Quiet Mode (-1) or specific track
            if (dto.FeaturedTrackId.HasValue) 
            {
                artist.FeaturedTrackId = dto.FeaturedTrackId.Value == -1 ? null : dto.FeaturedTrackId.Value;
            }
            
            artist.Name = user.Username ?? "Unknown";
            artist.Bio = user.Biography ?? "";
            artist.ImageUrl = user.ProfilePictureUrl ?? "";

            // Handle Profile Picture
            if (dto.ProfilePicture != null && dto.ProfilePicture.Length > 0)
            {
                // Ensure directory exists
                var appBase = _env.IsProduction() ? "/data" : Directory.GetCurrentDirectory();
                var uploadPath = Path.Combine(appBase, "uploads", "avatars");
                if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(dto.ProfilePicture.FileName)}";
                var filePath = Path.Combine(uploadPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.ProfilePicture.CopyToAsync(stream);
                }

                // Update URL
                user.ProfilePictureUrl = $"/uploads/avatars/{fileName}";
                
                // CRITICAL: Also update artist profile image to keep Discovery in sync
                if (artist != null) 
                {
                    artist.ImageUrl = user.ProfilePictureUrl;
                }
            }

            // Banner Upload
            if (dto.Banner != null && dto.Banner.Length > 0)
            {
                var appBase = _env.IsProduction() ? "/data" : Directory.GetCurrentDirectory();
                var uploadsPath = Path.Combine(appBase, "uploads", "banners");
                if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

                var fileName = $"{user.Id}_banner_{DateTime.UtcNow.Ticks}{Path.GetExtension(dto.Banner.FileName)}";
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.Banner.CopyToAsync(stream);
                }

                user.BannerUrl = $"/uploads/banners/{fileName}";
            }

            // Wallpaper Video Upload
            if (dto.WallpaperVideo != null && dto.WallpaperVideo.Length > 0)
            {
                var appBase = _env.IsProduction() ? "/data" : Directory.GetCurrentDirectory();
                var wallpaperPath = Path.Combine(appBase, "uploads", "wallpapers");
                if (!Directory.Exists(wallpaperPath)) Directory.CreateDirectory(wallpaperPath);

                var wallpaperFileName = $"{user.Id}_wallpaper_{DateTime.UtcNow.Ticks}{Path.GetExtension(dto.WallpaperVideo.FileName)}";
                var wallpaperFilePath = Path.Combine(wallpaperPath, wallpaperFileName);

                using (var stream = new FileStream(wallpaperFilePath, FileMode.Create))
                {
                    await dto.WallpaperVideo.CopyToAsync(stream);
                }

                user.WallpaperVideoUrl = $"/uploads/wallpapers/{wallpaperFileName}";
            }

            // Theme Color
            if (!string.IsNullOrEmpty(dto.ThemeColor))
            {
                user.ThemeColor = dto.ThemeColor;
            }

            // Text Color
            if (!string.IsNullOrEmpty(dto.TextColor))
            {
                user.TextColor = dto.TextColor;
            }

            // Background Color
            if (!string.IsNullOrEmpty(dto.BackgroundColor))
            {
                user.BackgroundColor = dto.BackgroundColor;
            }

            if (dto.IsGlass.HasValue)
            {
                user.IsGlass = dto.IsGlass.Value;
            }

            await _context.SaveChangesAsync();

            // Re-fetch artist to get latest data
            artist = await _context.Artists.FirstOrDefaultAsync(a => a.UserId == user.Id);

            return Ok(new { 
                message = "Profile updated successfully",
                user = user.ToDto(artist)
            });
        }

        // GET: api/User/{id}/following
        [HttpGet("{id}/following")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetFollowing(int id)
        {
            var following = await _context.UserArtistLikes
                .Where(l => l.UserId == id)
                .Include(l => l.Artist)
                .Where(l => l.Artist != null && l.Artist.UserId != null)
                .Select(l => l.Artist!.User!)
                .ToListAsync();
            
            return Ok(following.Select(u => u.ToDto()));
        }

        // GET: api/User/{id}/followers
        [HttpGet("{id}/followers")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetFollowers(int id)
        {
            var artist = await _context.Artists.FirstOrDefaultAsync(a => a.UserId == id);
            if (artist == null) return Ok(new List<UserDto>());
            
            var followers = await _context.UserArtistLikes
                .Where(l => l.ArtistId == artist.Id)
                .Include(l => l.User)
                .Where(l => l.User != null)
                .Select(l => l.User!)
                .ToListAsync();
            
            return Ok(followers.Select(u => u.ToDto()));
        }
    }
}
