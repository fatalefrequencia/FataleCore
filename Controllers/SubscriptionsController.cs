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
    public class SubscriptionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SubscriptionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("youtube-cache/tiers")]
        public IActionResult GetTiers()
        {
            var tiers = new[]
            {
                new { id = SubscriptionTier.Basic, name = "Basic", price = 6.99m, cacheLimit = 200, description = "200 Offline Tracks" },
                new { id = SubscriptionTier.Pro, name = "Pro", price = 12.99m, cacheLimit = 500, description = "500 Offline Tracks" },
                new { id = SubscriptionTier.Premium, name = "Premium", price = 19.99m, cacheLimit = -1, description = "Unlimited Offline Tracks" }
            };
            return Ok(tiers);
        }

        [HttpGet("youtube-cache/status")]
        public async Task<IActionResult> GetStatus()
        {
            if (!Request.Headers.TryGetValue("UserId", out var userIdVal) || !int.TryParse(userIdVal, out int userId))
            {
                return Unauthorized("User ID required");
            }

            var sub = await _context.YoutubeCacheSubscriptions
                .Where(s => s.UserId == userId && s.IsActive)
                .OrderByDescending(s => s.CurrentPeriodEnd)
                .FirstOrDefaultAsync();

            if (sub == null || sub.CurrentPeriodEnd < DateTime.UtcNow)
            {
                return Ok(new { isActive = false, tier = (int?)null, cacheLimit = 0 });
            }

            // Get current usage
            var used = await _context.CachedYoutubeTracks
                .CountAsync(c => c.UserId == userId && c.IsAvailable);

            return Ok(new 
            { 
                isActive = true, 
                tier = (int)sub.Tier,
                tierName = sub.Tier.ToString(),
                cacheLimit = sub.CacheLimit,
                cacheUsed = used,
                currentPeriodEnd = sub.CurrentPeriodEnd
            });
        }
        
        // Subscribe endpoint to be implemented with payment integration
        [HttpPost("youtube-cache/subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
        {
             if (!Request.Headers.TryGetValue("UserId", out var userIdVal) || !int.TryParse(userIdVal, out int userId))
            {
                return Unauthorized("User ID required");
            }

            // Simplified Mock Logic for V1
            var existing = await _context.YoutubeCacheSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);

            if (existing != null)
            {
                // Upgrade/Downgrade logic would go here
                existing.IsActive = false;
            }

            var newSub = new YoutubeCacheSubscription
            {
                UserId = userId,
                Tier = request.Tier,
                StartDate = DateTime.UtcNow,
                CurrentPeriodEnd = DateTime.UtcNow.AddDays(30),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            switch (request.Tier)
            {
                case SubscriptionTier.Basic:
                    newSub.MonthlyPrice = 6.99m;
                    newSub.CacheLimit = 200;
                    break;
                case SubscriptionTier.Pro:
                    newSub.MonthlyPrice = 12.99m;
                    newSub.CacheLimit = 500;
                    break;
                case SubscriptionTier.Premium:
                    newSub.MonthlyPrice = 19.99m;
                    newSub.CacheLimit = -1;
                    break;
            }

            _context.YoutubeCacheSubscriptions.Add(newSub);
            await _context.SaveChangesAsync();
            
            return Ok(new { message = "Subscription activated!", subscription = newSub });
        }
    }

    public class SubscribeRequest
    {
        public SubscriptionTier Tier { get; set; }
        public string? PaymentMethodId { get; set; }
    }
}
