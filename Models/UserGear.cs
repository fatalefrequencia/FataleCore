namespace FataleCore.Models
{
    public class UserGear
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "Other"; // e.g., Synth, DAW, Microphone, Controller, Drum Machine, Plugin, Other
        public string? Notes { get; set; }
        public int DisplayOrder { get; set; } = 0;

        [System.ComponentModel.DataAnnotations.Schema.ForeignKey("UserId")]
        public User? User { get; set; }
    }
}
