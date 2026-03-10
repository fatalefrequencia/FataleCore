using System.ComponentModel.DataAnnotations;

namespace FataleCore.Models
{
    public class UserListeningEvent
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        
        [MaxLength(50)]
        public string TrackType { get; set; } = string.Empty; // "youtube" or "native"

        [MaxLength(255)]
        public string TrackId { get; set; } = string.Empty; // YouTube videoId or native Track.Id as string

        [MaxLength(500)]
        public string TrackTitle { get; set; } = string.Empty;

        public string Tags { get; set; } = string.Empty; // Comma separated

        public DateTime ListenedAt { get; set; } = DateTime.UtcNow;

        public int DurationSeconds { get; set; }

        [MaxLength(50)]
        public string Source { get; set; } = "queue"; // "queue", "recommendation", "station"
    }
}
