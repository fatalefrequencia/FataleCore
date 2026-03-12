using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FataleCore.Models
{
    public class CommunityMessage
    {
        public int Id { get; set; }

        public int CommunityId { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User? Sender { get; set; }

        [MaxLength(280)]
        public string Content { get; set; } = string.Empty;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
