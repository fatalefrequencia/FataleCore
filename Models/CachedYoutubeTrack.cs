using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FataleCore.Models
{
    public class CachedYoutubeTrack
    {
        [Key]
        public int Id { get; set; }

        public int YoutubeTrackId { get; set; }
        [ForeignKey("YoutubeTrackId")]
        public YoutubeTrack? YoutubeTrack { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; }

        [Required]
        [MaxLength(500)]
        public string AudioFilePath { get; set; } = string.Empty; // Server path to cached audio

        public long FileSizeBytes { get; set; }
        public int AudioQuality { get; set; } // Bitrate, e.g., 128kbps

        public DateTime CachedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiresAt { get; set; } // Set when subscription lapses
        public bool IsAvailable { get; set; } = true; // False if subscription expired

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
