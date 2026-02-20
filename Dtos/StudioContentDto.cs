using Microsoft.AspNetCore.Http;

namespace FataleCore.Dtos
{
    public class StudioContentUploadDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Type { get; set; } = "PHOTO"; // PHOTO or VIDEO
        public IFormFile File { get; set; } = null!;
        public bool IsPosted { get; set; } = true;
    }
}
