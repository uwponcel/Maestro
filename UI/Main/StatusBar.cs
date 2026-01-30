using System;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Maestro.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maestro.UI.Main
{
    public class StatusBar : Panel
    {
        private class InstrumentSelectorPanel : Control
        {
            private const int ITEM_HEIGHT = 28;
            private const int PADDING_X = 10;
            private static readonly InstrumentType[] Instruments = { InstrumentType.Piano, InstrumentType.Harp, InstrumentType.Lute, InstrumentType.Bass };

            private readonly StatusBar _owner;
            private int _highlightedIndex = -1;

            public InstrumentSelectorPanel(StatusBar owner)
            {
                _owner = owner;

                var height = Instruments.Length * ITEM_HEIGHT + 8;
                _size = new Point(100, height);
                _location = GetPanelLocation();
                _zIndex = Screen.TOOLTIP_BASEZINDEX;

                Parent = GameService.Graphics.SpriteScreen;

                Input.Mouse.LeftMouseButtonPressed += OnMouseButtonPressed;
                Input.Mouse.RightMouseButtonPressed += OnMouseButtonPressed;
            }

            private Point GetPanelLocation()
            {
                var buttonLocation = _owner._createButton.AbsoluteBounds.Location;
                return buttonLocation + new Point(0, _owner._createButton.Height - 1);
            }

            private void OnMouseButtonPressed(object sender, MouseEventArgs e)
            {
                if (!MouseOver)
                {
                    Dispose();
                }
            }

            protected override void OnMouseMoved(MouseEventArgs e)
            {
                var y = RelativeMousePosition.Y - 4;
                _highlightedIndex = y >= 0 && y < Instruments.Length * ITEM_HEIGHT ? y / ITEM_HEIGHT : -1;
                base.OnMouseMoved(e);
            }

            protected override void OnClick(MouseEventArgs e)
            {
                if (_highlightedIndex >= 0 && _highlightedIndex < Instruments.Length)
                {
                    var instrument = Instruments[_highlightedIndex];
                    Dispose();
                    _owner.OnInstrumentSelected(instrument);
                }
                base.OnClick(e);
            }

            protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
            {
                // Background
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(Point.Zero, _size), Color.Black);

                // Border
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(0, 0, _size.X, 1), MaestroTheme.MediumGray);
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(0, _size.Y - 1, _size.X, 1), MaestroTheme.MediumGray);
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(0, 0, 1, _size.Y), MaestroTheme.MediumGray);
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(_size.X - 1, 0, 1, _size.Y), MaestroTheme.MediumGray);

                var y = 4;
                for (var i = 0; i < Instruments.Length; i++)
                {
                    var isHighlighted = _highlightedIndex == i;

                    if (isHighlighted)
                    {
                        spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel,
                            new Rectangle(2, y, _size.X - 4, ITEM_HEIGHT),
                            new Color(45, 37, 25, 255));
                    }

                    var textColor = isHighlighted ? ContentService.Colors.Chardonnay : Color.FromNonPremultiplied(239, 240, 239, 255);
                    spriteBatch.DrawStringOnCtrl(this, Instruments[i].ToString(), Content.DefaultFont14,
                        new Rectangle(PADDING_X, y, _size.X - PADDING_X * 2, ITEM_HEIGHT), textColor);

                    y += ITEM_HEIGHT;
                }
            }

            protected override void DisposeControl()
            {
                if (_owner != null)
                {
                    _owner._instrumentPanel = null;
                }

                Input.Mouse.LeftMouseButtonPressed -= OnMouseButtonPressed;
                Input.Mouse.RightMouseButtonPressed -= OnMouseButtonPressed;

                base.DisposeControl();
            }
        }

        public static class Layout
        {
            public static int Height => MaestroTheme.ActionButtonHeight;
        }

        public event EventHandler ImportClicked;
        public event EventHandler CommunityClicked;
        public event EventHandler<InstrumentType> CreateClicked;

        private readonly Label _statusLabel;
        private readonly StandardButton _communityButton;
        private readonly StandardButton _createButton;
        private readonly StandardButton _importButton;
        private InstrumentSelectorPanel _instrumentPanel;
        private bool _hadPanel;
        private int _visibleCount;
        private int _totalCount;

        public int VisibleCount
        {
            get => _visibleCount;
            set
            {
                _visibleCount = value;
                UpdateText();
            }
        }

        public int TotalCount
        {
            get => _totalCount;
            set
            {
                _totalCount = value;
                UpdateText();
            }
        }

        public StatusBar(int width)
        {
            Size = new Point(width, Layout.Height);
            BackgroundColor = Color.Transparent;

            const int buttonWidth = 70;
            const int communityButtonWidth = 95;
            const int buttonSpacing = 5;
            var buttonsWidth = communityButtonWidth + buttonWidth * 2 + buttonSpacing * 2;

            _statusLabel = new Label
            {
                Parent = this,
                Location = new Point(0, 0),
                Width = width - buttonsWidth - 10,
                Height = Height,
                Font = GameService.Content.DefaultFont12,
                TextColor = MaestroTheme.LightGray,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            // Position buttons starting from the right edge
            var x = width - buttonWidth; // Import at far right

            _importButton = new StandardButton
            {
                Parent = this,
                Text = "Import",
                Location = new Point(x, 0),
                Size = new Point(buttonWidth, MaestroTheme.ActionButtonHeight),
                BasicTooltipText = "Toggle Import"
            };
            _importButton.Click += (s, e) => ImportClicked?.Invoke(this, EventArgs.Empty);
            x -= buttonWidth + buttonSpacing;

            _createButton = new StandardButton
            {
                Parent = this,
                Text = "Create",
                Location = new Point(x, 0),
                Size = new Point(buttonWidth, MaestroTheme.ActionButtonHeight)
            };
            _createButton.Click += OnCreateButtonClick;
            x -= communityButtonWidth + buttonSpacing;

            _communityButton = new StandardButton
            {
                Parent = this,
                Text = "Community",
                Location = new Point(x, 0),
                Size = new Point(communityButtonWidth, MaestroTheme.ActionButtonHeight),
                BasicTooltipText = "Browse & upload community songs"
            };
            _communityButton.Click += (s, e) => CommunityClicked?.Invoke(this, EventArgs.Empty);
        }

        private void OnCreateButtonClick(object sender, MouseEventArgs e)
        {
            if (_instrumentPanel == null && !_hadPanel)
            {
                _instrumentPanel = new InstrumentSelectorPanel(this);
            }
            else
            {
                _hadPanel = false;
            }
        }

        private void OnInstrumentSelected(InstrumentType instrument)
        {
            _hadPanel = _createButton.MouseOver;
            CreateClicked?.Invoke(this, instrument);
        }

        public void SetCreateButtonEnabled(bool enabled)
        {
            _createButton.Enabled = enabled;
            _createButton.BasicTooltipText = enabled ? null : "Close the Creator window first";
        }

        private void UpdateText()
        {
            _statusLabel.Text = _visibleCount == _totalCount
                ? $"  {_totalCount} songs"
                : $"  {_visibleCount} of {_totalCount} songs";
        }

        protected override void DisposeControl()
        {
            _instrumentPanel?.Dispose();
            _statusLabel?.Dispose();
            _communityButton?.Dispose();
            _createButton?.Dispose();
            _importButton?.Dispose();
            base.DisposeControl();
        }
    }
}
