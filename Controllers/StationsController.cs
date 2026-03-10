using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FataleCore.Data;
using FataleCore.Models;

namespace FataleCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public StationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Stations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetStations()
        {
            try 
            {
                var stations = await _context.Stations
                    .Include(s => s.Artist)
                    .Include(s => s.CurrentTrack)
                        .ThenInclude(t => t.Album)
                            .ThenInclude(a => a.Artist)
                    .OrderByDescending(s => s.IsLive)
                    .ThenByDescending(s => s.ListenerCount)
                    .Select(s => new {
                        s.Id,
                        s.Name,
                        s.Genre,
                        s.Frequency,
                        s.IsLive,
                        s.CurrentSessionTitle,
                        s.ListenerCount,
                        ArtistName = s.Artist != null ? s.Artist.Name : "UNKNOWN",
                        CurrentTrack = s.CurrentTrack != null ? new {
                            s.CurrentTrack.Id,
                            s.CurrentTrack.Title,
                            Artist = s.CurrentTrack.Album != null ? s.CurrentTrack.Album.Artist : null
                        } : null
                    })
                    .ToListAsync();

                return Ok(stations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/Stations/favorites
        [HttpGet("favorites")]
        public async Task<ActionResult<IEnumerable<object>>> GetFavorites([FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized();

            var favorites = await _context.StationFavorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Station)
                    .ThenInclude(s => s!.Artist)
                .Include(f => f.Station)
                    .ThenInclude(s => s!.CurrentTrack)
                .Select(f => new {
                    f.Station!.Id,
                    f.Station.Name,
                    f.Station.Genre,
                    f.Station.Frequency,
                    f.Station.IsLive,
                    f.Station.CurrentSessionTitle,
                    f.Station.ListenerCount,
                    ArtistName = f.Station.Artist != null ? f.Station.Artist.Name : "UNKNOWN",
                    CurrentTrack = f.Station.CurrentTrack != null ? new {
                        f.Station.CurrentTrack.Id,
                        f.Station.CurrentTrack.Title,
                        Artist = f.Station.CurrentTrack.Album != null ? f.Station.CurrentTrack.Album.Artist : null
                    } : null
                })
                .ToListAsync();

            return Ok(favorites);
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<object>> GetStationByUser(int userId)
        {
            var station = await _context.Stations
                .Include(s => s.Artist)
                .Include(s => s.CurrentTrack)
                    .ThenInclude(t => t.Album)
                        .ThenInclude(a => a.Artist)
                .FirstOrDefaultAsync(s => s.Artist != null && s.Artist.UserId == userId);

            if (station == null) return NotFound();

            return Ok(new {
                station.Id,
                station.Name,
                station.Genre,
                station.Frequency,
                station.IsLive,
                station.CurrentSessionTitle,
                station.ListenerCount,
                ArtistName = station.Artist != null ? station.Artist.Name : "UNKNOWN",
                CurrentTrack = station.CurrentTrack != null ? new {
                    station.CurrentTrack.Id,
                    station.CurrentTrack.Title,
                    Artist = station.CurrentTrack.Album != null ? station.CurrentTrack.Album.Artist : null
                } : null
            });
        }

        // POST: api/Stations/favorite/{id}
        [HttpPost("favorite/{id}")]
        public async Task<IActionResult> ToggleFavorite(int id, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized();

            var existing = await _context.StationFavorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.StationId == id);

            if (existing != null)
            {
                _context.StationFavorites.Remove(existing);
                await _context.SaveChangesAsync();
                return Ok(new { favorited = false });
            }

            var fav = new StationFavorite
            {
                UserId = userId,
                StationId = id
            };

            _context.StationFavorites.Add(fav);
            await _context.SaveChangesAsync();

            return Ok(new { favorited = true });
        }

        // POST: api/Stations/go-live
        [HttpPost("go-live")]
        public async Task<IActionResult> GoLive([FromBody] GoLiveRequest request, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized();

            var station = await _context.Stations.FirstOrDefaultAsync(s => s.Artist != null && s.Artist.UserId == userId);
            if (station == null) return NotFound("No station found for this artist.");

            station.IsLive = true;
            station.CurrentSessionTitle = request.SessionTitle;
            station.Description = request.Description;
            station.CurrentTrackId = null; // Radio broadcast — not tied to a single track
            
            await _context.SaveChangesAsync();

            return Ok(new { success = true, stationId = station.Id });
        }

        // POST: api/Stations/end-live
        [HttpPost("end-live")]
        public async Task<IActionResult> EndLive([FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized();

            var station = await _context.Stations.FirstOrDefaultAsync(s => s.Artist != null && s.Artist.UserId == userId);
            if (station == null) return NotFound();

            station.IsLive = false;
            station.CurrentSessionTitle = null;
            station.Description = null;
            station.CurrentTrackId = null;

            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        public class GoLiveRequest
        {
            public string SessionTitle { get; set; } = string.Empty;
            public string? Description { get; set; }
        }
    }
}
