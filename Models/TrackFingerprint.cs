using System.ComponentModel.DataAnnotations;

namespace FataleCore.Models
{
    public class TrackFingerprint
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(50)]
        public string TrackType { get; set; } = string.Empty; // "youtube" or "native"

        [MaxLength(255)]
        public string TrackId { get; set; } = string.Empty; // YouTube videoId or native Track.Id as string

        public string Tags { get; set; } = string.Empty; // Comma-separated inferred tags

        public int ViewTier { get; set; } = 1; // 1-5 scale based on view count

        [MaxLength(100)]
        public string ChannelType { get; set; } = "community"; // "official", "topic", "community"

        public DateTime EnrichedAt { get; set; } = DateTime.UtcNow;

        public int PlayCount { get; set; } = 0; // Total plays across all users within Fatale
    }
}
