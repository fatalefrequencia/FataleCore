namespace FataleCore.DTOs
{
    public class AlbumDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public string CoverImageUrl { get; set; } = string.Empty;
        public int ArtistId { get; set; }
        public string? ArtistName { get; set; }
        public int? MapX { get; set; }
        public int? MapY { get; set; }
        public int? SectorId { get; set; }
    }
}
