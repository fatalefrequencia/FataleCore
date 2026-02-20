using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FataleCore.Data;
using FataleCore.Models;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace FataleCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CreditPurchaseController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CreditPurchaseController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("bundles")]
        public IActionResult GetBundles()
        {
            var bundles = new[]
            {
                new { id = 1, credits = 40, price = 4.99m, popular = false, bestValue = false, fee = "25%" },
                new { id = 2, credits = 100, price = 10.99m, popular = true, bestValue = false, fee = "10%" },
                new { id = 3, credits = 200, price = 21.99m, popular = false, bestValue = false, fee = "10%" },
                new { id = 4, credits = 500, price = 51.99m, popular = false, bestValue = true, fee = "4%" }
            };
            return Ok(bundles);
        }

        [HttpPost("purchase")]
        public async Task<IActionResult> PurchaseCredits([FromBody] PurchaseRequest request)
        {
             if (!Request.Headers.TryGetValue("UserId", out var userIdVal) || !int.TryParse(userIdVal, out int userId))
            {
                return Unauthorized("User ID required");
            }

            // Mock Payment Processing Logic
            // In reality, verify Stripe payment via PaymentMethodId
            
            int creditsToAdd = 0;
            switch (request.BundleId)
            {
                case 1: creditsToAdd = 40; break;
                case 2: creditsToAdd = 100; break;
                case 3: creditsToAdd = 200; break;
                case 4: creditsToAdd = 500; break;
                default: return BadRequest("Invalid bundle ID");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found");

            user.CreditsBalance += creditsToAdd;
            
            _context.Transactions.Add(new Transaction
            {
                UserId = user.Id,
                Type = "DEPOSIT",
                Amount = creditsToAdd,
                Description = $"Purchased bundle {request.BundleId}",
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Notify user via detailed logic if needed, but for now simple response
            return Ok(new { message = $"Successfully purchased {creditsToAdd} credits!", newBalance = user.CreditsBalance });
        }
    }

    public class PurchaseRequest
    {
        public int BundleId { get; set; }
        public string? PaymentMethodId { get; set; }
    }
}
