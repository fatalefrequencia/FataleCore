namespace FataleCore.DTOs
{
    public class AlbumUploadDto
    {
        public string AlbumTitle { get; set; } = string.Empty;
        public IFormFile? CoverImage { get; set; }
        // Track fields are sent as indexed arrays via multipart/form-data:
        // e.g. Tracks[0].Title, Tracks[0].AudioFile, etc.
        public List<AlbumTrackDto> Tracks { get; set; } = new();
    }

    public class AlbumTrackDto
    {
        public string Title { get; set; } = string.Empty;
        public string? Genre { get; set; }
        public IFormFile AudioFile { get; set; } = null!;
        public IFormFile? CoverImage { get; set; } // Optional per-track cover (overrides album cover)
        public int Price { get; set; } = 0;
        public bool IsLocked { get; set; } = false;
    }
}
