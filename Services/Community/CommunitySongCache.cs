using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Blish_HUD;
using Blish_HUD.Modules.Managers;
using LiteDB;
using Maestro.Models;
using Maestro.Services.Data;

namespace Maestro.Services.Community
{
    public class CommunitySongCache : IDisposable
    {
        private static readonly Logger Logger = Logger.GetLogger<CommunitySongCache>();

        private const string DatabaseFileName = "maestro-cache.db";
        private const string CommunitySongsCollection = "community_songs";
        private const string ImportedSongsCollection = "imported_songs";
        private const string ManifestCollection = "manifest";

        private readonly LiteDatabase _database;
        private readonly ILiteCollection<CachedSong> _communityCollection;
        private readonly ILiteCollection<CachedSong> _importedCollection;
        private readonly ILiteCollection<CachedManifest> _manifestCollection;

        public CommunitySongCache(DirectoriesManager directoriesManager)
        {
            var moduleDir = directoriesManager.GetFullDirectoryPath("maestro");
            Directory.CreateDirectory(moduleDir);
            var dbPath = Path.Combine(moduleDir, DatabaseFileName);

            Logger.Info($"Opening song cache at {dbPath}");
            _database = new LiteDatabase($"Filename={dbPath};Connection=shared");
            _communityCollection = _database.GetCollection<CachedSong>(CommunitySongsCollection);
            _importedCollection = _database.GetCollection<CachedSong>(ImportedSongsCollection);
            _manifestCollection = _database.GetCollection<CachedManifest>(ManifestCollection);

            _communityCollection.EnsureIndex(x => x.SongKey, unique: true);
            _importedCollection.EnsureIndex(x => x.SongKey, unique: true);
        }

        #region Community Songs

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

        public Song GetCachedSong(string communityId)
        {
            var cached = _communityCollection.FindOne(x => x.SongKey == communityId);
            if (cached?.Song != null)
            {
                EnsureCommandsParsed(cached.Song);
            }
            return cached?.Song;
        }

        public List<Song> GetAllCachedSongs()
        {
            var songs = _communityCollection
                .FindAll()
                .Select(x => x.Song)
                .ToList();

            foreach (var song in songs)
            {
                EnsureCommandsParsed(song);
            }

            return songs;
        }

        public void SaveSong(Song song)
        {
            if (string.IsNullOrEmpty(song.CommunityId))
            {
                Logger.Warn("Attempted to cache song without CommunityId");
                return;
            }

            var cached = new CachedSong
            {
                SongKey = song.CommunityId,
                Song = song,
                SavedAt = DateTime.UtcNow
            };

            _communityCollection.Upsert(cached);
            Logger.Info($"Cached community song: {song.Name} ({song.CommunityId})");
        }

        public bool IsSongCached(string communityId)
        {
            return _communityCollection.Exists(x => x.SongKey == communityId);
        }

        public void DeleteSong(string communityId)
        {
            _communityCollection.DeleteMany(x => x.SongKey == communityId);
            Logger.Info($"Removed cached song: {communityId}");
        }

        public int GetCachedSongCount()
        {
            return _communityCollection.Count();
        }

        #endregion

        #region Imported Songs

        public List<Song> GetAllImportedSongs()
        {
            var songs = _importedCollection
                .FindAll()
                .Select(x => x.Song)
                .ToList();

            foreach (var song in songs)
            {
                song.IsUserImported = true;
                EnsureCommandsParsed(song);
            }

            return songs;
        }

        public void SaveImportedSong(Song song)
        {
            var key = GetImportedSongKey(song);

            var cached = new CachedSong
            {
                SongKey = key,
                Song = song,
                SavedAt = DateTime.UtcNow
            };

            _importedCollection.Upsert(cached);
            Logger.Info($"Saved imported song: {song.Name} by {song.Artist}");
        }

        public void DeleteImportedSong(Song song)
        {
            var key = GetImportedSongKey(song);
            _importedCollection.DeleteMany(x => x.SongKey == key);
            Logger.Info($"Deleted imported song: {song.Name} by {song.Artist}");
        }

        private string GetImportedSongKey(Song song)
        {
            return $"{song.Name}|{song.Artist}|{song.Instrument}".ToLowerInvariant();
        }

        #endregion

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

        private class CachedSong
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
