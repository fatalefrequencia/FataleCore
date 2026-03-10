using FataleCore.Data;
using FataleCore.Models;
using Microsoft.AspNetCore.Mvc;

namespace FataleCore.Controllers
{
    [ApiController]
    [Route("api/listening-events")]
    public class ListeningEventsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ListeningEventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public class CreateEventDto
        {
            public string TrackType { get; set; } = string.Empty;
            public string TrackId { get; set; } = string.Empty;
            public string TrackTitle { get; set; } = string.Empty;
            public string Tags { get; set; } = string.Empty;
            public int DurationSeconds { get; set; }
            public string Source { get; set; } = "queue";
        }

        [HttpPost]
        public async Task<IActionResult> LogEvent([FromBody] CreateEventDto dto, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid UserId header.");
            if (string.IsNullOrWhiteSpace(dto.TrackId)) return BadRequest("TrackId is required");

            var listenedEvent = new UserListeningEvent
            {
                UserId = userId,
                TrackType = dto.TrackType,
                TrackId = dto.TrackId,
                TrackTitle = dto.TrackTitle,
                Tags = dto.Tags,
                DurationSeconds = dto.DurationSeconds,
                Source = dto.Source,
                ListenedAt = DateTime.UtcNow
            };

            _context.UserListeningEvents.Add(listenedEvent);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Listening event logged.", eventId = listenedEvent.Id });
        }

        public class UpdateDurationDto
        {
            public int DurationSeconds { get; set; }
        }

        [HttpPut("{id}/duration")]
        public async Task<IActionResult> UpdateEventDuration(int id, [FromBody] UpdateDurationDto dto, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid UserId header.");
            
            var listenedEvent = await _context.UserListeningEvents.FindAsync(id);
            if (listenedEvent == null) return NotFound("Event not found.");
            if (listenedEvent.UserId != userId) return Forbid();

            if (dto.DurationSeconds > listenedEvent.DurationSeconds)
            {
                listenedEvent.DurationSeconds = dto.DurationSeconds;
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Duration updated." });
        }
    }
}
