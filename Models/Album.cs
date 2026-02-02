using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

namespace FataleCore.Models
{
    public class Album
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime ReleaseDate { get; set; }
        public string CoverImageUrl { get; set; } = string.Empty;
        
        // Discovery Map Coordinates
        public int? MapX { get; set; }
        public int? MapY { get; set; }
        public int? SectorId { get; set; }

        [NotMapped]
        public string ImageUrl => CoverImageUrl; // Compatibility with Cyber_Pod

        public int ArtistId { get; set; }
        public Artist? Artist { get; set; }
    }
}
