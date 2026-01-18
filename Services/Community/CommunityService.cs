using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Blish_HUD;
using Maestro.Models;

namespace Maestro.Services.Community
{
    public class CommunityService : IDisposable
    {
        private static readonly Logger Logger = Logger.GetLogger<CommunityService>();

        public event EventHandler<DownloadProgressEventArgs> DownloadProgressChanged;
        public event EventHandler ManifestRefreshed;

        private readonly CommunityApiClient _apiClient;
        private readonly CommunitySongCache _cache;
        private readonly List<Song> _mainSongList;
        private readonly Dictionary<string, CancellationTokenSource> _activeDownloads;

        private CommunityManifest _manifest;
        private bool _isRefreshing;

        public CommunityManifest Manifest => _manifest;
        public bool IsRefreshing => _isRefreshing;
        public bool HasManifest => _manifest != null;

        public CommunityService(CommunitySongCache cache, List<Song> mainSongList)
        {
            _apiClient = new CommunityApiClient();
            _cache = cache;
            _mainSongList = mainSongList;
            _activeDownloads = new Dictionary<string, CancellationTokenSource>();

            _manifest = _cache.GetCachedManifest();
        }

        public async Task RefreshManifestAsync(CancellationToken cancellationToken = default)
        {
            if (_isRefreshing)
                return;

            _isRefreshing = true;

            try
            {
                _manifest = await _apiClient.FetchManifestAsync(cancellationToken);
                _cache.SaveManifest(_manifest);
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
                    _manifest = _cache.GetCachedManifest();
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
            return _cache.IsSongCached(communityId);
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
                return _cache.GetCachedSong(communitySong.Id);
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

                song.Downloads = communitySong.Downloads;
                _cache.SaveSong(song);

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

        public List<Song> GetDownloadedCommunitySongs()
        {
            return _cache.GetAllCachedSongs();
        }

        public void DeleteDownloadedSong(Song song)
        {
            if (string.IsNullOrEmpty(song.CommunityId))
                return;

            _cache.DeleteSong(song.CommunityId);
            _mainSongList.Remove(song);
            Logger.Info($"Deleted community song: {song.Name} ({song.CommunityId})");
        }

        public void LoadCachedSongsIntoMainList()
        {
            var cachedSongs = _cache.GetAllCachedSongs();
            var existingIds = new HashSet<string>(_mainSongList
                .Where(s => !string.IsNullOrEmpty(s.CommunityId))
                .Select(s => s.CommunityId));

            foreach (var song in cachedSongs)
            {
                if (!existingIds.Contains(song.CommunityId))
                {
                    _mainSongList.Add(song);
                }
            }

            Logger.Info($"Loaded {cachedSongs.Count} cached community songs");
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
