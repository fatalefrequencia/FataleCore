using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FataleCore.Models
{
    public enum SubscriptionTier
    {
        Basic = 1,    // $6.99, 200 tracks
        Pro = 2,      // $12.99, 500 tracks
        Premium = 3   // $19.99, unlimited
    }

    public class YoutubeCacheSubscription
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; }

        public SubscriptionTier Tier { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyPrice { get; set; }

        public int CacheLimit { get; set; } // 200, 500, or -1 for unlimited
        
        public DateTime StartDate { get; set; }
        public DateTime CurrentPeriodEnd { get; set; }
        
        public DateTime? CancelledAt { get; set; }
        public bool IsActive { get; set; }
        
        public string? StripeSubscriptionId { get; set; } // For future payment integration

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
