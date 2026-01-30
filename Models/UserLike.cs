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

        [Required]
        public int TrackId { get; set; }

        public DateTime LikedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties if needed (optional but good for FK constraints)
        // For simplicity and avoiding circular dependencies in simple setups, we might skip full nav props
        // or just keep them basic. Let's add them for referential integrity.
        
        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("TrackId")]
        public Track? Track { get; set; }
    }
}
