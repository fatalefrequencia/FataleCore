using FataleCore.Data;
using FataleCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FataleCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommunitiesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CommunitiesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/communities
        [HttpGet]
        public async Task<IActionResult> GetAllCommunities()
        {
            var communities = await _context.Communities
                .Include(c => c.Founder)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Description,
                    c.SectorId,
                    c.CreatedAt,
                    FounderName = c.Founder != null ? c.Founder.Username : "System",
                    FounderId = c.FounderUserId,
                    MemberCount = _context.Users.Count(u => u.CommunityId == c.Id)
                })
                .ToListAsync();

            return Ok(communities);
        }

        public class CreateCommunityDto
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public int SectorId { get; set; }
        }

        // POST: api/communities
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateCommunity([FromBody] CreateCommunityDto dto)
        {
            string? userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized(new { message = "Invalid token" });

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new { message = "User not found" });

            // Ensure user has enough credits
            int creationCost = 500; // E.g., cost to create a community
            if (user.CreditsBalance < creationCost)
            {
                return BadRequest(new { message = $"Insufficient credits. Requires {creationCost} credits." });
            }

            user.CreditsBalance -= creationCost;

            var community = new Community
            {
                Name = dto.Name,
                Description = dto.Description,
                SectorId = dto.SectorId,
                FounderUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Communities.Add(community);
            await _context.SaveChangesAsync();

            // Auto-join the creator to their new community
            user.CommunityId = community.Id;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Community created", communityId = community.Id });
        }

        // POST: api/communities/{id}/join
        [HttpPost("{id}/join")]
        [Authorize]
        public async Task<IActionResult> JoinCommunity(int id)
        {
            string? userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized(new { message = "Invalid token" });

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new { message = "User not found" });

            var community = await _context.Communities.FindAsync(id);
            if (community == null) return NotFound(new { message = "Community not found" });

            user.CommunityId = id;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Joined community successfully" });
        }

        // POST: api/communities/leave
        [HttpPost("leave")]
        [Authorize]
        public async Task<IActionResult> LeaveCommunity()
        {
            string? userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized(new { message = "Invalid token" });

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new { message = "User not found" });

            user.CommunityId = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Left community successfully" });
        }
    }
}
