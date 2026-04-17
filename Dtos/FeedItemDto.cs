namespace FataleCore.DTOs
{
    public class FeedItemDto
    {
        public string Id { get; set; } = string.Empty;
        public int ItemId { get; set; }
        public string Type { get; set; } = string.Empty; // track, studio, journal, system
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public int? ArtistId { get; set; }
        public int? ArtistUserId { get; set; }
        public int? SectorId { get; set; }
        public int? CommunityId { get; set; }
        public string? ImageUrl { get; set; }
        public string? Source { get; set; }
        public DateTime CreatedAt { get; set; }
        public int PlayCount { get; set; }
        public string? MediaType { get; set; } // PHOTO, VIDEO
        public string? ThumbnailUrl { get; set; }
        public int? TrackId { get; set; }

        // Social Metadata
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public int RepostCount { get; set; }
        public bool IsLiked { get; set; }
        public bool IsReposted { get; set; }

        // Re-sync Propagation
        public string? RepostedBy { get; set; }
        public bool IsOriginalSignal { get; set; } = true;
    }
}
