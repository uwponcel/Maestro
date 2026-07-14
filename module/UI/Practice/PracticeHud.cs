using System;
using Blish_HUD.Controls;
using Maestro.Models;
using Maestro.Services.Practice;
using Maestro.UI.Controls;
using Microsoft.Xna.Framework;

namespace Maestro.UI.Practice
{
    /// <summary>
    /// Top-toolbar HUD for Practice Mode. Hosts a speed dropdown, pause/restart/close
    /// icon buttons, and live score/combo labels. Reads and writes
    /// <see cref="PracticeSettings"/> directly so the player's choices persist
    /// between sessions.
    /// </summary>
    public class PracticeHud : Panel
    {
        private readonly PracticeSession _session;
        private readonly Dropdown _speedDropdown;
        private readonly IconButton _pauseButton;
        private readonly IconButton _restartButton;
        private readonly IconButton _closeButton;
        private readonly Label _scoreLabel;
        private readonly Label _comboLabel;

        public event Action PauseRequested;
        public event Action RestartRequested;
        public event Action CloseRequested;
        public event Action<float> SpeedChanged;

        public PracticeHud(PracticeSession session, PracticeSettings settings)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            HeightSizingMode = SizingMode.AutoSize;

            _speedDropdown = new Dropdown
            {
                Parent = this,
                Location = new Point(8, 8),
                Width = 80,
            };
            _speedDropdown.Items.Add("0.5x");
            _speedDropdown.Items.Add("0.75x");
            _speedDropdown.Items.Add("1.0x");
            _speedDropdown.SelectedItem = SpeedLabel(settings.LastUsedSpeed.Value);
            _speedDropdown.ValueChanged += (sender, e) =>
            {
                float speed = ParseSpeed(e.CurrentValue);
                settings.LastUsedSpeed.Value = speed;
                _session.Clock.Speed = speed;
                SpeedChanged?.Invoke(speed);
            };

            _pauseButton = new IconButton(MaestroIcons.Pause, MaestroTheme.IconGlyph)
            {
                Parent = this,
                BasicTooltipText = "Pause",
                Location = new Point(96, 8),
                Width = 40,
                Height = 22,
            };
            _pauseButton.Click += (sender, e) => PauseRequested?.Invoke();

            _restartButton = new IconButton(MaestroIcons.Refresh, MaestroTheme.IconGlyph)
            {
                Parent = this,
                BasicTooltipText = "Restart",
                Location = new Point(140, 8),
                Width = 40,
                Height = 22,
            };
            _restartButton.Click += (sender, e) => RestartRequested?.Invoke();

            _closeButton = new IconButton(MaestroIcons.Cancel, MaestroTheme.IconGlyph)
            {
                Parent = this,
                BasicTooltipText = "Close practice",
                Location = new Point(184, 8),
                Width = 40,
                Height = 22,
            };
            _closeButton.Click += (sender, e) => CloseRequested?.Invoke();

            _scoreLabel = new Label
            {
                Parent = this,
                Location = new Point(8, 38),
                Width = 200,
                Text = "SCORE 0",
            };

            _comboLabel = new Label
            {
                Parent = this,
                Location = new Point(220, 38),
                Width = 120,
                Text = "COMBO x0",
            };
        }

        /// <summary>
        /// Recompute the score and combo labels from the underlying session and mirror the
        /// clock state onto the pause button (play icon while paused, like the main player).
        /// Call once per frame from the owning window.
        /// </summary>
        public void RefreshStats()
        {
            int score = _session.PerfectCount * 100 + _session.GoodCount * 50;
            _scoreLabel.Text = $"SCORE {score}";
            _comboLabel.Text = $"COMBO x{_session.Combo}";

            var paused = _session.Clock.IsPaused;
            _pauseButton.IconTexture = paused ? MaestroIcons.Play : MaestroIcons.Pause;
            _pauseButton.BasicTooltipText = paused ? "Resume" : "Pause";
        }

        private static string SpeedLabel(float s)
        {
            if (s <= 0.5f) return "0.5x";
            if (s <= 0.75f) return "0.75x";
            return "1.0x";
        }

        private static float ParseSpeed(string label)
        {
            if (label == "0.5x") return 0.5f;
            if (label == "0.75x") return 0.75f;
            return 1.0f;
        }
    }
}
