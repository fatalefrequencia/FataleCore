using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FataleCore.Models
{
    public class TrackPurchase
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int TrackId { get; set; }

        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;
        public int Cost { get; set; } = 0;

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("TrackId")]
        public Track? Track { get; set; }
    }
}
