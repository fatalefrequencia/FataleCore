namespace FataleCore.DTOs
{
    /// <summary>
    /// Data Transfer Object for User profile information.
    /// </summary>
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
        public string? MonitorImageUrl { get; set; }
        public string? MonitorBackgroundColor { get; set; }
        public bool MonitorIsGlass { get; set; }
        public int? ResidentSectorId { get; set; }
        public int? CommunityId { get; set; }
        public string? StatusMessage { get; set; }
        
        // Artist Metadata (optional)
        public bool IsLive { get; set; }
        public int? FeaturedTrackId { get; set; }
    }
}
