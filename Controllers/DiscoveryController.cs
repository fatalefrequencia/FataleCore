using FataleCore.Data;
using FataleCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace FataleCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiscoveryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DiscoveryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/discovery/track-play/{id}
        [HttpPost("track-play/{id}")]
        public async Task<IActionResult> RecordPlay(int id, [FromHeader(Name = "UserId")] int? userId)
        {
            var track = await _context.Tracks.FindAsync(id);
            if (track == null) return NotFound("Track not found");

            // Increment PlayCount
            track.PlayCount++;

            // Create Event
            var discoveryEvent = new DiscoveryEvent
            {
                TrackId = id,
                UserId = userId > 0 ? userId : null,
                EventType = "Play",
                Timestamp = DateTime.UtcNow,
                MapX = track.MapX,
                MapY = track.MapY
            };

            _context.DiscoveryEvents.Add(discoveryEvent);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Play recorded", playCount = track.PlayCount });
        }

        // GET: api/discovery/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetGlobalStats()
        {
            var totalPlays = await _context.Tracks.SumAsync(t => t.PlayCount);
            var topTracks = await _context.Tracks
                .OrderByDescending(t => t.PlayCount)
                .Take(5)
                .Select(t => new { t.Id, t.Title, t.PlayCount })
                .ToListAsync();

            var totalLikes = await _context.UserLikes.CountAsync();

            return Ok(new
            {
                totalScans = totalPlays + totalLikes, // Lore-friendly metric
                totalPlays,
                topTracks,
                activeUsers = await GetOnlineUserCount(), // Now strictly "Online/Active"
                totalUsers = await _context.Users.CountAsync(),
                onlineUsers = await GetOnlineUserCount()
            });
        }

        // GET: api/discovery/online-users
        [HttpGet("online-users")]
        public async Task<IActionResult> GetOnlineUsers()
        {
            var onlineCount = await GetOnlineUserCount();
            return Ok(new { onlineUsers = onlineCount });
        }

        // POST: api/discovery/heartbeat
        [HttpPost("heartbeat")]
        public async Task<IActionResult> RecordHeartbeat([FromHeader(Name = "UserId")] int? userId)
        {
            if (userId == null || userId <= 0) return Unauthorized();

            // Find a valid TrackId to satisfy FK constraint (placeholder)
            // We use the first available track. If no tracks exist, we can't log (which is fine, empty app).
            var placeholderTrackId = await _context.Tracks.Select(t => t.Id).FirstOrDefaultAsync();
            
            if (placeholderTrackId == 0) return Ok(); // No tracks, can't log heartbeat safely without risking FK error.

            var discoveryEvent = new DiscoveryEvent
            {
                UserId = userId,
                TrackId = placeholderTrackId,
                EventType = "Heartbeat",
                Timestamp = DateTime.UtcNow
            };

            _context.DiscoveryEvents.Add(discoveryEvent);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // Helper: Calculate online users based on recent activity (last 5 minutes)
        private async Task<int> GetOnlineUserCount()
        {
            var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);
            var onlineUsers = await _context.DiscoveryEvents
                .Where(e => e.Timestamp >= fiveMinutesAgo && e.UserId != null)
                .Select(e => e.UserId)
                .Distinct()
                .CountAsync();
            
            return Math.Max(onlineUsers, 1); // Always show at least 1 (the current user)
        }
    }
}
