using System.Collections.Generic;
using System.Threading.Tasks;
using Blish_HUD;
using Maestro.Models;
using Maestro.Services.Community;

namespace Maestro.Services.Data
{
    public class UserSongStorage
    {
        private static readonly Logger Logger = Logger.GetLogger<UserSongStorage>();

        private readonly CommunitySongCache _cache;

        public UserSongStorage(CommunitySongCache cache)
        {
            _cache = cache;
        }

        public Task<List<Song>> LoadUserSongsAsync()
        {
            var songs = _cache.GetAllImportedSongs();
            Logger.Info($"Loaded {songs.Count} user songs from cache");
            return Task.FromResult(songs);
        }

        public Task SaveSongAsync(Song song)
        {
            _cache.SaveImportedSong(song);
            return Task.CompletedTask;
        }

        public void DeleteSong(Song song)
        {
            _cache.DeleteImportedSong(song);
        }
    }
}
