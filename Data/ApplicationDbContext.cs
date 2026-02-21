using Microsoft.EntityFrameworkCore;
using FataleCore.Models;

namespace FataleCore.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Artist> Artists { get; set; }
        public DbSet<Album> Albums { get; set; }
        public DbSet<Track> Tracks { get; set; }
        public DbSet<UserLike> UserLikes { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<TrackPurchase> TrackPurchases { get; set; }
        public DbSet<UserArtistLike> UserArtistLikes { get; set; }
        public DbSet<Playlist> Playlists { get; set; }
        public DbSet<DiscoveryEvent> DiscoveryEvents { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<PlaylistTrack> PlaylistTracks { get; set; }
        public DbSet<YoutubeTrack> YoutubeTracks { get; set; }
        public DbSet<YoutubeCacheSubscription> YoutubeCacheSubscriptions { get; set; }
        public DbSet<CachedYoutubeTrack> CachedYoutubeTracks { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<JournalEntry> JournalEntries { get; set; }
        public DbSet<StudioContent> StudioContents { get; set; }
        public DbSet<FeedInteraction> FeedInteractions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Suppress the pending model changes warning to allow manual schema updates in Program.cs
            optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }
    }
}
