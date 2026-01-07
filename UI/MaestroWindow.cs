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
        public static class Layout
        {
            // Content area dimensions
            public const int ContentWidth = 390;
            public const int ContentHeight = 420;

            // Component gap
            public const int ComponentGap = 5;

            // Y positions for child components (vertical stacking)
            public const int NowPlayingY = 0;
            public static int FilterBarY => NowPlayingPanel.Layout.Height + ComponentGap;
            public static int SongListY => FilterBarY + SongFilterBar.Layout.Height - 1;
            public static int StatusBarY => ContentHeight - StatusBar.Layout.Height;

            // SongList fills remaining space
            public static int SongListHeight => ContentHeight - SongListY - StatusBar.Layout.Height;
        }

        private static readonly Logger Logger = Logger.GetLogger<MaestroWindow>();

        private readonly SongPlayer _songPlayer;
        private readonly List<Song> _allSongs;

        private NowPlayingPanel _nowPlayingPanel;
        private SongFilterBar _filterBar;
        private SongListPanel _songListPanel;
        private StatusBar _statusBar;

        public MaestroWindow(Texture2D background, SongPlayer songPlayer, List<Song> songs)
            : base(
                background,
                new Rectangle(0, 0, 420, 460),
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
            _nowPlayingPanel = new NowPlayingPanel(_songPlayer, Layout.ContentWidth)
            {
                Parent = this,
                Location = new Point(0, Layout.NowPlayingY)
            };

            _filterBar = new SongFilterBar(Layout.ContentWidth)
            {
                Parent = this,
                Location = new Point(0, Layout.FilterBarY)
            };
            _filterBar.SearchChanged += OnFilterChanged;
            _filterBar.FilterChanged += OnFilterChanged;

            _songListPanel = new SongListPanel(_songPlayer, Layout.ContentWidth, Layout.SongListHeight)
            {
                Parent = this,
                Location = new Point(0, Layout.SongListY)
            };
            _songListPanel.SongPlayRequested += OnSongPlayRequested;
            _songListPanel.CountChanged += OnCountChanged;

            _statusBar = new StatusBar(Layout.ContentWidth)
            {
                Parent = this,
                Location = new Point(0, Layout.StatusBarY)
            };
            _statusBar.TotalCount = _allSongs.Count;

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

        private void RefreshSongList()
        {
            var filteredSongs = GetFilteredSongs();
            _songListPanel.RefreshSongs(filteredSongs);
        }

        private IEnumerable<Song> GetFilteredSongs()
        {
            var songs = _allSongs.AsEnumerable();

            var filter = _filterBar.SelectedInstrument;
            if (filter != "All Instruments")
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
            _songListPanel.CountChanged -= OnCountChanged;

            _songPlayer.Stop();

            _nowPlayingPanel?.Dispose();
            _filterBar?.Dispose();
            _songListPanel?.Dispose();
            _statusBar?.Dispose();

            base.DisposeControl();
        }
    }
}
