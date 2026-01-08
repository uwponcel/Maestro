using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Modules.Managers;
using Maestro.Models;

namespace Maestro.Services.Data
{
    public class UserSongStorage
    {
        private static readonly Logger Logger = Logger.GetLogger<UserSongStorage>();

        private readonly string _userSongsPath;

        public UserSongStorage(DirectoriesManager directories)
        {
            var moduleDir = directories.GetFullDirectoryPath("common");
            _userSongsPath = Path.Combine(moduleDir, "UserSongs");

            EnsureDirectoryExists();
        }

        private void EnsureDirectoryExists()
        {
            if (!Directory.Exists(_userSongsPath))
            {
                Directory.CreateDirectory(_userSongsPath);
                Logger.Info($"Created user songs directory: {_userSongsPath}");
            }
        }

        public async Task<List<Song>> LoadUserSongsAsync()
        {
            var songs = new List<Song>();

            if (!Directory.Exists(_userSongsPath))
                return songs;

            var files = Directory.GetFiles(_userSongsPath, "*.json");

            foreach (var file in files)
            {
                try
                {
                    var json = await Task.Run(() => File.ReadAllText(file));
                    var song = SongSerializer.DeserializeJsonContent(json);
                    song.IsUserImported = true;
                    songs.Add(song);
                    Logger.Debug($"Loaded user song: {song.DisplayName}");
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, $"Failed to load user song from {file}");
                }
            }

            Logger.Info($"Loaded {songs.Count} user songs");
            return songs;
        }

        public async Task SaveSongAsync(Song song)
        {
            EnsureDirectoryExists();

            var fileName = GetSafeFileName(song);
            var filePath = Path.Combine(_userSongsPath, fileName);

            try
            {
                var json = SongSerializer.SerializeToJson(song);
                await Task.Run(() => File.WriteAllText(filePath, json));
                Logger.Info($"Saved user song: {song.DisplayName} to {filePath}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to save user song: {song.DisplayName}");
                throw;
            }
        }

        public void DeleteSong(Song song)
        {
            var fileName = GetSafeFileName(song);
            var filePath = Path.Combine(_userSongsPath, fileName);

            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Logger.Info($"Deleted user song: {song.DisplayName}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to delete user song: {song.DisplayName}");
                throw;
            }
        }

        private string GetSafeFileName(Song song)
        {
            var name = $"{song.Name} - {song.Artist}.json";
            var invalidChars = Path.GetInvalidFileNameChars();
            return new string(name.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
        }
    }
}
