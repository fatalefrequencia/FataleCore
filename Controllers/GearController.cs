using FataleCore.Data;
using FataleCore.DTOs;
using FataleCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FataleCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GearController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public GearController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Gear/user/5
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<GearDto>>> GetUserGear(int userId)
        {
            var gear = await _context.UserGear
                .Where(g => g.UserId == userId)
                .OrderBy(g => g.DisplayOrder)
                .ToListAsync();

            return Ok(gear.Select(g => g.ToDto()));
        }

        // GET: api/Gear/5
        [HttpGet("{id}")]
        public async Task<ActionResult<GearDto>> GetGear(int id)
        {
            var gear = await _context.UserGear.FindAsync(id);

            if (gear == null) return NotFound();

            return Ok(gear.ToDto());
        }

        // POST: api/Gear
        [HttpPost]
        public async Task<ActionResult<GearDto>> PostGear([FromBody] CreateGearDto dto, [FromHeader(Name = "UserId")] int userId)
        {
            if (userId == 0) return Unauthorized("User session invalid.");

            var gear = new UserGear
            {
                UserId = userId,
                Name = dto.Name,
                Category = dto.Category,
                Notes = dto.Notes,
                DisplayOrder = dto.DisplayOrder
            };

            _context.UserGear.Add(gear);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGear), new { id = gear.Id }, gear.ToDto());
        }

        // PUT: api/Gear/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGear(int id, [FromBody] GearDto dto, [FromHeader(Name = "UserId")] int requestUserId)
        {
            var gear = await _context.UserGear.FindAsync(id);

            if (gear == null) return NotFound("Gear item not found.");
            if (gear.UserId != requestUserId) return Unauthorized("You are not the owner of this gear item.");

            gear.Name = dto.Name;
            gear.Category = dto.Category;
            gear.Notes = dto.Notes;
            gear.DisplayOrder = dto.DisplayOrder;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Gear/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGear(int id, [FromHeader(Name = "UserId")] int requestUserId)
        {
            var gear = await _context.UserGear.FindAsync(id);

            if (gear == null) return NotFound("Gear item not found.");
            if (gear.UserId != requestUserId) return Unauthorized("You are not the owner of this gear item.");

            _context.UserGear.Remove(gear);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Gear item removed." });
        }
    }
}
