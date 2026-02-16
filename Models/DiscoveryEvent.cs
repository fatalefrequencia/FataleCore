using System;

namespace FataleCore.Models
{
    public class DiscoveryEvent
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int TrackId { get; set; }
        
        // EventType: "Play", "Like", "MapClick", "Purchase"
        public string EventType { get; set; } = string.Empty;
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Optional: Location data for heatmap tracking
        public int? MapX { get; set; }
        public int? MapY { get; set; }

        // Navigation properties
        public User? User { get; set; }
        public Track? Track { get; set; }
    }
}
