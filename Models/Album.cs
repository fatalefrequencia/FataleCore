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
        
        [NotMapped]
        public string ImageUrl => CoverImageUrl; // Compatibility with Cyber_Pod

        public int ArtistId { get; set; }
        [JsonIgnore]
        public Artist? Artist { get; set; }
    }
}
