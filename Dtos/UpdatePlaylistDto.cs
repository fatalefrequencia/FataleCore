using System.ComponentModel.DataAnnotations;

namespace FataleCore.DTOs
{
    public class UpdatePlaylistDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
    }
}
