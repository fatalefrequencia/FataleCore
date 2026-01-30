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
    }
}
