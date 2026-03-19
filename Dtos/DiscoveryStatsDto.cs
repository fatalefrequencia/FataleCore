namespace FataleCore.DTOs
{
    public class DiscoveryStatsDto
    {
        public long TotalScans { get; set; }
        public int TotalPlays { get; set; }
        public List<TopTrackDto> TopTracks { get; set; } = new();
        public int ActiveUsers { get; set; }
        public int TotalUsers { get; set; }
        public int OnlineUsers { get; set; }
    }

    public class TopTrackDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int PlayCount { get; set; }
    }
}
