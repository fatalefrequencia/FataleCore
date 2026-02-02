using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FataleCore.Data;
using FataleCore.Models;
using System.Security.Claims;

namespace FataleCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MessagesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Messages/conversations
        [HttpGet("conversations")]
        public async Task<ActionResult<IEnumerable<object>>> GetConversations([FromHeader(Name = "UserId")] int userId)
        {
            // Get all messages where the user is either sender or receiver
            var messages = await _context.Messages
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .OrderByDescending(m => m.Timestamp)
                .ToListAsync();

            // Group by the "other" user ID to find unique conversations
            var conversations = messages
                .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                .Select(g => new
                {
                    OtherUserId = g.Key,
                    LatestMessage = g.First(),
                    UnreadCount = g.Count(m => m.ReceiverId == userId && !m.IsRead)
                })
                .ToList();

            // Fetch user details for each conversation partner
            var result = new List<object>();
            foreach (var conv in conversations)
            {
                var otherUser = await _context.Users.FindAsync(conv.OtherUserId);
                result.Add(new
                {
                    UserId = conv.OtherUserId,
                    Username = otherUser?.Username ?? "Unknown",
                    ProfileImageUrl = otherUser?.ProfilePictureUrl ?? "",
                    Content = conv.LatestMessage.Content,
                    Timestamp = conv.LatestMessage.Timestamp,
                    UnreadCount = conv.UnreadCount
                });
            }

            return result;
        }

        // GET: api/Messages/conversation/{otherUserId}
        [HttpGet("conversation/{otherUserId}")]
        public async Task<ActionResult<IEnumerable<Message>>> GetConversation(int otherUserId, [FromHeader(Name = "UserId")] int userId)
        {
            var history = await _context.Messages
                .Where(m => (m.SenderId == userId && m.ReceiverId == otherUserId) ||
                            (m.SenderId == otherUserId && m.ReceiverId == userId))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            // Mark as read
            var unread = history.Where(m => m.ReceiverId == userId && !m.IsRead).ToList();
            if (unread.Any())
            {
                foreach (var m in unread) m.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return history;
        }

        // POST: api/Messages/send
        [HttpPost("send")]
        public async Task<ActionResult<Message>> SendMessage([FromBody] MessageDto dto, [FromHeader(Name = "UserId")] int userId)
        {
            var message = new Message
            {
                SenderId = userId,
                ReceiverId = dto.ReceiverId,
                Content = dto.Content,
                Timestamp = DateTime.UtcNow,
                IsRead = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return Ok(message);
        }
    }

    public class MessageDto
    {
        public int ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
