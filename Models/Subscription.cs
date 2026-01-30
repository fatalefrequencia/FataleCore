using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FataleCore.Models
{
    public class Subscription
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public string PlanType { get; set; } = "Free"; // Free, Pro, Artist

        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;

        [ForeignKey("UserId")]
        public User? User { get; set; }
    }
}
