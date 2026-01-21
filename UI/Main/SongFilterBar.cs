using System;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;

namespace Maestro.UI.Main
{
    public class SongFilterBar : Panel
    {
        public static class Layout
        {
            public const int Height = 36;

            // Control positioning
            public const int ControlY = 4;
            public const int SearchBoxX = 0;
            public const int SearchBoxWidth = 180;
            public const int FilterButtonWidth = 180;
        }

        public event EventHandler SearchChanged;
        public event EventHandler FilterChanged;

        private readonly TextBox _searchBox;
        private readonly FilterButton _filterButton;

        public string SearchText => _searchBox.Text?.Trim().ToLower() ?? string.Empty;
        public string SelectedSource => _filterButton.SelectedSource;
        public string SelectedInstrument => _filterButton.SelectedInstrument;

        public static bool IsTextInputFocused { get; private set; }
        public static bool WasJustUnfocused { get; set; }

        public SongFilterBar(int width)
        {
            Size = new Point(width, Layout.Height);
            BackgroundColor = Color.Transparent;

            _searchBox = new TextBox
            {
                Parent = this,
                Location = new Point(Layout.SearchBoxX, Layout.ControlY),
                Width = Layout.SearchBoxWidth,
                PlaceholderText = "Search songs..."
            };
            _searchBox.TextChanged += (s, e) => SearchChanged?.Invoke(this, EventArgs.Empty);
            _searchBox.InputFocusChanged += (s, e) =>
            {
                if (!e.Value && IsTextInputFocused)
                    WasJustUnfocused = true;
                IsTextInputFocused = e.Value;
            };

            _filterButton = new FilterButton
            {
                Parent = this,
                Location = new Point(width - Layout.FilterButtonWidth, Layout.ControlY),
                Width = Layout.FilterButtonWidth
            };
            _filterButton.FilterChanged += (s, e) => FilterChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void DisposeControl()
        {
            _searchBox?.Dispose();
            _filterButton?.Dispose();
            base.DisposeControl();
        }
    }
}
