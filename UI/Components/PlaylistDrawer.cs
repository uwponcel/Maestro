using System;
using System.Collections.Generic;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Maestro.Models;
using Maestro.Services;
using Microsoft.Xna.Framework;

namespace Maestro.UI.Components
{
    public class PlaylistDrawer : Panel
    {
        public static class Layout
        {
            public const int DrawerWidth = 180;
            public const int HeaderHeight = 32;
            public const int HeaderPaddingX = 8;
            public const int ClearButtonWidth = 50;
            public const int ClearButtonHeight = 22;
            public const int CardSpacing = 2;
            public const int ContentPadding = 4;
        }

        private readonly PlaylistService _playlistService;
        private readonly Dictionary<Song, QueueSongCard> _cardMap = new Dictionary<Song, QueueSongCard>();

        private Panel _header;
        private Panel _separator;
        private Label _titleLabel;
        private StandardButton _clearButton;
        private FlowPanel _songList;

        private QueueSongCard _draggingCard;
        private QueueSongCard _dragGhost;
        private int _dragTargetIndex = -1;
        private int _cardWidth;

        public PlaylistDrawer(PlaylistService playlistService, int height)
        {
            _playlistService = playlistService;

            Size = new Point(Layout.DrawerWidth, height);
            BackgroundColor = new Color(20, 20, 20, 240);

            BuildHeader();
            BuildSongList(height);

            _cardWidth = Layout.DrawerWidth - Layout.ContentPadding * 2 - 4;

            _playlistService.QueueChanged += OnQueueChanged;
            Input.Mouse.LeftMouseButtonReleased += OnGlobalMouseReleased;
            Input.Mouse.MouseMoved += OnGlobalMouseMoved;
            RefreshCards();
        }

        private void BuildHeader()
        {
            _header = new Panel
            {
                Parent = this,
                Location = Point.Zero,
                Size = new Point(Layout.DrawerWidth, Layout.HeaderHeight),
                BackgroundColor = Color.Transparent
            };

            _titleLabel = new Label
            {
                Parent = _header,
                Text = "Queue (0)",
                Location = new Point(Layout.HeaderPaddingX, (Layout.HeaderHeight - 16) / 2),
                Font = Content.DefaultFont14,
                TextColor = MaestroTheme.CreamWhite,
                AutoSizeWidth = true
            };

            _clearButton = new StandardButton
            {
                Parent = _header,
                Text = "Clear",
                Location = new Point(Layout.DrawerWidth - Layout.ClearButtonWidth - Layout.HeaderPaddingX,
                    (Layout.HeaderHeight - Layout.ClearButtonHeight) / 2),
                Width = Layout.ClearButtonWidth,
                Height = Layout.ClearButtonHeight
            };
            _clearButton.Click += OnClearClicked;

            // Separator line under header
            _separator = new Panel
            {
                Parent = this,
                Location = new Point(Layout.ContentPadding, Layout.HeaderHeight - 1),
                Size = new Point(Layout.DrawerWidth - Layout.ContentPadding * 2, 1),
                BackgroundColor = MaestroTheme.MediumGray
            };
        }

        private void BuildSongList(int totalHeight)
        {
            var listHeight = totalHeight - Layout.HeaderHeight - Layout.ContentPadding * 2;

            _songList = new FlowPanel
            {
                Parent = this,
                Location = new Point(Layout.ContentPadding, Layout.HeaderHeight),
                Size = new Point(Layout.DrawerWidth - Layout.ContentPadding * 2, listHeight),
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                ControlPadding = new Vector2(0, Layout.CardSpacing),
                CanScroll = true,
                ShowBorder = false,
                BackgroundColor = Color.Transparent
            };
        }

        private void OnQueueChanged(object sender, EventArgs e)
        {
            RefreshCards();
        }

        private void OnClearClicked(object sender, MouseEventArgs e)
        {
            _playlistService.Clear();
        }

        private void RefreshCards()
        {
            // Clear existing cards
            foreach (var card in _cardMap.Values)
            {
                card.RemoveRequested -= OnCardRemoveRequested;
                card.DragStarted -= OnCardDragStarted;
                card.DragEnded -= OnCardDragEnded;
                card.Dispose();
            }
            _cardMap.Clear();

            // Create new cards
            var queue = _playlistService.Queue;

            for (var i = 0; i < queue.Count; i++)
            {
                var song = queue[i];
                var card = new QueueSongCard(song, i, _cardWidth)
                {
                    Parent = _songList
                };
                card.RemoveRequested += OnCardRemoveRequested;
                card.DragStarted += OnCardDragStarted;
                card.DragEnded += OnCardDragEnded;
                _cardMap[song] = card;
            }

            // Update title
            _titleLabel.Text = $"Queue ({queue.Count})";
            _clearButton.Enabled = queue.Count > 0;
        }

        private void OnCardRemoveRequested(object sender, EventArgs e)
        {
            if (sender is QueueSongCard card)
            {
                _playlistService.Remove(card.Song);
            }
        }

        private void OnCardDragStarted(object sender, EventArgs e)
        {
            _draggingCard = sender as QueueSongCard;

            if (_draggingCard != null)
            {
                // Dim the original card
                _draggingCard.Opacity = 0.3f;

                // Create ghost that follows cursor
                _dragGhost = new QueueSongCard(_draggingCard.Song, _draggingCard.Index, _cardWidth)
                {
                    Parent = GameService.Graphics.SpriteScreen,
                    ZIndex = Screen.TOOLTIP_BASEZINDEX,
                    Location = new Point(
                        Input.Mouse.Position.X - _cardWidth / 2,
                        Input.Mouse.Position.Y - QueueSongCard.Layout.Height / 2)
                };
            }
        }

        private void OnCardDragEnded(object sender, EventArgs e)
        {
            FinalizeDrag();
        }

        private void OnGlobalMouseReleased(object sender, MouseEventArgs e)
        {
            if (_draggingCard != null)
            {
                FinalizeDrag();
            }
        }

        private void OnGlobalMouseMoved(object sender, MouseEventArgs e)
        {
            // Update ghost position while dragging
            if (_dragGhost != null)
            {
                _dragGhost.Location = new Point(
                    Input.Mouse.Position.X - _cardWidth / 2,
                    Input.Mouse.Position.Y - QueueSongCard.Layout.Height / 2);
            }
        }

        private void FinalizeDrag()
        {
            if (_draggingCard != null)
            {
                // Restore original card opacity
                _draggingCard.Opacity = 1f;

                // Calculate target index based on current mouse position
                UpdateDragTarget();

                if (_dragTargetIndex >= 0)
                {
                    var fromIndex = _playlistService.IndexOf(_draggingCard.Song);
                    if (fromIndex >= 0 && fromIndex != _dragTargetIndex)
                    {
                        _playlistService.Move(fromIndex, _dragTargetIndex);
                    }
                }
            }

            // Dispose ghost
            if (_dragGhost != null)
            {
                _dragGhost.Dispose();
                _dragGhost = null;
            }

            _draggingCard = null;
            _dragTargetIndex = -1;
        }

        private void UpdateDragTarget()
        {
            if (!_songList.AbsoluteBounds.Contains(Input.Mouse.Position))
            {
                _dragTargetIndex = -1;
                return;
            }

            var mouseY = Input.Mouse.Position.Y - _songList.AbsoluteBounds.Y + _songList.VerticalScrollOffset;
            var cardHeight = QueueSongCard.Layout.Height + Layout.CardSpacing;
            _dragTargetIndex = Math.Max(0, Math.Min(_playlistService.Count - 1, mouseY / cardHeight));
        }

        protected override void DisposeControl()
        {
            _playlistService.QueueChanged -= OnQueueChanged;
            _clearButton.Click -= OnClearClicked;
            Input.Mouse.LeftMouseButtonReleased -= OnGlobalMouseReleased;
            Input.Mouse.MouseMoved -= OnGlobalMouseMoved;

            _dragGhost?.Dispose();
            _dragGhost = null;

            foreach (var card in _cardMap.Values)
            {
                card.RemoveRequested -= OnCardRemoveRequested;
                card.DragStarted -= OnCardDragStarted;
                card.DragEnded -= OnCardDragEnded;
                card.Dispose();
            }
            _cardMap.Clear();

            _header?.Dispose();
            _separator?.Dispose();
            _songList?.Dispose();

            base.DisposeControl();
        }
    }
}
