namespace FataleCore.Models
{
    public class Artist
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;

        // Discovery Map Coordinates
        public int? MapX { get; set; }
        public int? MapY { get; set; }
        public int? SectorId { get; set; }

        public int CreditsBalance { get; set; } = 0;
        public int? UserId { get; set; }
        
        [System.ComponentModel.DataAnnotations.Schema.ForeignKey("UserId")]
        public User? User { get; set; }
    }
}
