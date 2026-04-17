namespace FataleCore.DTOs
{
    public class TrackDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string? Source { get; set; }
        public string CoverImageUrl { get; set; } = string.Empty;
        public int? MapX { get; set; }
        public int? MapY { get; set; }
        public int? SectorId { get; set; }
        public int Price { get; set; }
        public bool IsDownloadable { get; set; }
        public bool IsPinned { get; set; }
        public bool IsPosted { get; set; }
        public DateTime CreatedAt { get; set; }
        public int AlbumId { get; set; }
        public string? AlbumTitle { get; set; }
        public string? ArtistName { get; set; }
        public int? ArtistUserId { get; set; }
        public int PlayCount { get; set; }
    }
}
