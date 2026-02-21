using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FataleCore.Models
{
    public class FeedInteraction
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(20)]
        public string ItemType { get; set; } = string.Empty; // track, studio, journal

        [Required]
        public int ItemId { get; set; }

        [Required]
        [StringLength(20)]
        public string InteractionType { get; set; } = string.Empty; // LIKE, COMMENT, REPOST

        public string? Content { get; set; } // Only for COMMENT

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
 
        public int? ParentId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("ParentId")]
        public FeedInteraction? Parent { get; set; }

        public ICollection<FeedInteraction> Replies { get; set; } = new List<FeedInteraction>();
    }
}
