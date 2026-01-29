using System;
using System.Net.Http;
using System.Text;
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

        private const string BASE_URL = "https://raw.githubusercontent.com/uwponcel/Maestro/main/community";
        private const string UPLOAD_API_URL = "https://maestro-api.uwponcel.workers.dev/api";
        private readonly HttpClient _httpClient;
        private readonly string _clientId;

        public CommunityApiClient(string clientId = null)
        {
            _clientId = clientId;
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

        public async Task<UploadResponse> UploadSongAsync(Song song, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"{UPLOAD_API_URL}/upload-song";
                Logger.Info($"Uploading song {song.Name} to community");

                var songJson = SongSerializer.SerializeToJson(song);
                var payload = new
                {
                    song = JsonConvert.DeserializeObject(songJson),
                    transcriber = song.Transcriber,
                    clientId = _clientId
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(payload),
                    Encoding.UTF8,
                    "application/json");

                var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
#if DEBUG
                request.Headers.Add("X-Debug-Key", "maestro-debug");
#endif
                var response = await _httpClient.SendAsync(request, cancellationToken);

                var responseBody = await response.Content.ReadAsStringAsync();
                var uploadResponse = JsonConvert.DeserializeObject<UploadResponse>(responseBody);

                if (!response.IsSuccessStatusCode && uploadResponse != null && string.IsNullOrEmpty(uploadResponse.Error))
                {
                    uploadResponse.Error = $"Upload failed with status {(int)response.StatusCode}";
                }

                Logger.Info($"Upload response: {(uploadResponse?.Success == true ? "Success" : "Failed")}");
                return uploadResponse ?? new UploadResponse { Success = false, Error = "Invalid response from server" };
            }
            catch (OperationCanceledException)
            {
                Logger.Debug("Song upload cancelled");
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to upload song {song.Name}");
                return new UploadResponse { Success = false, Error = ex.Message };
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
