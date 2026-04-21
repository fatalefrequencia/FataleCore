using FataleCore.Data;
using FataleCore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FataleCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
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
                    id = c.Id,
                    name = c.Name,
                    description = c.Description,
                    sectorId = c.SectorId,
                    createdAt = c.CreatedAt,
                    founderName = c.Founder != null ? c.Founder.Username : "System",
                    founderId = c.FounderUserId,
                    memberCount = _context.Users.Count(u => u.CommunityId == c.Id),
                    imageUrl = c.ImageUrl
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

        public class UpdateImageDto
        {
            public string ImageUrl { get; set; } = string.Empty;
        }

        // POST: api/communities
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateCommunity([FromBody] CreateCommunityDto dto, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0)
                return Unauthorized(new { message = "Invalid or missing User ID header" });

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new { message = "User not found" });

            // Ensure user has enough credits
            int creationCost = 0; // E.g., cost to create a community
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
        public async Task<IActionResult> JoinCommunity(int id, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0)
                return Unauthorized(new { message = "Invalid or missing User ID header" });

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
        public async Task<IActionResult> LeaveCommunity([FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0)
                return Unauthorized(new { message = "Invalid or missing User ID header" });

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new { message = "User not found" });

            user.CommunityId = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Left community successfully" });
        }
        // GET: api/communities/{id}/members
        [HttpGet("{id}/members")]
        public async Task<IActionResult> GetCommunityMembers(int id)
        {
            var members = await _context.Users
                .Where(u => u.CommunityId == id)
                .Select(u => new
                {
                    id = u.Id,
                    username = u.Username,
                    profilePictureUrl = u.ProfilePictureUrl,
                    themeColor = u.ThemeColor,
                    biography = u.Biography,
                    isArtist = _context.Artists.Any(a => a.UserId == u.Id)
                })
                .ToListAsync();

            return Ok(members);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCommunity(int id, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized();

            var community = await _context.Communities
                .Include(c => c.Members)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (community == null) return NotFound();

            if (community.FounderUserId != userId)
                return Forbid("Only the founder can terminate this community.");

            // Clear community link for all members
            var members = await _context.Users.Where(u => u.CommunityId == id).ToListAsync();
            foreach (var m in members)
            {
                m.CommunityId = null;
            }

            // Remove all associated chat messages
            var messages = await _context.CommunityMessages.Where(m => m.CommunityId == id).ToListAsync();
            _context.CommunityMessages.RemoveRange(messages);

            _context.Communities.Remove(community);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Community signal terminated." });
        }

        // POST: api/communities/{id}/image
        [HttpPost("{id}/image")]
        public async Task<IActionResult> UpdateCommunityImage(int id, [FromBody] UpdateImageDto dto, [FromHeader(Name = "UserId")] int userId)
        {
            var community = await _context.Communities.FindAsync(id);
            if (community == null) return NotFound(new { message = "Community not found" });

            if (community.FounderUserId != userId)
                return Forbid("Only the founder can update the community image.");

            community.ImageUrl = dto.ImageUrl;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Image updated", imageUrl = community.ImageUrl });
        }
    }
}
