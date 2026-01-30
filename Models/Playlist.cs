using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FataleCore.Models
{
    public class Playlist
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = "New Playlist";
        
        public string Description { get; set; } = string.Empty;
        
        public string ImageUrl { get; set; } = string.Empty; // Required for Cyber_Pod
        
        public bool IsPublic { get; set; } = true;
        
        public int UserId { get; set; }
        public int TrackCount { get; set; } = 0;

        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}
