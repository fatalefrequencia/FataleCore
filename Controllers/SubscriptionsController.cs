using FataleCore.Data;
using FataleCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FataleCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SubscriptionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Subscriptions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Subscription>>> GetSubscriptions([FromHeader(Name = "UserId")] int userId)
        {
            if (userId <= 0) return Unauthorized(new { message = "Invalid User ID" });

            return await _context.Subscriptions
                .Where(s => s.UserId == userId)
                .ToListAsync();
        }

        // POST: api/Subscriptions
        [HttpPost]
        public async Task<ActionResult<Subscription>> CreateSubscription([FromHeader(Name = "UserId")] int userId, [FromBody] Subscription subscription)
        {
             if (userId <= 0) return Unauthorized(new { message = "Invalid User ID" });

            subscription.UserId = userId;
            subscription.StartDate = DateTime.UtcNow;
            subscription.IsActive = true;

            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetSubscriptions", new { id = subscription.Id }, subscription);
        }
    }
}
