namespace FataleCore.DTOs
{
    public class NeuroGraphDto
    {
        public int UserId { get; set; }
        public List<NeuroNodeDto> Nodes { get; set; } = new();
        public int TotalTracksAnalyzed { get; set; }
    }

    public class NeuroNodeDto
    {
        public string Tag { get; set; } = string.Empty;
        public double Weight { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    public class ResonantStationsResponseDto
    {
        public string TopTag { get; set; } = string.Empty;
        public int MatchCount { get; set; }
        public List<ResonantStationDto> Stations { get; set; } = new();
    }

    public class ResonantStationDto
    {
        public int StationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public int ListenerCount { get; set; }
        public string? SessionTitle { get; set; }
        public string DjName { get; set; } = "Unknown DJ";
        public int? ArtistId { get; set; }
    }
}
