using System;
using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;

namespace Maestro.UI.Components
{
    public class StatusBar : Panel
    {
        public static class Layout
        {
            public static int Height => MaestroTheme.ActionButtonHeight;
        }

        public event EventHandler ImportClicked;

        private readonly Label _statusLabel;
        private readonly StandardButton _importButton;
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
                Width = width - MaestroTheme.ActionButtonWidth - 10,
                Height = Height,
                Font = GameService.Content.DefaultFont12,
                TextColor = MaestroTheme.LightGray,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            _importButton = new StandardButton
            {
                Parent = this,
                Text = "Import",
                Location = new Point(width - MaestroTheme.ActionButtonWidth, 0),
                Size = new Point(MaestroTheme.ActionButtonWidth, MaestroTheme.ActionButtonHeight)
            };
            _importButton.Click += (s, e) => ImportClicked?.Invoke(this, EventArgs.Empty);
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
            _importButton?.Dispose();
            base.DisposeControl();
        }
    }
}
