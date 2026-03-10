using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FataleCore.Data;
using FataleCore.Models;

namespace FataleCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JournalController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public JournalController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<JournalEntry>>> GetMyJournal([FromHeader(Name = "UserId")] int userId)
        {
            Console.WriteLine($"[JOURNAL] GetMyJournal for UserId: {userId}");
            if (userId <= 0) return Unauthorized("Invalid User ID");

            var results = await _context.JournalEntries
                .Where(j => j.UserId == userId)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();
            
            Console.WriteLine($"[JOURNAL] Found {results.Count} entries for UserId: {userId}");
            return results;
        }

        // GET: api/Journal/user/{targetUserId}
        [HttpGet("user/{targetUserId}")]
        public async Task<ActionResult<IEnumerable<JournalEntry>>> GetUserJournal(int targetUserId)
        {
            // Return all entries (previously restricted to posted only)
            return await _context.JournalEntries
                .Where(j => j.UserId == targetUserId)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();
        }

        // POST: api/Journal
        [HttpPost]
        public async Task<ActionResult<JournalEntry>> CreateEntry([FromBody] JournalEntryDto dto, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid User ID");

            var entry = new JournalEntry
            {
                UserId = userId,
                Title = dto.Title,
                Content = dto.Content,
                CreatedAt = DateTime.UtcNow,
                IsPosted = true,
                IsPinned = dto.IsPinned
            };

            _context.JournalEntries.Add(entry);
            await _context.SaveChangesAsync();

            return Ok(entry);
        }

        // PUT: api/Journal/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEntry(int id, [FromBody] JournalEntryDto dto, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid User ID");

            var entry = await _context.JournalEntries.FindAsync(id);
            if (entry == null) return NotFound();
            if (entry.UserId != userId) return Forbid();

            entry.Title = dto.Title;
            entry.Content = dto.Content;
            entry.IsPosted = dto.IsPosted;
            entry.IsPinned = dto.IsPinned;

            await _context.SaveChangesAsync();
            return Ok(entry);
        }

        // POST: api/Journal/toggle-post/{id}
        [HttpPost("toggle-post/{id}")]
        public async Task<IActionResult> TogglePost(int id, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid User ID");

            var entry = await _context.JournalEntries.FindAsync(id);
            if (entry == null) return NotFound();
            if (entry.UserId != userId) return Forbid();

            entry.IsPosted = !entry.IsPosted;
            await _context.SaveChangesAsync();

            return Ok(new { isPosted = entry.IsPosted });
        }

        // POST: api/Journal/toggle-pin/{id}
        [HttpPost("toggle-pin/{id}")]
        public async Task<IActionResult> TogglePin(int id, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid User ID");

            var entry = await _context.JournalEntries.FindAsync(id);
            if (entry == null) return NotFound();
            if (entry.UserId != userId) return Forbid();

            // Toggle pinning
            bool newState = !entry.IsPinned;

            if (newState)
            {
                // Unpin all other entries for this user (only one pin per user)
                var others = await _context.JournalEntries
                    .Where(j => j.UserId == userId && j.IsPinned && j.Id != id)
                    .ToListAsync();
                
                foreach (var o in others) o.IsPinned = false;
            }

            entry.IsPinned = newState;
            await _context.SaveChangesAsync();

            return Ok(new { isPinned = entry.IsPinned });
        }

        // DELETE: api/Journal/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEntry(int id, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid User ID");

            var entry = await _context.JournalEntries.FindAsync(id);
            if (entry == null) return NotFound();
            if (entry.UserId != userId) return Forbid();

            _context.JournalEntries.Remove(entry);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }

    public class JournalEntryDto
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsPosted { get; set; } = false;
        public bool IsPinned { get; set; } = false;
    }
}
