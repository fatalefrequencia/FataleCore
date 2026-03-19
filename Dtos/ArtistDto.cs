namespace FataleCore.DTOs
{
    public class ArtistDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? ImageUrl { get; set; }
        public int? MapX { get; set; }
        public int? MapY { get; set; }
        public int? SectorId { get; set; }
        public bool IsLive { get; set; }
        public int? FeaturedTrackId { get; set; }
        public int? UserId { get; set; }
    }
}
