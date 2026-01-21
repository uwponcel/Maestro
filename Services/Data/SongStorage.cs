using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Blish_HUD;
using Blish_HUD.Modules.Managers;
using LiteDB;
using Maestro.Models;

namespace Maestro.Services.Data
{
    public class SongStorage : IDisposable
    {
        private static readonly Logger Logger = Logger.GetLogger<SongStorage>();

        private const string DATABASE_FILE_NAME = "maestro-songs.db";
        private const string SONGS_COLLECTION = "songs";
        private const string MANIFEST_COLLECTION = "manifest";

        private readonly LiteDatabase _database;
        private readonly ILiteCollection<StoredSong> _songsCollection;
        private readonly ILiteCollection<CachedManifest> _manifestCollection;

        public SongStorage(DirectoriesManager directoriesManager)
        {
            var moduleDir = directoriesManager.GetFullDirectoryPath("maestro");
            Directory.CreateDirectory(moduleDir);
            var dbPath = Path.Combine(moduleDir, DATABASE_FILE_NAME);

            Logger.Info($"Opening song storage at {dbPath}");
            _database = new LiteDatabase($"Filename={dbPath};Connection=shared");
            _songsCollection = _database.GetCollection<StoredSong>(SONGS_COLLECTION);
            _manifestCollection = _database.GetCollection<CachedManifest>(MANIFEST_COLLECTION);

            _songsCollection.EnsureIndex(x => x.SongKey, unique: true);
        }

        #region Song Operations

        public List<Song> GetAllSongs()
        {
            var songs = _songsCollection
                .FindAll()
                .Select(x => x.Song)
                .ToList();

            foreach (var song in songs)
            {
                EnsureCommandsParsed(song);
            }

            return songs;
        }

        public Song GetSong(string key)
        {
            var stored = _songsCollection.FindOne(x => x.SongKey == key);
            if (stored?.Song != null)
            {
                EnsureCommandsParsed(stored.Song);
            }
            return stored?.Song;
        }

        public void SaveSong(Song song)
        {
            var key = GetSongKey(song);

            var stored = new StoredSong
            {
                SongKey = key,
                Song = song,
                SavedAt = DateTime.UtcNow
            };

            _songsCollection.Upsert(stored);
            Logger.Info($"Saved song: {song.Name} by {song.Artist} (key: {key})");
        }

        public void DeleteSong(Song song)
        {
            var key = GetSongKey(song);
            _songsCollection.DeleteMany(x => x.SongKey == key);
            Logger.Info($"Deleted song: {song.Name} by {song.Artist}");
        }

        public bool SongExists(string key)
        {
            return _songsCollection.Exists(x => x.SongKey == key);
        }

        public bool SongExists(Song song)
        {
            var key = GetSongKey(song);
            return SongExists(key);
        }

        #endregion

        #region Manifest Operations

        public CommunityManifest GetCachedManifest()
        {
            var cached = _manifestCollection.FindById(1);
            return cached?.Manifest;
        }

        public void SaveManifest(CommunityManifest manifest)
        {
            var cached = new CachedManifest
            {
                Id = 1,
                Manifest = manifest,
                CachedAt = DateTime.UtcNow
            };
            _manifestCollection.Upsert(cached);
            Logger.Debug("Saved manifest to cache");
        }

        #endregion

        private string GetSongKey(Song song)
        {
            // Use CommunityId if present, otherwise use Name|Artist|Instrument
            if (!string.IsNullOrEmpty(song.CommunityId))
            {
                return song.CommunityId;
            }

            return $"{song.Name}|{song.Artist}|{song.Instrument}".ToLowerInvariant();
        }

        private void EnsureCommandsParsed(Song song)
        {
            if (song.Commands.Count == 0 && song.Notes?.Count > 0)
            {
                var commands = NoteParser.Parse(song.Notes);
                song.Commands.AddRange(commands);
            }
        }

        public void Dispose()
        {
            _database?.Dispose();
        }

        private class StoredSong
        {
            public ObjectId Id { get; set; }
            public string SongKey { get; set; }
            public Song Song { get; set; }
            public DateTime SavedAt { get; set; }
        }

        private class CachedManifest
        {
            public int Id { get; set; }
            public CommunityManifest Manifest { get; set; }
            public DateTime CachedAt { get; set; }
        }
    }
}
