using System;
using System.Collections.Generic;
using System.Linq;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Maestro.Models;
using Maestro.Services.Community;
using Maestro.UI.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maestro.UI.Community
{
    public class CommunityWindow : StandardWindow
    {
        private static class Layout
        {
            public const int WindowWidth = 420;
            public const int WindowHeight = 350;
            public const int ContentWidth = 390;
            public const int FilterBarHeight = 32;
            public const int SongListHeight = 250;
            public const int StatusBarHeight = 30;
            public const int CardWidth = 378;
        }

        private static Texture2D _backgroundTexture;

        private static Texture2D GetBackground()
        {
            return _backgroundTexture ?? (_backgroundTexture = MaestroTheme.CreateWindowBackground(Layout.WindowWidth, Layout.WindowHeight));
        }

        public event EventHandler<Song> SongDownloaded;
        public event EventHandler<string> SongDeleteRequested;
        public event EventHandler UploadRequested;

        private readonly CommunityService _communityService;
        private readonly Dictionary<string, CommunitySongCard> _songCards;

        private TextBox _searchBox;
        private GenericFilterButton _filterButton;
        private FlowPanel _songListPanel;
        private Label _statusLabel;
        private StandardButton _refreshButton;
        private StandardButton _uploadButton;
        private LoadingSpinner _loadingSpinner;

        public CommunityWindow(CommunityService communityService)
            : base(
                GetBackground(),
                new Rectangle(0, 0, Layout.WindowWidth, Layout.WindowHeight),
                new Rectangle(15, MaestroTheme.WindowContentTopPadding, Layout.ContentWidth, Layout.WindowHeight))
        {
            _communityService = communityService;
            _songCards = new Dictionary<string, CommunitySongCard>();

            Title = "Community Songs";
            Emblem = Module.Instance.ContentsManager.GetTexture("community-emblem.png");
            SavesPosition = true;
            Id = "CommunityWindow_v1";
            CanResize = false;
            Parent = GameService.Graphics.SpriteScreen;

            BuildUi();
            SubscribeToEvents();

            LeftMouseButtonPressed += OnWindowClicked;
        }

        private void OnWindowClicked(object sender, MouseEventArgs e)
        {
            if (FocusedControl is TextInputBase textInput && !textInput.MouseOver)
                textInput.Focused = false;
        }

        private void BuildUi()
        {
            var currentY = MaestroTheme.PaddingContentTop;

            var filterPanel = new Panel
            {
                Parent = this,
                Location = new Point(0, currentY),
                Size = new Point(Layout.ContentWidth, Layout.FilterBarHeight),
                BackgroundColor = Color.Transparent
            };

            var uploadWidth = 70;
            var availableWidth = Layout.ContentWidth - uploadWidth - MaestroTheme.InputSpacing;
            var searchWidth = (availableWidth - MaestroTheme.InputSpacing) / 2;
            var filterWidth = availableWidth - searchWidth - MaestroTheme.InputSpacing;

            _searchBox = new TextBox
            {
                Parent = filterPanel,
                Location = new Point(0, 0),
                Width = searchWidth,
                Height = 26,
                PlaceholderText = "Search..."
            };
            _searchBox.TextChanged += OnFilterChanged;

            _filterButton = new GenericFilterButton(
                new FilterSection { Items = new[] { "All", "Piano", "Harp", "Lute", "Bass" }, DefaultValue = "All" },
                new FilterSection { Items = new[] { "Newest", "Name A-Z", "Name Z-A" }, DefaultValue = "Newest" })
            {
                Parent = filterPanel,
                Location = new Point(searchWidth + MaestroTheme.InputSpacing, 0),
                Width = filterWidth
            };
            _filterButton.FilterChanged += OnFilterChanged;

            _uploadButton = new StandardButton
            {
                Parent = filterPanel,
                Text = "Upload",
                Location = new Point(Layout.ContentWidth - uploadWidth, 0),
                Width = uploadWidth
            };
            _uploadButton.Click += OnUploadClicked;

            currentY += Layout.FilterBarHeight + MaestroTheme.InputSpacing;

            _songListPanel = new FlowPanel
            {
                Parent = this,
                Location = new Point(0, currentY),
                Size = new Point(Layout.ContentWidth, Layout.SongListHeight),
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                ControlPadding = new Vector2(0, 5),
                CanScroll = true,
                ShowBorder = true
            };

            currentY += Layout.SongListHeight + MaestroTheme.InputSpacing;

            var refreshWidth = 70;
            var spinnerSize = 26;

            _loadingSpinner = new LoadingSpinner
            {
                Parent = this,
                Location = new Point(Layout.ContentWidth - refreshWidth - MaestroTheme.InputSpacing - spinnerSize, currentY),
                Size = new Point(spinnerSize, spinnerSize),
                Visible = false
            };

            _refreshButton = new StandardButton
            {
                Parent = this,
                Text = "Refresh",
                Location = new Point(Layout.ContentWidth - refreshWidth, currentY),
                Width = refreshWidth,
                Height = 26
            };
            _refreshButton.Click += OnRefreshClicked;

            _statusLabel = new Label
            {
                Parent = this,
                Location = new Point(0, currentY),
                Width = Layout.ContentWidth - refreshWidth - MaestroTheme.InputSpacing - spinnerSize - MaestroTheme.InputSpacing,
                Height = Layout.StatusBarHeight,
                Font = GameService.Content.DefaultFont12,
                TextColor = MaestroTheme.LightGray,
                Text = "Loading..."
            };
        }

        private void OnUploadClicked(object sender, MouseEventArgs e)
        {
            UploadRequested?.Invoke(this, EventArgs.Empty);
        }

        private void SubscribeToEvents()
        {
            _communityService.ManifestRefreshed += OnManifestRefreshed;
            _communityService.DownloadProgressChanged += OnDownloadProgressChanged;
        }

        private void OnManifestRefreshed(object sender, EventArgs e)
        {
            RefreshSongList();
        }

        private void OnDownloadProgressChanged(object sender, DownloadProgressEventArgs e)
        {
            if (_songCards.TryGetValue(e.CommunityId, out var card))
            {
                card.UpdateDownloadProgress(e.Progress, e.State);
            }

            if (e.State == DownloadState.Completed)
            {
                UpdateStatusLabel();
            }
        }

        private void OnFilterChanged(object sender, EventArgs e)
        {
            RefreshSongList();
        }

        private async void OnRefreshClicked(object sender, MouseEventArgs e)
        {
            _refreshButton.Enabled = false;
            _loadingSpinner.Visible = true;
            _statusLabel.Text = "Refreshing...";

            try
            {
                await _communityService.RefreshManifestAsync();
            }
            finally
            {
                _refreshButton.Enabled = true;
                _loadingSpinner.Visible = false;
            }
        }

        public async void LoadContent()
        {
            _loadingSpinner.Visible = true;
            _statusLabel.Text = "Loading community songs...";

            try
            {
                await _communityService.RefreshManifestAsync();
                RefreshSongList();
            }
            catch (Exception ex)
            {
                Logger.GetLogger<CommunityWindow>().Error(ex, "Failed to load community songs");
                _statusLabel.Text = "Failed to load. Click Refresh to try again.";
            }
            finally
            {
                _loadingSpinner.Visible = false;
            }
        }

        private void RefreshSongList()
        {
            foreach (var card in _songCards.Values)
            {
                card.Dispose();
            }
            _songCards.Clear();
            _songListPanel.ClearChildren();

            var searchTerm = _searchBox?.Text?.ToLower() ?? "";
            var instrumentFilter = _filterButton?.SelectedValue1 ?? "All";
            var sortOption = _filterButton?.SelectedValue2 ?? "Newest";

            var songs = _communityService.SearchSongs(searchTerm, instrumentFilter);

            switch (sortOption)
            {
                case "Newest":
                    songs = songs.OrderByDescending(s => s.CreatedAt);
                    break;
                case "Name A-Z":
                    songs = songs.OrderBy(s => s.Name);
                    break;
                case "Name Z-A":
                    songs = songs.OrderByDescending(s => s.Name);
                    break;
            }

            var songList = songs.ToList();

            foreach (var song in songList)
            {
                var isDownloaded = _communityService.IsSongDownloaded(song.Id);
                var card = new CommunitySongCard(song, Layout.CardWidth, isDownloaded)
                {
                    Parent = _songListPanel
                };
                card.DownloadRequested += OnDownloadRequested;
                card.DeleteRequested += OnDeleteRequested;
                _songCards[song.Id] = card;
            }

            UpdateStatusLabel();
        }

        private async void OnDownloadRequested(object sender, CommunitySong song)
        {
            var downloadedSong = await _communityService.DownloadSongAsync(song);
            if (downloadedSong != null)
            {
                SongDownloaded?.Invoke(this, downloadedSong);
                ScreenNotification.ShowNotification($"Downloaded: {downloadedSong.Name}");
            }
        }

        private void OnDeleteRequested(object sender, CommunitySong song)
        {
            SongDeleteRequested?.Invoke(this, song.Id);
        }

        public void MarkSongAsDeleted(string communityId)
        {
            if (_songCards.TryGetValue(communityId, out var card))
            {
                card.UpdateDownloadProgress(0, DownloadState.Idle);
                UpdateStatusLabel();
            }
        }

        private void UpdateStatusLabel()
        {
            var total = _communityService.GetAvailableSongs().Count();
            var displayed = _songCards.Count;
            var downloaded = _songCards.Values.Count(c => c.IsDownloaded);

            _statusLabel.Text = displayed == total
                ? $"{total} songs available | {downloaded} downloaded"
                : $"Showing {displayed} of {total} songs | {downloaded} downloaded";
        }

        protected override void DisposeControl()
        {
            _communityService.ManifestRefreshed -= OnManifestRefreshed;
            _communityService.DownloadProgressChanged -= OnDownloadProgressChanged;

            foreach (var card in _songCards.Values)
            {
                card.DownloadRequested -= OnDownloadRequested;
                card.DeleteRequested -= OnDeleteRequested;
                card.Dispose();
            }
            _songCards.Clear();

            _uploadButton.Click -= OnUploadClicked;

            _searchBox?.Dispose();
            _filterButton?.Dispose();
            _songListPanel?.Dispose();
            _statusLabel?.Dispose();
            _refreshButton?.Dispose();
            _uploadButton?.Dispose();
            _loadingSpinner?.Dispose();

            base.DisposeControl();
        }
    }
}
