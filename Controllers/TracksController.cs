using FataleCore.Data;
using FataleCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FataleCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TracksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public TracksController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpPost("upload-full")]
        public async Task<IActionResult> UploadTrack([FromForm] FataleCore.Dtos.TrackUploadDto dto)
        {
            try
            {
                // 1. Basic Validation
                if (dto.AudioFile == null || dto.AudioFile.Length == 0)
                    return BadRequest("Audio file is empty.");
                if (dto.CoverImage == null || dto.CoverImage.Length == 0)
                    return BadRequest("Cover image is empty.");

                // 2. Prepare Uploads Directory
                var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads");
                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                // 3. Save Audio File
                var audioFileName = $"{Guid.NewGuid()}_{dto.AudioFile.FileName}";
                var audioFilePath = Path.Combine(uploadsPath, audioFileName);
                using (var stream = new FileStream(audioFilePath, FileMode.Create))
                {
                    await dto.AudioFile.CopyToAsync(stream);
                }

                // 4. Save Cover Image
                var coverFileName = $"{Guid.NewGuid()}_{dto.CoverImage.FileName}";
                var coverFilePath = Path.Combine(uploadsPath, coverFileName);
                using (var stream = new FileStream(coverFilePath, FileMode.Create))
                {
                    await dto.CoverImage.CopyToAsync(stream);
                }

                // 5. Create Track Entity
                // NOTE: For now, we associate with a default "Singles" album if no AlbumId is provided.
                // Or simplified: We just allow 0 if DB constraints permit, or create a placeholder.
                // Assuming we need a valid AlbumId, we'll try to find a generic "Singles" album or create it.
                // For simplicity of this specific user request, we will check if ANY album exists, if not create one.
                
                var defaultAlbum = await _context.Albums.FirstOrDefaultAsync(a => a.Title == "Singles");
                if (defaultAlbum == null)
                {
                    // Ensure there's an artist too
                    var defaultArtist = await _context.Artists.FirstOrDefaultAsync(a => a.Name == "Unknown Artist");
                    if (defaultArtist == null)
                    {
                        defaultArtist = new Artist { Name = "Unknown Artist", Bio = "Auto-generated", ImageUrl = $"/uploads/{coverFileName}" };
                        _context.Artists.Add(defaultArtist);
                        await _context.SaveChangesAsync();
                    }

                    defaultAlbum = new Album { Title = "Singles", ArtistId = defaultArtist.Id, ReleaseDate = DateTime.Now, CoverImageUrl = $"/uploads/{coverFileName}" };
                    _context.Albums.Add(defaultAlbum);
                    await _context.SaveChangesAsync();
                }

                var track = new Track
                {
                    Title = dto.TrackTitle,
                    Genre = dto.Genre,
                    FilePath = $"/uploads/{audioFileName}",
                    CoverImageUrl = $"/uploads/{coverFileName}",
                    Duration = "0:00", // Would need a library to calculate duration
                    AlbumId = defaultAlbum.Id
                };

                _context.Tracks.Add(track);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetTrack), new { id = track.Id }, track);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Track>>> GetTracks()
        {
            return await _context.Tracks.Include(t => t.Album).ThenInclude(a => a.Artist).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Track>> GetTrack(int id)
        {
            var track = await _context.Tracks.Include(t => t.Album).ThenInclude(a => a!.Artist).FirstOrDefaultAsync(t => t.Id == id);

            if (track == null)
            {
                return NotFound();
            }

            return track;
        }

        [HttpGet("{id}/stream")]
        public async Task<IActionResult> GetTrackStream(int id)
        {
            var track = await _context.Tracks.FindAsync(id);
            if (track == null) return NotFound();

            var filePath = Path.Combine(_environment.ContentRootPath, track.FilePath.TrimStart('/').Replace("/", "\\"));
            
            // Handle if file path is stored as relative or absolute, normalize it
            if (!System.IO.File.Exists(filePath))
            {
                 // Fallback: try removing "uploads" prefix if it's double
                 filePath = Path.Combine(_environment.ContentRootPath, "uploads", Path.GetFileName(track.FilePath));
                 if (!System.IO.File.Exists(filePath)) return NotFound("File not found on server");
            }

            return PhysicalFile(filePath, "audio/mpeg", enableRangeProcessing: true);
        }

        [HttpPost]
        public async Task<ActionResult<Track>> PostTrack(Track track)
        {
            _context.Tracks.Add(track);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTrack", new { id = track.Id }, track);
        }
    }
}
