using System;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maestro.UI.Controls
{
    public class FilterSection
    {
        public string[] Items { get; set; }
        public string DefaultValue { get; set; }
    }

    public class GenericFilterButton : Control
    {
        private class FilterPanel : Control
        {
            private const int ITEM_HEIGHT = 24;
            private const int SEPARATOR_HEIGHT = 8;
            private const int PADDING_X = 8;

            private readonly GenericFilterButton _owner;
            private int _highlightedIndex = -1;

            public FilterPanel(GenericFilterButton owner)
            {
                _owner = owner;

                var totalItems = _owner._section1.Items.Length + _owner._section2.Items.Length;
                var separatorCount = 1;

                if (_owner._section3 != null)
                {
                    totalItems += _owner._section3.Items.Length;
                    separatorCount = 2;
                }

                var height = totalItems * ITEM_HEIGHT + separatorCount * SEPARATOR_HEIGHT;

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
                var section1Height = _owner._section1.Items.Length * ITEM_HEIGHT;

                if (y < section1Height)
                    return y / ITEM_HEIGHT;

                if (y < section1Height + SEPARATOR_HEIGHT)
                    return -1; // Separator 1

                var section2Start = section1Height + SEPARATOR_HEIGHT;
                var section2Height = _owner._section2.Items.Length * ITEM_HEIGHT;

                if (y < section2Start + section2Height)
                {
                    var section2Y = y - section2Start;
                    return _owner._section1.Items.Length + section2Y / ITEM_HEIGHT;
                }

                if (_owner._section3 == null)
                    return -1;

                var section3Start = section2Start + section2Height + SEPARATOR_HEIGHT;

                if (y < section2Start + section2Height + SEPARATOR_HEIGHT)
                    return -1; // Separator 2

                var section3Y = y - section3Start;
                return _owner._section1.Items.Length + _owner._section2.Items.Length + section3Y / ITEM_HEIGHT;
            }

            protected override void OnClick(MouseEventArgs e)
            {
                var index = GetItemIndexAt(RelativeMousePosition.Y);

                if (index >= 0 && index < _owner._section1.Items.Length)
                {
                    _owner.SelectedValue1 = _owner._section1.Items[index];
                }
                else if (index >= _owner._section1.Items.Length && index < _owner._section1.Items.Length + _owner._section2.Items.Length)
                {
                    var section2Index = index - _owner._section1.Items.Length;
                    _owner.SelectedValue2 = _owner._section2.Items[section2Index];
                }
                else if (_owner._section3 != null && index >= _owner._section1.Items.Length + _owner._section2.Items.Length)
                {
                    var section3Index = index - _owner._section1.Items.Length - _owner._section2.Items.Length;
                    if (section3Index < _owner._section3.Items.Length)
                    {
                        _owner.SelectedValue3 = _owner._section3.Items[section3Index];
                    }
                }

                base.OnClick(e);
            }

            protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
            {
                // Background
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(Point.Zero, _size), Color.Black);

                var y = 0;

                // Section 1
                for (var i = 0; i < _owner._section1.Items.Length; i++)
                {
                    var item = _owner._section1.Items[i];
                    var isSelected = item == _owner.SelectedValue1;
                    var isHighlighted = _highlightedIndex == i;

                    DrawItem(spriteBatch, item, y, isSelected, isHighlighted);
                    y += ITEM_HEIGHT;
                }

                // Separator 1
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel,
                    new Rectangle(PADDING_X, y + SEPARATOR_HEIGHT / 2 - 1, _size.X - PADDING_X * 2, 2),
                    MaestroTheme.MediumGray);
                y += SEPARATOR_HEIGHT;

                // Section 2
                for (var i = 0; i < _owner._section2.Items.Length; i++)
                {
                    var item = _owner._section2.Items[i];
                    var isSelected = item == _owner.SelectedValue2;
                    var isHighlighted = _highlightedIndex == _owner._section1.Items.Length + i;

                    DrawItem(spriteBatch, item, y, isSelected, isHighlighted);
                    y += ITEM_HEIGHT;
                }

                // Section 3 (optional)
                if (_owner._section3 != null)
                {
                    // Separator 2
                    spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel,
                        new Rectangle(PADDING_X, y + SEPARATOR_HEIGHT / 2 - 1, _size.X - PADDING_X * 2, 2),
                        MaestroTheme.MediumGray);
                    y += SEPARATOR_HEIGHT;

                    for (var i = 0; i < _owner._section3.Items.Length; i++)
                    {
                        var item = _owner._section3.Items[i];
                        var isSelected = item == _owner.SelectedValue3;
                        var isHighlighted = _highlightedIndex == _owner._section1.Items.Length + _owner._section2.Items.Length + i;

                        DrawItem(spriteBatch, item, y, isSelected, isHighlighted);
                        y += ITEM_HEIGHT;
                    }
                }
            }

            private void DrawItem(SpriteBatch spriteBatch, string text, int y, bool isSelected, bool isHighlighted)
            {
                if (isHighlighted)
                {
                    spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel,
                        new Rectangle(2, y + 2, _size.X - 4, ITEM_HEIGHT - 4),
                        new Color(45, 37, 25, 255));
                }

                // Radio indicator
                var radioX = PADDING_X;
                var radioY = y + ITEM_HEIGHT / 2 - 4;
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
                    new Rectangle(PADDING_X + 14, y, _size.X - PADDING_X - 14, ITEM_HEIGHT), textColor);
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

        private readonly FilterSection _section1;
        private readonly FilterSection _section2;
        private readonly FilterSection _section3;

        private string _selectedValue1;
        public string SelectedValue1
        {
            get => _selectedValue1;
            set
            {
                if (_selectedValue1 != value)
                {
                    _selectedValue1 = value;
                    FilterChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private string _selectedValue2;
        public string SelectedValue2
        {
            get => _selectedValue2;
            set
            {
                if (_selectedValue2 != value)
                {
                    _selectedValue2 = value;
                    FilterChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private string _selectedValue3;
        public string SelectedValue3
        {
            get => _selectedValue3;
            set
            {
                if (_selectedValue3 != value)
                {
                    _selectedValue3 = value;
                    FilterChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool PanelOpen => _panel != null;

        private string DisplayText => _section3 != null
            ? $"{_selectedValue1} / {_selectedValue2} / {_selectedValue3}"
            : $"{_selectedValue1} / {_selectedValue2}";

        public GenericFilterButton(FilterSection section1, FilterSection section2, FilterSection section3 = null)
        {
            _section1 = section1;
            _section2 = section2;
            _section3 = section3;
            _selectedValue1 = section1.DefaultValue;
            _selectedValue2 = section2.DefaultValue;
            _selectedValue3 = section3?.DefaultValue;

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
