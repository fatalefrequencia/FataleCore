using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace FataleCore.Models
{
    public class TrackPurchase
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int TrackId { get; set; }
        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
        
        // Renamed from PricePaid to match EconomyController usage
        public int Cost { get; set; }

        // Navigation Property for Include() calls in PurchasesController
        [ForeignKey("TrackId")]
        public Track? Track { get; set; }
    }
}
