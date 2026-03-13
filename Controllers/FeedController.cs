using FataleCore.Data;
using FataleCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FataleCore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeedController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FeedController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetGlobalFeed()
        {
            // Try to get authenticated user ID from headers (set by frontend interceptor)
            int? currentUserId = null;
            if (Request.Headers.TryGetValue("UserId", out var userIdHeader) && int.TryParse(userIdHeader, out var uid))
            {
                currentUserId = uid;
            }

            // Get the list of artists/users the current user follows
            var followedArtistIds = new List<int>();
            var followedUserIds = new List<int>();
            if (currentUserId.HasValue)
            {
                followedArtistIds = await _context.UserArtistLikes
                    .Where(ual => ual.UserId == currentUserId.Value)
                    .Select(ual => ual.ArtistId)
                    .ToListAsync();

                followedUserIds = await _context.Artists
                    .Where(a => followedArtistIds.Contains(a.Id) && a.UserId.HasValue)
                    .Select(a => a.UserId!.Value)
                    .ToListAsync();

                // Also follow yourself to see your own reposts/content in list checks
                if (!followedUserIds.Contains(currentUserId.Value))
                {
                    followedUserIds.Add(currentUserId.Value);
                }
            }

            // 1. Fetch Tracks (User's own or from followed artists)
            // Filter out "The Archive" (ArtistUserId == null) unless specifically followed or it's a small set of system tracks
            var tracksQuery = _context.Tracks
                .Where(t => !t.IsDelisted)
                .Include(t => t.Album)
                    .ThenInclude(a => a!.Artist)
                        .ThenInclude(art => art!.User)
                .AsQueryable();

            if (currentUserId.HasValue)
            {
                // Strict Personalization: Own tracks OR followed artists' tracks ONLY.
                // "The Archive" (ArtistId 2) must be followed to appear here.
                tracksQuery = tracksQuery.Where(t => 
                    (t.Album != null && t.Album.Artist != null && t.Album.Artist.UserId == currentUserId.Value) || 
                    (t.Album != null && t.Album.Artist != null && followedArtistIds.Contains(t.Album.Artist.Id)) ||
                    (t.Album != null && t.Album.Artist != null && t.Album.Artist.Id == 2)
                );
            }
            else
            {
                // Guest View: Only Archive tracks
                tracksQuery = tracksQuery.Where(t => t.Album != null && t.Album.Artist != null && (t.Album.Artist.UserId == null || t.Album.Artist.Id == 2));
            }

            var tracks = await tracksQuery
                .OrderByDescending(t => t.CreatedAt)
                .Take(25)
                .Select(t => new FeedItem { 
                    Id = "track-" + t.Id,
                    ItemId = t.Id,
                    Type = "track",
                    Title = t.Title,
                    Content = t.Genre,
                    Artist = (t.Album != null && t.Album.Artist != null) ? t.Album.Artist.Name : "UNKNOWN_SIGNAL",
                    ArtistId = (t.Album != null && t.Album.Artist != null) ? (int?)t.Album.Artist.Id : null,
                    ArtistUserId = (t.Album != null && t.Album.Artist != null) ? t.Album.Artist.UserId : null,
                    CommunityId = (t.Album != null && t.Album.Artist != null && t.Album.Artist.User != null) ? t.Album.Artist.User.CommunityId : null,
                    SectorId = t.SectorId ?? (t.Album != null && t.Album.Artist != null ? t.Album.Artist.SectorId : null),
                    ImageUrl = t.CoverImageUrl,
                    Source = t.Source ?? t.FilePath,
                    CreatedAt = t.CreatedAt,
                    PlayCount = t.PlayCount,
                    TrackId = t.Id,
                    LikeCount = _context.FeedInteractions.Count(i => i.ItemType == "track" && i.ItemId == t.Id && i.InteractionType == "LIKE"),
                    CommentCount = _context.FeedInteractions.Count(i => i.ItemType == "track" && i.ItemId == t.Id && i.InteractionType == "COMMENT"),
                    RepostCount = _context.FeedInteractions.Count(i => i.ItemType == "track" && i.ItemId == t.Id && i.InteractionType == "REPOST"),
                    IsLiked = currentUserId.HasValue && _context.FeedInteractions.Any(i => i.UserId == currentUserId.Value && i.ItemType == "track" && i.ItemId == t.Id && i.InteractionType == "LIKE"),
                    IsReposted = currentUserId.HasValue && _context.FeedInteractions.Any(i => i.UserId == currentUserId.Value && i.ItemType == "track" && i.ItemId == t.Id && i.InteractionType == "REPOST")
                })
                .ToListAsync();

            // 2. Fetch Studio Content (User's own or from followed users)
            var studioItems = await (from s in _context.StudioContents.Include(sc => sc.User)
                                    join a in _context.Artists on s.UserId equals a.UserId into artists
                                    from a in artists.DefaultIfEmpty()
                                    where currentUserId.HasValue ? followedUserIds.Contains(s.UserId) : false
                                    orderby s.CreatedAt descending
                                    select new FeedItem {
                                        Id = "studio-" + s.Id,
                                        ItemId = s.Id,
                                        Type = "studio",
                                        Title = s.Title,
                                        Content = s.Description ?? string.Empty,
                                        Artist = s.User != null ? s.User.Username : "UNKNOWN_SIGNAL",
                                        ArtistId = a != null ? (int?)a.Id : null,
                                        ArtistUserId = s.UserId,
                                        CommunityId = s.User != null ? s.User.CommunityId : null,
                                        SectorId = a != null ? a.SectorId : null,
                                        ImageUrl = s.Url,
                                        CreatedAt = s.CreatedAt,
                                        LikeCount = _context.FeedInteractions.Count(i => i.ItemType == "studio" && i.ItemId == s.Id && i.InteractionType == "LIKE"),
                                        CommentCount = _context.FeedInteractions.Count(i => i.ItemType == "studio" && i.ItemId == s.Id && i.InteractionType == "COMMENT"),
                                        RepostCount = _context.FeedInteractions.Count(i => i.ItemType == "studio" && i.ItemId == s.Id && i.InteractionType == "REPOST"),
                                        IsLiked = currentUserId.HasValue && _context.FeedInteractions.Any(i => i.UserId == currentUserId.Value && i.ItemType == "studio" && i.ItemId == s.Id && i.InteractionType == "LIKE"),
                                        IsReposted = currentUserId.HasValue && _context.FeedInteractions.Any(i => i.UserId == currentUserId.Value && i.ItemType == "studio" && i.ItemId == s.Id && i.InteractionType == "REPOST")
                                    })
                                    .Take(25)
                                    .ToListAsync();

            // 3. Fetch Journals (User's own or from followed users)
            var journalItems = await (from j in _context.JournalEntries.Include(je => je.User)
                                     join a in _context.Artists on j.UserId equals a.UserId into artists
                                     from a in artists.DefaultIfEmpty()
                                     where currentUserId.HasValue ? followedUserIds.Contains(j.UserId) : (j.UserId == 3)
                                     orderby j.CreatedAt descending
                                     select new FeedItem {
                                         Id = "journal-" + j.Id,
                                         ItemId = j.Id,
                                         Type = "journal",
                                         Title = j.Title,
                                         Content = j.Content ?? string.Empty,
                                         Artist = j.User != null ? j.User.Username : "UNKNOWN_SIGNAL",
                                         ArtistId = a != null ? (int?)a.Id : null,
                                         ArtistUserId = j.UserId,
                                         CommunityId = j.User != null ? j.User.CommunityId : null,
                                         SectorId = a != null ? a.SectorId : null,
                                         CreatedAt = j.CreatedAt,
                                         LikeCount = _context.FeedInteractions.Count(i => i.ItemType == "journal" && i.ItemId == j.Id && i.InteractionType == "LIKE"),
                                         CommentCount = _context.FeedInteractions.Count(i => i.ItemType == "journal" && i.ItemId == j.Id && i.InteractionType == "COMMENT"),
                                         RepostCount = _context.FeedInteractions.Count(i => i.ItemType == "journal" && i.ItemId == j.Id && i.InteractionType == "REPOST"),
                                         IsLiked = currentUserId.HasValue && _context.FeedInteractions.Any(i => i.UserId == currentUserId.Value && i.ItemType == "journal" && i.ItemId == j.Id && i.InteractionType == "LIKE"),
                                         IsReposted = currentUserId.HasValue && _context.FeedInteractions.Any(i => i.UserId == currentUserId.Value && i.ItemType == "journal" && i.ItemId == j.Id && i.InteractionType == "REPOST")
                                     })
                                     .Take(25)
                                     .ToListAsync();

            // 4. Fetch RE_SYNCs (Reposts) from followed users

            var repostInteractions = await _context.FeedInteractions
                .Where(i => i.InteractionType == "REPOST" && followedUserIds.Contains(i.UserId))
                .Include(i => i.User)
                .OrderByDescending(i => i.CreatedAt)
                .Take(50)
                .ToListAsync();

            var repostItems = new List<FeedItem>();
            foreach (var rep in repostInteractions)
            {
                FeedItem? item = null;
                var type = rep.ItemType?.ToLower() ?? "";
                if (type == "track")
                {
                    var t = await _context.Tracks
                        .Include(track => track.Album).ThenInclude(a => a!.Artist)
                        .FirstOrDefaultAsync(track => track.Id == rep.ItemId);
                    if (t != null)
                    {
                        item = new FeedItem
                        {
                            Id = $"repost-track-{rep.Id}",
                            ItemId = t.Id,
                            Type = "track",
                            Title = t.Title,
                            Content = t.Genre,
                            Artist = (t.Album != null && t.Album.Artist != null) ? t.Album.Artist.Name : "UNKNOWN_SIGNAL",
                            ArtistId = (t.Album != null && t.Album.Artist != null) ? (int?)t.Album.Artist.Id : null,
                            ArtistUserId = (t.Album != null && t.Album.Artist != null) ? t.Album.Artist.UserId : null,
                            CommunityId = (t.Album != null && t.Album.Artist != null && t.Album.Artist.User != null) ? t.Album.Artist.User.CommunityId : null,
                            SectorId = t.SectorId ?? (t.Album != null && t.Album.Artist != null ? t.Album.Artist.SectorId : null),
                            ImageUrl = t.CoverImageUrl,
                            Source = t.Source ?? t.FilePath,
                            CreatedAt = rep.CreatedAt, // Time of RE_SYNC
                            PlayCount = t.PlayCount,
                            TrackId = t.Id,
                            RepostedBy = rep.User?.Username ?? "RESERVED_NODE",
                            IsOriginalSignal = false
                        };
                    }
                }
                else if (type == "studio")
                {
                    var s = await _context.StudioContents.Include(sc => sc.User).FirstOrDefaultAsync(sc => sc.Id == rep.ItemId);
                    if (s != null)
                    {
                        item = new FeedItem
                        {
                            Id = $"repost-studio-{rep.Id}",
                            ItemId = s.Id,
                            Type = "studio",
                            Title = s.Title,
                            Content = s.Description ?? string.Empty,
                            Artist = s.User != null ? s.User.Username : "UNKNOWN_SIGNAL",
                            ArtistId = s.User != null ? _context.Artists.Where(a => a.UserId == s.UserId).Select(a => (int?)a.Id).FirstOrDefault() : null,
                            ArtistUserId = s.UserId,
                            CommunityId = s.User != null ? s.User.CommunityId : null,
                            SectorId = s.User != null ? _context.Artists.Where(a => a.UserId == s.UserId).Select(a => a.SectorId).FirstOrDefault() : null,
                            ImageUrl = s.Url,
                            CreatedAt = rep.CreatedAt,
                            MediaType = s.Type,
                            RepostedBy = rep.User?.Username ?? "RESERVED_NODE",
                            IsOriginalSignal = false
                        };
                    }
                }
                else if (type == "journal")
                {
                    var j = await _context.JournalEntries.Include(je => je.User).FirstOrDefaultAsync(je => je.Id == rep.ItemId);
                    if (j != null)
                    {
                        item = new FeedItem
                        {
                            Id = $"repost-journal-{rep.Id}",
                            ItemId = j.Id,
                            Type = "journal",
                            Title = j.Title,
                            Content = j.Content ?? string.Empty,
                            Artist = j.User != null ? j.User.Username : "UNKNOWN_SIGNAL",
                            ArtistId = j.User != null ? _context.Artists.Where(a => a.UserId == j.UserId).Select(a => (int?)a.Id).FirstOrDefault() : null,
                            ArtistUserId = j.UserId,
                            CommunityId = j.User != null ? j.User.CommunityId : null,
                            SectorId = j.User != null ? _context.Artists.Where(a => a.UserId == j.UserId).Select(a => a.SectorId).FirstOrDefault() : null,
                            CreatedAt = rep.CreatedAt,
                            RepostedBy = rep.User?.Username ?? "RESERVED_NODE",
                            IsOriginalSignal = false
                        };
                    }
                }

                if (item != null)
                {
                    // Social counts for the original item
                    item.LikeCount = await _context.FeedInteractions.CountAsync(i => i.ItemType == rep.ItemType && i.ItemId == rep.ItemId && i.InteractionType == "LIKE");
                    item.CommentCount = await _context.FeedInteractions.CountAsync(i => i.ItemType == rep.ItemType && i.ItemId == rep.ItemId && i.InteractionType == "COMMENT");
                    item.RepostCount = await _context.FeedInteractions.CountAsync(i => i.ItemType == rep.ItemType && i.ItemId == rep.ItemId && i.InteractionType == "REPOST");
                    item.IsLiked = currentUserId.HasValue && await _context.FeedInteractions.AnyAsync(i => i.UserId == currentUserId.Value && i.ItemType == rep.ItemType && i.ItemId == rep.ItemId && i.InteractionType == "LIKE");
                    item.IsReposted = currentUserId.HasValue && await _context.FeedInteractions.AnyAsync(i => i.UserId == currentUserId.Value && i.ItemType == rep.ItemType && i.ItemId == rep.ItemId && i.InteractionType == "REPOST");
                    
                    repostItems.Add(item);
                }
            }

            var shoutouts = new List<FeedItem> {
                new FeedItem {
                    Id = "sys-1",
                    Type = "system",
                    Title = "NODE_SYNC_COMPLETE",
                    Content = "Neural Net residency established in Silicon Heights Sector.",
                    Artist = "FATALE_CORE",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-45)
                },
                new FeedItem {
                    Id = "sys-2",
                    Type = "system",
                    Title = "SIGNAL_BURST",
                    Content = "Multiple new artist frequencies detected in Neon Slums.",
                    Artist = "FATALE_CORE",
                    CreatedAt = DateTime.UtcNow.AddHours(-3)
                }
            };

            var combined = tracks.Concat(studioItems).Concat(journalItems).Concat(repostItems).Concat(shoutouts)
                .OrderByDescending(x => x.CreatedAt)
                .Take(100)
                .ToList();

            return Ok(combined);
        }

        public class FeedItem
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
}
