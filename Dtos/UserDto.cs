namespace FataleCore.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int CreditsBalance { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Biography { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? BannerUrl { get; set; }
        public string? ThemeColor { get; set; }
        public string? TextColor { get; set; }
        public string? BackgroundColor { get; set; }
        public bool IsGlass { get; set; }
        public string? WallpaperVideoUrl { get; set; }
        public int? ResidentSectorId { get; set; }
        public int? CommunityId { get; set; }
        
        // Artist Metadata (optional)
        public bool IsLive { get; set; }
        public int? FeaturedTrackId { get; set; }
    }
}
