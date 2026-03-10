using FataleCore.Data;
using FataleCore.Models;
using Microsoft.EntityFrameworkCore;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;

namespace FataleCore.Services.Intelligence
{
    public class MeditationWorkerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MeditationWorkerService> _logger;
        private readonly TimeSpan _period = TimeSpan.FromMinutes(5); // Configurable

        public MeditationWorkerService(IServiceProvider serviceProvider, ILogger<MeditationWorkerService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[MEDITATION_WORKER] Started. Will run every {Minutes} minutes.", _period.TotalMinutes);

            using PeriodicTimer timer = new PeriodicTimer(_period);
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await PerformMeditationCycleAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[MEDITATION_WORKER] Error during cycle.");
                }
            }
        }

        private async Task PerformMeditationCycleAsync()
        {
            _logger.LogInformation("[MEDITATION_WORKER] Cycle started: Silently enriching tracks...");

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            // 1. Find tracks listened to recently
            var cutoffTime = DateTime.UtcNow.Subtract(_period).Subtract(TimeSpan.FromMinutes(1)); // slightly longer than period to catch overlaps
            
            var recentTracks = await context.UserListeningEvents
                .Where(e => e.ListenedAt >= cutoffTime && e.TrackType == "youtube")
                .Select(e => e.TrackId)
                .Distinct()
                .ToListAsync();

            if (!recentTracks.Any())
            {
                 _logger.LogInformation("[MEDITATION_WORKER] Cycle skipped: No recent listening events.");
                 return;
            }

            int enrichedCount = 0;
            var apiKey = config["YoutubeSettings:YouTubeApiKey"];
            
            YouTubeService? ytService = null;
            if (!string.IsNullOrEmpty(apiKey))
            {
                ytService = new YouTubeService(new BaseClientService.Initializer() { ApiKey = apiKey, ApplicationName = "FataleCore" });
            }

            // 2. Process each track
            foreach (var videoId in recentTracks)
            {
                var fingerprint = await context.TrackFingerprints.FirstOrDefaultAsync(f => f.TrackId == videoId && f.TrackType == "youtube");
                
                // If it exists and was enriched recently, skip deep metadata fetch, just bump play count
                if (fingerprint != null)
                {
                    fingerprint.PlayCount += 1; // Basic playcount bump
                    
                    if (fingerprint.EnrichedAt > DateTime.UtcNow.AddDays(-7))
                    {
                        continue; // Still fresh
                    }
                }

                // Need full enrichment
                fingerprint ??= new TrackFingerprint { TrackType = "youtube", TrackId = videoId };
                
                if (ytService != null)
                {
                    try 
                    {
                        var request = ytService.Videos.List("snippet,statistics");
                        request.Id = videoId;
                        var response = await request.ExecuteAsync();
                        var video = response.Items.FirstOrDefault();

                        if (video != null)
                        {
                            // Infer channel type
                            string channelTitle = video.Snippet.ChannelTitle.ToLower();
                            if (channelTitle.EndsWith("vevo") || channelTitle.Contains("official")) 
                                fingerprint.ChannelType = "official";
                            else if (channelTitle.EndsWith("- topic")) 
                                fingerprint.ChannelType = "topic";
                            else 
                                fingerprint.ChannelType = "community";

                            // Setup tags combo
                            var rawTags = new List<string>();
                            if (video.Snippet.Tags != null) rawTags.AddRange(video.Snippet.Tags);
                            
                            // Naive keyword extraction from title
                            var titleWords = video.Snippet.Title.ToLower()
                                .Split(new[] { ' ', '-', '|', '(', ')', '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
                            
                            foreach(var word in titleWords) {
                                if (word.Length > 3) rawTags.Add(word);
                            }

                            fingerprint.Tags = string.Join(",", rawTags.Take(20)); // Limit size
                            
                            // View tier
                            var views = video.Statistics.ViewCount ?? 0;
                            fingerprint.ViewTier = CalculateTier((long)views);
                            
                            fingerprint.EnrichedAt = DateTime.UtcNow;
                            fingerprint.PlayCount = fingerprint.PlayCount == 0 ? 1 : fingerprint.PlayCount;

                            if (fingerprint.Id == 0) context.TrackFingerprints.Add(fingerprint);
                            enrichedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                         _logger.LogWarning($"[MEDITATION_WORKER] Failed to enrich video {videoId}: {ex.Message}");
                    }
                }
            }

            await context.SaveChangesAsync();
            _logger.LogInformation($"[MEDITATION_WORKER] Cycle complete. Enriched {enrichedCount} tracks.");
        }

        private int CalculateTier(long viewCount)
        {
            if (viewCount < 10000) return 1;
            if (viewCount < 100000) return 2;
            if (viewCount < 1000000) return 3;
            if (viewCount < 10000000) return 4;
            return 5;
        }
    }
}
