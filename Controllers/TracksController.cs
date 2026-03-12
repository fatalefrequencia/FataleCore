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
        public async Task<IActionResult> UploadTrack([FromForm] FataleCore.Dtos.TrackUploadDto dto, [FromHeader(Name = "UserId")] int userId)
        {
            try
            {
                Console.WriteLine($"[TRACK_UPLOAD] Initialized for User: {userId}");

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

                // 5. Link to User's Artist Profile
                var artist = await _context.Artists.FirstOrDefaultAsync(a => a.UserId == userId && userId != 0);
                
                if (artist == null && userId != 0)
                {
                    Console.WriteLine($"[TRACK_UPLOAD] Creating new artist profile for User: {userId}");
                    var user = await _context.Users.FindAsync(userId);
                    artist = new Artist 
                    { 
                        Name = user?.Username ?? $"User_{userId}", 
                        Bio = user?.Biography ?? "New Artist Profile", 
                        ImageUrl = $"/uploads/{coverFileName}",
                        UserId = userId
                    };
                    _context.Artists.Add(artist);
                    await _context.SaveChangesAsync();
                }

                if (artist == null)
                {
                    Console.WriteLine($"[TRACK_UPLOAD] WARNING: No artist and no userId found. Saving with fallback artist.");
                    // Last resort fallback
                     artist = await _context.Artists.FirstOrDefaultAsync(a => a.Id == 1); // System artist fallback
                     if (artist == null) return Unauthorized("User session invalid or no artist profile found.");
                }

                // Fallback to "Singles" album for the specific artist
                var defaultAlbum = await _context.Albums.FirstOrDefaultAsync(a => a.ArtistId == artist.Id && a.Title == "Singles");
                
                if (defaultAlbum == null)
                {
                    defaultAlbum = new Album { Title = "Singles", ArtistId = artist.Id, ReleaseDate = DateTime.Now, CoverImageUrl = $"/uploads/{coverFileName}" };
                    _context.Albums.Add(defaultAlbum);
                    await _context.SaveChangesAsync();
                }

                var track = new Track
                {
                    Title = dto.TrackTitle,
                    Genre = dto.Genre ?? "Unknown",
                    FilePath = $"/uploads/{audioFileName}",
                    CoverImageUrl = $"/uploads/{coverFileName}",
                    Duration = "0:00", 
                    AlbumId = defaultAlbum.Id,
                    CreatedAt = DateTime.UtcNow,
                    
                    Price = dto.Price > 0 ? 1 : 0,
                    IsLocked = dto.IsLocked,
                    IsDownloadable = true
                };

                _context.Tracks.Add(track);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[TRACK_UPLOAD] SUCCESS: Track {track.Id} saved and linked to Artist {artist.Id} (User {artist.UserId})");

                return CreatedAtAction(nameof(GetTrack), new { id = track.Id }, track);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TRACK_UPLOAD] ERROR: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Track>>> GetTracks([FromQuery] string sort = "newest")
        {
            var query = _context.Tracks
                .Where(t => !t.IsDelisted)
                .Include(t => t.Album)
                    .ThenInclude(a => a!.Artist)
                .AsQueryable();

            if (sort == "trending")
            {
                // Trending is currently PlayCount weighted by age (if we had age, but let's use PlayCount desc)
                query = query.OrderByDescending(t => t.PlayCount);
            }
            else
            {
                query = query.OrderByDescending(t => t.CreatedAt);
            }

            return await query.ToListAsync();
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

        // DELETE: api/Tracks/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTrack(int id, [FromHeader(Name = "UserId")] int requestUserId)
        {
            var track = await _context.Tracks
                .Include(t => t.Album)
                    .ThenInclude(a => a!.Artist)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (track == null) return NotFound("Track not found");

            // 1. Verify Ownership
            // Find the artist linked to this track to check their UserId
            int? trackOwnerId = track.Album?.Artist?.UserId;
            
            if (trackOwnerId == null || trackOwnerId != requestUserId)
            {
                return Unauthorized("You are not the owner of this track.");
            }

            // 2. Check for Purchases
            var hasPurchases = await _context.TrackPurchases.AnyAsync(p => p.TrackId == id);

            if (!hasPurchases)
            {
                // Permanent Delete (Safety Check: Only if NO ONE has bought it)
                _context.Tracks.Remove(track);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Track permanently removed (no existing purchases)." });
            }
            else
            {
                // Soft Delete / Delist (Safety Check: Hide from store but keep for owners)
                track.IsDelisted = true;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Track delisted from store. Existing purchasers still have access." });
            }
        }
        [HttpPost("{id}/toggle-pin")]
        public async Task<IActionResult> TogglePin(int id, [FromHeader(Name = "UserId")] int requestUserId)
        {
            var track = await _context.Tracks
                .Include(t => t.Album)
                    .ThenInclude(a => a!.Artist)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (track == null) return NotFound("Track not found");

            // Verify Ownership
            int? trackOwnerId = track.Album?.Artist?.UserId;
            if (trackOwnerId == null || trackOwnerId != requestUserId)
            {
                return Unauthorized("You are not the owner of this track.");
            }

            track.IsPinned = !track.IsPinned;
            await _context.SaveChangesAsync();

            return Ok(new { isPinned = track.IsPinned });
        }

        [HttpPost("{id}/toggle-post")]
        public async Task<IActionResult> TogglePost(int id, [FromHeader(Name = "UserId")] int requestUserId)
        {
            var track = await _context.Tracks
                .Include(t => t.Album)
                    .ThenInclude(a => a!.Artist)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (track == null) return NotFound("Track not found");

            // Verify Ownership
            int? trackOwnerId = track.Album?.Artist?.UserId;
            if (trackOwnerId == null || trackOwnerId != requestUserId)
            {
                return Unauthorized("You are not the owner of this track.");
            }

            track.IsPosted = !track.IsPosted;
            await _context.SaveChangesAsync();

            return Ok(new { isPosted = track.IsPosted });
        }
    }
}
