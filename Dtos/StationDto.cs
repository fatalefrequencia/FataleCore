namespace FataleCore.DTOs
{
    public class StationDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public bool IsLive { get; set; }
        public string? CurrentSessionTitle { get; set; }
        public int ListenerCount { get; set; }
        public string ArtistName { get; set; } = "UNKNOWN";
        public int? ArtistUserId { get; set; }
        public StationTrackDto? CurrentTrack { get; set; }
    }

    public class StationTrackDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public ArtistDto? Artist { get; set; }
    }
}
