using FataleCore.Data;
using FataleCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FataleCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EconomyController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EconomyController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Economy/balance
        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance([FromHeader(Name = "UserId")] int userId)
        {
            // Dev Mode Fallback
            var user = await _context.Users.FindAsync(userId);
            if (user == null) user = await _context.Users.FirstOrDefaultAsync();

            if (user == null) return NotFound("User not found (No users in DB)");

            return Ok(new { balance = user.CreditsBalance });
        }

        // POST: api/Economy/purchase/{trackId}
        [HttpPost("purchase/{trackId}")]
        public async Task<IActionResult> PurchaseTrack(int trackId, [FromHeader(Name = "UserId")] int userId)
        {
            // Dev Mode Fallback
            var user = await _context.Users.FindAsync(userId);
            if (user == null) user = await _context.Users.FirstOrDefaultAsync();

            if (user == null) return NotFound("User not found");

            // Check if already purchased
            var alreadyPurchased = await _context.TrackPurchases
                .AnyAsync(tp => tp.UserId == user.Id && tp.TrackId == trackId);
            
            if (alreadyPurchased) return BadRequest(new { message = "Track already purchased" });

            // Assuming a fixed cost or fetching from track if it had a price
            // For now, let's say a track costs 10 credits by default
            int cost = 10; 

            if (user.CreditsBalance < cost)
            {
                return BadRequest(new { message = "Insufficient credits" });
            }

            // Deduct credits
            user.CreditsBalance -= cost;

            // Record purchase
            var purchase = new TrackPurchase
            {
                UserId = user.Id, // Use the resolved user.Id
                TrackId = trackId,
                Cost = cost,
                PurchaseDate = DateTime.UtcNow
            };

            _context.TrackPurchases.Add(purchase);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, newBalance = user.CreditsBalance, message = "Track purchased successfully" });
        }

        // POST: api/Economy/add
        [HttpPost("add")]
        public async Task<IActionResult> AddCredits([FromBody] AddCreditsDto request, [FromHeader(Name = "UserId")] int userId)
        {
            // Dev Mode Fallback: If userId is 0 or invalid, try to get the first user
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null)
            {
                 // Fallback to first user for dev convenience
                 user = await _context.Users.FirstOrDefaultAsync();
            }

            if (user == null) return NotFound("User not found and no users available in DB");

            if (request.Amount <= 0) return BadRequest(new { message = "Amount must be positive" });

            user.CreditsBalance += request.Amount;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, newBalance = user.CreditsBalance, message = $"Added {request.Amount} credits" });
        }

        public class AddCreditsDto
        {
            public int Amount { get; set; }
        }
    }
}
