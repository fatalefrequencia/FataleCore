using FataleCore.Data;
using FataleCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FataleCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SocialController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SocialController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/social/like/{trackId}
        [HttpPost("like/{trackId}")]
        public async Task<IActionResult> LikeTrack(int trackId, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid User ID");

            var existingLike = await _context.UserLikes
                .FirstOrDefaultAsync(l => l.UserId == userId && l.TrackId == trackId);

            if (existingLike != null)
            {
                return Ok(new { message = "Already liked", liked = true });
            }

            var like = new UserLike
            {
                UserId = userId,
                TrackId = trackId,
                LikedAt = DateTime.UtcNow
            };

            _context.UserLikes.Add(like);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Track liked", liked = true });
        }

        // DELETE: api/social/like/{trackId}
        [HttpDelete("like/{trackId}")]
        public async Task<IActionResult> UnlikeTrack(int trackId, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid User ID");

            var existingLike = await _context.UserLikes
                .FirstOrDefaultAsync(l => l.UserId == userId && l.TrackId == trackId);

            if (existingLike == null)
            {
                return Ok(new { message = "Not liked", liked = false });
            }

            _context.UserLikes.Remove(existingLike);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Track unliked", liked = false });
        }

        // POST: api/social/repost/{trackId}
        [HttpPost("repost/{trackId}")]
        public async Task<IActionResult> RepostTrack(int trackId, [FromHeader(Name = "UserId")] int userId)
        {
            // Placeholder for Repost logic if needed
            return Ok(new { message = "Reposted (Stub)" });
        }

        // GET: api/social/comments/{trackId}
        [HttpGet("comments/{trackId}")]
        public async Task<IActionResult> GetComments(int trackId)
        {
            // Placeholder for Comments logic
            return Ok(new List<object>());
        }
    }
}
