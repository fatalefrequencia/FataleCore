using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FataleCore.Models
{
    public class Station
    {
        public int Id { get; set; }
        
        [Required]
        public int ArtistId { get; set; }
        
        public string Name { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty; // e.g. "101.5"
        
        // Live Status
        public bool IsLive { get; set; } = false;
        public string? CurrentSessionTitle { get; set; }
        public string? Description { get; set; } // Broadcast session description / tagline
        public int? CurrentTrackId { get; set; }
        public int ListenerCount { get; set; } = 0;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ArtistId")]
        public Artist? Artist { get; set; }

        [ForeignKey("CurrentTrackId")]
        public Track? CurrentTrack { get; set; }
    }
}
