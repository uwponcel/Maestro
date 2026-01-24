using System;
using System.Collections.Generic;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Maestro.Models;
using Maestro.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maestro.UI.Playlist
{
    public class PlaylistDrawerWindow : StandardWindow
    {
        private static class Layout
        {
            public const int WindowWidth = 250;
            public const int WindowHeight = 400;
            public const int ContentWidth = 220;
            public const int ContentHeight = 365;
            public const int ContentPaddingX = 15;
            public const int HeaderHeight = 50;
            public const int ButtonSpacing = 8;
            public const int ButtonHeight = 30;
            public const int CardSpacing = 4;
            public const int OuterPadding = 6;
            public const int DragHandleHalfWidth = 6;
        }

        public event EventHandler PlayQueueRequested;

        private const float ANIMATION_DURATION = 0.15f;
        private const int SLIDE_DISTANCE = 80;

        private static Texture2D _backgroundTexture;

        private readonly PlaylistService _playlistService;
        private readonly List<QueueSongCard> _cards = new List<QueueSongCard>();
        private readonly int _cardWidth;

        private Panel _header;
        private StandardButton _clearButton;
        private StandardButton _playButton;
        private FlowPanel _songList;

        private QueueSongCard _draggingCard;
        private QueueSongCard _dragGhost;
        private int _dragTargetIndex = -1;
        private bool _isPlayingFromQueue;

        private bool _isAnimating;
        private float _animationProgress;
        private int _targetX;

        private Panel _instrumentOverlay;
        private Label _instrumentOverlayLabel;

        public PlaylistDrawerWindow(PlaylistService playlistService)
            : base(
                GetBackground(),
                new Rectangle(0, 0, Layout.WindowWidth, Layout.WindowHeight),
                new Rectangle(Layout.ContentPaddingX, MaestroTheme.WindowContentTopPadding, Layout.ContentWidth, Layout.ContentHeight))
        {
            _playlistService = playlistService;
            _cardWidth = Layout.ContentWidth - Layout.OuterPadding * 2;

            Title = "";
            CanClose = true;
            CanResize = false;
            SavesPosition = false;
            Id = "MaestroQueueDrawer_v5";

            BuildContent();
            SubscribeToEvents();
            RefreshCards();
        }

        public void SetQueuePlaybackMode(bool isPlaying)
        {
            _isPlayingFromQueue = isPlaying;
            UpdatePlayButtonText();
        }

        public void ShowInstrumentConfirmation(InstrumentType instrument)
        {
            _instrumentOverlayLabel.Text = $"Equip {instrument} and press play\nin Maestro to continue";
            _instrumentOverlay.Visible = true;
        }

        public void HideInstrumentConfirmation()
        {
            _instrumentOverlay.Visible = false;
        }

        public void ShowWithAnimation(int targetX, int targetY)
        {
            _targetX = targetX;
            Location = new Point(targetX - SLIDE_DISTANCE, targetY);
            _animationProgress = 0f;
            _isAnimating = true;
            base.Show();
        }

        public override void UpdateContainer(GameTime gameTime)
        {
            base.UpdateContainer(gameTime);

            if (!_isAnimating) return;
            _animationProgress += (float)gameTime.ElapsedGameTime.TotalSeconds / ANIMATION_DURATION;

            if (_animationProgress >= 1f)
            {
                _animationProgress = 1f;
                _isAnimating = false;
                Location = new Point(_targetX, Location.Y);
            }
            else
            {
                var eased = EaseOutCubic(_animationProgress);
                var currentX = (int)(_targetX - SLIDE_DISTANCE + SLIDE_DISTANCE * eased);
                Location = new Point(currentX, Location.Y);
            }
        }

        protected override void OnLeftMouseButtonPressed(MouseEventArgs e)
        {
            if (MouseOverExitButton && CanClose)
                Hide();
        }

        protected override void DisposeControl()
        {
            UnsubscribeFromEvents();
            DisposeDragGhost();
            DisposeCards();
            DisposeControls();

            base.DisposeControl();
        }

        private static Texture2D GetBackground()
        {
            return _backgroundTexture ?? (_backgroundTexture = MaestroTheme.CreateDrawerBackground(Layout.WindowWidth, Layout.WindowHeight));
        }

        private void BuildContent()
        {
            BuildHeader();
            BuildButtons();
            BuildSongList();
            BuildInstrumentOverlay();
        }

        private void BuildHeader()
        {
            _header = new Panel
            {
                Parent = this,
                Location = Point.Zero,
                Size = new Point(Layout.ContentWidth, Layout.HeaderHeight),
                BackgroundColor = MaestroTheme.DrawerHeader,
                ShowBorder = true
            };
        }

        private void BuildButtons()
        {
            const int buttonY = (Layout.HeaderHeight - Layout.ButtonHeight) / 2;
            const int availableWidth = Layout.ContentWidth - Layout.OuterPadding * 2 - Layout.ButtonSpacing;
            const int buttonWidth = availableWidth / 2;

            _clearButton = new StandardButton
            {
                Parent = this,
                Text = "Clear",
                Location = new Point(Layout.OuterPadding, buttonY),
                Width = buttonWidth,
                Height = Layout.ButtonHeight
            };
            _clearButton.Click += OnClearClicked;

            _playButton = new StandardButton
            {
                Parent = this,
                Text = "Play queue",
                Location = new Point(Layout.OuterPadding + buttonWidth + Layout.ButtonSpacing, buttonY),
                Width = buttonWidth,
                Height = Layout.ButtonHeight
            };
            _playButton.Click += OnPlayClicked;
        }

        private void BuildSongList()
        {
            const int listTop = Layout.HeaderHeight + Layout.OuterPadding;
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

        private void BuildInstrumentOverlay()
        {
            const int listTop = Layout.HeaderHeight + Layout.OuterPadding;
            const int overlayHeight = Layout.ContentHeight - listTop;

            _instrumentOverlay = new Panel
            {
                Parent = this,
                Location = new Point(0, listTop),
                Size = new Point(Layout.ContentWidth, overlayHeight),
                BackgroundColor = new Color(20, 25, 35, 240),
                ZIndex = 100,
                Visible = false
            };

            var labelHeight = 60;
            var centerY = (overlayHeight - labelHeight) / 2;

            _instrumentOverlayLabel = new Label
            {
                Parent = _instrumentOverlay,
                Text = "",
                Font = GameService.Content.DefaultFont14,
                AutoSizeWidth = false,
                AutoSizeHeight = false,
                Width = Layout.ContentWidth,
                Height = labelHeight,
                Location = new Point(0, centerY),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Middle,
                TextColor = new Color(200, 220, 255)
            };
        }

        private void SubscribeToEvents()
        {
            _playlistService.QueueChanged += OnQueueChanged;
            Input.Mouse.LeftMouseButtonReleased += OnGlobalMouseReleased;
            Input.Mouse.MouseMoved += OnGlobalMouseMoved;
        }

        private void OnQueueChanged(object sender, EventArgs e) => RefreshCards();

        private void OnClearClicked(object sender, MouseEventArgs e) => _playlistService.Clear();

        private void OnPlayClicked(object sender, MouseEventArgs e) => PlayQueueRequested?.Invoke(this, EventArgs.Empty);

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
            CreateDragGhost();
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

        private void RefreshCards()
        {
            ClearCards();
            CreateCards();
            UpdateButtonStates();
        }

        private void ClearCards()
        {
            foreach (var card in _cards)
            {
                card.RemoveRequested -= OnCardRemoveRequested;
                card.DragStarted -= OnCardDragStarted;
                card.DragEnded -= OnCardDragEnded;
                card.Dispose();
            }
            _cards.Clear();
        }

        private void CreateCards()
        {
            var queue = _playlistService.Queue;
            for (var i = 0; i < queue.Count; i++)
            {
                var card = new QueueSongCard(queue[i], i, _cardWidth) { Parent = _songList };
                card.RemoveRequested += OnCardRemoveRequested;
                card.DragStarted += OnCardDragStarted;
                card.DragEnded += OnCardDragEnded;
                _cards.Add(card);
            }
        }

        private void UpdateButtonStates()
        {
            var hasItems = _playlistService.HasItems;
            _clearButton.Enabled = hasItems;
            _playButton.Enabled = hasItems;
            UpdatePlayButtonText();
        }

        private void UpdatePlayButtonText()
        {
            _playButton.Text = _isPlayingFromQueue && _playlistService.HasItems ? "Next" : "Play queue";
        }

        private static Point GetDragHandleOffset()
        {
            return new Point(
                QueueSongCard.Layout.DragHandleX + Layout.DragHandleHalfWidth,
                QueueSongCard.Layout.Height / 2);
        }

        private void CreateDragGhost()
        {
            var offset = GetDragHandleOffset();
            _dragGhost = new QueueSongCard(_draggingCard.Song, _draggingCard.Index, _cardWidth)
            {
                Parent = GameService.Graphics.SpriteScreen,
                ZIndex = Screen.TOOLTIP_BASEZINDEX,
                Location = new Point(Input.Mouse.Position.X - offset.X, Input.Mouse.Position.Y - offset.Y),
                IsGhost = true
            };
        }

        private void FinalizeDrag()
        {
            if (_draggingCard != null)
            {
                _draggingCard.Opacity = 1f;
                _draggingCard.EndDrag();

                UpdateDragTarget();
                ApplyDragResult();
            }

            ResetDragState();
        }

        private void ApplyDragResult()
        {
            if (_dragTargetIndex < 0) return;

            var fromIndex = _playlistService.IndexOf(_draggingCard.Song);
            if (fromIndex >= 0 && fromIndex != _dragTargetIndex)
                _playlistService.Move(fromIndex, _dragTargetIndex);
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

        private void ResetDragState()
        {
            _dragGhost?.Dispose();
            _dragGhost = null;
            _draggingCard = null;
            _dragTargetIndex = -1;
        }

        private static float EaseOutCubic(float t)
        {
            return 1f - (float)Math.Pow(1 - t, 3);
        }

        private void UnsubscribeFromEvents()
        {
            _playlistService.QueueChanged -= OnQueueChanged;
            _clearButton.Click -= OnClearClicked;
            _playButton.Click -= OnPlayClicked;
            Input.Mouse.LeftMouseButtonReleased -= OnGlobalMouseReleased;
            Input.Mouse.MouseMoved -= OnGlobalMouseMoved;
        }

        private void DisposeDragGhost()
        {
            _dragGhost?.Dispose();
        }

        private void DisposeCards()
        {
            foreach (var card in _cards)
            {
                card.RemoveRequested -= OnCardRemoveRequested;
                card.DragStarted -= OnCardDragStarted;
                card.DragEnded -= OnCardDragEnded;
                card.Dispose();
            }
            _cards.Clear();
        }

        private void DisposeControls()
        {
            _header?.Dispose();
            _clearButton?.Dispose();
            _playButton?.Dispose();
            _songList?.Dispose();
            _instrumentOverlay?.Dispose();
        }
    }
}
