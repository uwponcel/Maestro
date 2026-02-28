using System;
using System.Collections.Generic;
using System.Linq;
using Blish_HUD;
using LiteDB;
using Maestro.Models;

namespace Maestro.Services
{
    public class FavoriteService
    {
        private static readonly Logger Logger = Logger.GetLogger<FavoriteService>();

        private const string FAVORITES_COLLECTION = "favorites";

        public event EventHandler FavoritesChanged;

        private readonly ILiteCollection<FavoriteEntry> _favoritesCollection;
        private readonly HashSet<string> _favoriteKeys;

        public FavoriteService(LiteDatabase database)
        {
            _favoritesCollection = database.GetCollection<FavoriteEntry>(FAVORITES_COLLECTION);
            _favoritesCollection.EnsureIndex(x => x.SongKey, unique: true);

            _favoriteKeys = new HashSet<string>(
                _favoritesCollection.FindAll().Select(f => f.SongKey));

            Logger.Info($"Loaded {_favoriteKeys.Count} favorites");
        }

        public bool IsFavorite(Song song)
        {
            var key = GetSongKey(song);
            return _favoriteKeys.Contains(key);
        }

        public void ToggleFavorite(Song song)
        {
            var key = GetSongKey(song);

            if (_favoriteKeys.Contains(key))
            {
                _favoritesCollection.DeleteMany(x => x.SongKey == key);
                _favoriteKeys.Remove(key);
                Logger.Info($"Removed favorite: {song.Name}");
            }
            else
            {
                _favoritesCollection.Upsert(new FavoriteEntry
                {
                    Id = ObjectId.NewObjectId(),
                    SongKey = key,
                    FavoritedAt = DateTime.UtcNow
                });
                _favoriteKeys.Add(key);
                Logger.Info($"Added favorite: {song.Name}");
            }

            FavoritesChanged?.Invoke(this, EventArgs.Empty);
        }

        public HashSet<string> GetAllFavoriteKeys()
        {
            return new HashSet<string>(_favoriteKeys);
        }

        public static string GetSongKey(Song song)
        {
            if (!string.IsNullOrEmpty(song.CommunityId))
                return song.CommunityId;

            return $"{song.Name}|{song.Artist}|{song.Instrument}".ToLowerInvariant();
        }

        private class FavoriteEntry
        {
            public ObjectId Id { get; set; }
            public string SongKey { get; set; }
            public DateTime FavoritedAt { get; set; }
        }
    }
}
