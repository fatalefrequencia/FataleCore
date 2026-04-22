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

            _context.UserLikes.Add(like);

            // SYNC: Ensure FeedInteraction (Social Feed System) is also updated
            var existingInteraction = await _context.FeedInteractions
                .FirstOrDefaultAsync(i => i.UserId == userId && i.ItemType == "track" && i.ItemId == trackId && i.InteractionType == "LIKE");
            if (existingInteraction == null)
            {
                _context.FeedInteractions.Add(new FeedInteraction
                {
                    UserId = userId,
                    ItemType = "track",
                    ItemId = trackId,
                    InteractionType = "LIKE",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Track liked", liked = true });
        }

        // POST: api/social/unlike/{trackId}
        [HttpPost("unlike/{trackId}")]
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

            // SYNC: Remove from FeedInteractions (Social Feed System)
            var interactions = await _context.FeedInteractions
                .Where(i => i.UserId == userId && i.ItemType == "track" && i.ItemId == trackId && i.InteractionType == "LIKE")
                .ToListAsync();
            if (interactions.Any())
            {
                _context.FeedInteractions.RemoveRange(interactions);
            }

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
