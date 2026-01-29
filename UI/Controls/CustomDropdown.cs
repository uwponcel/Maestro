using System;
using System.Collections.Generic;
using System.Linq;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;

namespace Maestro.UI.Controls
{
    public class CustomDropdown : Control
    {
        private class DropdownPanel : Control
        {
            private const int ITEM_HEIGHT = 24;
            private const int PADDING_X = 8;
            private const int MAX_VISIBLE_ITEMS = 8;

            private readonly CustomDropdown _owner;
            private int _highlightedIndex = -1;

            public DropdownPanel(CustomDropdown owner)
            {
                _owner = owner;

                var visibleItems = Math.Min(_owner._items.Count, MAX_VISIBLE_ITEMS);
                var height = visibleItems * ITEM_HEIGHT + 4;

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

                // Update tooltip only if text is truncated
                if (_highlightedIndex >= 0 && _highlightedIndex < _owner._items.Count)
                {
                    var item = _owner._items[_highlightedIndex];
                    var availableWidth = _size.X - PADDING_X * 2;
                    var truncatedText = TruncateText(item.DisplayText, Content.DefaultFont14, availableWidth);
                    BasicTooltipText = truncatedText != item.DisplayText ? item.DisplayText : null;
                }
                else
                {
                    BasicTooltipText = null;
                }

                base.OnMouseMoved(e);
            }

            private int GetItemIndexAt(int y)
            {
                var adjustedY = y - 2;
                if (adjustedY < 0) return -1;

                var index = adjustedY / ITEM_HEIGHT;
                if (index >= _owner._items.Count) return -1;
                return index;
            }

            protected override void OnClick(MouseEventArgs e)
            {
                var index = GetItemIndexAt(RelativeMousePosition.Y);
                if (index >= 0 && index < _owner._items.Count)
                {
                    _owner.SelectedIndex = index;
                    Dispose();
                }
                base.OnClick(e);
            }

            protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
            {
                // Background
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(Point.Zero, _size), Color.Black);

                var y = 2;
                var visibleItems = Math.Min(_owner._items.Count, MAX_VISIBLE_ITEMS);

                for (var i = 0; i < visibleItems; i++)
                {
                    var item = _owner._items[i];
                    var isSelected = i == _owner.SelectedIndex;
                    var isHighlighted = _highlightedIndex == i;

                    DrawItem(spriteBatch, item, y, isSelected, isHighlighted);
                    y += ITEM_HEIGHT;
                }
            }

            private void DrawItem(SpriteBatch spriteBatch, DropdownItem item, int y, bool isSelected, bool isHighlighted)
            {
                // Full-width highlight
                if (isHighlighted)
                {
                    spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel,
                        new Rectangle(2, y, _size.X - 4, ITEM_HEIGHT),
                        new Color(45, 37, 25, 255));
                }

                // Text with padding
                var textColor = isHighlighted ? ContentService.Colors.Chardonnay :
                    (isSelected ? MaestroTheme.AmberGold : Color.FromNonPremultiplied(239, 240, 239, 255));

                var availableWidth = _size.X - PADDING_X * 2;
                var displayText = TruncateText(item.DisplayText, Content.DefaultFont14, availableWidth);

                spriteBatch.DrawStringOnCtrl(this, displayText, Content.DefaultFont14,
                    new Rectangle(PADDING_X, y, availableWidth, ITEM_HEIGHT),
                    textColor, false, false, 1, HorizontalAlignment.Left, VerticalAlignment.Middle);
            }

            private static string TruncateText(string text, BitmapFont font, int maxWidth)
            {
                if (string.IsNullOrEmpty(text)) return text;

                var textWidth = (int)font.MeasureString(text).Width;
                if (textWidth <= maxWidth) return text;

                var ellipsis = "...";
                var ellipsisWidth = (int)font.MeasureString(ellipsis).Width;
                var targetWidth = maxWidth - ellipsisWidth;

                for (var i = text.Length - 1; i > 0; i--)
                {
                    var truncated = text.Substring(0, i);
                    var truncatedWidth = (int)font.MeasureString(truncated).Width;
                    if (truncatedWidth <= targetWidth)
                    {
                        return truncated + ellipsis;
                    }
                }

                return ellipsis;
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

        public class DropdownItem
        {
            public string DisplayText { get; set; }
            public string TooltipText { get; set; }
            public object Value { get; set; }

            public DropdownItem(string displayText, string tooltipText = null, object value = null)
            {
                DisplayText = displayText;
                TooltipText = tooltipText ?? displayText;
                Value = value;
            }
        }

        private static readonly Texture2D TextureInputBox = Content.GetTexture("input-box");

        public event EventHandler<ValueChangedEventArgs> ValueChanged;

        private DropdownPanel _panel;
        private bool _hadPanel;
        private readonly List<DropdownItem> _items = new List<DropdownItem>();

        private int _selectedIndex = -1;
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex != value && value >= -1 && value < _items.Count)
                {
                    var oldValue = _selectedIndex >= 0 ? _items[_selectedIndex].DisplayText : null;
                    _selectedIndex = value;
                    var newValue = _selectedIndex >= 0 ? _items[_selectedIndex].DisplayText : null;

                    ValueChanged?.Invoke(this, new ValueChangedEventArgs(oldValue, newValue));
                }
            }
        }

        public DropdownItem SelectedItem => _selectedIndex >= 0 && _selectedIndex < _items.Count ? _items[_selectedIndex] : null;
        public string SelectedValue => SelectedItem?.DisplayText;

        public bool PanelOpen => _panel != null;

        public CustomDropdown()
        {
            Size = new Point(200, 27);
        }

        public void AddItem(string displayText, string tooltipText = null, object value = null)
        {
            _items.Add(new DropdownItem(displayText, tooltipText, value));
            if (_selectedIndex == -1 && _items.Count == 1)
            {
                SelectedIndex = 0;
            }
        }

        public void AddItem(DropdownItem item)
        {
            _items.Add(item);
            if (_selectedIndex == -1 && _items.Count == 1)
            {
                SelectedIndex = 0;
            }
        }

        public void ClearItems()
        {
            _items.Clear();
            _selectedIndex = -1;
        }

        public int ItemCount => _items.Count;

        protected override void OnClick(MouseEventArgs e)
        {
            base.OnClick(e);

            if (!Enabled) return;

            if (_panel == null && !_hadPanel)
            {
                _panel = new DropdownPanel(this);
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

            // Draw dropdown arrow
            var arrowColor = (Enabled && MouseOver) ? ContentService.Colors.Chardonnay : MaestroTheme.MutedCream;
            var arrowX = _size.X - 18;
            var arrowY = _size.Y / 2 - 2;

            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel,
                new Rectangle(arrowX, arrowY, 8, 2), arrowColor);
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel,
                new Rectangle(arrowX + 2, arrowY + 2, 4, 2), arrowColor);

            // Draw text with padding and truncation
            var fullText = SelectedItem?.DisplayText ?? "";
            var textColor = Enabled ? Color.FromNonPremultiplied(239, 240, 239, 255) : StandardColors.DisabledText;
            var availableWidth = _size.X - 30;
            var displayText = TruncateText(fullText, Content.DefaultFont14, availableWidth);

            // Show tooltip if text was truncated
            BasicTooltipText = displayText != fullText ? fullText : null;

            spriteBatch.DrawStringOnCtrl(this, displayText, Content.DefaultFont14,
                new Rectangle(8, 0, availableWidth, _size.Y),
                textColor, false, false, 1, HorizontalAlignment.Left, VerticalAlignment.Middle);
        }

        private static string TruncateText(string text, BitmapFont font, int maxWidth)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var textWidth = (int)font.MeasureString(text).Width;
            if (textWidth <= maxWidth) return text;

            var ellipsis = "...";
            var ellipsisWidth = (int)font.MeasureString(ellipsis).Width;
            var targetWidth = maxWidth - ellipsisWidth;

            for (var i = text.Length - 1; i > 0; i--)
            {
                var truncated = text.Substring(0, i);
                var truncatedWidth = (int)font.MeasureString(truncated).Width;
                if (truncatedWidth <= targetWidth)
                {
                    return truncated + ellipsis;
                }
            }

            return ellipsis;
        }
    }
}