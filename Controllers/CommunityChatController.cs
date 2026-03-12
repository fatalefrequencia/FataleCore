using FataleCore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FataleCore.Models;

namespace FataleCore.Controllers
{
    [ApiController]
    [Route("api/community-chat")]
    [AllowAnonymous]
    public class CommunityChatController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CommunityChatController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/community-chat/{communityId}?afterId=<lastMessageId>
        [HttpGet("{communityId}")]
        public async Task<IActionResult> GetMessages(int communityId, [FromQuery] int? afterId = null)
        {
            var query = _context.CommunityMessages
                .Include(m => m.Sender)
                .Where(m => m.CommunityId == communityId);

            if (afterId.HasValue)
                query = query.Where(m => m.Id > afterId.Value);

            var messages = await query
                .OrderBy(m => m.Id)
                .Take(50)
                .Select(m => new
                {
                    id = m.Id,
                    userId = m.UserId,
                    username = m.Sender != null ? m.Sender.Username : "Unknown",
                    themeColor = m.Sender != null ? m.Sender.ThemeColor : "#ff006e",
                    profilePictureUrl = m.Sender != null ? m.Sender.ProfilePictureUrl : null,
                    content = m.Content,
                    sentAt = m.SentAt
                })
                .ToListAsync();

            return Ok(messages);
        }

        // POST: api/community-chat/{communityId}
        [HttpPost("{communityId}")]
        public async Task<IActionResult> SendMessage(
            int communityId,
            [FromBody] SendMessageDto dto,
            [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0)
                return Unauthorized(new { message = "Invalid or missing User ID" });

            if (string.IsNullOrWhiteSpace(dto?.Content))
                return BadRequest(new { message = "Message content is required" });

            var content = dto.Content.Trim();
            if (content.Length > 280)
                content = content[..280];

            var message = new CommunityMessage
            {
                CommunityId = communityId,
                UserId = userId,
                Content = content,
                SentAt = DateTime.UtcNow
            };

            _context.CommunityMessages.Add(message);
            await _context.SaveChangesAsync();

            // Load sender details to return
            var sender = await _context.Users.FindAsync(userId);

            return Ok(new
            {
                id = message.Id,
                userId = message.UserId,
                username = sender?.Username ?? "Unknown",
                themeColor = sender?.ThemeColor ?? "#ff006e",
                profilePictureUrl = sender?.ProfilePictureUrl,
                content = message.Content,
                sentAt = message.SentAt
            });
        }

        public class SendMessageDto
        {
            public string Content { get; set; } = string.Empty;
        }
    }
}
