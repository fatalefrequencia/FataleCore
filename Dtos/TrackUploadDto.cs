using System.ComponentModel.DataAnnotations;

namespace FataleCore.Dtos
{
    public class TrackUploadDto
    {
        [Required]
        public string TrackTitle { get; set; } = string.Empty;

        [Required]
        public string Genre { get; set; } = string.Empty;

        [Required]
        public IFormFile AudioFile { get; set; } = null!;

        [Required]
        public IFormFile CoverImage { get; set; } = null!;
    }
}
