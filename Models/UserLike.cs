using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FataleCore.Models
{
    public class UserLike
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }


        // All tracks (local and YouTube) now live in the Tracks table
        [Required]
        public int TrackId { get; set; }

        public DateTime LikedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        
        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("TrackId")]
        public Track? Track { get; set; }
    }
}
