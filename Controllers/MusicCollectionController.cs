using FataleCore.Data;
using FataleCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FataleCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MusicCollectionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MusicCollectionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/musiccollection/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<Playlist>>> GetUserPlaylists(int userId)
        {
            // For now, return real playlists
            var playlists = await _context.Playlists
                .Where(p => p.UserId == userId || p.IsPublic)
                .ToListAsync();

            // If empty, maybe return some defaults or empty list?
            return playlists;
        }

        // POST: api/musiccollection
        [HttpPost]
        public async Task<ActionResult<Playlist>> CreatePlaylist([FromBody] Playlist playlist, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized("Invalid User ID");

            playlist.UserId = userId;
            // Default image if none
            if (string.IsNullOrEmpty(playlist.ImageUrl)) 
            {
                 playlist.ImageUrl = "uploads/default_playlist.png"; // Or a valid default
            }

            _context.Playlists.Add(playlist);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUserPlaylists", new { userId = userId }, playlist);
        }
    }
}
