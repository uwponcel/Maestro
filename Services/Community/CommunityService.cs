using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Blish_HUD;
using Maestro.Models;
using Maestro.Services.Data;

namespace Maestro.Services.Community
{
    public class CommunityService : IDisposable
    {
        private static readonly Logger Logger = Logger.GetLogger<CommunityService>();

        public event EventHandler<DownloadProgressEventArgs> DownloadProgressChanged;
        public event EventHandler ManifestRefreshed;

        private readonly CommunityApiClient _apiClient;
        private readonly SongStorage _songStorage;
        private readonly List<Song> _mainSongList;
        private readonly Dictionary<string, CancellationTokenSource> _activeDownloads;

        private CommunityManifest _manifest;
        private bool _isRefreshing;

        public CommunityManifest Manifest => _manifest;
        public bool IsRefreshing => _isRefreshing;
        public bool HasManifest => _manifest != null;

        public CommunityService(SongStorage songStorage, List<Song> mainSongList)
        {
            _apiClient = new CommunityApiClient();
            _songStorage = songStorage;
            _mainSongList = mainSongList;
            _activeDownloads = new Dictionary<string, CancellationTokenSource>();

            _manifest = _songStorage.GetCachedManifest();
        }

        public async Task RefreshManifestAsync(CancellationToken cancellationToken = default)
        {
            if (_isRefreshing)
                return;

            _isRefreshing = true;

            try
            {
                _manifest = await _apiClient.FetchManifestAsync(cancellationToken);
                _songStorage.SaveManifest(_manifest);
                ManifestRefreshed?.Invoke(this, EventArgs.Empty);
                Logger.Info($"Refreshed manifest with {_manifest.Songs.Count} songs");
            }
            catch (OperationCanceledException)
            {
                Logger.Debug("Manifest refresh cancelled");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to refresh manifest");
                if (_manifest == null)
                    _manifest = _songStorage.GetCachedManifest();
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        public IEnumerable<CommunitySong> GetAvailableSongs()
        {
            return _manifest?.Songs ?? Enumerable.Empty<CommunitySong>();
        }

        public IEnumerable<CommunitySong> SearchSongs(string searchTerm, string instrumentFilter)
        {
            var songs = GetAvailableSongs();

            if (!string.IsNullOrEmpty(instrumentFilter) && instrumentFilter != "All")
            {
                songs = songs.Where(s =>
                    s.Instrument.Equals(instrumentFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var term = searchTerm.ToLower();
                songs = songs.Where(s =>
                    s.Name.ToLower().Contains(term) ||
                    s.Artist.ToLower().Contains(term) ||
                    s.Transcriber.ToLower().Contains(term));
            }

            return songs;
        }

        public bool IsSongDownloaded(string communityId)
        {
            return _songStorage.SongExists(communityId);
        }

        public bool IsDownloading(string communityId)
        {
            return _activeDownloads.ContainsKey(communityId);
        }

        public async Task<Song> DownloadSongAsync(CommunitySong communitySong, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            if (IsSongDownloaded(communitySong.Id))
            {
                Logger.Info($"Song {communitySong.Id} already downloaded, returning cached version");
                return _songStorage.GetSong(communitySong.Id);
            }

            if (_activeDownloads.ContainsKey(communitySong.Id))
            {
                Logger.Warn($"Download already in progress for {communitySong.Id}");
                return null;
            }

            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _activeDownloads[communitySong.Id] = cts;

            try
            {
                progress?.Report(10);
                RaiseDownloadProgress(communitySong.Id, 10, DownloadState.Downloading);

                var song = await _apiClient.FetchSongAsync(communitySong.Id, cts.Token);

                progress?.Report(80);
                RaiseDownloadProgress(communitySong.Id, 80, DownloadState.Downloading);

                if (song == null)
                {
                    RaiseDownloadProgress(communitySong.Id, 0, DownloadState.Failed);
                    return null;
                }

                _songStorage.SaveSong(song);

                progress?.Report(100);
                RaiseDownloadProgress(communitySong.Id, 100, DownloadState.Completed);

                _mainSongList.Add(song);
                Logger.Info($"Downloaded and added song: {song.Name}");

                return song;
            }
            catch (OperationCanceledException)
            {
                Logger.Debug($"Download cancelled for {communitySong.Id}");
                RaiseDownloadProgress(communitySong.Id, 0, DownloadState.Cancelled);
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to download song {communitySong.Id}");
                RaiseDownloadProgress(communitySong.Id, 0, DownloadState.Failed);
                return null;
            }
            finally
            {
                _activeDownloads.Remove(communitySong.Id);
                cts.Dispose();
            }
        }

        public void CancelDownload(string communityId)
        {
            if (_activeDownloads.TryGetValue(communityId, out var cts))
            {
                cts.Cancel();
                Logger.Info($"Cancelled download for {communityId}");
            }
        }

        public void DeleteDownloadedSong(Song song)
        {
            _songStorage.DeleteSong(song);
            _mainSongList.Remove(song);
            Logger.Info($"Deleted song: {song.Name} ({song.CommunityId ?? "imported"})");
        }

        public async Task<List<Song>> LoadSubmittalsAsync(CancellationToken cancellationToken = default)
        {
            var submittals = new List<Song>();

            try
            {
                var pendingManifest = await _apiClient.FetchPendingManifestAsync(cancellationToken);
                if (pendingManifest?.Songs == null || pendingManifest.Songs.Count == 0)
                    return submittals;

                var mainManifest = _manifest ?? await _apiClient.FetchManifestAsync(cancellationToken);
                var mainSongIds = new HashSet<string>(
                    mainManifest?.Songs?.Select(s => s.Id) ?? Enumerable.Empty<string>());

                var newSongs = pendingManifest.Songs.Where(s => !mainSongIds.Contains(s.Id)).ToList();
                Logger.Info($"Found {newSongs.Count} submittal(s) in pending branch");

                foreach (var communitySong in newSongs)
                {
                    try
                    {
                        var song = await _apiClient.FetchPendingSongAsync(communitySong.Id, cancellationToken);
                        if (song != null)
                        {
                            song.IsSubmittal = true;
                            submittals.Add(song);
                            Logger.Info($"Loaded submittal: {song.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, $"Failed to load submittal {communitySong.Id}, skipping");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load submittals from pending branch");
            }

            return submittals;
        }

        private void RaiseDownloadProgress(string communityId, int progress, DownloadState state)
        {
            DownloadProgressChanged?.Invoke(this, new DownloadProgressEventArgs(communityId, progress, state));
        }

        public void Dispose()
        {
            foreach (var cts in _activeDownloads.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }
            _activeDownloads.Clear();

            _apiClient?.Dispose();
        }
    }

    public enum DownloadState
    {
        Idle,
        Downloading,
        Completed,
        Failed,
        Cancelled
    }

    public class DownloadProgressEventArgs : EventArgs
    {
        public string CommunityId { get; }
        public int Progress { get; }
        public DownloadState State { get; }

        public DownloadProgressEventArgs(string communityId, int progress, DownloadState state)
        {
            CommunityId = communityId;
            Progress = progress;
            State = state;
        }
    }
}
