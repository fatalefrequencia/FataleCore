using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using FataleCore.Data;
using FataleCore.Models;
using FataleCore.Hubs;
using FataleCore.DTOs;

namespace FataleCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<RadioHub> _hubContext;

        public StationsController(ApplicationDbContext context, IHubContext<RadioHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // GET: api/Stations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StationDto>>> GetStations()
        {
            try 
            {
                var stations = await _context.Stations
                    .Include(s => s.Artist)
                    .Include(s => s.CurrentTrack)
                        .ThenInclude(t => t!.Album)
                            .ThenInclude(a => a!.Artist)
                    .OrderByDescending(s => s.IsLive)
                    .ThenByDescending(s => s.ListenerCount)
                    .ToListAsync();

                var dtos = stations.Select(s => new StationDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Genre = s.Genre,
                    Frequency = s.Frequency,
                    IsLive = s.IsLive,
                    CurrentSessionTitle = s.CurrentSessionTitle,
                    ListenerCount = s.ListenerCount,
                    ArtistName = s.Artist?.Name ?? "UNKNOWN",
                    ArtistUserId = s.Artist?.UserId,
                    CurrentTrack = s.CurrentTrack != null ? new StationTrackDto 
                    {
                        Id = s.CurrentTrack.Id,
                        Title = s.CurrentTrack.Title,
                        Artist = s.CurrentTrack.Album?.Artist?.ToDto()
                    } : null
                });

                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/Stations/favorites
        [HttpGet("favorites")]
        public async Task<ActionResult<IEnumerable<StationDto>>> GetFavorites([FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized();

            var favorites = await _context.StationFavorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Station)
                    .ThenInclude(s => s!.Artist)
                .Include(f => f.Station)
                    .ThenInclude(s => s!.CurrentTrack)
                        .ThenInclude(t => t!.Album)
                            .ThenInclude(a => a!.Artist)
                .ToListAsync();

            var dtos = favorites.Select(f => new StationDto
            {
                Id = f.Station!.Id,
                Name = f.Station.Name,
                Genre = f.Station.Genre,
                Frequency = f.Station.Frequency,
                IsLive = f.Station.IsLive,
                CurrentSessionTitle = f.Station.CurrentSessionTitle,
                ListenerCount = f.Station.ListenerCount,
                ArtistName = f.Station.Artist?.Name ?? "UNKNOWN",
                ArtistUserId = f.Station.Artist?.UserId,
                CurrentTrack = f.Station.CurrentTrack != null ? new StationTrackDto 
                {
                    Id = f.Station.CurrentTrack.Id,
                    Title = f.Station.CurrentTrack.Title,
                    Artist = f.Station.CurrentTrack.Album?.Artist?.ToDto()
                } : null
            });

            return Ok(dtos);
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<StationDto>> GetStationByUser(int userId)
        {
            var station = await _context.Stations
                .Include(s => s.Artist)
                .Include(s => s.CurrentTrack)
                    .ThenInclude(t => t!.Album)
                        .ThenInclude(a => a!.Artist)
                .FirstOrDefaultAsync(s => s.Artist != null && s.Artist.UserId == userId);

            if (station == null) return NotFound();

            return Ok(new StationDto
            {
                Id = station.Id,
                Name = station.Name,
                Genre = station.Genre,
                Frequency = station.Frequency,
                IsLive = station.IsLive,
                CurrentSessionTitle = station.CurrentSessionTitle,
                ListenerCount = station.ListenerCount,
                ArtistName = station.Artist?.Name ?? "UNKNOWN",
                ArtistUserId = station.Artist?.UserId,
                CurrentTrack = station.CurrentTrack != null ? new StationTrackDto 
                {
                    Id = station.CurrentTrack.Id,
                    Title = station.CurrentTrack.Title,
                    Artist = station.CurrentTrack.Album?.Artist?.ToDto()
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
            if (station == null)
            {
                var artist = await _context.Artists.FirstOrDefaultAsync(a => a.UserId == userId);
                if (artist == null) return NotFound("Artist profile required to create a station.");
                
                station = new Station {
                    Name = artist.Name + " Radio",
                    ArtistId = artist.Id,
                    Genre = "Mixed",
                    Frequency = "100.1",
                    IsLive = false
                };
                _context.Stations.Add(station);
                await _context.SaveChangesAsync();
            }

            station.IsLive = true;
            station.CurrentSessionTitle = request.SessionTitle;
            station.Description = request.Description;
            station.CurrentTrackId = null; // Radio broadcast — not tied to a single track
            station.IsChatEnabled = request.IsChatEnabled;
            station.IsQueueEnabled = request.IsQueueEnabled;
            
            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("StationWentLive", new { stationId = station.Id });

            return Ok(new { success = true, stationId = station.Id });
        }

        // POST: api/Stations/end-live
        [HttpPost("end-live")]
        public async Task<IActionResult> EndLive([FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized();

            var station = await _context.Stations.FirstOrDefaultAsync(s => s.Artist != null && s.Artist.UserId == userId);
            if (station == null) return Ok(new { success = true }); // If no station, implicitly not live

            station.IsLive = false;
            station.CurrentSessionTitle = null;
            station.Description = null;
            station.CurrentTrackId = null;

            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("StationEnded", new { stationId = station.Id });

            return Ok(new { success = true });
        }

        public class GoLiveRequest
        {
            public string SessionTitle { get; set; } = string.Empty;
            public string? Description { get; set; }
            public bool IsChatEnabled { get; set; } = true;
            public bool IsQueueEnabled { get; set; } = true;
        }
    }
}
