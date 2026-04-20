namespace FataleCore.DTOs
{
    public class GearDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "Other";
        public string? Notes { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class CreateGearDto
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "Other";
        public string? Notes { get; set; }
        public int DisplayOrder { get; set; }
    }
}
