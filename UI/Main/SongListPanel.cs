using System;
using System.Collections.Generic;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Maestro.Models;
using Maestro.Services.Playback;
using Microsoft.Xna.Framework;

namespace Maestro.UI.Main
{
    public class SongListPanel : FlowPanel
    {
        public event EventHandler<Song> SongSelected;
        public event EventHandler<Song> SongPlayRequested;
        public event EventHandler<Song> SongDeleteRequested;
        public event EventHandler<Song> EditRequested;
        public event EventHandler<Song> AddToQueueRequested;
        public event EventHandler<int> CountChanged;
        
        public static class Layout
        {
            public const int Height = 280;
            public const int CardSpacing = 4;
            public const int OuterPadding = 4;
            public const int ScrollbarWidth = 12;
        }

        private readonly SongPlayer _songPlayer;
        private readonly Dictionary<Song, SongCard> _songCards = new Dictionary<Song, SongCard>();
        private readonly int _cardWidth;

        public Song SelectedSong { get; private set; }

        public SongListPanel(SongPlayer songPlayer, int contentWidth)
        {
            _songPlayer = songPlayer;
            _cardWidth = contentWidth - Layout.ScrollbarWidth - Layout.OuterPadding;

            Size = new Point(contentWidth, Layout.Height);
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
                card.DeleteRequested += OnCardDeleteRequested;
                card.EditRequested += OnCardEditRequested;
                card.AddToQueueRequested += OnCardAddToQueueRequested;
                _songCards[song] = card;
            }

            UpdateCardStates();
            CountChanged?.Invoke(this, _songCards.Count);
        }

        private void OnCardPlayClicked(object sender, MouseEventArgs e)
        {
            if (FocusedControl is TextInputBase textInput)
            {
                textInput.Focused = false;
            }

            var card = sender as SongCard;
            if (card?.Song != null)
            {
                SelectSong(card.Song);
                SongPlayRequested?.Invoke(this, card.Song);
            }
        }

        private void OnCardClicked(object sender, MouseEventArgs e)
        {
            if (FocusedControl is TextInputBase textInput)
            {
                textInput.Focused = false;
            }

            var card = sender as SongCard;
            if (card?.Song != null)
            {
                SelectSong(card.Song);
            }
        }

        private void OnCardDeleteRequested(object sender, EventArgs e)
        {
            var card = sender as SongCard;
            if (card?.Song != null)
            {
                SongDeleteRequested?.Invoke(this, card.Song);
            }
        }

        private void OnCardEditRequested(object sender, EventArgs e)
        {
            var card = sender as SongCard;
            if (card?.Song != null)
            {
                EditRequested?.Invoke(this, card.Song);
            }
        }

        private void OnCardAddToQueueRequested(object sender, EventArgs e)
        {
            var card = sender as SongCard;
            if (card?.Song != null)
            {
                AddToQueueRequested?.Invoke(this, card.Song);
            }
        }

        public void SelectSong(Song song)
        {
            SelectedSong = song;
            UpdateCardStates();
            SongSelected?.Invoke(this, song);
        }

        public void UpdateCardStates()
        {
            foreach (var kvp in _songCards)
            {
                var isPlaying = _songPlayer.IsPlaying && _songPlayer.CurrentSong == kvp.Key;
                var isSelected = kvp.Key == SelectedSong;

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
                card.DeleteRequested -= OnCardDeleteRequested;
                card.EditRequested -= OnCardEditRequested;
                card.AddToQueueRequested -= OnCardAddToQueueRequested;
                card.Dispose();
            }
            _songCards.Clear();

            base.DisposeControl();
        }
    }
}
