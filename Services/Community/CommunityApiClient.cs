using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Blish_HUD;
using Maestro.Models;
using Maestro.Services.Data;
using Newtonsoft.Json;

namespace Maestro.Services.Community
{
    public class CommunityApiClient : IDisposable
    {
        private static readonly Logger Logger = Logger.GetLogger<CommunityApiClient>();

        private const string BASE_URL = "https://raw.githubusercontent.com/uwponcel/maestro-songs/master";
        private readonly HttpClient _httpClient;

        public CommunityApiClient()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Maestro-BlishHUD-Module");
        }

        public async Task<CommunityManifest> FetchManifestAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{BASE_URL}/manifest.json";
                Logger.Info($"Fetching community manifest from {url}");

                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var manifest = JsonConvert.DeserializeObject<CommunityManifest>(json);

                Logger.Info($"Fetched manifest with {manifest?.Songs?.Count ?? 0} songs");
                return manifest;
            }
            catch (OperationCanceledException)
            {
                Logger.Debug("Manifest fetch cancelled");
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to fetch community manifest");
                throw;
            }
        }

        public async Task<Song> FetchSongAsync(string songId, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{BASE_URL}/songs/{songId}.json";
                Logger.Info($"Fetching community song {songId}");

                var response = await _httpClient.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var song = SongSerializer.DeserializeJsonContent(json);

                if (song != null)
                {
                    song.CommunityId = songId;
                }

                Logger.Info($"Fetched song: {song?.Name}");
                return song;
            }
            catch (OperationCanceledException)
            {
                Logger.Debug($"Song fetch cancelled for {songId}");
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to fetch community song {songId}");
                throw;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
