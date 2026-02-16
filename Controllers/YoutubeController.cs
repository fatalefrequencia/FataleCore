using FataleCore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos.Streams;

namespace FataleCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class YoutubeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly YoutubeClient _youtubeClient;
        private readonly IConfiguration _configuration;

        public YoutubeController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;

            var cookies = _configuration["YoutubeSettings:Cookies"];
            var httpClient = new HttpClient();

            if (!string.IsNullOrEmpty(cookies))
            {
                var handler = new HttpClientHandler();
                handler.UseCookies = true;
                handler.CookieContainer = new System.Net.CookieContainer();
                
                // Parse simple cookie string (semi-colon separated)
                foreach (var cookie in cookies.Split(';'))
                {
                    var parts = cookie.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        var name = parts[0].Trim();
                        var value = parts[1].Trim();
                        handler.CookieContainer.Add(new System.Net.Cookie(name, value, "/", ".youtube.com"));
                    }
                }
                httpClient = new HttpClient(handler);
            }
            
            // Add user agent to act like a browser
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            _youtubeClient = new YoutubeClient(httpClient);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Query cannot be empty.");
            }

            try
            {
                var searchResults = await _youtubeClient.Search.GetVideosAsync(query).CollectAsync(10);
                
                // Fetch details in parallel to get ViewCount
                var detailedVideos = await Task.WhenAll(searchResults.Select(async v => 
                {
                    try 
                    {
                        return await _youtubeClient.Videos.GetAsync(v.Id);
                    }
                    catch 
                    {
                        return null; 
                    }
                }));

                var random = new Random();
                var results = detailedVideos.Where(v => v != null).Select(v => {
                    var viewCount = v.Engagement.ViewCount;
                    var scale = CalculateScale(viewCount);

                    return new
                    {
                        Id = v.Id.Value,
                        Title = v.Title,
                        Author = v.Author.ChannelTitle,
                        ThumbnailUrl = v.Thumbnails.GetWithHighestResolution().Url,
                        ViewCount = viewCount,
                        Scale = scale,
                        // Visual Layout Properties
                        PositionX = random.Next(0, 1000),
                        PositionY = random.Next(0, 1000),
                        NodeSize = scale // Determine node size based on popularity scale (1-5)
                    };
                });

                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("discovery-nodes")]
        public async Task<IActionResult> GetDiscoveryNodes([FromQuery] string? query = null)
        {
            try
            {
                // Use provided query or default to a discovery mix
                var discoveryQuery = string.IsNullOrWhiteSpace(query) ? "Best New Music" : query; 
                var searchResults = await _youtubeClient.Search.GetVideosAsync(discoveryQuery).CollectAsync(20);

                var detailedVideos = await Task.WhenAll(searchResults.Select(async v => 
                {
                     try { return await _youtubeClient.Videos.GetAsync(v.Id); } catch { return null; }
                }));

                var random = new Random();

                // Map to "Local Track" format with Layout Properties
                var nodes = detailedVideos.Where(v => v != null).Select(v => {
                    var viewCount = v.Engagement.ViewCount;
                    var scale = CalculateScale(viewCount);

                    return new
                    {
                        Id = v.Id.Value, // String ID for YouTube
                        Title = v.Title,
                        Genre = "YouTube",
                        Duration = v.Duration.HasValue ? v.Duration.Value.ToString(@"m\:ss") : "0:00",
                        FilePath = $"youtube:{v.Id.Value}", // Signal for frontend
                        CoverImageUrl = v.Thumbnails.GetWithHighestResolution().Url,
                        AlbumId = 0, // No real album
                        Album = new 
                        { 
                            Title = "YouTube Discovery", 
                            Artist = new 
                            { 
                                Name = v.Author.ChannelTitle,
                                ImageUrl = "" 
                            } 
                        },
                        // Enhanced Metadata & Layout
                        ViewCount = viewCount,
                        Scale = scale,
                        PositionX = random.Next(0, 1000),
                        PositionY = random.Next(0, 1000),
                        NodeSize = scale
                    };
                });

                return Ok(nodes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private int CalculateScale(long viewCount)
        {
            if (viewCount < 10000) return 1;
            if (viewCount < 100000) return 2;
            if (viewCount < 1000000) return 3;
            if (viewCount < 10000000) return 4;
            return 5;
        }

        [HttpGet("stream")]
        public async Task<IActionResult> Stream([FromQuery] string videoId, [FromQuery] int userId)
        {
            if (string.IsNullOrWhiteSpace(videoId))
            {
                return BadRequest("VideoId is required.");
            }

            // check user existence and balance (Read-Only first)
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (user.CreditsBalance <= 0)
            {
                return StatusCode(403, "Insufficient credits.");
            }

            try
            {
                // 1. Get Video Metadata (needed for fallback search)
                var video = await _youtubeClient.Videos.GetAsync(videoId);
                var targetVideoId = videoId;
                StreamManifest? streamManifest = null;

                // 2. Try Fetching Manifest with Fallback
                try
                {
                    streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(targetVideoId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[YOUTUBE_FALLBACK] Primary video {videoId} failed: {ex.Message}. Searching fallback...");
                    
                    var fallbackQuery = $"{video.Title} {video.Author.ChannelTitle} lyrics"; // Prefer explicit lyric videos
                    // Get top 3 to have better chance, but just taking first for now
                    var fallbackResults = await _youtubeClient.Search.GetVideosAsync(fallbackQuery).CollectAsync(3);
                    var fallbackVideo = fallbackResults.FirstOrDefault(v => v.Id.Value != videoId) ?? fallbackResults.FirstOrDefault();

                    if (fallbackVideo != null)
                    {
                        targetVideoId = fallbackVideo.Id;
                        Console.WriteLine($"[YOUTUBE_FALLBACK] Found fallback: {fallbackVideo.Title} ({targetVideoId})");
                        try 
                        {
                            streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(targetVideoId);
                        }
                        catch(Exception fallbackEx)
                        {
                            Console.WriteLine($"[YOUTUBE_FALLBACK] Fallback also failed: {fallbackEx.Message}");
                             return StatusCode(500, $"Unable to stream video. Error: {fallbackEx.Message}");
                        }
                    }
                    else
                    {
                         Console.WriteLine("[YOUTUBE_FALLBACK] No fallback found.");
                         return StatusCode(500, $"Unable to stream video. Error: {ex.Message}");
                    }
                }

                if (streamManifest == null)
                {
                    return StatusCode(500, "Unable to retrieve stream manifest.");
                }

                // 3. Select Stream (Strictly Prefer M4A/AAC for browser compatibility)
                var audioStreams = streamManifest.GetAudioOnlyStreams();
                
                // Try to get the highest bitrate MP4 (AAC) audio first
                var streamInfo = audioStreams
                    .Where(s => s.Container == Container.Mp4)
                    .GetWithHighestBitrate();

                // If no MP4 audio, try WebM (Opus) but warn/log, as Safari/iOS might struggle without transcoding
                if (streamInfo == null)
                {
                    Console.WriteLine("[YOUTUBE_STREAM] No M4A/AAC stream found. Falling back to WebM/Opus.");
                    streamInfo = audioStreams.GetWithHighestBitrate();
                }

                if (streamInfo == null)
                {
                    return NotFound("Audio stream not available in any supported format.");
                }

                Console.WriteLine($"[YOUTUBE_STREAM] Serving stream: {streamInfo.Container} @ {streamInfo.Bitrate}");

                // 4. Deduct Credits (Only on success)
                user.CreditsBalance -= 1;
                await _context.SaveChangesAsync();

                return Ok(new { AudioUrl = streamInfo.Url, FallbackUsed = targetVideoId != videoId, Format = streamInfo.Container.Name });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[YOUTUBE_ERROR] Stream fetch failed: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
