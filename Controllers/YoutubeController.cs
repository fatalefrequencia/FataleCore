using FataleCore.Data;
using FataleCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using Google.Apis.YouTube.v3;
using Google.Apis.Services;
using Google.Apis.YouTube.v3.Data;

namespace FataleCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class YoutubeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly YouTubeService _youtubeService;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly bool _hasApiKey;

        // Cache durations
        private static readonly TimeSpan SearchCacheDuration = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan DiscoveryCacheDuration = TimeSpan.FromHours(1);

        public YoutubeController(ApplicationDbContext context, IConfiguration configuration, IMemoryCache cache)
        {
            _context = context;
            _configuration = configuration;
            _cache = cache;

            var apiKey = _configuration["YoutubeSettings:YouTubeApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = _configuration["YOUTUBE_API_KEY"] ?? Environment.GetEnvironmentVariable("YOUTUBE_API_KEY");
            }

            _hasApiKey = !string.IsNullOrEmpty(apiKey);

            if (!_hasApiKey)
            {
                Console.WriteLine("[YOUTUBE] WARNING: YouTube API Key is missing or empty.");
            }
            else 
            {
                Console.WriteLine($"[YOUTUBE] API Key found (Length: {apiKey?.Length ?? 0})");
            }

            _youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = apiKey ?? "",
                ApplicationName = "FataleCore"
            });
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Query cannot be empty.");
            }

            var cacheKey = $"yt_search_{query.ToLowerInvariant().Trim()}";

            // 1. Check in-memory cache first
            if (_cache.TryGetValue(cacheKey, out object? cachedResults) && cachedResults != null)
            {
                Console.WriteLine($"[YOUTUBE] Cache hit for search: '{query}'");
                return Ok(cachedResults);
            }

            // 2. Try YouTube API
            if (_hasApiKey)
            {
                try
                {
                    var results = await FetchFromYouTubeApi(query, maxResults: 5);
                    
                    if (results.Any())
                    {
                        // Cache the results
                        _cache.Set(cacheKey, results, SearchCacheDuration);
                        Console.WriteLine($"[YOUTUBE] API success for search: '{query}' — {results.Count()} results cached for 30min");

                        // Persist to DB in background (fire-and-forget for speed)
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await PersistTracksToDb(results);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[YOUTUBE] DB persist error (non-fatal): {ex.Message}");
                            }
                        });

                        return Ok(results);
                    }
                }
                catch (Google.GoogleApiException gex) when (gex.HttpStatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    Console.WriteLine($"[YOUTUBE] QUOTA EXHAUSTED — falling back to DB for: '{query}'");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[YOUTUBE] API error: {ex.Message} — falling back to DB");
                }
            }

            // 3. Fallback: search local DB
            var dbResults = await SearchLocalDb(query);
            if (dbResults.Any())
            {
                Console.WriteLine($"[YOUTUBE] DB fallback returned {dbResults.Count()} results for: '{query}'");
                _cache.Set(cacheKey, dbResults, TimeSpan.FromMinutes(5)); // shorter cache for DB results
                return Ok(dbResults);
            }

            Console.WriteLine($"[YOUTUBE] No results found anywhere for: '{query}'");
            return Ok(new List<object>());
        }

        [HttpGet("discovery-nodes")]
        public async Task<IActionResult> GetDiscoveryNodes([FromQuery] string? query = null)
        {
            var discoveryQuery = string.IsNullOrWhiteSpace(query) ? "Best New Music 2024" : query;
            var cacheKey = $"yt_discovery_{discoveryQuery.ToLowerInvariant().Trim()}";

            // 1. Check in-memory cache first
            if (_cache.TryGetValue(cacheKey, out object? cachedNodes) && cachedNodes != null)
            {
                Console.WriteLine($"[YOUTUBE] Cache hit for discovery: '{discoveryQuery}'");
                return Ok(cachedNodes);
            }

            // 2. Try YouTube API
            if (_hasApiKey)
            {
                try
                {
                    var searchRequest = _youtubeService.Search.List("snippet");
                    searchRequest.Q = discoveryQuery;
                    searchRequest.Type = "video";
                    searchRequest.MaxResults = 5;

                    var searchResponse = await searchRequest.ExecuteAsync();
                    var videoIds = searchResponse.Items
                        .Where(i => i.Id?.VideoId != null)
                        .Select(i => i.Id.VideoId)
                        .ToList();

                    if (videoIds.Any())
                    {
                        var detailsRequest = _youtubeService.Videos.List("snippet,statistics,contentDetails");
                        detailsRequest.Id = string.Join(",", videoIds);
                        var detailsResponse = await detailsRequest.ExecuteAsync();

                        var random = new Random();
                        var nodes = detailsResponse.Items.Select(v => {
                            var viewCount = v.Statistics?.ViewCount ?? 0;
                            var scale = CalculateScale((long)viewCount);
                            
                            string durationStr = "0:00";
                            try {
                                var duration = System.Xml.XmlConvert.ToTimeSpan(v.ContentDetails.Duration);
                                durationStr = $"{(int)duration.TotalMinutes}:{duration.Seconds:D2}";
                            } catch {}

                            return new
                            {
                                Id = v.Id,
                                Title = v.Snippet.Title,
                                Genre = "YouTube",
                                Duration = durationStr,
                                FilePath = $"youtube:{v.Id}",
                                CoverImageUrl = v.Snippet.Thumbnails.High?.Url ?? v.Snippet.Thumbnails.Default__?.Url,
                                AlbumId = 0,
                                Album = new 
                                { 
                                    Title = "YouTube Discovery", 
                                    Artist = new 
                                    { 
                                        Name = v.Snippet.ChannelTitle,
                                        ImageUrl = "" 
                                    } 
                                },
                                ViewCount = viewCount,
                                Scale = scale,
                                PositionX = random.Next(0, 1000),
                                PositionY = random.Next(0, 1000),
                                NodeSize = scale
                            };
                        }).ToList();

                        _cache.Set(cacheKey, nodes, DiscoveryCacheDuration);
                        Console.WriteLine($"[YOUTUBE] Discovery API success — {nodes.Count} nodes cached for 1hr");

                        // Also persist these to DB
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                var trackData = nodes.Select(n => new
                                {
                                    n.Id,
                                    n.Title,
                                    Author = n.Album.Artist.Name,
                                    ThumbnailUrl = n.CoverImageUrl,
                                    n.ViewCount,
                                    n.Duration
                                });
                                await PersistTracksToDb(trackData);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[YOUTUBE] Discovery DB persist error (non-fatal): {ex.Message}");
                            }
                        });

                        return Ok(nodes);
                    }
                }
                catch (Google.GoogleApiException gex) when (gex.HttpStatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    Console.WriteLine($"[YOUTUBE] QUOTA EXHAUSTED — falling back to DB for discovery");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[YOUTUBE] Discovery API error: {ex.Message}");
                }
            }

            // 3. Fallback: return whatever we have in the DB
            var dbFallback = await GetDiscoveryFromDb(discoveryQuery);
            if (dbFallback.Any())
            {
                _cache.Set(cacheKey, dbFallback, TimeSpan.FromMinutes(10));
                Console.WriteLine($"[YOUTUBE] Discovery DB fallback: {dbFallback.Count()} nodes");
            }
            return Ok(dbFallback);
        }

        [HttpGet("stream")]
        public async Task<IActionResult> Stream([FromQuery] string videoId, [FromQuery] int userId)
        {
            if (string.IsNullOrWhiteSpace(videoId))
            {
                return BadRequest("VideoId is required.");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound($"User ID {userId} not found.");
            }

            return Ok(new 
            { 
                AudioUrl = "", 
                VideoId = videoId,
                UseEmbed = true, 
                Format = "youtube-embed" 
            });
        }

        // ── Private Helpers ──────────────────────────────────────────────

        private async Task<IEnumerable<object>> FetchFromYouTubeApi(string query, int maxResults)
        {
            var searchRequest = _youtubeService.Search.List("snippet");
            searchRequest.Q = query;
            searchRequest.Type = "video";
            searchRequest.MaxResults = maxResults;

            var searchResponse = await searchRequest.ExecuteAsync();
            var videoIds = searchResponse.Items
                .Where(i => i.Id?.VideoId != null)
                .Select(i => i.Id.VideoId)
                .ToList();

            if (!videoIds.Any()) return Enumerable.Empty<object>();

            var detailsRequest = _youtubeService.Videos.List("snippet,statistics,contentDetails");
            detailsRequest.Id = string.Join(",", videoIds);
            var detailsResponse = await detailsRequest.ExecuteAsync();

            var random = new Random();
            return detailsResponse.Items.Select(v => {
                var viewCount = v.Statistics?.ViewCount ?? 0;
                var scale = CalculateScale((long)viewCount);

                string durationStr = "0:00";
                try {
                    var duration = System.Xml.XmlConvert.ToTimeSpan(v.ContentDetails.Duration);
                    durationStr = $"{(int)duration.TotalMinutes}:{duration.Seconds:D2}";
                } catch {}

                return (object)new
                {
                    Id = v.Id,
                    Title = v.Snippet.Title,
                    Author = v.Snippet.ChannelTitle,
                    ThumbnailUrl = v.Snippet.Thumbnails.High?.Url ?? v.Snippet.Thumbnails.Default__?.Url,
                    ViewCount = viewCount,
                    Duration = durationStr,
                    Scale = scale,
                    PositionX = random.Next(0, 1000),
                    PositionY = random.Next(0, 1000),
                    NodeSize = scale
                };
            }).ToList();
        }

        private async Task PersistTracksToDb(IEnumerable<dynamic> tracks)
        {
            // Use a separate context to avoid threading issues
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(_context.Database.GetConnectionString());
            
            using var dbContext = new ApplicationDbContext(optionsBuilder.Options);

            foreach (var track in tracks)
            {
                string youtubeId = track.Id?.ToString() ?? "";
                if (string.IsNullOrEmpty(youtubeId)) continue;

                var exists = await dbContext.YoutubeTracks
                    .AnyAsync(t => t.YoutubeId == youtubeId);

                if (!exists)
                {
                    dbContext.YoutubeTracks.Add(new YoutubeTrack
                    {
                        YoutubeId = youtubeId,
                        Title = track.Title?.ToString() ?? "",
                        ChannelTitle = (track.Author ?? "").ToString(),
                        ThumbnailUrl = (track.ThumbnailUrl ?? "").ToString(),
                        ViewCount = (long)(track.ViewCount ?? 0),
                        Duration = (track.Duration ?? "0:00").ToString()
                    });
                }
            }

            await dbContext.SaveChangesAsync();
        }

        private async Task<List<object>> SearchLocalDb(string query)
        {
            var random = new Random();
            var queryLower = query.ToLowerInvariant();
            
            var tracks = await _context.YoutubeTracks
                .Where(t => t.Title.ToLower().Contains(queryLower) || 
                            t.ChannelTitle.ToLower().Contains(queryLower))
                .OrderByDescending(t => t.ViewCount)
                .Take(5)
                .ToListAsync();

            return tracks.Select(t => (object)new
            {
                Id = t.YoutubeId,
                Title = t.Title,
                Author = t.ChannelTitle,
                ThumbnailUrl = t.ThumbnailUrl,
                ViewCount = t.ViewCount,
                Duration = t.Duration,
                Scale = CalculateScale(t.ViewCount),
                PositionX = random.Next(0, 1000),
                PositionY = random.Next(0, 1000),
                NodeSize = CalculateScale(t.ViewCount)
            }).ToList();
        }

        private async Task<List<object>> GetDiscoveryFromDb(string query)
        {
            var random = new Random();

            // Try matching by query first, then fall back to any recent tracks
            var tracks = await _context.YoutubeTracks
                .OrderByDescending(t => t.ViewCount)
                .Take(5)
                .ToListAsync();

            return tracks.Select(t => (object)new
            {
                Id = t.YoutubeId,
                Title = t.Title,
                Genre = "YouTube",
                Duration = t.Duration,
                FilePath = $"youtube:{t.YoutubeId}",
                CoverImageUrl = t.ThumbnailUrl,
                AlbumId = 0,
                Album = new 
                { 
                    Title = "YouTube Discovery", 
                    Artist = new 
                    { 
                        Name = t.ChannelTitle,
                        ImageUrl = "" 
                    } 
                },
                ViewCount = t.ViewCount,
                Scale = CalculateScale(t.ViewCount),
                PositionX = random.Next(0, 1000),
                PositionY = random.Next(0, 1000),
                NodeSize = CalculateScale(t.ViewCount)
            }).ToList();
        }

        private int CalculateScale(long viewCount)
        {
            if (viewCount < 10000) return 1;
            if (viewCount < 100000) return 2;
            if (viewCount < 1000000) return 3;
            if (viewCount < 10000000) return 4;
            return 5;
        }
    }
}
