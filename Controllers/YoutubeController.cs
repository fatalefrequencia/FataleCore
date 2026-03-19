using FataleCore.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public YoutubeController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;

            var apiKey = _configuration["YoutubeSettings:YouTubeApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = _configuration["YOUTUBE_API_KEY"] ?? Environment.GetEnvironmentVariable("YOUTUBE_API_KEY");
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("[YOUTUBE] WARNING: YouTube API Key is missing or empty.");
            }
            else 
            {
                Console.WriteLine($"[YOUTUBE] API Key found (Length: {apiKey.Length})");
            }

            _youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = apiKey,
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

            try
            {
                var searchRequest = _youtubeService.Search.List("snippet");
                searchRequest.Q = query;
                searchRequest.Type = "video";
                searchRequest.MaxResults = 10;

                var searchResponse = await searchRequest.ExecuteAsync();
                var videoIds = searchResponse.Items.Select(i => i.Id.VideoId).ToList();

                if (!videoIds.Any()) return Ok(new List<object>());

                var detailsRequest = _youtubeService.Videos.List("snippet,statistics,contentDetails");
                detailsRequest.Id = string.Join(",", videoIds);
                var detailsResponse = await detailsRequest.ExecuteAsync();

                var random = new Random();
                var results = detailsResponse.Items.Select(v => {
                    var viewCount = v.Statistics.ViewCount ?? 0;
                    var scale = CalculateScale((long)viewCount);

                    return new
                    {
                        Id = v.Id,
                        Title = v.Snippet.Title,
                        Author = v.Snippet.ChannelTitle,
                        ThumbnailUrl = v.Snippet.Thumbnails.High?.Url ?? v.Snippet.Thumbnails.Default__?.Url,
                        ViewCount = viewCount,
                        Scale = scale,
                        PositionX = random.Next(0, 1000),
                        PositionY = random.Next(0, 1000),
                        NodeSize = scale
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
                var discoveryQuery = string.IsNullOrWhiteSpace(query) ? "Best New Music 2024" : query; 
                
                var searchRequest = _youtubeService.Search.List("snippet");
                searchRequest.Q = discoveryQuery;
                searchRequest.Type = "video";
                searchRequest.MaxResults = 20;

                var searchResponse = await searchRequest.ExecuteAsync();
                var videoIds = searchResponse.Items.Select(i => i.Id.VideoId).ToList();

                if (!videoIds.Any()) return Ok(new List<object>());

                var detailsRequest = _youtubeService.Videos.List("snippet,statistics,contentDetails");
                detailsRequest.Id = string.Join(",", videoIds);
                var detailsResponse = await detailsRequest.ExecuteAsync();

                var random = new Random();
                var nodes = detailsResponse.Items.Select(v => {
                    var viewCount = v.Statistics.ViewCount ?? 0;
                    var scale = CalculateScale((long)viewCount);
                    
                    // Convert ISO 8601 duration to M:SS
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

            // check user existence
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound($"User ID {userId} not found.");
            }

            // Return standardized response for Frontend Player (Official API)
            // No more stream extraction. We trigger the IFrame player.
            return Ok(new 
            { 
                AudioUrl = "", 
                VideoId = videoId,
                UseEmbed = true, 
                Format = "youtube-embed" 
            });
        }
    }
}
