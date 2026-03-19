using System.ComponentModel.DataAnnotations;

namespace FataleCore.DTOs
{
    public class PlaylistDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
        public bool IsPinned { get; set; }
        public bool IsPosted { get; set; }
        public int UserId { get; set; }
        public int TrackCount { get; set; }
    }

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
