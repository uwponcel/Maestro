using System;
using System.Collections.Generic;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Maestro.Models;
using Maestro.Services;
using Microsoft.Xna.Framework;

namespace Maestro.UI.Components
{
    public class SongListPanel : FlowPanel
    {
        public static class Layout
        {
            public const int CardSpacing = 4;
            public const int OuterPadding = 4;
            public const int ScrollbarWidth = 20;
        }

        public event EventHandler<Song> SongSelected;
        public event EventHandler<Song> SongPlayRequested;
        public event EventHandler<int> CountChanged;

        private readonly SongPlayer _songPlayer;
        private readonly Dictionary<Song, SongCard> _songCards = new Dictionary<Song, SongCard>();
        private Song _selectedSong;
        private readonly int _cardWidth;

        public Song SelectedSong => _selectedSong;

        public SongListPanel(SongPlayer songPlayer, int width, int height)
        {
            _songPlayer = songPlayer;
            _cardWidth = width - Layout.ScrollbarWidth - Layout.OuterPadding;

            Size = new Point(width, height);
            FlowDirection = ControlFlowDirection.SingleTopToBottom;
            CanScroll = true;
            ShowBorder = true;
            ControlPadding = new Vector2(0, Layout.CardSpacing);
            OuterControlPadding = new Vector2(Layout.OuterPadding, Layout.OuterPadding);
        }

        public void RefreshSongs(IEnumerable<Song> songs)
        {
            ClearChildren();
            _songCards.Clear();

            foreach (var song in songs)
            {
                var card = new SongCard(song, _cardWidth)
                {
                    Parent = this
                };
                card.PlayClicked += OnCardPlayClicked;
                card.CardClicked += OnCardClicked;
                _songCards[song] = card;
            }

            UpdateCardStates();
            CountChanged?.Invoke(this, _songCards.Count);
        }

        private void OnCardPlayClicked(object sender, MouseEventArgs e)
        {
            var card = sender as SongCard;
            if (card?.Song != null)
            {
                SelectSong(card.Song);
                SongPlayRequested?.Invoke(this, card.Song);
            }
        }

        private void OnCardClicked(object sender, MouseEventArgs e)
        {
            var card = sender as SongCard;
            if (card?.Song != null)
            {
                SelectSong(card.Song);
            }
        }

        public void SelectSong(Song song)
        {
            _selectedSong = song;
            UpdateCardStates();
            SongSelected?.Invoke(this, song);
        }

        public void UpdateCardStates()
        {
            foreach (var kvp in _songCards)
            {
                var isPlaying = _songPlayer.IsPlaying && _songPlayer.CurrentSong == kvp.Key;
                var isSelected = kvp.Key == _selectedSong;

                kvp.Value.IsPlaying = isPlaying;
                kvp.Value.IsSelected = isSelected;
            }
        }

        protected override void DisposeControl()
        {
            foreach (var card in _songCards.Values)
            {
                card.PlayClicked -= OnCardPlayClicked;
                card.CardClicked -= OnCardClicked;
                card.Dispose();
            }
            _songCards.Clear();

            base.DisposeControl();
        }
    }
}
