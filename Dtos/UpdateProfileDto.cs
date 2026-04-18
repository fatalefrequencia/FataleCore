using Microsoft.AspNetCore.Http;

namespace FataleCore.DTOs
{
    public class UpdateProfileDto
    {
        public string? Username { get; set; }
        public string? Biography { get; set; }
        public int? ResidentSectorId { get; set; }
        public IFormFile? ProfilePicture { get; set; }
        public IFormFile? Banner { get; set; } // New
        public IFormFile? WallpaperVideo { get; set; } // New
        public string? ThemeColor { get; set; } // New
        public string? TextColor { get; set; } // New
        public string? BackgroundColor { get; set; } // New
        public bool? IsGlass { get; set; }
        public bool? IsLive { get; set; }
        public int? FeaturedTrackId { get; set; }
        public IFormFile? MonitorImage { get; set; }
        public string? MonitorBackgroundColor { get; set; }
        public bool? MonitorIsGlass { get; set; }
    }
}
