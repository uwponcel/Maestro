using System;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maestro.UI.Components
{
    public class FilterButton : Control
    {
        private class FilterPanel : Control
        {
            private const int ItemHeight = 24;
            private const int SeparatorHeight = 8;
            private const int PaddingX = 8;

            private static readonly string[] SourceItems = { "All", "Bundled", "Imported" };
            private static readonly string[] InstrumentItems = { "All", "Piano", "Harp", "Lute", "Bass" };

            private readonly FilterButton _owner;
            private int _highlightedIndex = -1;

            public FilterPanel(FilterButton owner)
            {
                _owner = owner;

                var totalItems = SourceItems.Length + InstrumentItems.Length;
                var height = totalItems * ItemHeight + SeparatorHeight;

                _size = new Point(_owner.Width, height);
                _location = GetPanelLocation();
                _zIndex = Screen.TOOLTIP_BASEZINDEX;

                Parent = GameService.Graphics.SpriteScreen;

                Input.Mouse.LeftMouseButtonPressed += OnMouseButtonPressed;
                Input.Mouse.RightMouseButtonPressed += OnMouseButtonPressed;
            }

            private Point GetPanelLocation()
            {
                var ownerLocation = _owner.AbsoluteBounds.Location;
                return ownerLocation + new Point(0, _owner.Height - 1);
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
                _highlightedIndex = GetItemIndexAt(RelativeMousePosition.Y);
                base.OnMouseMoved(e);
            }

            private int GetItemIndexAt(int y)
            {
                var sourceHeight = SourceItems.Length * ItemHeight;

                if (y < sourceHeight)
                    return y / ItemHeight;

                if (y < sourceHeight + SeparatorHeight)
                    return -1; // Separator

                var instrumentY = y - sourceHeight - SeparatorHeight;
                return SourceItems.Length + instrumentY / ItemHeight;
            }

            protected override void OnClick(MouseEventArgs e)
            {
                var index = GetItemIndexAt(RelativeMousePosition.Y);

                if (index >= 0 && index < SourceItems.Length)
                {
                    _owner.SelectedSource = SourceItems[index];
                }
                else if (index >= SourceItems.Length)
                {
                    var instrumentIndex = index - SourceItems.Length;
                    if (instrumentIndex < InstrumentItems.Length)
                    {
                        _owner.SelectedInstrument = InstrumentItems[instrumentIndex];
                    }
                }

                base.OnClick(e);
            }

            protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
            {
                // Background
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(Point.Zero, _size), Color.Black);

                var y = 0;

                // Source section
                for (var i = 0; i < SourceItems.Length; i++)
                {
                    var item = SourceItems[i];
                    var isSelected = item == _owner.SelectedSource;
                    var isHighlighted = _highlightedIndex == i;

                    DrawItem(spriteBatch, item, y, isSelected, isHighlighted);
                    y += ItemHeight;
                }

                // Separator
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel,
                    new Rectangle(PaddingX, y + SeparatorHeight / 2 - 1, _size.X - PaddingX * 2, 2),
                    MaestroTheme.MediumGray);
                y += SeparatorHeight;

                // Instrument section
                for (var i = 0; i < InstrumentItems.Length; i++)
                {
                    var item = InstrumentItems[i];
                    var isSelected = item == _owner.SelectedInstrument;
                    var isHighlighted = _highlightedIndex == SourceItems.Length + i;

                    DrawItem(spriteBatch, item, y, isSelected, isHighlighted);
                    y += ItemHeight;
                }
            }

            private void DrawItem(SpriteBatch spriteBatch, string text, int y, bool isSelected, bool isHighlighted)
            {
                if (isHighlighted)
                {
                    spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel,
                        new Rectangle(2, y + 2, _size.X - 4, ItemHeight - 4),
                        new Color(45, 37, 25, 255));
                }

                // Radio indicator
                var radioX = PaddingX;
                var radioY = y + ItemHeight / 2 - 4;
                var radioColor = isSelected ? MaestroTheme.AmberGold : MaestroTheme.MediumGray;

                // Draw radio circle outline
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel,
                    new Rectangle(radioX, radioY, 8, 8), radioColor);

                if (isSelected)
                {
                    // Draw filled inner circle
                    spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel,
                        new Rectangle(radioX + 2, radioY + 2, 4, 4), MaestroTheme.CreamWhite);
                }
                else
                {
                    // Draw hollow center
                    spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel,
                        new Rectangle(radioX + 2, radioY + 2, 4, 4), Color.Black);
                }

                // Text
                var textColor = isHighlighted ? ContentService.Colors.Chardonnay : Color.FromNonPremultiplied(239, 240, 239, 255);
                spriteBatch.DrawStringOnCtrl(this, text, Content.DefaultFont14,
                    new Rectangle(PaddingX + 14, y, _size.X - PaddingX - 14, ItemHeight), textColor);
            }

            protected override void DisposeControl()
            {
                if (_owner != null)
                {
                    _owner._panel = null;
                }

                Input.Mouse.LeftMouseButtonPressed -= OnMouseButtonPressed;
                Input.Mouse.RightMouseButtonPressed -= OnMouseButtonPressed;

                base.DisposeControl();
            }
        }

        private static readonly Texture2D TextureInputBox = Content.GetTexture("input-box");

        public event EventHandler FilterChanged;

        private FilterPanel _panel;
        private bool _hadPanel;

        private string _selectedSource = "All";
        public string SelectedSource
        {
            get => _selectedSource;
            set
            {
                if (_selectedSource != value)
                {
                    _selectedSource = value;
                    FilterChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private string _selectedInstrument = "All";
        public string SelectedInstrument
        {
            get => _selectedInstrument;
            set
            {
                if (_selectedInstrument != value)
                {
                    _selectedInstrument = value;
                    FilterChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool PanelOpen => _panel != null;

        private string DisplayText => $"{_selectedSource} / {_selectedInstrument}";

        public FilterButton()
        {
            Size = new Point(180, 27);
        }

        protected override void OnClick(MouseEventArgs e)
        {
            base.OnClick(e);

            if (_panel == null && !_hadPanel)
            {
                _panel = new FilterPanel(this);
            }
            else
            {
                _hadPanel = false;
            }
        }

        public void HidePanel()
        {
            _hadPanel = _mouseOver;
            _panel?.Dispose();
        }

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
        {
            // Draw dropdown background
            spriteBatch.DrawOnCtrl(this, TextureInputBox,
                new Rectangle(Point.Zero, _size).Subtract(new Rectangle(0, 0, 5, 0)),
                new Rectangle(0, 0, Math.Min(TextureInputBox.Width - 5, Width - 5), TextureInputBox.Height));

            // Draw right side
            spriteBatch.DrawOnCtrl(this, TextureInputBox,
                new Rectangle(_size.X - 5, 0, 5, _size.Y),
                new Rectangle(TextureInputBox.Width - 5, 0, 5, TextureInputBox.Height));

            // Draw simple dropdown arrow (triangle)
            var arrowColor = (Enabled && MouseOver) ? ContentService.Colors.Chardonnay : MaestroTheme.MutedCream;
            var arrowX = _size.X - 18;
            var arrowY = _size.Y / 2 - 2;

            // Draw a simple "v" shape arrow
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel,
                new Rectangle(arrowX, arrowY, 8, 2), arrowColor);
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel,
                new Rectangle(arrowX + 2, arrowY + 2, 4, 2), arrowColor);

            // Draw text
            spriteBatch.DrawStringOnCtrl(this, DisplayText, Content.DefaultFont14,
                new Rectangle(5, 0, _size.X - 25, _size.Y),
                Enabled ? Color.FromNonPremultiplied(239, 240, 239, 255) : StandardColors.DisabledText);
        }
    }
}
