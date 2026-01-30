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

        public int AlbumId { get; set; }
        [JsonIgnore]
        public Album? Album { get; set; }
    }
}
