using System.ComponentModel.DataAnnotations;

namespace FataleCore.DTOs
{
    public class TrackUploadDto
    {
        [Required]
        public string TrackTitle { get; set; } = string.Empty;

        public string? Genre { get; set; }

        [Required]
        public IFormFile AudioFile { get; set; } = null!;

        [Required]
        public IFormFile CoverImage { get; set; } = null!;

        // Economy Fields
        public int Price { get; set; } = 0;
        public bool IsLocked { get; set; } = false;
    }
}
