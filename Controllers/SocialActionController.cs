using FataleCore.Data;
using FataleCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FataleCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SocialActionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SocialActionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/socialaction/like
        [HttpPost("like")]
        public async Task<IActionResult> ToggleLike([FromBody] InteractionRequest request, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid User ID");

            var existing = await _context.FeedInteractions
                .FirstOrDefaultAsync(i => i.UserId == userId && 
                                         i.ItemType == request.ItemType && 
                                         i.ItemId == request.ItemId && 
                                         i.InteractionType == "LIKE");

            bool liked;
            if (existing != null)
            {
                _context.FeedInteractions.Remove(existing);
                liked = false;
            }
            else
            {
                var interaction = new FeedInteraction
                {
                    UserId = userId,
                    ItemType = request.ItemType,
                    ItemId = request.ItemId,
                    InteractionType = "LIKE",
                    CreatedAt = DateTime.UtcNow
                };
                _context.FeedInteractions.Add(interaction);
                liked = true;
            }

            await _context.SaveChangesAsync();

            var count = await _context.FeedInteractions
                .CountAsync(i => i.ItemType == request.ItemType && 
                                 i.ItemId == request.ItemId && 
                                 i.InteractionType == "LIKE");

            return Ok(new { liked, likeCount = count });
        }

        // POST: api/socialaction/comment
        [HttpPost("comment")]
        public async Task<IActionResult> AddComment([FromBody] InteractionRequest request, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid User ID");
            if (string.IsNullOrWhiteSpace(request.Content)) return BadRequest("Content required");

            var interaction = new FeedInteraction
            {
                UserId = userId,
                ItemType = request.ItemType,
                ItemId = request.ItemId,
                InteractionType = "COMMENT",
                Content = request.Content,
                ParentId = request.ParentId,
                CreatedAt = DateTime.UtcNow
            };

            _context.FeedInteractions.Add(interaction);
            await _context.SaveChangesAsync();

            var count = await _context.FeedInteractions
                .CountAsync(i => i.ItemType == request.ItemType && 
                                 i.ItemId == request.ItemId && 
                                 i.InteractionType == "COMMENT");

            return Ok(new { success = true, commentCount = count, id = interaction.Id });
        }

        // GET: api/socialaction/comments/{type}/{id}
        [HttpGet("comments/{type}/{id}")]
        public async Task<IActionResult> GetComments(string type, int id)
        {
            var ownerId = await GetItemOwnerId(type, id);

            var comments = await _context.FeedInteractions
                .Where(i => i.ItemType == type && i.ItemId == id && i.InteractionType == "COMMENT")
                .Include(i => i.User)
                .OrderBy(i => i.CreatedAt)
                .Select(i => new {
                    i.Id,
                    i.UserId,
                    Username = i.User != null ? i.User.Username : "UNKNOWN_SIGNAL",
                    i.Content,
                    i.CreatedAt,
                    i.ParentId,
                    IsOperator = ownerId.HasValue && i.UserId == ownerId.Value
                })
                .ToListAsync();

            return Ok(comments);
        }

        private async Task<int?> GetItemOwnerId(string type, int id)
        {
            return type.ToLower() switch
            {
                "track" => await _context.Tracks
                    .Where(t => t.Id == id)
                    .Include(t => t.Album)
                    .ThenInclude(a => a!.Artist)
                    .Select(t => t.Album != null && t.Album.Artist != null ? t.Album.Artist.UserId : null)
                    .FirstOrDefaultAsync(),
                "studio" => await _context.StudioContents
                    .Where(s => s.Id == id)
                    .Select(s => (int?)s.UserId)
                    .FirstOrDefaultAsync(),
                "journal" => await _context.JournalEntries
                    .Where(j => j.Id == id)
                    .Select(j => (int?)j.UserId)
                    .FirstOrDefaultAsync(),
                _ => null
            };
        }

        // POST: api/socialaction/repost
        [HttpPost("repost")]
        public async Task<IActionResult> ToggleRepost([FromBody] InteractionRequest request, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid User ID");

            var existing = await _context.FeedInteractions
                .FirstOrDefaultAsync(i => i.UserId == userId && 
                                         i.ItemType == request.ItemType && 
                                         i.ItemId == request.ItemId && 
                                         i.InteractionType == "REPOST");

            bool reposted;
            if (existing != null)
            {
                _context.FeedInteractions.Remove(existing);
                reposted = false;
            }
            else
            {
                var interaction = new FeedInteraction
                {
                    UserId = userId,
                    ItemType = request.ItemType,
                    ItemId = request.ItemId,
                    InteractionType = "REPOST",
                    CreatedAt = DateTime.UtcNow
                };
                _context.FeedInteractions.Add(interaction);
                reposted = true;
            }

            await _context.SaveChangesAsync();

            var count = await _context.FeedInteractions
                .CountAsync(i => i.ItemType == request.ItemType && 
                                 i.ItemId == request.ItemId && 
                                 i.InteractionType == "REPOST");

            return Ok(new { reposted, repostCount = count });
        }

        // DELETE: api/socialaction/comment/{id}
        [HttpDelete("comment/{id}")]
        public async Task<IActionResult> DeleteComment(int id, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid User ID");

            var comment = await _context.FeedInteractions.FindAsync(id);
            if (comment == null) return NotFound();

            if (comment.InteractionType != "COMMENT") return BadRequest("Not a comment signal");

            // Authorization: Author only
            if (comment.UserId != userId)
            {
                return Unauthorized("Unauthorized access to signal disposal");
            }

            _context.FeedInteractions.Remove(comment);
            
            // Delete direct replies to maintain data integrity
            var replies = await _context.FeedInteractions
                .Where(i => i.ParentId == id && i.InteractionType == "COMMENT")
                .ToListAsync();
            _context.FeedInteractions.RemoveRange(replies);

            await _context.SaveChangesAsync();

            var count = await _context.FeedInteractions
                .CountAsync(i => i.ItemType == comment.ItemType && 
                                 i.ItemId == comment.ItemId && 
                                 i.InteractionType == "COMMENT");

            return Ok(new { success = true, commentCount = count });
        }

        public class InteractionRequest
        {
            public string ItemType { get; set; } = string.Empty;
            public int ItemId { get; set; }
            public string? Content { get; set; }
            public int? ParentId { get; set; }
        }
    }
}
