using FataleCore.Models;

namespace FataleCore.DTOs
{
    public static class MappingExtensions
    {
        public static UserDto ToDto(this User user, Artist? artist = null)
        {
            if (user == null) return null!;
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                CreditsBalance = user.CreditsBalance,
                CreatedAt = user.CreatedAt,
                Biography = user.Biography,
                ProfilePictureUrl = user.ProfilePictureUrl,
                BannerUrl = user.BannerUrl,
                ThemeColor = user.ThemeColor,
                TextColor = user.TextColor,
                BackgroundColor = user.BackgroundColor,
                IsGlass = user.IsGlass,
                WallpaperVideoUrl = user.WallpaperVideoUrl,
                MonitorImageUrl = user.MonitorImageUrl,
                MonitorBackgroundColor = user.MonitorBackgroundColor,
                MonitorIsGlass = user.MonitorIsGlass,
                ResidentSectorId = user.ResidentSectorId,
                CommunityId = user.CommunityId,
                StatusMessage = user.StatusMessage,
                IsLive = artist?.IsLive ?? false,
                FeaturedTrackId = artist?.FeaturedTrackId
            };
        }

        public static ArtistDto ToDto(this Artist artist)
        {
            if (artist == null) return null!;
            return new ArtistDto
            {
                Id = artist.Id,
                Name = artist.Name,
                Bio = artist.Bio,
                ImageUrl = artist.ImageUrl,
                MapX = artist.MapX,
                MapY = artist.MapY,
                SectorId = artist.SectorId,
                IsLive = artist.IsLive,
                FeaturedTrackId = artist.FeaturedTrackId,
                UserId = artist.UserId
            };
        }

        public static AlbumDto ToDto(this Album album)
        {
            if (album == null) return null!;
            return new AlbumDto
            {
                Id = album.Id,
                Title = album.Title,
                ReleaseDate = album.ReleaseDate,
                CoverImageUrl = album.CoverImageUrl,
                ArtistId = album.ArtistId,
                ArtistName = album.Artist?.Name,
                MapX = album.MapX,
                MapY = album.MapY,
                SectorId = album.SectorId
            };
        }

        public static TrackDto ToDto(this Track track)
        {
            if (track == null) return null!;
            return new TrackDto
            {
                Id = track.Id,
                Title = track.Title,
                Genre = track.Genre,
                Duration = track.Duration,
                FilePath = track.FilePath,
                Source = track.Source,
                CoverImageUrl = track.CoverImageUrl,
                MapX = track.MapX,
                MapY = track.MapY,
                SectorId = track.SectorId,
                Price = track.Price,
                IsDownloadable = track.IsDownloadable,
                IsPinned = track.IsPinned,
                IsPosted = track.IsPosted,
                CreatedAt = track.CreatedAt,
                AlbumId = track.AlbumId,
                AlbumTitle = track.Album?.Title,
                ArtistName = track.Album?.Artist?.Name,
                ArtistUserId = track.Album?.Artist?.UserId,
                PlayCount = track.PlayCount
            };
        }

        public static PlaylistDto ToDto(this Playlist playlist)
        {
            if (playlist == null) return null!;
            return new PlaylistDto
            {
                Id = playlist.Id,
                Name = playlist.Name,
                Description = playlist.Description,
                ImageUrl = playlist.ImageUrl,
                IsPublic = playlist.IsPublic,
                IsPinned = playlist.IsPinned,
                IsPosted = playlist.IsPosted,
                UserId = playlist.UserId,
                TrackCount = playlist.TrackCount
            };
        }

        public static GearDto ToDto(this UserGear gear)
        {
            if (gear == null) return null!;
            return new GearDto
            {
                Id = gear.Id,
                UserId = gear.UserId,
                Name = gear.Name,
                Category = gear.Category,
                Notes = gear.Notes,
                DisplayOrder = gear.DisplayOrder
            };
        }
    }
}
