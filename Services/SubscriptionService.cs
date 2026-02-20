using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FataleCore.Data;
using FataleCore.Models;

namespace FataleCore.Services
{
    public interface ISubscriptionService
    {
        Task<bool> CanCacheTrackAsync(int userId);
        Task<(int used, int limit)> GetCacheUsageAsync(int userId);
        Task HandleSubscriptionExpiryAsync(int userId);
    }

    public class SubscriptionService : ISubscriptionService
    {
        private readonly ApplicationDbContext _context;

        public SubscriptionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CanCacheTrackAsync(int userId)
        {
            var sub = await _context.YoutubeCacheSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive && s.CurrentPeriodEnd > DateTime.UtcNow);

            if (sub == null) return false;
            
            if (sub.CacheLimit == -1) return true; // Unlimited

            var currentCount = await _context.CachedYoutubeTracks
                .CountAsync(c => c.UserId == userId && c.IsAvailable);

            return currentCount < sub.CacheLimit;
        }

        public async Task<(int used, int limit)> GetCacheUsageAsync(int userId)
        {
            var sub = await _context.YoutubeCacheSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive && s.CurrentPeriodEnd > DateTime.UtcNow);

            if (sub == null) return (0, 0);

            var used = await _context.CachedYoutubeTracks
                .CountAsync(c => c.UserId == userId && c.IsAvailable);

            return (used, sub.CacheLimit);
        }

        public async Task HandleSubscriptionExpiryAsync(int userId)
        {
            var sub = await _context.YoutubeCacheSubscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);

            // If no active sub or expired
            if (sub == null || sub.CurrentPeriodEnd <= DateTime.UtcNow)
            {
                var cachedTracks = await _context.CachedYoutubeTracks
                    .Where(c => c.UserId == userId && c.IsAvailable)
                    .ToListAsync();

                foreach (var track in cachedTracks)
                {
                    track.IsAvailable = false;
                    track.ExpiresAt = DateTime.UtcNow;
                }

                if (sub != null)
                {
                    sub.IsActive = false;
                }
                
                await _context.SaveChangesAsync();
            }
        }
    }
}
