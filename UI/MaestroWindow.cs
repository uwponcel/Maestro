using System;
using System.Collections.Generic;
using System.Linq;
using Blish_HUD;
using Blish_HUD.Controls;
using Maestro.Models;
using Maestro.Services.Playback;
using Maestro.UI.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maestro.UI
{
    public class MaestroWindow : StandardWindow
    {
        public event EventHandler ImportRequested;
        public event EventHandler<Song> SongDeleteRequested;

        private static class Layout
        {
            public const int WindowWidth = 420;
            public const int WindowHeight = 495;

            public const int ContentWidth = 390;
            public const int ContentHeight = 445;
        }

        private readonly SongPlayer _songPlayer;
        private readonly List<Song> _allSongs;

        private NowPlayingPanel _nowPlayingPanel;
        private SongFilterBar _filterBar;
        private SongListPanel _songListPanel;
        private StatusBar _statusBar;

        private static Texture2D _backgroundTexture;

        private static Texture2D GetBackground()
        {
            return _backgroundTexture ?? (_backgroundTexture = MaestroTheme.CreateWindowBackground(Layout.WindowWidth, Layout.WindowHeight));
        }

        public MaestroWindow(SongPlayer songPlayer, List<Song> songs)
            : base(
                GetBackground(),
                new Rectangle(0, 0, Layout.WindowWidth, Layout.WindowHeight),
                new Rectangle(15, 30, Layout.ContentWidth, Layout.ContentHeight))
        {
            _songPlayer = songPlayer;
            _allSongs = songs;

            Title = "Maestro";
            Subtitle = "Music player";
            Emblem = Module.Instance.ContentsManager.GetTexture("emblem.png");
            SavesPosition = true;
            Id = "MaestroWindow_v3";
            CanResize = false;
            Parent = GameService.Graphics.SpriteScreen;

            BuildUi();
            SubscribeToEvents();
        }

        private void BuildUi()
        {
            var currentY = MaestroTheme.PaddingContentTop;

            _nowPlayingPanel = new NowPlayingPanel(_songPlayer, Layout.ContentWidth)
            {
                Parent = this,
                Location = new Point(0, currentY)
            };
            currentY += NowPlayingPanel.Layout.Height + MaestroTheme.InputSpacing;

            _filterBar = new SongFilterBar(Layout.ContentWidth)
            {
                Parent = this,
                Location = new Point(0, currentY)
            };
            _filterBar.SearchChanged += OnFilterChanged;
            _filterBar.FilterChanged += OnFilterChanged;
            
            currentY += SongFilterBar.Layout.Height + MaestroTheme.InputSpacing - 3;

            _songListPanel = new SongListPanel(_songPlayer, Layout.ContentWidth)
            {
                Parent = this,
                Location = new Point(0, currentY)
            };
            _songListPanel.SongPlayRequested += OnSongPlayRequested;
            _songListPanel.SongDeleteRequested += OnSongDeleteRequested;
            _songListPanel.CountChanged += OnCountChanged;

            currentY += SongListPanel.Layout.Height + MaestroTheme.InputSpacing;
            
            _statusBar = new StatusBar(Layout.ContentWidth)
            {
                Parent = this,
                Location = new Point(0, currentY)
            };
            _statusBar.TotalCount = _allSongs.Count;
            _statusBar.ImportClicked += OnImportClicked;

            RefreshSongList();
        }

        private void SubscribeToEvents()
        {
            _songPlayer.OnStarted += OnPlaybackStateChanged;
            _songPlayer.OnPaused += OnPlaybackStateChanged;
            _songPlayer.OnResumed += OnPlaybackStateChanged;
            _songPlayer.OnStopped += OnPlaybackStateChanged;
            _songPlayer.OnCompleted += OnPlaybackStateChanged;
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
            _songPlayer.Play(song);
        }

        private void OnCountChanged(object sender, int count)
        {
            _statusBar.VisibleCount = count;
        }

        private void OnImportClicked(object sender, EventArgs e)
        {
            ImportRequested?.Invoke(this, EventArgs.Empty);
        }

        public void AddImportedSong(Song song)
        {
            _allSongs.Add(song);
            _statusBar.TotalCount = _allSongs.Count;
            RefreshSongList();
        }

        private void OnSongDeleteRequested(object sender, Song song)
        {
            SongDeleteRequested?.Invoke(this, song);
        }

        public void RemoveSong(Song song)
        {
            if (_songPlayer.CurrentSong == song)
                _songPlayer.Stop();

            _allSongs.Remove(song);
            _statusBar.TotalCount = _allSongs.Count;
            RefreshSongList();
        }

        private void RefreshSongList()
        {
            var filteredSongs = GetFilteredSongs();
            _songListPanel.RefreshSongs(filteredSongs);
        }

        private IEnumerable<Song> GetFilteredSongs()
        {
            var songs = _allSongs.AsEnumerable();

            var source = _filterBar.SelectedSource;
            if (source == "Bundled")
                songs = songs.Where(s => !s.IsUserImported);
            else if (source == "Imported")
                songs = songs.Where(s => s.IsUserImported);

            var filter = _filterBar.SelectedInstrument;
            if (filter != "All")
            {
                if (Enum.TryParse<InstrumentType>(filter, out var instrument))
                {
                    songs = songs.Where(s => s.Instrument == instrument);
                }
            }

            var searchTerm = _filterBar.SearchText;
            if (!string.IsNullOrEmpty(searchTerm))
            {
                songs = songs.Where(s =>
                    s.Name.ToLower().Contains(searchTerm) ||
                    s.Artist.ToLower().Contains(searchTerm));
            }

            return songs.OrderBy(s => s.Name);
        }

        protected override void DisposeControl()
        {
            _songPlayer.OnStarted -= OnPlaybackStateChanged;
            _songPlayer.OnPaused -= OnPlaybackStateChanged;
            _songPlayer.OnResumed -= OnPlaybackStateChanged;
            _songPlayer.OnStopped -= OnPlaybackStateChanged;
            _songPlayer.OnCompleted -= OnPlaybackStateChanged;

            _filterBar.SearchChanged -= OnFilterChanged;
            _filterBar.FilterChanged -= OnFilterChanged;
            _songListPanel.SongPlayRequested -= OnSongPlayRequested;
            _songListPanel.SongDeleteRequested -= OnSongDeleteRequested;
            _songListPanel.CountChanged -= OnCountChanged;
            _statusBar.ImportClicked -= OnImportClicked;

            _songPlayer.Stop();

            _nowPlayingPanel?.Dispose();
            _filterBar?.Dispose();
            _songListPanel?.Dispose();
            _statusBar?.Dispose();

            base.DisposeControl();
        }
    }
}
