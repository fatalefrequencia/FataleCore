using System.Text.Json.Serialization;

namespace FataleCore.Models
{
    public class Track
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty; // e.g., "3:45"
        public string FilePath { get; set; } = string.Empty;
        public string CoverImageUrl { get; set; } = string.Empty;

        // Discovery Map Coordinates
        public int? MapX { get; set; }
        public int? MapY { get; set; }
        public int? SectorId { get; set; }

        // Economy & Access Control
        public int Price { get; set; } = 0; // Cost in Credits
        public bool IsDownloadable { get; set; } = true;
        public bool IsLocked { get; set; } = false; // "Encrypted" / Preview Only
        public bool IsDelisted { get; set; } = false; // Hidden from store but kept for previous purchasers

        // Analytics
        public int PlayCount { get; set; } = 0;

        public int AlbumId { get; set; }
        public Album? Album { get; set; }
    }
}
