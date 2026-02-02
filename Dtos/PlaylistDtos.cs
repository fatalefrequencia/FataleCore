using System.ComponentModel.DataAnnotations;

namespace FataleCore.Dtos
{
    public class CreatePlaylistDto
    {
        [Required]
        public string Name { get; set; } = "New Playlist";
        
        public string Description { get; set; } = string.Empty;
        
        public bool IsPublic { get; set; } = true;
    }

    public class AddTrackDto
    {
        [Required]
        public int TrackId { get; set; }
    }
}
