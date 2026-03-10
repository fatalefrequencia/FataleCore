using FataleCore.Data;
using FataleCore.Dtos;
using FataleCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FataleCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlbumsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public AlbumsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Album>>> GetAlbums()
        {
            return await _context.Albums.Include(a => a.Artist).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Album>> GetAlbum(int id)
        {
            var album = await _context.Albums.Include(a => a.Artist).FirstOrDefaultAsync(a => a.Id == id);

            if (album == null)
            {
                return NotFound();
            }

            return album;
        }

        [HttpPost]
        public async Task<ActionResult<Album>> PostAlbum(Album album)
        {
            _context.Albums.Add(album);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetAlbum", new { id = album.Id }, album);
        }

        /// <summary>
        /// Upload a full album with multiple tracks, each with individual encryption and pricing.
        /// Tracks are passed as indexed form fields: Tracks[0].Title, Tracks[0].AudioFile, etc.
        /// </summary>
        [HttpPost("upload-full")]
        public async Task<IActionResult> UploadAlbum([FromForm] AlbumUploadDto dto, [FromHeader(Name = "UserId")] int userId)
        {
            try
            {
                Console.WriteLine($"[ALBUM_UPLOAD] Initialized for User: {userId}, Album: '{dto.AlbumTitle}'");

                if (string.IsNullOrWhiteSpace(dto.AlbumTitle))
                    return BadRequest("Album title is required.");

                if (dto.Tracks == null || dto.Tracks.Count == 0)
                    return BadRequest("At least one track is required.");

                // Validate all track audio files are present
                for (int i = 0; i < dto.Tracks.Count; i++)
                {
                    if (dto.Tracks[i].AudioFile == null || dto.Tracks[i].AudioFile.Length == 0)
                        return BadRequest($"Track {i + 1} is missing an audio file.");
                    if (string.IsNullOrWhiteSpace(dto.Tracks[i].Title))
                        return BadRequest($"Track {i + 1} is missing a title.");
                }

                // Prepare uploads directory
                var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads");
                if (!Directory.Exists(uploadsPath))
                    Directory.CreateDirectory(uploadsPath);

                // Resolve or create artist profile (same pattern as TracksController)
                var artist = await _context.Artists.FirstOrDefaultAsync(a => a.UserId == userId && userId != 0);

                if (artist == null && userId != 0)
                {
                    Console.WriteLine($"[ALBUM_UPLOAD] Creating new artist profile for User: {userId}");
                    var user = await _context.Users.FindAsync(userId);
                    artist = new Artist
                    {
                        Name = user?.Username ?? $"User_{userId}",
                        Bio = user?.Biography ?? "New Artist Profile",
                        ImageUrl = string.Empty,
                        UserId = userId
                    };
                    _context.Artists.Add(artist);
                    await _context.SaveChangesAsync();
                }

                if (artist == null)
                    return Unauthorized("No artist profile found and no valid user session.");

                // Save album cover image (if provided)
                string albumCoverUrl = string.Empty;
                if (dto.CoverImage != null && dto.CoverImage.Length > 0)
                {
                    var coverFileName = $"{Guid.NewGuid()}_{dto.CoverImage.FileName}";
                    var coverFilePath = Path.Combine(uploadsPath, coverFileName);
                    using (var stream = new FileStream(coverFilePath, FileMode.Create))
                    {
                        await dto.CoverImage.CopyToAsync(stream);
                    }
                    albumCoverUrl = $"/uploads/{coverFileName}";
                }

                // Create Album record
                var album = new Album
                {
                    Title = dto.AlbumTitle,
                    ArtistId = artist.Id,
                    ReleaseDate = DateTime.UtcNow,
                    CoverImageUrl = albumCoverUrl
                };
                _context.Albums.Add(album);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[ALBUM_UPLOAD] Album '{album.Title}' created (ID: {album.Id})");

                // Create tracks and link to album
                var createdTracks = new List<Track>();
                for (int i = 0; i < dto.Tracks.Count; i++)
                {
                    var trackDto = dto.Tracks[i];

                    // Save audio file
                    var audioFileName = $"{Guid.NewGuid()}_{trackDto.AudioFile.FileName}";
                    var audioFilePath = Path.Combine(uploadsPath, audioFileName);
                    using (var stream = new FileStream(audioFilePath, FileMode.Create))
                    {
                        await trackDto.AudioFile.CopyToAsync(stream);
                    }

                    // Per-track cover (override album cover if provided)
                    string trackCoverUrl = albumCoverUrl;
                    if (trackDto.CoverImage != null && trackDto.CoverImage.Length > 0)
                    {
                        var trackCoverFileName = $"{Guid.NewGuid()}_{trackDto.CoverImage.FileName}";
                        var trackCoverFilePath = Path.Combine(uploadsPath, trackCoverFileName);
                        using (var stream = new FileStream(trackCoverFilePath, FileMode.Create))
                        {
                            await trackDto.CoverImage.CopyToAsync(stream);
                        }
                        trackCoverUrl = $"/uploads/{trackCoverFileName}";
                    }

                    var track = new Track
                    {
                        Title = trackDto.Title,
                        Genre = trackDto.Genre ?? "Unknown",
                        FilePath = $"/uploads/{audioFileName}",
                        CoverImageUrl = trackCoverUrl,
                        Duration = "0:00",
                        AlbumId = album.Id,
                        CreatedAt = DateTime.UtcNow,
                        Price = trackDto.Price,
                        IsLocked = trackDto.IsLocked,
                        IsDownloadable = true
                    };

                    _context.Tracks.Add(track);
                    createdTracks.Add(track);
                    Console.WriteLine($"[ALBUM_UPLOAD] Track '{track.Title}' queued (Locked: {track.IsLocked}, Price: {track.Price})");
                }

                await _context.SaveChangesAsync();

                Console.WriteLine($"[ALBUM_UPLOAD] SUCCESS: Album {album.Id} with {createdTracks.Count} tracks saved for Artist {artist.Id} (User {userId})");

                return CreatedAtAction("GetAlbum", new { id = album.Id }, new
                {
                    album.Id,
                    album.Title,
                    album.CoverImageUrl,
                    TrackCount = createdTracks.Count,
                    Tracks = createdTracks.Select(t => new { t.Id, t.Title, t.IsLocked, t.Price })
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ALBUM_UPLOAD] ERROR: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
