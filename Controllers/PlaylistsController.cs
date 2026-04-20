using FataleCore.Data;
using FataleCore.DTOs;
using FataleCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FataleCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlaylistsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PlaylistsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Playlists
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PlaylistDto>>> GetTrendingPlaylists()
        {
            var playlists = await _context.Playlists
                .Where(p => p.IsPublic)
                .OrderByDescending(p => p.TrackCount)
                .Take(20)
                .ToListAsync();

            return Ok(playlists.Select(p => p.ToDto()));
        }

        // GET: api/Playlists/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<PlaylistDto>>> GetUserPlaylists(int userId)
        {
            var playlists = await _context.Playlists
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            return Ok(playlists.Select(p => p.ToDto()));
        }

        // GET: api/Playlists/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetPlaylist(int id)
        {
            var playlist = await _context.Playlists.FindAsync(id);
            if (playlist == null) return NotFound();

            var tracks = await _context.PlaylistTracks
                .Where(pt => pt.PlaylistId == id)
                .Include(pt => pt.Track)
                    .ThenInclude(t => t!.Album)
                        .ThenInclude(a => a!.Artist)
                .Select(pt => pt.Track)
                .ToListAsync();

            return Ok(new
            {
                Playlist = playlist.ToDto(),
                Tracks = tracks.Where(t => t != null).Select(t => t!.ToDto())
            });
        }

        // POST: api/Playlists
        [HttpPost]
        public async Task<ActionResult<PlaylistDto>> CreatePlaylist([FromBody] CreatePlaylistDto dto, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid User ID");

            var playlist = new Playlist
            {
                UserId = userId,
                Name = dto.Name,
                Description = dto.Description,
                IsPublic = dto.IsPublic,
                TrackCount = 0
            };

            _context.Playlists.Add(playlist);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPlaylist), new { id = playlist.Id }, playlist.ToDto());
        }

        // PUT: api/Playlists/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePlaylist(int id, [FromBody] UpdatePlaylistDto dto, [FromHeader(Name = "UserId")] int userId)
        {
            var playlist = await _context.Playlists.FindAsync(id);
            if (playlist == null) return NotFound();
            if (playlist.UserId != userId) return Forbid();

            playlist.Name = dto.Name;
            playlist.Description = dto.Description;
            playlist.IsPublic = dto.IsPublic;

            await _context.SaveChangesAsync();
            return Ok(playlist.ToDto());
        }

        // POST: api/Playlists/{id}/tracks
        [HttpPost("{id}/tracks")]
        public async Task<IActionResult> AddTrackToPlaylist(int id, [FromBody] AddTrackDto dto, [FromHeader(Name = "UserId")] int userId)
        {
            var playlist = await _context.Playlists.FindAsync(id);
            if (playlist == null) return NotFound("Playlist not found");

            if (playlist.UserId != userId) return Forbid();

            // Check if track exists
            var track = await _context.Tracks.FindAsync(dto.TrackId);
            if (track == null) return NotFound("Track not found");

            // Check if already in playlist
            var existing = await _context.PlaylistTracks
                .FirstOrDefaultAsync(pt => pt.PlaylistId == id && pt.TrackId == dto.TrackId);
            
            if (existing != null) return Conflict("Track already in playlist");

            var playlistTrack = new PlaylistTrack
            {
                PlaylistId = id,
                TrackId = dto.TrackId
            };

            _context.PlaylistTracks.Add(playlistTrack);
            
            // Update count
            playlist.TrackCount++;
            
            // If playlist image is empty, use this track's cover
            if (string.IsNullOrEmpty(playlist.ImageUrl) && !string.IsNullOrEmpty(track.CoverImageUrl))
            {
                playlist.ImageUrl = track.CoverImageUrl;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Track added" });
        }

        // DELETE: api/Playlists/{id}/tracks/{trackId}
        [HttpDelete("{id}/tracks/{trackId}")]
        public async Task<IActionResult> RemoveTrackFromPlaylist(int id, int trackId, [FromHeader(Name = "UserId")] int userId)
        {
            var playlist = await _context.Playlists.FindAsync(id);
            if (playlist == null) return NotFound();
            if (playlist.UserId != userId) return Forbid();

            var item = await _context.PlaylistTracks
                .FirstOrDefaultAsync(pt => pt.PlaylistId == id && pt.TrackId == trackId);
            
            if (item == null) return NotFound("Track not in playlist");

            _context.PlaylistTracks.Remove(item);
            playlist.TrackCount = Math.Max(0, playlist.TrackCount - 1);
            
            await _context.SaveChangesAsync();

            return Ok(new { message = "Track removed" });
        }

        // POST: api/Playlists/{id}/toggle-pin
        [HttpPost("{id}/toggle-pin")]
        public async Task<IActionResult> TogglePin(int id, [FromHeader(Name = "UserId")] int userId)
        {
            var playlist = await _context.Playlists.FindAsync(id);
            if (playlist == null) return NotFound();
            if (playlist.UserId != userId) return Forbid();

            playlist.IsPinned = !playlist.IsPinned;
            await _context.SaveChangesAsync();
            return Ok(new { isPinned = playlist.IsPinned });
        }

        // POST: api/Playlists/{id}/toggle-post
        [HttpPost("{id}/toggle-post")]
        public async Task<IActionResult> TogglePost(int id, [FromHeader(Name = "UserId")] int userId)
        {
            var playlist = await _context.Playlists.FindAsync(id);
            if (playlist == null) return NotFound();
            if (playlist.UserId != userId) return Forbid();

            playlist.IsPosted = !playlist.IsPosted;
            await _context.SaveChangesAsync();
            return Ok(new { isPosted = playlist.IsPosted });
        }

        // DELETE: api/Playlists/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlaylist(int id, [FromHeader(Name = "UserId")] int userId)
        {
            var playlist = await _context.Playlists.FindAsync(id);
            if (playlist == null) return NotFound();
            if (playlist.UserId != userId) return Forbid();

            // Remove tracks first
            var tracks = await _context.PlaylistTracks.Where(pt => pt.PlaylistId == id).ToListAsync();
            _context.PlaylistTracks.RemoveRange(tracks);

            _context.Playlists.Remove(playlist);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Playlists/{id}/upload-cover
        [HttpPost("{id}/upload-cover")]
        public async Task<IActionResult> UploadCover(int id, IFormFile file, [FromHeader(Name = "UserId")] int userId, [FromServices] IWebHostEnvironment environment)
        {
            var playlist = await _context.Playlists.FindAsync(id);
            if (playlist == null) return NotFound();
            if (playlist.UserId != userId) return Forbid();

            if (file == null || file.Length == 0)
                return BadRequest("No file provided.");

            var appBase = environment.IsProduction() ? "/data" : Directory.GetCurrentDirectory();
            var uploadsPath = Path.Combine(appBase, "uploads");
            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            playlist.ImageUrl = $"/uploads/{fileName}";
            await _context.SaveChangesAsync();

            return Ok(new { imageUrl = playlist.ImageUrl });
        }
    }
}
