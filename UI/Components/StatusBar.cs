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
            public const int QueueButtonWidth = 36;
            public const int ButtonSpacing = 5;
        }

        public event EventHandler ImportClicked;
        public event EventHandler QueueToggleClicked;

        private readonly Label _statusLabel;
        private readonly StandardButton _importButton;
        private readonly StandardButton _queueButton;
        private int _visibleCount;
        private int _totalCount;
        private int _queueCount;

        public int VisibleCount
        {
            get => _visibleCount;
            set
            {
                _visibleCount = value;
                UpdateStatusText();
            }
        }

        public int TotalCount
        {
            get => _totalCount;
            set
            {
                _totalCount = value;
                UpdateStatusText();
            }
        }

        public int QueueCount
        {
            get => _queueCount;
            set
            {
                _queueCount = value;
                UpdateQueueButtonText();
            }
        }

        public StatusBar(int width)
        {
            Size = new Point(width, Layout.Height);
            BackgroundColor = Color.Transparent;

            var buttonsWidth = MaestroTheme.ActionButtonWidth + Layout.ButtonSpacing + Layout.QueueButtonWidth;

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

            _importButton = new StandardButton
            {
                Parent = this,
                Text = "Import",
                Location = new Point(width - buttonsWidth, 0),
                Size = new Point(MaestroTheme.ActionButtonWidth, MaestroTheme.ActionButtonHeight)
            };
            _importButton.Click += (s, e) => ImportClicked?.Invoke(this, EventArgs.Empty);

            _queueButton = new StandardButton
            {
                Parent = this,
                Text = "Q",
                Location = new Point(width - Layout.QueueButtonWidth, 0),
                Size = new Point(Layout.QueueButtonWidth, MaestroTheme.ActionButtonHeight),
                BasicTooltipText = "Toggle Queue"
            };
            _queueButton.Click += (s, e) => QueueToggleClicked?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateStatusText()
        {
            _statusLabel.Text = _visibleCount == _totalCount
                ? $"  {_totalCount} songs"
                : $"  {_visibleCount} of {_totalCount} songs";
        }

        private void UpdateQueueButtonText()
        {
            _queueButton.Text = _queueCount > 0 ? $"Q({_queueCount})" : "Q";
        }

        protected override void DisposeControl()
        {
            _statusLabel?.Dispose();
            _importButton?.Dispose();
            _queueButton?.Dispose();
            base.DisposeControl();
        }
    }
}
