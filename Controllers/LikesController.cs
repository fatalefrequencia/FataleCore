using FataleCore.Data;
using FataleCore.Dtos;
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
        // Get all liked tracks for the user
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Track>>> GetLikedTracks([FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid User ID");

            var likedTracks = await _context.UserLikes
                .Where(l => l.UserId == userId)
                .Include(l => l.Track)
                .ThenInclude(t => t!.Album)
                .ThenInclude(a => a!.Artist)
                .Where(l => l.Track != null)
                .Select(l => l.Track!)
                .ToListAsync();

            return likedTracks;
        }
    }
}
