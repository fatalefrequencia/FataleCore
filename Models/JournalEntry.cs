using System.ComponentModel.DataAnnotations;

namespace FataleCore.Models
{
    public class JournalEntry
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsPosted { get; set; } = false;
        public bool IsPinned { get; set; } = false;

        [System.ComponentModel.DataAnnotations.Schema.ForeignKey("UserId")]
        public User? User { get; set; }
    }
}
