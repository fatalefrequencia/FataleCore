using FataleCore.Data;
using FataleCore.DTOs;
using FataleCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FataleCore.Controllers
{
    // [Authorize] // Assuming we want auth, but user didn't specify auth middleware setup in detail.
    // Based on social.js headers 'UserId', it seems to rely on client-side user ID for now 
    // rather than JWT token claims, matching the "Hybrid Economy" conversation context.
    // I will use the 'UserId' header as expected by the frontend.

    [Route("api/[controller]")]
    [ApiController]
    public class LikesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LikesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/likes
        // Toggle like (actually social.js uses POST to like, DELETE to unlike)
        [HttpPost]
        public async Task<IActionResult> LikeTrack([FromBody] LikeDto dto, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid User ID");

            var existingLike = await _context.UserLikes
                .FirstOrDefaultAsync(l => l.UserId == userId && l.TrackId == dto.TrackId);

            if (existingLike != null)
            {
                return BadRequest("Track already liked");
            }

            var like = new UserLike
            {
                UserId = userId,
                TrackId = dto.TrackId,
                LikedAt = DateTime.UtcNow
            };

            _context.UserLikes.Add(like);
            await _context.SaveChangesAsync();

            var count = await _context.UserLikes.CountAsync(l => l.TrackId == dto.TrackId);

            return Ok(new { liked = true, likeCount = count });
        }

        // DELETE: api/likes/{trackId}
        [HttpDelete("{trackId}")]
        public async Task<IActionResult> UnlikeTrack(int trackId, [FromHeader(Name = "UserId")] int userId)
        {
             if (userId <= 0) return Unauthorized("Invalid User ID");

            var existingLike = await _context.UserLikes
                .FirstOrDefaultAsync(l => l.UserId == userId && l.TrackId == trackId);

            if (existingLike == null)
            {
                return BadRequest("Track not liked");
            }

            _context.UserLikes.Remove(existingLike);
            await _context.SaveChangesAsync();

            var count = await _context.UserLikes.CountAsync(l => l.TrackId == trackId);

            return Ok(new { liked = false, likeCount = count });
        }

        // GET: api/likes/check/{trackId}
        [HttpGet("check/{trackId}")]
        public async Task<IActionResult> CheckLikeStatus(int trackId, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid User ID"); // Or just return false?

            var isLiked = await _context.UserLikes
                .AnyAsync(l => l.UserId == userId && l.TrackId == trackId);
            
            var count = await _context.UserLikes.CountAsync(l => l.TrackId == trackId);

            return Ok(new { liked = isLiked, likeCount = count });
        }

        // GET: api/likes
        // Get all liked tracks for the user (Unified Library)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TrackDto>>> GetLikedTracks([FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid User ID");

            // 1. Get Track IDs from UserLikes (System A)
            var trackIdsA = await _context.UserLikes
                .Where(l => l.UserId == userId)
                .Select(l => l.TrackId)
                .ToListAsync();

            // 2. Get Track IDs from FeedInteractions (System B)
            var trackIdsB = await _context.FeedInteractions
                .Where(i => i.UserId == userId && i.ItemType == "track" && i.InteractionType == "LIKE")
                .Select(i => i.ItemId)
                .ToListAsync();

            // 3. Union them to avoid duplicates
            var allTrackIds = trackIdsA.Union(trackIdsB).Distinct().ToList();

            // 4. Fetch the actual tracks
            var likedTracks = await _context.Tracks
                .Where(t => !t.IsDelisted && allTrackIds.Contains(t.Id))
                .Include(t => t.Album)
                    .ThenInclude(a => a!.Artist)
                .ToListAsync();

            return Ok(likedTracks.Select(t => t.ToDto()));
        }

        // POST: api/likes/cleanup
        // Clears legacy "YoutubeExplode" style likes once and for all
        [HttpPost("cleanup")]
        public async Task<IActionResult> CleanupLegacyLikes([FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid User ID");

            // Since we've updated the model, existing records with YoutubeTrackId
            // might be in an inconsistent state in the DB if not migrated.
            // We'll use Raw SQL to purge them to be extremely safe against model mismatches.
            
            try 
            {
                // Delete records where YoutubeTrackId was present (legacy schema)
                // Note: If the column is already dropped by EF/Manual change, this might need adjustment.
                // But for now, we'll assume it exists in the physical schema but not the C# model.
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM UserLikes WHERE YoutubeTrackId IS NOT NULL");
                await _context.SaveChangesAsync();
                return Ok(new { message = "Legacy signal interference cleared. Archive purged." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Cleanup failed: {ex.Message}");
            }
        }
    }
}
