using System;
using System.ComponentModel.DataAnnotations;

namespace FataleCore.Models
{
    public class YoutubeTrack
    {
        public int Id { get; set; }

        [Required]
        public string YoutubeId { get; set; } = string.Empty;

        [Required]
        public string Title { get; set; } = string.Empty;

        public string ChannelTitle { get; set; } = string.Empty;

        public string ThumbnailUrl { get; set; } = string.Empty;

        public long ViewCount { get; set; }

        public string Duration { get; set; } = "0:00"; // Store as string for consistency with Track model

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
