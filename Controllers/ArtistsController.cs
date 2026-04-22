using FataleCore.Data;
using FataleCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FataleCore.DTOs;

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
        public async Task<IActionResult> GetArtists()
        {
            var artists = await _context.Artists.Include(a => a.User).ToListAsync();
            return Ok(artists.Select(a => a.ToDto()));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ArtistDto>> GetArtist(int id)
        {
            var artist = await _context.Artists.FindAsync(id);

            if (artist == null)
            {
                return NotFound();
            }

            return artist.ToDto();
        }

        [HttpPost]
        public async Task<ActionResult<ArtistDto>> PostArtist(Artist artist)
        {
            _context.Artists.Add(artist);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetArtist", new { id = artist.Id }, artist.ToDto());
        }
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<ArtistDto>> GetByUserId(int userId)
        {
            var artist = await _context.Artists.FirstOrDefaultAsync(a => a.UserId == userId);
            if (artist == null) return NotFound();
            return artist.ToDto();
        }

        [HttpPost("like/{targetUserId}")]
        public async Task<IActionResult> LikeArtist(int targetUserId, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid User ID");

            // Find the artist profile for the target user
            var artist = await _context.Artists.FirstOrDefaultAsync(a => a.UserId == targetUserId);
            
            if (artist == null)
            {
                // Auto-create a hidden artist profile for this user so they can be followed
                 var targetUser = await _context.Users.FindAsync(targetUserId);
                 if (targetUser == null) return NotFound("Target user not found");

                 artist = new Artist 
                 { 
                     Name = targetUser.Username,
                     Bio = "New Artist Profile", // This specific string is filtered from the map
                     ImageUrl = targetUser.ProfilePictureUrl ?? "",
                     UserId = targetUser.Id
                 };
                 _context.Artists.Add(artist);
                 await _context.SaveChangesAsync();
            }

            var existingLike = await _context.UserArtistLikes
                .FirstOrDefaultAsync(l => l.UserId == userId && l.ArtistId == artist.Id);

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
                ArtistId = artist.Id,
                LikedAt = DateTime.UtcNow
            };

            _context.UserArtistLikes.Add(like);
            await _context.SaveChangesAsync();

            return Ok(new { liked = true });
        }
        
        
        [HttpGet("like/check/{targetUserId}")]
        public async Task<IActionResult> CheckArtistLike(int targetUserId, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Ok(new { liked = false });

            // Find the artist profile for the target user
            var artist = await _context.Artists.FirstOrDefaultAsync(a => a.UserId == targetUserId);
            if (artist == null) return Ok(new { liked = false });

            var isLiked = await _context.UserArtistLikes
                .AnyAsync(l => l.UserId == userId && l.ArtistId == artist.Id);

            return Ok(new { liked = isLiked });
        }
        [HttpGet("liked")]
        public async Task<ActionResult<IEnumerable<ArtistDto>>> GetLikedArtists([FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid User ID");

            var likedArtists = await _context.UserArtistLikes
                .Where(l => l.UserId == userId)
                .Include(l => l.Artist)
                .ThenInclude(a => a != null ? a.User : null)
                .Where(l => l.Artist != null)
                .Select(l => l.Artist!)
                .ToListAsync();

            return Ok(likedArtists.Select(a => a.ToDto()));
        }
    }
}
