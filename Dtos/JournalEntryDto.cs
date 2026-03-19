namespace FataleCore.DTOs
{
    public class JournalEntryDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsPosted { get; set; }
        public bool IsPinned { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
