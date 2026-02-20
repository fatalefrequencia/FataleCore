using System.ComponentModel.DataAnnotations;

namespace FataleCore.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public int CreditsBalance { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? Biography { get; set; }
        public string? ProfilePictureUrl { get; set; }

        // Personalization
        public string? BannerUrl { get; set; }
        public string? ThemeColor { get; set; } = "#ff006e";
        public string? TextColor { get; set; } = "#ffffff";
        public string? BackgroundColor { get; set; } = "#000000";
        public bool IsGlass { get; set; } = false;

        // Discovery Map Residency
        public int? ResidentSectorId { get; set; }
    }
}
