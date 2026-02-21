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
            if (currentUserId.HasValue)
            {
                followedArtistIds = await _context.UserArtistLikes
                    .Where(ual => ual.UserId == currentUserId.Value)
                    .Select(ual => ual.ArtistId)
                    .ToListAsync();
            }

            // 1. Fetch Tracks (User's own or from followed artists)
            // Filter out "The Archive" (ArtistUserId == null) unless specifically followed or it's a small set of system tracks
            var tracksQuery = _context.Tracks
                .Where(t => !t.IsDelisted)
                .Include(t => t.Album)
                    .ThenInclude(a => a!.Artist)
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
                    ArtistUserId = (t.Album != null && t.Album.Artist != null) ? t.Album.Artist.UserId : null,
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
            var studioQuery = _context.StudioContents
                .Include(s => s.User)
                .AsQueryable();

            if (currentUserId.HasValue)
            {
                var followedUserIds = await _context.Artists
                    .Where(a => followedArtistIds.Contains(a.Id))
                    .Select(a => a.UserId)
                    .ToListAsync();

                studioQuery = studioQuery.Where(s => 
                    s.UserId == currentUserId.Value || 
                    followedUserIds.Contains(s.UserId)
                );
            }
            else
            {
                // Guests don't see studio content unless we have a specific public artist
                studioQuery = studioQuery.Where(s => false); 
            }

            var studio = await studioQuery
                .OrderByDescending(s => s.CreatedAt)
                .Take(25)
                .Select(s => new FeedItem {
                    Id = "studio-" + s.Id,
                    ItemId = s.Id,
                    Type = "studio",
                    Title = s.Title,
                    Content = s.Description ?? string.Empty,
                    Artist = s.User != null ? s.User.Username : "UNKNOWN_SIGNAL",
                    ArtistUserId = s.UserId,
                    ImageUrl = s.Url,
                    CreatedAt = s.CreatedAt,
                    MediaType = s.Type,
                    LikeCount = _context.FeedInteractions.Count(i => i.ItemType == "studio" && i.ItemId == s.Id && i.InteractionType == "LIKE"),
                    CommentCount = _context.FeedInteractions.Count(i => i.ItemType == "studio" && i.ItemId == s.Id && i.InteractionType == "COMMENT"),
                    RepostCount = _context.FeedInteractions.Count(i => i.ItemType == "studio" && i.ItemId == s.Id && i.InteractionType == "REPOST"),
                    IsLiked = currentUserId.HasValue && _context.FeedInteractions.Any(i => i.UserId == currentUserId.Value && i.ItemType == "studio" && i.ItemId == s.Id && i.InteractionType == "LIKE"),
                    IsReposted = currentUserId.HasValue && _context.FeedInteractions.Any(i => i.UserId == currentUserId.Value && i.ItemType == "studio" && i.ItemId == s.Id && i.InteractionType == "REPOST")
                })
                .ToListAsync();

            // 3. Fetch Journals (User's own or from followed users)
            var journalsQuery = _context.JournalEntries
                .Include(j => j.User)
                .AsQueryable();

            if (currentUserId.HasValue)
            {
                var followedUserIds = await _context.Artists
                    .Where(a => followedArtistIds.Contains(a.Id))
                    .Select(a => a.UserId)
                    .ToListAsync();

                journalsQuery = journalsQuery.Where(j => 
                    j.UserId == currentUserId.Value || 
                    followedUserIds.Contains(j.UserId)
                );
            }
            else
            {
                // Guests see public/tester journals
                journalsQuery = journalsQuery.Where(j => j.UserId == 3);
            }

            var journals = await journalsQuery
                .OrderByDescending(j => j.CreatedAt)
                .Take(25)
                .Select(j => new FeedItem {
                    Id = "journal-" + j.Id,
                    ItemId = j.Id,
                    Type = "journal",
                    Title = j.Title,
                    Content = j.Content ?? string.Empty,
                    Artist = j.User != null ? j.User.Username : "UNKNOWN_SIGNAL",
                    ArtistUserId = j.UserId,
                    CreatedAt = j.CreatedAt,
                    LikeCount = _context.FeedInteractions.Count(i => i.ItemType == "journal" && i.ItemId == j.Id && i.InteractionType == "LIKE"),
                    CommentCount = _context.FeedInteractions.Count(i => i.ItemType == "journal" && i.ItemId == j.Id && i.InteractionType == "COMMENT"),
                    RepostCount = _context.FeedInteractions.Count(i => i.ItemType == "journal" && i.ItemId == j.Id && i.InteractionType == "REPOST"),
                    IsLiked = currentUserId.HasValue && _context.FeedInteractions.Any(i => i.UserId == currentUserId.Value && i.ItemType == "journal" && i.ItemId == j.Id && i.InteractionType == "LIKE"),
                    IsReposted = currentUserId.HasValue && _context.FeedInteractions.Any(i => i.UserId == currentUserId.Value && i.ItemType == "journal" && i.ItemId == j.Id && i.InteractionType == "REPOST")
                })
                .ToListAsync();

            // 4. Fetch RE_SYNCs (Reposts) from followed users
            var followedUserIds = new List<int>();
            if (currentUserId.HasValue)
            {
                followedUserIds = await _context.Artists
                    .Where(a => followedArtistIds.Contains(a.Id))
                    .Select(a => a.UserId)
                    .ToListAsync();
                
                // Also follow yourself to see your own reposts in feed
                followedUserIds.Add(currentUserId.Value);
            }

            var repostInteractions = await _context.FeedInteractions
                .Where(i => i.InteractionType == "REPOST" && followedUserIds.Contains(i.UserId))
                .Include(i => i.User)
                .OrderByDescending(i => i.CreatedAt)
                .Take(20)
                .ToListAsync();

            var repostItems = new List<FeedItem>();
            foreach (var rep in repostInteractions)
            {
                FeedItem? item = null;
                if (rep.ItemType == "track")
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
                            ArtistUserId = (t.Album != null && t.Album.Artist != null) ? t.Album.Artist.UserId : null,
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
                else if (rep.ItemType == "studio")
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
                            ArtistUserId = s.UserId,
                            ImageUrl = s.Url,
                            CreatedAt = rep.CreatedAt,
                            MediaType = s.Type,
                            RepostedBy = rep.User?.Username ?? "RESERVED_NODE",
                            IsOriginalSignal = false
                        };
                    }
                }
                else if (rep.ItemType == "journal")
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
                            ArtistUserId = j.UserId,
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

            var combined = tracks.Concat(studio).Concat(journals).Concat(repostItems).Concat(shoutouts)
                .OrderByDescending(x => x.CreatedAt)
                .Take(50)
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
            public int? ArtistUserId { get; set; }
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
