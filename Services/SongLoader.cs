using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Blish_HUD;
using Maestro.Models;

namespace Maestro.Services
{
    public static class SongLoader
    {
        private static readonly Logger Logger = Logger.GetLogger(typeof(SongLoader));

        private const string DebugSongsPath = @"C:\git\Maestro\Songs";
        private const string EmbeddedResourceName = "Maestro.Data.songs.bin";

        public static async Task<List<Song>> LoadAllAsync()
        {
            return await Task.Run(() =>
            {
                var songs = new List<Song>();

                try
                {
                    if (Directory.Exists(DebugSongsPath))
                    {
                        Logger.Info($"Debug mode: Loading songs from source directory: {DebugSongsPath}");
                        LoadFromDirectory(songs, DebugSongsPath);
                    }

                    if (songs.Count == 0)
                    {
                        LoadFromBundle(songs);
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

        private static void LoadFromBundle(List<Song> songs)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                using (var stream = assembly.GetManifestResourceStream(EmbeddedResourceName))
                {
                    if (stream == null)
                    {
                        Logger.Warn($"Could not find embedded resource: {EmbeddedResourceName}");
                        return;
                    }

                    var bundleSongs = SongSerializer.DeserializeBundle(stream);
                    songs.AddRange(bundleSongs);
                    Logger.Info($"Loaded {bundleSongs.Count} songs from embedded bundle");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to load songs from embedded bundle");
            }
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
                    Logger.Debug($"Loaded song: {song.DisplayName}");
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, $"Failed to load song: {file}");
                }
            }
        }
    }
}
