using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FataleCore.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        
        public int UserId { get; set; } // The user who owns this transaction record
        
        // Types: "PURCHASE", "TIP_SENT", "TIP_RECEIVED", "EARNING_SALE", "DEPOSIT", "WITHDRAWAL", "TRANSFER_SENT", "TRANSFER_RECEIVED"
        public string Type { get; set; } = string.Empty;
        
        public int Amount { get; set; } // Positive for addition, negative for deduction
        
        public string Description { get; set; } = string.Empty;
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        // Optional Relations for context
        public int? RelatedUserId { get; set; } // Sender/Receiver ID
        public int? TrackId { get; set; } // For track sales/purchases
        
        [ForeignKey("UserId")]
        public User? User { get; set; }
        
        [ForeignKey("RelatedUserId")]
        public User? RelatedUser { get; set; }
        
        [ForeignKey("TrackId")]
        public Track? Track { get; set; }
    }
}
