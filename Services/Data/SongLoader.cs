using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Blish_HUD;
using Maestro.Models;

namespace Maestro.Services.Data
{
    public static class SongLoader
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(SongLoader));

        public static async Task<List<Song>> LoadAllAsync(string songsDirectory)
        {
            return await Task.Run(() =>
            {
                var songs = new List<Song>();

                try
                {
                    if (Directory.Exists(songsDirectory))
                    {
                        Logger.Info($"Loading songs from: {songsDirectory}");
                        LoadFromDirectory(songs, songsDirectory);
                    }
                    else
                    {
                        Logger.Warn($"Songs directory not found: {songsDirectory}");
                    }

                    Logger.Info($"Total songs loaded: {songs.Count}");
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Failed to load songs - module will continue with empty song list");
                }

                return songs;
            });
        }

        private static void LoadFromDirectory(List<Song> songs, string path)
        {
            var jsonFiles = Directory.GetFiles(path, "*.json");
            Logger.Info($"Found {jsonFiles.Length} .json files in {path}");

            foreach (var file in jsonFiles)
            {
                try
                {
                    var song = SongSerializer.DeserializeJson(file);
                    songs.Add(song);
                    Logger.Debug($"Loaded song: {song.DisplayName} ({song.Commands.Count} commands)");
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, $"Failed to load song: {file}");
                }
            }
        }
    }
}
