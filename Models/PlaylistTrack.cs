using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FataleCore.Models
{
    public class PlaylistTrack
    {
        public int Id { get; set; }
        
        public int PlaylistId { get; set; }
        
        public int TrackId { get; set; }
        
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("PlaylistId")]
        public Playlist? Playlist { get; set; }

        [ForeignKey("TrackId")]
        public Track? Track { get; set; }
    }
}
