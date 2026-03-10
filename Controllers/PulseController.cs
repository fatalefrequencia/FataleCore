using FataleCore.Data;
using FataleCore.Models;
using FataleCore.Services.Intelligence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FataleCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PulseController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IIntelligenceService _intelligenceService;

        private static readonly string[] _vocabulary = new[]
        {
            "pop", "rock", "rap", "hip hop", "r&b", "electronic", "dance", "house", "techno",
            "indie", "alternative", "metal", "punk", "jazz", "blues", "country", "folk",
            "classical", "ambient", "latin", "reggaeton", "k-pop", "lo-fi", "synth", "wave",
            "vaporwave", "chill", "acoustic", "instrumental", "vocal"
        };

        public PulseController(ApplicationDbContext context, IIntelligenceService intelligenceService)
        {
            _context = context;
            _intelligenceService = intelligenceService;
        }

        /// <summary>
        /// GET /api/pulse/neuro-graph
        /// Returns the user's current taste vector as weighted nodes.
        /// </summary>
        [HttpGet("neuro-graph")]
        public async Task<IActionResult> GetNeuroGraph([FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("UserId is required.");

            var history = await _context.UserListeningEvents
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.ListenedAt)
                .Take(100)
                .ToListAsync();

            if (!history.Any())
                return Ok(new { nodes = Array.Empty<object>() });

            var userVector = _intelligenceService.BuildUserTasteVector(history);

            // Build nodes from vocabulary + weights
            var nodes = _vocabulary
                .Select((tag, i) => new { tag, weight = userVector[i] })
                .Where(n => n.weight > 0.01f) // Only meaningful nodes
                .OrderByDescending(n => n.weight)
                .Take(10)
                .Select(n => new
                {
                    tag = n.tag,
                    weight = Math.Round(n.weight, 3),
                    category = GetCategory(n.tag)
                })
                .ToList();

            return Ok(new { userId, nodes, totalTracksAnalyzed = history.Count });
        }

        /// <summary>
        /// GET /api/pulse/resonant-stations?topTag=electronic
        /// Returns live human stations matching the given vibe tag.
        /// </summary>
        [HttpGet("resonant-stations")]
        public async Task<IActionResult> GetResonantStations([FromQuery] string topTag, [FromHeader(Name = "UserId")] int userId)
        {
            if (string.IsNullOrWhiteSpace(topTag)) return BadRequest("topTag is required.");

            var lowerTag = topTag.ToLowerInvariant();

            var liveStations = await _context.Stations
                .Include(s => s.Artist)
                .Where(s => s.IsLive && (
                    s.Genre.ToLower().Contains(lowerTag) ||
                    s.Name.ToLower().Contains(lowerTag) ||
                    (s.Description != null && s.Description.ToLower().Contains(lowerTag)) ||
                    (s.CurrentSessionTitle != null && s.CurrentSessionTitle.ToLower().Contains(lowerTag))
                ))
                .OrderByDescending(s => s.ListenerCount)
                .Take(5)
                .Select(s => new
                {
                    stationId = s.Id,
                    name = s.Name,
                    genre = s.Genre,
                    frequency = s.Frequency,
                    listenerCount = s.ListenerCount,
                    sessionTitle = s.CurrentSessionTitle,
                    djName = s.Artist != null ? s.Artist.Name : "Unknown DJ",
                    artistId = s.ArtistId
                })
                .ToListAsync();

            // If no exact matches, show all live stations as fallback
            if (!liveStations.Any())
            {
                liveStations = await _context.Stations
                    .Include(s => s.Artist)
                    .Where(s => s.IsLive)
                    .OrderByDescending(s => s.ListenerCount)
                    .Take(3)
                    .Select(s => new
                    {
                        stationId = s.Id,
                        name = s.Name,
                        genre = s.Genre,
                        frequency = s.Frequency,
                        listenerCount = s.ListenerCount,
                        sessionTitle = s.CurrentSessionTitle,
                        djName = s.Artist != null ? s.Artist.Name : "Unknown DJ",
                        artistId = s.ArtistId
                    })
                    .ToListAsync();
            }

            return Ok(new
            {
                topTag,
                matchCount = liveStations.Count,
                stations = liveStations
            });
        }

        private static string GetCategory(string tag)
        {
            var electronic = new[] { "electronic", "dance", "house", "techno", "synth", "wave", "vaporwave", "lo-fi", "ambient" };
            var urban = new[] { "rap", "hip hop", "r&b", "reggaeton" };
            var organic = new[] { "rock", "indie", "alternative", "folk", "country", "acoustic", "blues", "jazz" };
            if (electronic.Contains(tag)) return "electronic";
            if (urban.Contains(tag)) return "urban";
            if (organic.Contains(tag)) return "organic";
            return "other";
        }
    }
}
