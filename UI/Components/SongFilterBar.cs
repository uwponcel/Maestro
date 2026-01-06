using System;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;

namespace Maestro.UI.Components
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
            public const int DropdownWidth = 150;
        }

        public event EventHandler SearchChanged;
        public event EventHandler<ValueChangedEventArgs> FilterChanged;

        private readonly TextBox _searchBox;
        private readonly Dropdown _instrumentFilter;

        public string SearchText => _searchBox.Text?.Trim().ToLower() ?? string.Empty;
        public string SelectedInstrument => _instrumentFilter.SelectedItem;

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

            _instrumentFilter = new Dropdown
            {
                Parent = this,
                Location = new Point(width - Layout.DropdownWidth, Layout.ControlY),
                Width = Layout.DropdownWidth
            };
            _instrumentFilter.Items.Add("All Instruments");
            _instrumentFilter.Items.Add("Piano");
            _instrumentFilter.Items.Add("Harp");
            _instrumentFilter.Items.Add("Lute");
            _instrumentFilter.Items.Add("Bass");
            _instrumentFilter.SelectedItem = "All Instruments";
            _instrumentFilter.ValueChanged += (s, e) => FilterChanged?.Invoke(this, e);
        }

        protected override void DisposeControl()
        {
            _searchBox?.Dispose();
            _instrumentFilter?.Dispose();
            base.DisposeControl();
        }
    }
}
