using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FataleCore.Data;
using FataleCore.Models;
using FataleCore.Services;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.IO;

namespace FataleCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class YoutubeCacheController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ISubscriptionService _subscriptionService;
        private readonly string _cacheDirectory;

        public YoutubeCacheController(ApplicationDbContext context, ISubscriptionService subscriptionService)
        {
            _context = context;
            _subscriptionService = subscriptionService;
            _cacheDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Cache");
            if (!Directory.Exists(_cacheDirectory))
            {
                Directory.CreateDirectory(_cacheDirectory);
            }
        }

        [HttpGet("my-cached-tracks")]
        public async Task<IActionResult> GetMyCachedTracks()
        {
            if (!Request.Headers.TryGetValue("UserId", out var userIdVal) || !int.TryParse(userIdVal, out int userId))
            {
                return Unauthorized("User ID required");
            }

            var tracks = await _context.CachedYoutubeTracks
                .Include(c => c.YoutubeTrack)
                .Where(c => c.UserId == userId && c.IsAvailable)
                .Select(c => new 
                {
                    c.Id,
                    c.YoutubeTrackId,
                    youtubeId = c.YoutubeTrack!.YoutubeId,
                    title = c.YoutubeTrack.Title,
                    artist = c.YoutubeTrack.ChannelTitle,
                    duration = c.YoutubeTrack.Duration,
                    cachedAt = c.CachedAt,
                    fileSize = c.FileSizeBytes
                })
                .ToListAsync();

            return Ok(tracks);
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            if (!Request.Headers.TryGetValue("UserId", out var userIdVal) || !int.TryParse(userIdVal, out int userId))
            {
                return Unauthorized("User ID required");
            }
            
            var (used, limit) = await _subscriptionService.GetCacheUsageAsync(userId);
            return Ok(new { used, limit });
        }

        [HttpPost("{youtubeTrackId}/cache")]
        public async Task<IActionResult> CacheTrack(int youtubeTrackId)
        {
            if (!Request.Headers.TryGetValue("UserId", out var userIdVal) || !int.TryParse(userIdVal, out int userId))
            {
                return Unauthorized("User ID required");
            }

            // 1. Check Permissions via Service
            if (!await _subscriptionService.CanCacheTrackAsync(userId))
            {
                var (used, limit) = await _subscriptionService.GetCacheUsageAsync(userId);
                if (limit > 0 && used >= limit)
                    return BadRequest($"Cache limit reached ({used}/{limit}). Upgrade your plan.");
                
                return BadRequest("Active subscription required to cache tracks.");
            }

            // 2. Check if already cached
            var existing = await _context.CachedYoutubeTracks
                .FirstOrDefaultAsync(c => c.UserId == userId && c.YoutubeTrackId == youtubeTrackId);

            if (existing != null && existing.IsAvailable)
            {
                return Ok(new { message = "Track already cached." });
            }

            // 3. Get YouTube Track Info
            var ytTrack = await _context.YoutubeTracks.FindAsync(youtubeTrackId);
            if (ytTrack == null)
            {
                return NotFound("YouTube track not found in database. Like it first?");
            }

            // 4. Mock Download (Real implementation would use a valid download service)
            string fileName = $"{userId}_{ytTrack.YoutubeId}.mp3";
            string filePath = Path.Combine(_cacheDirectory, fileName);
            
            if (!System.IO.File.Exists(filePath))
            {
                await System.IO.File.WriteAllTextAsync(filePath, "DUMMY AUDIO CONTENT");
            }

            if (existing != null)
            {
                existing.IsAvailable = true;
                existing.CachedAt = DateTime.UtcNow;
                existing.ExpiresAt = null;
                 _context.CachedYoutubeTracks.Update(existing);
            }
            else
            {
                var cachedTrack = new CachedYoutubeTrack
                {
                    UserId = userId,
                    YoutubeTrackId = youtubeTrackId,
                    AudioFilePath = fileName,
                    FileSizeBytes = 1024, // Mock size
                    AudioQuality = 128,
                    IsAvailable = true
                };
                _context.CachedYoutubeTracks.Add(cachedTrack);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Track cached successfully!" });
        }

        [HttpDelete("{youtubeTrackId}")]
        public async Task<IActionResult> UncacheTrack(int youtubeTrackId)
        {
             if (!Request.Headers.TryGetValue("UserId", out var userIdVal) || !int.TryParse(userIdVal, out int userId))
            {
                return Unauthorized("User ID required");
            }

            var cached = await _context.CachedYoutubeTracks
                .FirstOrDefaultAsync(c => c.UserId == userId && c.YoutubeTrackId == youtubeTrackId);

            if (cached != null)
            {
                _context.CachedYoutubeTracks.Remove(cached);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Track removed from cache." });
        }
    }
}
