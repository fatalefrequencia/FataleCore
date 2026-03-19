namespace FataleCore.DTOs
{
    public class MessageDto
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
    }

    public class ConversationDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = "Unknown";
        public string ProfileImageUrl { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public int UnreadCount { get; set; }
    }
}
