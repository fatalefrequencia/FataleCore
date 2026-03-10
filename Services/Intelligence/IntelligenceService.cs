using FataleCore.Data;
using FataleCore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FataleCore.Services.Intelligence
{
    public interface IIntelligenceService
    {
        Task<List<object>> GetRecommendationsAsync(int userId, string lastTrackId, string lastTrackType, int count = 10);
        float[] BuildUserTasteVector(List<UserListeningEvent> history);
        float[] GetTagVector(string tags);
        float CosineSimilarity(float[] a, float[] b);
    }

    public class IntelligenceService : IIntelligenceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        // A static small vocabulary of core genres/tags for simple v1 vectorization
        private static readonly string[] _vocabulary = new[]
        {
            "pop", "rock", "rap", "hip hop", "r&b", "electronic", "dance", "house", "techno", 
            "indie", "alternative", "metal", "punk", "jazz", "blues", "country", "folk", 
            "classical", "ambient", "latin", "reggaeton", "k-pop", "lo-fi", "synth", "wave",
            "vaporwave", "chill", "acoustic", "instrumental", "vocal"
        };

        public IntelligenceService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<List<object>> GetRecommendationsAsync(int userId, string lastTrackId, string lastTrackType, int count = 10)
        {
            // 1. Get User History (Last 50 tracks)
            var history = await _context.UserListeningEvents
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.ListenedAt)
                .Take(50)
                .ToListAsync();

            // 2. Cold Start Fallback: If no history or fingerprints exist, return popular YouTube tracks
            var hasFingerprints = await _context.TrackFingerprints.AnyAsync();
            if (history.Count == 0 || !hasFingerprints)
            {
                return await GetColdStartRecommendationsAsync(count, lastTrackId);
            }

            // 3. Build User Taste Vector
            var userVector = BuildUserTasteVector(history);

            // 4. Get all fingerprints (In a real massive app, we'd pre-filter or use vector DB. Here, in-memory is fine for v1)
            var allFingerprints = await _context.TrackFingerprints
                .Where(f => f.TrackId != lastTrackId) // Don't recommend what just played
                .ToListAsync();

            // Exclude recent history to avoid repeating the same 5 songs
            var recentTrackIds = history.Take(20).Select(h => h.TrackId).ToHashSet();
            var candidates = allFingerprints.Where(f => !recentTrackIds.Contains(f.TrackId)).ToList();
            
            if (!candidates.Any())
            {
                candidates = allFingerprints; // Fallback if they've listened to literally everything
            }

            // --- Phase 2: Dynamic YouTube Expansion ---
            var random = new Random();
            
            // Find top tag from userVector
            float maxWeight = 0;
            string topTag = "music";
            for (int i = 0; i < _vocabulary.Length; i++)
            {
                if (userVector[i] > maxWeight)
                {
                    maxWeight = userVector[i];
                    topTag = _vocabulary[i];
                }
            }

            // Expand pool if small OR randomly 1 in 3 times to always inject fresh music
            if (candidates.Count < 30 || random.Next(3) == 0)
            {
                try
                {
                    var apiKey = _configuration["YoutubeSettings:YouTubeApiKey"];
                    if (!string.IsNullOrEmpty(apiKey))
                    {
                        var ytService = new Google.Apis.YouTube.v3.YouTubeService(new Google.Apis.Services.BaseClientService.Initializer() { ApiKey = apiKey, ApplicationName = "FataleCore" });
                        
                        var searchReq = ytService.Search.List("snippet");
                        string[] flavor = { "new", "mix", "audio", "track", "official", "playlist" };
                        searchReq.Q = topTag + " " + flavor[random.Next(flavor.Length)];
                        searchReq.Type = "video";
                        searchReq.MaxResults = 10;
                        searchReq.VideoCategoryId = "10"; // Music
                        
                        var searchRes = await searchReq.ExecuteAsync();
                        
                        foreach(var item in searchRes.Items)
                        {
                            var vidId = item.Id.VideoId;
                            if (string.IsNullOrEmpty(vidId)) continue;
                            
                            // Check if already in system
                            if (!allFingerprints.Any(f => f.TrackId == vidId))
                            {
                                // Save to YoutubeTracks
                                var newYtTrack = new YoutubeTrack
                                {
                                    YoutubeId = vidId,
                                    Title = item.Snippet.Title ?? "Unknown Title",
                                    ChannelTitle = item.Snippet.ChannelTitle ?? "Unknown Artist",
                                    ThumbnailUrl = item.Snippet.Thumbnails?.High?.Url ?? item.Snippet.Thumbnails?.Default__?.Url ?? "",
                                    ViewCount = 10000, // Initial dummy weight
                                    Duration = "3:30" // Dummy duration until worker updates it
                                };
                                _context.YoutubeTracks.Add(newYtTrack);
                                
                                // Create fingerprint
                                var newFp = new TrackFingerprint
                                {
                                    TrackType = "youtube",
                                    TrackId = vidId,
                                    Tags = topTag,
                                    ViewTier = 3,
                                    ChannelType = (item.Snippet.ChannelTitle?.ToLower().Contains("topic") == true || item.Snippet.ChannelTitle?.ToLower().Contains("vevo") == true) ? "official" : "standard",
                                    EnrichedAt = DateTime.UtcNow,
                                    PlayCount = 0
                                };
                                _context.TrackFingerprints.Add(newFp);
                                candidates.Add(newFp);
                            }
                        }
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[ORGANIC] YouTube search expansion failed: " + ex.Message);
                }
            }
            // ------------------------------------------

            // --- Phase 2: Social Resonance ---
            var followedArtistIds = await _context.UserArtistLikes
                .Where(u => u.UserId == userId)
                .Select(u => u.ArtistId)
                .ToListAsync();

            var followedUserIds = await _context.Artists
                .Where(a => followedArtistIds.Contains(a.Id) && a.UserId.HasValue)
                .Select(a => a.UserId!.Value)
                .ToListAsync();

            var yesterday = DateTime.UtcNow.AddDays(-1);
            var socialRecentTracks = await _context.UserListeningEvents
                .Where(e => followedUserIds.Contains(e.UserId) && e.ListenedAt >= yesterday && e.DurationSeconds >= 30)
                .Select(e => e.TrackId)
                .Distinct()
                .ToListAsync();
                
            var socialResonanceSet = socialRecentTracks.ToHashSet();
            // ---------------------------------

            // 5. Score Candidates
            var scoredCandidates = candidates.Select(f =>
            {
                var trackVector = GetTagVector(f.Tags);
                var sim = CosineSimilarity(userVector, trackVector);
                
                // Boost official/topic channels slightly
                float qualityBoost = (f.ChannelType == "official" || f.ChannelType == "topic") ? 0.1f : 0f;
                // Boost based on view tier
                float popularityBoost = (f.ViewTier * 0.02f); 
                // Social Resonance boost
                float socialBoost = socialResonanceSet.Contains(f.TrackId) ? 0.15f : 0f;
                // Add minor random jitter (0-0.05) to ensure diversity among similar scores
                float jitter = (float)random.NextDouble() * 0.05f;
                
                return new
                {
                    Fingerprint = f,
                    Score = sim + qualityBoost + popularityBoost + socialBoost + jitter
                };
            })
            .OrderByDescending(x => x.Score)
            .Take(count * 2) // Take a larger pool
            .OrderBy(_ => random.Next()) // Shuffle the top results
            .Take(count)
            .ToList();

            // 6. Format Output
            var recommendations = new List<object>();
            foreach (var c in scoredCandidates)
            {
                // We need to fetch the actual title/info.
                if (c.Fingerprint.TrackType == "youtube")
                {
                    var ytTrack = await _context.YoutubeTracks.FirstOrDefaultAsync(y => y.YoutubeId == c.Fingerprint.TrackId);
                    if (ytTrack != null)
                    {
                        recommendations.Add(new
                        {
                            trackId = ytTrack.YoutubeId,
                            trackType = "youtube",
                            title = ytTrack.Title,
                            author = ytTrack.ChannelTitle,
                            thumbnailUrl = ytTrack.ThumbnailUrl,
                            duration = ytTrack.Duration,
                            tags = c.Fingerprint.Tags,
                            matchScore = Math.Round(c.Score, 2)
                        });
                    }
                }
                else if (c.Fingerprint.TrackType == "native")
                {
                     if (int.TryParse(c.Fingerprint.TrackId, out int nativeId))
                     {
                         var nativeTrack = await _context.Tracks
                            .Include(t => t.Album).ThenInclude(a => a!.Artist)
                            .FirstOrDefaultAsync(t => t.Id == nativeId);
                         
                         if (nativeTrack != null)
                         {
                             recommendations.Add(new
                             {
                                  trackId = nativeTrack.Id.ToString(),
                                  trackType = "native",
                                  title = nativeTrack.Title,
                                  author = nativeTrack.Album?.Artist?.Name ?? "Unknown",
                                  thumbnailUrl = nativeTrack.CoverImageUrl,
                                  duration = nativeTrack.Duration,
                                  tags = c.Fingerprint.Tags,
                                  matchScore = Math.Round(c.Score, 2)
                             });
                         }
                     }
                }
            }
            
            // If scoring didn't yield enough (e.g. tracks deleted), pad with popular
            if (recommendations.Count < count)
            {
                var padded = await GetColdStartRecommendationsAsync(count - recommendations.Count, lastTrackId);
                recommendations.AddRange(padded.Where(p => !recommendations.Any(r => GetPropValue(r, "trackId")?.ToString() == GetPropValue(p, "trackId")?.ToString())));
            }

            return recommendations.Take(count).ToList();
        }

        private async Task<List<object>> GetColdStartRecommendationsAsync(int count, string excludeTrackId)
        {
            var random = new Random();
            // Take top 30 most viewed and shuffle them to pick the 'count' requested
            var popularYtPool = await _context.YoutubeTracks
                .Where(t => t.YoutubeId != excludeTrackId)
                .OrderByDescending(t => t.ViewCount)
                .Take(30)
                .ToListAsync();

            var popularYt = popularYtPool
                .OrderBy(_ => random.Next())
                .Take(count)
                .Select(t => (object)new
                {
                    trackId = t.YoutubeId,
                    trackType = "youtube",
                    title = t.Title,
                    author = t.ChannelTitle,
                    thumbnailUrl = t.ThumbnailUrl,
                    duration = t.Duration,
                    tags = "popular",
                    matchScore = 0.5f
                })
                .ToList();

            return popularYt;
        }

        public float[] BuildUserTasteVector(List<UserListeningEvent> history)
        {
            var userVector = new float[_vocabulary.Length];
            if (history == null || history.Count == 0) return userVector;

            // Mood vs Identity & Skip Detection
            for (int i = 0; i < history.Count; i++)
            {
                var trackEvent = history[i];
                var trackVector = GetTagVector(trackEvent.Tags);
                
                // Track skip detection: if listened for < 30 seconds, it's a negative signal
                bool isSkip = trackEvent.DurationSeconds > 0 && trackEvent.DurationSeconds < 30;

                // i = 0 is most recent. 
                // Mood (0-9): high weight. Identity (10+): low weight.
                float weight = (i < 10) ? 1.0f - (i * 0.05f) : 0.3f - ((i - 10) * 0.005f); 
                weight = Math.Max(0.05f, weight);

                float multiplier = isSkip ? -1.0f : 1.0f;
                
                for (int j = 0; j < _vocabulary.Length; j++)
                {
                    userVector[j] += trackVector[j] * weight * multiplier;
                }
            }

            // Normalize
            float magnitude = (float)Math.Sqrt(userVector.Sum(x => x * x));
            if (magnitude > 0)
            {
                for (int i = 0; i < userVector.Length; i++) userVector[i] /= magnitude;
            }

            return userVector;
        }

        public float[] GetTagVector(string tags)
        {
            var vector = new float[_vocabulary.Length];
            if (string.IsNullOrWhiteSpace(tags)) return vector;

            var lowerTags = tags.ToLowerInvariant();
            
            for (int i = 0; i < _vocabulary.Length; i++)
            {
                if (lowerTags.Contains(_vocabulary[i]))
                {
                    vector[i] = 1.0f;
                }
            }

            // Normalize
            float magnitude = (float)Math.Sqrt(vector.Sum(x => x * x));
            if (magnitude > 0)
            {
                for (int i = 0; i < vector.Length; i++) vector[i] /= magnitude;
            }

            return vector;
        }

        public float CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length) throw new ArgumentException("Vectors must be same length");
            
            float dotProduct = 0;
            float magA = 0;
            float magB = 0;

            for (int i = 0; i < a.Length; i++)
            {
                dotProduct += a[i] * b[i];
                magA += a[i] * a[i];
                magB += b[i] * b[i];
            }

            magA = (float)Math.Sqrt(magA);
            magB = (float)Math.Sqrt(magB);

            if (magA == 0 || magB == 0) return 0;
            return dotProduct / (magA * magB);
        }

        private static object? GetPropValue(object src, string propName)
        {
            return src.GetType().GetProperty(propName)?.GetValue(src, null);
        }
    }
}
