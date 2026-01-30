using FataleCore.Data;
using FataleCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FataleCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ArtistsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ArtistsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Artist>>> GetArtists()
        {
            return await _context.Artists.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Artist>> GetArtist(int id)
        {
            var artist = await _context.Artists.FindAsync(id);

            if (artist == null)
            {
                return NotFound();
            }

            return artist;
        }

        [HttpPost]
        public async Task<ActionResult<Artist>> PostArtist(Artist artist)
        {
            _context.Artists.Add(artist);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetArtist", new { id = artist.Id }, artist);
        }
        [HttpPost("like/{artistId}")]
        public async Task<IActionResult> LikeArtist(int artistId, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid User ID");

            var existingLike = await _context.UserArtistLikes
                .FirstOrDefaultAsync(l => l.UserId == userId && l.ArtistId == artistId);

            if (existingLike != null)
            {
                // Unlike if already liked
                _context.UserArtistLikes.Remove(existingLike);
                await _context.SaveChangesAsync();
                return Ok(new { liked = false });
            }

            var like = new UserArtistLike
            {
                UserId = userId,
                ArtistId = artistId,
                LikedAt = DateTime.UtcNow
            };

            _context.UserArtistLikes.Add(like);
            await _context.SaveChangesAsync();

            return Ok(new { liked = true });
        }
        
        [HttpGet("like/check/{artistId}")]
        public async Task<IActionResult> CheckArtistLike(int artistId, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Ok(new { liked = false });

            var isLiked = await _context.UserArtistLikes
                .AnyAsync(l => l.UserId == userId && l.ArtistId == artistId);

            return Ok(new { liked = isLiked });
        }
        [HttpGet("liked")]
        public async Task<ActionResult<IEnumerable<Artist>>> GetLikedArtists([FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid User ID");

            var likedArtists = await _context.UserArtistLikes
                .Where(l => l.UserId == userId)
                .Include(l => l.Artist)
                .Where(l => l.Artist != null)
                .Select(l => l.Artist!)
                .ToListAsync();

            return likedArtists;
        }
    }
}
