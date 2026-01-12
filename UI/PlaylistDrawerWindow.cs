using System;
using System.Collections.Generic;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Maestro.Services;
using Maestro.UI.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maestro.UI
{
    public class PlaylistDrawerWindow : StandardWindow
    {
        private static class Layout
        {
            public const int WindowWidth = 230;
            public const int WindowHeight = 400;
            public const int ContentWidth = 200;
            public const int ContentHeight = 350;
            public const int ContentPaddingX = 15;
            public const int ContentPaddingY = 30;
            public const int HeaderHeight = 40;
            public const int ClearButtonWidth = 50;
            public const int ClearButtonHeight = 22;
            public const int CardSpacing = 4;
            public const int OuterPadding = 4;
            public const int DragHandleHalfWidth = 6;
        }

        private static Texture2D _backgroundTexture;

        private readonly PlaylistService _playlistService;
        private readonly List<QueueSongCard> _cards = new List<QueueSongCard>();
        private readonly int _cardWidth;

        private Panel _header;
        private StandardButton _clearButton;
        private FlowPanel _songList;

        private QueueSongCard _draggingCard;
        private QueueSongCard _dragGhost;
        private int _dragTargetIndex = -1;

        public PlaylistDrawerWindow(PlaylistService playlistService)
            : base(
                GetBackground(),
                new Rectangle(0, 0, Layout.WindowWidth, Layout.WindowHeight),
                new Rectangle(Layout.ContentPaddingX, Layout.ContentPaddingY, Layout.ContentWidth, Layout.ContentHeight))
        {
            _playlistService = playlistService;
            _cardWidth = Layout.ContentWidth - Layout.OuterPadding * 2;

            Title = "Queue songs";
            CanClose = true;
            CanResize = false;
            SavesPosition = false;
            Id = "MaestroQueueDrawer_v1";

            BuildContent();

            _playlistService.QueueChanged += OnQueueChanged;
            Input.Mouse.LeftMouseButtonReleased += OnGlobalMouseReleased;
            Input.Mouse.MouseMoved += OnGlobalMouseMoved;

            RefreshCards();
        }

        private static Texture2D GetBackground()
        {
            return _backgroundTexture ?? (_backgroundTexture = MaestroTheme.CreateDrawerBackground(Layout.WindowWidth, Layout.WindowHeight));
        }

        private static Point GetDragHandleOffset()
        {
            return new Point(
                QueueSongCard.Layout.DragHandleX + Layout.DragHandleHalfWidth,
                QueueSongCard.Layout.Height / 2);
        }

        private void BuildContent()
        {
            _header = new Panel
            {
                Parent = this,
                Location = Point.Zero,
                Size = new Point(Layout.ContentWidth, Layout.HeaderHeight),
                BackgroundColor = MaestroTheme.SlateGray,
                ShowBorder = true
            };

            _clearButton = new StandardButton
            {
                Parent = this,
                Text = "Clear",
                Location = new Point(
                    Layout.ContentWidth - Layout.ClearButtonWidth - Layout.OuterPadding,
                    (Layout.HeaderHeight - Layout.ClearButtonHeight) / 2),
                Width = Layout.ClearButtonWidth,
                Height = Layout.ClearButtonHeight
            };
            _clearButton.Click += OnClearClicked;

            var listTop = Layout.HeaderHeight + Layout.OuterPadding;
            _songList = new FlowPanel
            {
                Parent = this,
                Location = new Point(0, listTop),
                Size = new Point(Layout.ContentWidth, Layout.ContentHeight - listTop),
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                ControlPadding = new Vector2(0, Layout.CardSpacing),
                OuterControlPadding = new Vector2(Layout.OuterPadding, Layout.OuterPadding),
                CanScroll = true,
                ShowBorder = true,
                BackgroundColor = Color.Transparent
            };
        }

        private void OnQueueChanged(object sender, EventArgs e) => RefreshCards();

        private void OnClearClicked(object sender, MouseEventArgs e) => _playlistService.Clear();

        private void RefreshCards()
        {
            foreach (var card in _cards)
            {
                card.RemoveRequested -= OnCardRemoveRequested;
                card.DragStarted -= OnCardDragStarted;
                card.DragEnded -= OnCardDragEnded;
                card.Dispose();
            }
            _cards.Clear();

            var queue = _playlistService.Queue;
            for (var i = 0; i < queue.Count; i++)
            {
                var card = new QueueSongCard(queue[i], i, _cardWidth) { Parent = _songList };
                card.RemoveRequested += OnCardRemoveRequested;
                card.DragStarted += OnCardDragStarted;
                card.DragEnded += OnCardDragEnded;
                _cards.Add(card);
            }

            _clearButton.Enabled = queue.Count > 0;
        }

        private void OnCardRemoveRequested(object sender, EventArgs e)
        {
            if (sender is QueueSongCard card)
                _playlistService.RemoveAt(card.Index);
        }

        private void OnCardDragStarted(object sender, EventArgs e)
        {
            _draggingCard = sender as QueueSongCard;
            if (_draggingCard == null) return;

            _draggingCard.Opacity = 0.3f;

            var offset = GetDragHandleOffset();
            _dragGhost = new QueueSongCard(_draggingCard.Song, _draggingCard.Index, _cardWidth)
            {
                Parent = GameService.Graphics.SpriteScreen,
                ZIndex = Screen.TOOLTIP_BASEZINDEX,
                Location = new Point(Input.Mouse.Position.X - offset.X, Input.Mouse.Position.Y - offset.Y)
            };
        }

        private void OnCardDragEnded(object sender, EventArgs e) => FinalizeDrag();

        private void OnGlobalMouseReleased(object sender, MouseEventArgs e)
        {
            if (_draggingCard != null)
                FinalizeDrag();
        }

        private void OnGlobalMouseMoved(object sender, MouseEventArgs e)
        {
            if (_dragGhost == null) return;

            var offset = GetDragHandleOffset();
            _dragGhost.Location = new Point(Input.Mouse.Position.X - offset.X, Input.Mouse.Position.Y - offset.Y);
        }

        private void FinalizeDrag()
        {
            if (_draggingCard != null)
            {
                _draggingCard.Opacity = 1f;
                _draggingCard.EndDrag();

                UpdateDragTarget();
                if (_dragTargetIndex >= 0)
                {
                    var fromIndex = _playlistService.IndexOf(_draggingCard.Song);
                    if (fromIndex >= 0 && fromIndex != _dragTargetIndex)
                        _playlistService.Move(fromIndex, _dragTargetIndex);
                }
            }

            _dragGhost?.Dispose();
            _dragGhost = null;
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

        // Prevent window dragging while allowing close button to work
        protected override void OnLeftMouseButtonPressed(MouseEventArgs e)
        {
            if (MouseOverExitButton && CanClose)
                Hide();
        }

        protected override void DisposeControl()
        {
            _playlistService.QueueChanged -= OnQueueChanged;
            _clearButton.Click -= OnClearClicked;
            Input.Mouse.LeftMouseButtonReleased -= OnGlobalMouseReleased;
            Input.Mouse.MouseMoved -= OnGlobalMouseMoved;

            _dragGhost?.Dispose();

            foreach (var card in _cards)
            {
                card.RemoveRequested -= OnCardRemoveRequested;
                card.DragStarted -= OnCardDragStarted;
                card.DragEnded -= OnCardDragEnded;
                card.Dispose();
            }
            _cards.Clear();

            _header?.Dispose();
            _clearButton?.Dispose();
            _songList?.Dispose();

            base.DisposeControl();
        }
    }
}
