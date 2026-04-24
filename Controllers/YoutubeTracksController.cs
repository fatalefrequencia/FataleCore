using FataleCore.Data;
using FataleCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FataleCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class YoutubeTracksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public YoutubeTracksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST: api/YoutubeTracks/save
        [HttpPost("save")]
        public async Task<IActionResult> SaveTrack([FromBody] YoutubeTrack trackMetadata)
        {
            if (string.IsNullOrEmpty(trackMetadata.YoutubeId))
                return BadRequest("YoutubeId is required");

            var youtubeSource = $"youtube:{trackMetadata.YoutubeId}";

            // Check if track exists in common Tracks table
            var existingTrack = await _context.Tracks
                .FirstOrDefaultAsync(t => t.Source == youtubeSource);

            if (existingTrack != null)
            {
                // Update metadata if needed
                existingTrack.Title = trackMetadata.Title;
                existingTrack.CoverImageUrl = trackMetadata.ThumbnailUrl;
                existingTrack.PlayCount = (int)trackMetadata.ViewCount;
                existingTrack.Duration = trackMetadata.Duration; // Already a string
                
                await _context.SaveChangesAsync();
                return Ok(existingTrack);
            }

            // Assign to system archive album
            var systemAlbum = await _context.Albums.FirstOrDefaultAsync(a => a.Title == "YouTube Signals");
            if (systemAlbum == null)
            {
                var systemArtist = await _context.Artists.FirstOrDefaultAsync(a => a.Name == "The Archive");
                if (systemArtist == null) return StatusCode(500, "System Archive not initialized.");
                
                systemAlbum = new Album
                {
                    Title = "YouTube Signals",
                    ReleaseDate = DateTime.UtcNow,
                    CoverImageUrl = "https://img.youtube.com/vi/dQw4w9WgXcQ/hqdefault.jpg",
                    ArtistId = systemArtist.Id
                };
                _context.Albums.Add(systemAlbum);
                await _context.SaveChangesAsync();
            }

            // Create new entry in common Tracks table
            var newTrack = new Track
            {
                Title = trackMetadata.Title,
                Genre = "YouTube",
                Duration = trackMetadata.Duration,
                Source = youtubeSource,
                CoverImageUrl = trackMetadata.ThumbnailUrl,
                PlayCount = (int)trackMetadata.ViewCount,
                Price = 0,
                IsLocked = false,
                IsDownloadable = false,
                AlbumId = systemAlbum.Id 
            };

            _context.Tracks.Add(newTrack);
            await _context.SaveChangesAsync();

            return Ok(newTrack);
        }

        // GET: api/YoutubeTracks/by-youtube-id/{youtubeId}
        [HttpGet("by-youtube-id/{youtubeId}")]
        public async Task<IActionResult> GetByYoutubeId(string youtubeId)
        {
            var track = await _context.YoutubeTracks
                .FirstOrDefaultAsync(t => t.YoutubeId == youtubeId);

            if (track == null)
            {
                return NotFound();
            }

            return Ok(track);
        }
    }
}
