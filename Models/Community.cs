using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FataleCore.Models
{
    public class Community
    {
        public int Id { get; set; }
        
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        // The sector (genre) the community resides in (0-4)
        public int SectorId { get; set; }

        public int FounderUserId { get; set; }
        
        [ForeignKey("FounderUserId")]
        public User Founder { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<User> Members { get; set; } = new List<User>();
    }
}
