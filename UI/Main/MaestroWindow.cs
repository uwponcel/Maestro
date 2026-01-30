using System;
using System.Collections.Generic;
using System.Linq;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Maestro.Models;
using Maestro.Services;
using Maestro.Services.Playback;
using Maestro.UI.Playlist;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maestro.UI.Main
{
    public class MaestroWindow : StandardWindow
    {
        private static class Layout
        {
            public const int WindowWidth = 420;
            public const int WindowHeight = 495;
            public const int ContentWidth = 390;
        }

        public event EventHandler ImportRequested;
        public event EventHandler CommunityRequested;
        public event EventHandler<InstrumentType> CreateRequested;
        public event EventHandler<Song> SongDeleteRequested;
        public event EventHandler<Song> EditRequested;

        private static Texture2D _backgroundTexture;

        private readonly SongPlayer _songPlayer;
        private readonly List<Song> _allSongs;
        private readonly PlaylistService _playlistService;

        private NowPlayingPanel _nowPlayingPanel;
        private SongFilterBar _filterBar;
        private SongListPanel _songListPanel;
        private StatusBar _statusBar;
        private PlaylistDrawerWindow _playlistDrawer;

        private bool _isDrawerOpen;
        private bool _isPlayingFromQueue;

        // Instrument tracking
        private InstrumentType? _lastPlayedInstrument;
        private Song _pendingSong;

        public MaestroWindow(SongPlayer songPlayer, List<Song> songs)
            : base(
                GetBackground(),
                new Rectangle(0, 0, Layout.WindowWidth, Layout.WindowHeight),
                new Rectangle(15, MaestroTheme.WindowContentTopPadding, Layout.ContentWidth, Layout.WindowHeight))
        {
            _songPlayer = songPlayer;
            _allSongs = songs;
            _playlistService = new PlaylistService();

            Title = "Maestro";
            Subtitle = "Music player";
            Emblem = Module.Instance.ContentsManager.GetTexture("maestro-emblem.png");
            SavesPosition = true;
            Id = "MaestroWindow_v4";
            CanResize = false;
            Parent = GameService.Graphics.SpriteScreen;

            BuildUi();
            SubscribeToEvents();

            LeftMouseButtonPressed += OnWindowClicked;
        }

        public void AddImportedSong(Song song)
        {
            _allSongs.Add(song);
            _statusBar.TotalCount = _allSongs.Count;
            RefreshSongList();
        }

        public void RefreshAfterCommunityDownload()
        {
            _statusBar.TotalCount = _allSongs.Count;
            RefreshSongList();
        }

        public void SetCreateButtonEnabled(bool enabled)
        {
            _statusBar.SetCreateButtonEnabled(enabled);
        }

        public void RemoveSong(Song song)
        {
            if (_songPlayer.CurrentSong == song)
                _songPlayer.Stop();

            _allSongs.Remove(song);
            _statusBar.TotalCount = _allSongs.Count;
            RefreshSongList();
        }

        protected override void OnMoved(MovedEventArgs e)
        {
            base.OnMoved(e);
            if (_isDrawerOpen)
            {
                UpdateDrawerPosition();
            }
        }

        protected override void OnHidden(EventArgs e)
        {
            base.OnHidden(e);
            _playlistDrawer?.Hide();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (_isDrawerOpen)
            {
                var targetX = AbsoluteBounds.Right + 5;
                var targetY = AbsoluteBounds.Top + 35;
                _playlistDrawer.ShowWithAnimation(targetX, targetY);
            }
        }

        protected override void DisposeControl()
        {
            UnsubscribeFromEvents();
            _songPlayer.Stop();
            DisposeControls();

            base.DisposeControl();
        }

        private static Texture2D GetBackground()
        {
            return _backgroundTexture ?? (_backgroundTexture = MaestroTheme.CreateWindowBackground(Layout.WindowWidth, Layout.WindowHeight));
        }

        private void BuildUi()
        {
            var currentY = MaestroTheme.PaddingContentTop;

            currentY = BuildNowPlayingPanel(currentY);
            currentY = BuildFilterBar(currentY);
            currentY = BuildSongListPanel(currentY);
            BuildStatusBar(currentY);
            BuildPlaylistDrawer();

            RefreshSongList();
        }

        private int BuildNowPlayingPanel(int currentY)
        {
            _nowPlayingPanel = new NowPlayingPanel(_songPlayer, Layout.ContentWidth)
            {
                Parent = this,
                Location = new Point(0, currentY)
            };
            _nowPlayingPanel.StopRequested += OnStopRequested;
            _nowPlayingPanel.PlayPendingRequested += OnPlayPendingRequested;
            _nowPlayingPanel.QueueToggleClicked += OnQueueToggleClicked;
            return currentY + NowPlayingPanel.Layout.Height + MaestroTheme.InputSpacing;
        }

        private int BuildFilterBar(int currentY)
        {
            _filterBar = new SongFilterBar(Layout.ContentWidth)
            {
                Parent = this,
                Location = new Point(0, currentY)
            };
            _filterBar.SearchChanged += OnFilterChanged;
            _filterBar.FilterChanged += OnFilterChanged;
            return currentY + SongFilterBar.Layout.Height + MaestroTheme.InputSpacing - 3;
        }

        private int BuildSongListPanel(int currentY)
        {
            _songListPanel = new SongListPanel(_songPlayer, Layout.ContentWidth)
            {
                Parent = this,
                Location = new Point(0, currentY)
            };
            _songListPanel.SongPlayRequested += OnSongPlayRequested;
            _songListPanel.SongDeleteRequested += OnSongDeleteRequested;
            _songListPanel.EditRequested += OnEditRequested;
            _songListPanel.AddToQueueRequested += OnAddToQueueRequested;
            _songListPanel.CountChanged += OnCountChanged;
            return currentY + SongListPanel.Layout.Height + MaestroTheme.InputSpacing;
        }

        private void BuildStatusBar(int currentY)
        {
            _statusBar = new StatusBar(Layout.ContentWidth)
            {
                Parent = this,
                Location = new Point(0, currentY)
            };
            _statusBar.TotalCount = _allSongs.Count;
            _statusBar.ImportClicked += OnImportClicked;
            _statusBar.CommunityClicked += OnCommunityClicked;
            _statusBar.CreateClicked += OnCreateClicked;
        }

        private void BuildPlaylistDrawer()
        {
            _playlistDrawer = new PlaylistDrawerWindow(_playlistService)
            {
                Parent = GameService.Graphics.SpriteScreen,
                Visible = false
            };
            _playlistDrawer.Hidden += OnDrawerHidden;
            _playlistDrawer.PlayQueueRequested += OnPlayQueueRequested;
        }

        private void SubscribeToEvents()
        {
            _songPlayer.OnStarted += OnPlaybackStateChanged;
            _songPlayer.OnPaused += OnPlaybackStateChanged;
            _songPlayer.OnResumed += OnPlaybackStateChanged;
            _songPlayer.OnStopped += OnPlaybackStateChanged;
            _songPlayer.OnCompleted += OnPlaybackStateChanged;
            _songPlayer.OnCompleted += OnSongCompleted;

            _playlistService.QueueChanged += OnQueueChanged;
        }

        private void OnWindowClicked(object sender, MouseEventArgs e)
        {
            if (FocusedControl is TextInputBase textInput && !textInput.MouseOver)
                textInput.Focused = false;
        }

        private void OnPlaybackStateChanged(object sender, EventArgs e)
        {
            _songListPanel.UpdateCardStates();
        }

        private void OnFilterChanged(object sender, EventArgs e)
        {
            RefreshSongList();
        }

        private void OnSongPlayRequested(object sender, Song song)
        {
            // Clear any pending state
            if (_pendingSong != null)
            {
                _pendingSong = null;
                _nowPlayingPanel.ClearPendingSong();
                _playlistDrawer.HideInstrumentConfirmation();
            }

            PlaySongDirectly(song);
        }

        private void OnSongDeleteRequested(object sender, Song song)
        {
            SongDeleteRequested?.Invoke(this, song);
        }

        private void OnEditRequested(object sender, Song song)
        {
            EditRequested?.Invoke(this, song);
        }

        private void OnCountChanged(object sender, int count)
        {
            _statusBar.VisibleCount = count;
        }

        private void OnImportClicked(object sender, EventArgs e)
        {
            ImportRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnCommunityClicked(object sender, EventArgs e)
        {
            CommunityRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnCreateClicked(object sender, InstrumentType instrument)
        {
            CreateRequested?.Invoke(this, instrument);
        }

        private void OnQueueToggleClicked(object sender, EventArgs e)
        {
            ToggleDrawer();
        }

        private void OnAddToQueueRequested(object sender, Song song)
        {
            _playlistService.Add(song);
        }

        private void OnQueueChanged(object sender, EventArgs e)
        {
            // Queue count changed - could be used for visual feedback if needed
        }

        private void OnDrawerHidden(object sender, EventArgs e)
        {
            _isDrawerOpen = false;
            if (_pendingSong != null)
            {
                _pendingSong = null;
                _nowPlayingPanel.ClearPendingSong();
                if (_isPlayingFromQueue)
                    SetQueuePlaybackMode(false);
            }
        }

        private void OnPlayPendingRequested(object sender, Song song)
        {
            _playlistDrawer.HideInstrumentConfirmation();

            if (!_isPlayingFromQueue)
                SetQueuePlaybackMode(false);

            // Close drawer if queue is empty
            if (!_playlistService.HasItems)
            {
                _isDrawerOpen = false;
                _playlistDrawer.Hide();
            }

            _lastPlayedInstrument = song.Instrument;
            _nowPlayingPanel.SetCurrentInstrument(song.Instrument);
            _pendingSong = null;
            _songPlayer.Play(song);
        }

        private void ShowInstrumentConfirmation(Song song)
        {
            _pendingSong = song;
            _nowPlayingPanel.SetPendingSong(song);

            // Open drawer if not already open and show overlay
            if (!_isDrawerOpen)
            {
                _isDrawerOpen = true;
                var targetX = AbsoluteBounds.Right + 5;
                var targetY = AbsoluteBounds.Top + 35;
                _playlistDrawer.ShowWithAnimation(targetX, targetY);
            }

            _playlistDrawer.ShowInstrumentConfirmation(song.Instrument);
        }

        private void PlaySongDirectly(Song song)
        {
            SetQueuePlaybackMode(false);
            _lastPlayedInstrument = song.Instrument;
            _nowPlayingPanel.SetCurrentInstrument(song.Instrument);
            _songPlayer.Play(song);
        }

        private void OnSongCompleted(object sender, EventArgs e)
        {
            if (_isPlayingFromQueue && _playlistService.HasItems)
            {
                PlayNextFromQueue();
            }
            else
            {
                SetQueuePlaybackMode(false);
            }
        }

        private void OnPlayQueueRequested(object sender, EventArgs e)
        {
            if (_playlistService.HasItems)
            {
                // Stop any currently playing song
                _songPlayer.Stop();

                SetQueuePlaybackMode(true);
                PlayNextFromQueue();
            }
        }

        private void OnStopRequested(object sender, EventArgs e)
        {
            SetQueuePlaybackMode(false);
            _songPlayer.Stop();
        }

        private void SetQueuePlaybackMode(bool isPlaying)
        {
            _isPlayingFromQueue = isPlaying;
            _playlistDrawer.SetQueuePlaybackMode(isPlaying);
            _nowPlayingPanel.SetQueuePlaybackMode(isPlaying);
        }

        private void PlayNextFromQueue()
        {
            var nextSong = _playlistService.Dequeue();
            if (nextSong != null)
            {
                // Compare against pending song's instrument if there is one, otherwise last played
                var currentInstrument = _pendingSong?.Instrument ?? _lastPlayedInstrument;

                if (currentInstrument.HasValue && nextSong.Instrument != currentInstrument.Value)
                {
                    ShowInstrumentConfirmation(nextSong);
                }
                else
                {
                    if (_pendingSong != null)
                    {
                        _pendingSong = null;
                        _nowPlayingPanel.ClearPendingSong();
                        _playlistDrawer.HideInstrumentConfirmation();
                    }

                    _lastPlayedInstrument = nextSong.Instrument;
                    _nowPlayingPanel.SetCurrentInstrument(nextSong.Instrument);
                    _songPlayer.Play(nextSong);
                }
            }
            else
            {
                SetQueuePlaybackMode(false);
            }
        }

        private void ToggleDrawer()
        {
            _isDrawerOpen = !_isDrawerOpen;

            if (_isDrawerOpen)
            {
                var targetX = AbsoluteBounds.Right + 5;
                var targetY = AbsoluteBounds.Top + 35;
                _playlistDrawer.ShowWithAnimation(targetX, targetY);
            }
            else
            {
                _playlistDrawer.Hide();
            }
        }

        private void UpdateDrawerPosition()
        {
            if (_playlistDrawer != null)
            {
                _playlistDrawer.Location = new Point(
                    AbsoluteBounds.Right + 5,
                    AbsoluteBounds.Top + 35);
            }
        }

        private void RefreshSongList()
        {
            var filteredSongs = GetFilteredSongs();
            _songListPanel.RefreshSongs(filteredSongs);
        }

        private IEnumerable<Song> GetFilteredSongs()
        {
            var songs = _allSongs.AsEnumerable();

            songs = FilterBySource(songs);
            songs = FilterByInstrument(songs);
            songs = FilterBySearchTerm(songs);
            songs = ApplySort(songs);

            return songs;
        }

        private IEnumerable<Song> ApplySort(IEnumerable<Song> songs)
        {
            var sort = _filterBar.SelectedSort;
            switch (sort)
            {
                case "Name Z-A":
                    return songs.OrderByDescending(s => s.Name);
                case "Name A-Z":
                default:
                    return songs.OrderBy(s => s.Name);
            }
        }

        private IEnumerable<Song> FilterBySource(IEnumerable<Song> songs)
        {
            var source = _filterBar.SelectedSource;
            switch (source)
            {
                case "Bundled":
                    return songs.Where(s => !s.IsUserImported && !s.IsCreated && !s.IsCommunityDownloaded);
                case "Community":
                    return songs.Where(s => s.IsCommunityDownloaded);
                case "Created":
                    return songs.Where(s => s.IsCreated);
                case "Imported":
                    return songs.Where(s => s.IsUserImported);
                case "Submittals":
                    return songs.Where(s => s.IsSubmittal);
                default:
                    return songs;
            }
        }

        private IEnumerable<Song> FilterByInstrument(IEnumerable<Song> songs)
        {
            var filter = _filterBar.SelectedInstrument;
            if (filter == "All") return songs;

            if (Enum.TryParse<InstrumentType>(filter, out var instrument))
            {
                return songs.Where(s => s.Instrument == instrument);
            }

            return songs;
        }

        private IEnumerable<Song> FilterBySearchTerm(IEnumerable<Song> songs)
        {
            var searchTerm = _filterBar.SearchText;
            if (string.IsNullOrEmpty(searchTerm)) return songs;

            return songs.Where(s =>
                s.Name.ToLower().Contains(searchTerm) ||
                s.Artist.ToLower().Contains(searchTerm));
        }

        private void UnsubscribeFromEvents()
        {
            _songPlayer.OnStarted -= OnPlaybackStateChanged;
            _songPlayer.OnPaused -= OnPlaybackStateChanged;
            _songPlayer.OnResumed -= OnPlaybackStateChanged;
            _songPlayer.OnStopped -= OnPlaybackStateChanged;
            _songPlayer.OnCompleted -= OnPlaybackStateChanged;
            _songPlayer.OnCompleted -= OnSongCompleted;

            _playlistService.QueueChanged -= OnQueueChanged;

            _filterBar.SearchChanged -= OnFilterChanged;
            _filterBar.FilterChanged -= OnFilterChanged;
            _songListPanel.SongPlayRequested -= OnSongPlayRequested;
            _songListPanel.SongDeleteRequested -= OnSongDeleteRequested;
            _songListPanel.EditRequested -= OnEditRequested;
            _songListPanel.AddToQueueRequested -= OnAddToQueueRequested;
            _songListPanel.CountChanged -= OnCountChanged;
            _statusBar.ImportClicked -= OnImportClicked;
            _statusBar.CommunityClicked -= OnCommunityClicked;
            _statusBar.CreateClicked -= OnCreateClicked;
            _playlistDrawer.Hidden -= OnDrawerHidden;
            _playlistDrawer.PlayQueueRequested -= OnPlayQueueRequested;
            _nowPlayingPanel.StopRequested -= OnStopRequested;
            _nowPlayingPanel.PlayPendingRequested -= OnPlayPendingRequested;
            _nowPlayingPanel.QueueToggleClicked -= OnQueueToggleClicked;
        }

        private void DisposeControls()
        {
            _nowPlayingPanel?.Dispose();
            _filterBar?.Dispose();
            _songListPanel?.Dispose();
            _statusBar?.Dispose();
            _playlistDrawer?.Dispose();
        }
    }
}
