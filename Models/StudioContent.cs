using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FataleCore.Models
{
    public class StudioContent
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public string Type { get; set; } = "PHOTO"; // PHOTO or VIDEO
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsPosted { get; set; } = false;
        public bool IsPinned { get; set; } = false;

        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}
