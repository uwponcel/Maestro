using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;

namespace Maestro.UI.Components
{
    public class StatusBar : Panel
    {
        public static class Layout
        {
            public const int Height = 24;
        }

        private readonly Label _statusLabel;
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

            _statusLabel = new Label
            {
                Parent = this,
                Location = new Point(0, 0),
                Width = Width,
                Height = Height,
                Font = GameService.Content.DefaultFont12,
                TextColor = MaestroColors.LightGray,
                HorizontalAlignment = HorizontalAlignment.Left
            };
        }

        private void UpdateText()
        {
            _statusLabel.Text = _visibleCount == _totalCount
                ? $"  {_totalCount} songs"
                : $"  {_visibleCount} of {_totalCount} songs";
        }

        protected override void DisposeControl()
        {
            _statusLabel?.Dispose();
            base.DisposeControl();
        }
    }
}
