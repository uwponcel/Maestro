using System;
using System.Collections.Generic;
using System.Linq;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Maestro.Models;
using Maestro.Services.Community;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maestro.UI.Community
{
    public class CommunityWindow : StandardWindow
    {
        private static class Layout
        {
            public const int WindowWidth = 450;
            public const int WindowHeight = 405;
            public const int ContentWidth = 420;
            public const int ContentHeight = 380;
            public const int TopPadding = 10;
            public const int FilterBarHeight = 32;
            public const int SongListHeight = 283;
            public const int StatusBarHeight = 30;
            public const int CardWidth = 400;
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
        private Dropdown _instrumentFilter;
        private Dropdown _sortFilter;
        private FlowPanel _songListPanel;
        private Label _statusLabel;
        private StandardButton _refreshButton;
        private StandardButton _uploadButton;
        private LoadingSpinner _loadingSpinner;

        public CommunityWindow(CommunityService communityService)
            : base(
                GetBackground(),
                new Rectangle(0, 0, Layout.WindowWidth, Layout.WindowHeight),
                new Rectangle(15, MaestroTheme.WindowContentTopPadding, Layout.ContentWidth, Layout.ContentHeight))
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
            var currentY = MaestroTheme.PaddingContentTop + Layout.TopPadding;

            var filterPanel = new Panel
            {
                Parent = this,
                Location = new Point(0, currentY),
                Size = new Point(Layout.ContentWidth, Layout.FilterBarHeight),
                BackgroundColor = Color.Transparent
            };

            _searchBox = new TextBox
            {
                Parent = filterPanel,
                Location = new Point(0, 0),
                Width = 150,
                Height = 26,
                PlaceholderText = "Search songs..."
            };
            _searchBox.TextChanged += OnFilterChanged;

            _instrumentFilter = new Dropdown
            {
                Parent = filterPanel,
                Location = new Point(155, 0),
                Width = 75
            };
            _instrumentFilter.Items.Add("All");
            _instrumentFilter.Items.Add("Piano");
            _instrumentFilter.Items.Add("Harp");
            _instrumentFilter.Items.Add("Lute");
            _instrumentFilter.Items.Add("Bass");
            _instrumentFilter.SelectedItem = "All";
            _instrumentFilter.ValueChanged += OnFilterChanged;

            _sortFilter = new Dropdown
            {
                Parent = filterPanel,
                Location = new Point(235, 0),
                Width = 80
            };
            _sortFilter.Items.Add("Newest");
            _sortFilter.Items.Add("Name A-Z");
            _sortFilter.SelectedItem = "Newest";
            _sortFilter.ValueChanged += OnFilterChanged;

            _loadingSpinner = new LoadingSpinner
            {
                Parent = filterPanel,
                Location = new Point(320, 0),
                Size = new Point(26, 26),
                Visible = false
            };

            _uploadButton = new StandardButton
            {
                Parent = filterPanel,
                Text = "Upload",
                Location = new Point(350, 0),
                Width = 70
            };
            _uploadButton.Click += OnUploadClicked;

            _refreshButton = new StandardButton
            {
                Parent = this,
                Text = "Refresh",
                Location = new Point(Layout.ContentWidth - 70, MaestroTheme.PaddingContentTop + Layout.TopPadding + Layout.FilterBarHeight + MaestroTheme.InputSpacing + Layout.SongListHeight + MaestroTheme.InputSpacing),
                Width = 70,
                Height = 26
            };
            _refreshButton.Click += OnRefreshClicked;

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

            _statusLabel = new Label
            {
                Parent = this,
                Location = new Point(0, currentY),
                Width = Layout.ContentWidth - 80,
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
            var instrumentFilter = _instrumentFilter?.SelectedItem ?? "All";
            var sortOption = _sortFilter?.SelectedItem ?? "Newest";

            var songs = _communityService.SearchSongs(searchTerm, instrumentFilter);

            switch (sortOption)
            {
                case "Newest":
                    songs = songs.OrderByDescending(s => s.CreatedAt);
                    break;
                case "Name A-Z":
                    songs = songs.OrderBy(s => s.Name);
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
            _instrumentFilter?.Dispose();
            _sortFilter?.Dispose();
            _songListPanel?.Dispose();
            _statusLabel?.Dispose();
            _refreshButton?.Dispose();
            _uploadButton?.Dispose();
            _loadingSpinner?.Dispose();

            base.DisposeControl();
        }
    }
}
