using System;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Maestro.Services.Playback;
using Microsoft.Xna.Framework;

namespace Maestro.UI.Main
{
    public class NowPlayingPanel : Panel
    {
        public static class Layout
        {
            public const int Height = 95;

            // Button positioning (centered: (Height - ButtonHeight) / 2, ButtonHeight ≈ 26)
            public const int ButtonY = 18;
            public const int ButtonWidth = 40;
            public const int PauseButtonX = 8;
            public const int StopButtonX = 52;

            // Label positioning
            public const int LabelX = 100;
            public const int LabelYPlaying = 8;
            public const int LabelYCentered = 21;
            public const int ProgressLabelY = 28;
            public const int LabelWidth = 300;

            // Speed slider positioning
            public const int SpeedLabelX = 8;
            public const int SpeedLabelY = 52;
            public const int SpeedSliderX = 60;
            public const int SpeedSliderY = 55;
            public const int SpeedSliderWidth = 180;
            public const int SpeedValueX = 248;
        }

        private readonly SongPlayer _songPlayer;
        private readonly StandardButton _pauseButton;
        private readonly StandardButton _stopButton;
        private readonly Label _nowPlayingLabel;
        private readonly Label _progressLabel;
        private readonly Label _speedLabel;
        private readonly TrackBar _speedSlider;
        private readonly Label _speedValueLabel;

        public NowPlayingPanel(SongPlayer songPlayer, int width)
        {
            _songPlayer = songPlayer;

            Size = new Point(width, Layout.Height);
            BackgroundColor = MaestroTheme.WithAlpha(MaestroTheme.SlateGray, 150);
            ShowBorder = true;

            _pauseButton = new StandardButton
            {
                Parent = this,
                Text = "||",
                Location = new Point(Layout.PauseButtonX, Layout.ButtonY),
                Width = Layout.ButtonWidth,
                Enabled = false
            };
            _pauseButton.Click += OnPauseClicked;

            _stopButton = new StandardButton
            {
                Parent = this,
                Text = "X",
                Location = new Point(Layout.StopButtonX, Layout.ButtonY),
                Width = Layout.ButtonWidth,
                Enabled = false
            };
            _stopButton.Click += OnStopClicked;

            _nowPlayingLabel = new Label
            {
                Parent = this,
                Text = "No song playing",
                Location = new Point(Layout.LabelX, Layout.LabelYCentered),
                Width = Layout.LabelWidth,
                Font = GameService.Content.DefaultFont14,
                TextColor = MaestroTheme.MutedCream
            };

            _progressLabel = new Label
            {
                Parent = this,
                Text = "",
                Location = new Point(Layout.LabelX, Layout.ProgressLabelY),
                Width = Layout.LabelWidth,
                Font = GameService.Content.DefaultFont12,
                TextColor = MaestroTheme.MutedCream
            };

            _speedLabel = new Label
            {
                Parent = this,
                Text = "Speed:",
                Location = new Point(Layout.SpeedLabelX, Layout.SpeedLabelY),
                AutoSizeWidth = true,
                Font = GameService.Content.DefaultFont12,
                TextColor = MaestroTheme.MutedCream
            };

            _speedSlider = new TrackBar
            {
                Parent = this,
                Location = new Point(Layout.SpeedSliderX, Layout.SpeedSliderY),
                Width = Layout.SpeedSliderWidth,
                MinValue = 1,
                MaxValue = 20,
                Value = 10,
                SmallStep = true
            };
            _speedSlider.ValueChanged += OnSpeedSliderChanged;

            _speedValueLabel = new Label
            {
                Parent = this,
                Text = "1.0x",
                Location = new Point(Layout.SpeedValueX, Layout.SpeedLabelY),
                AutoSizeWidth = true,
                Font = GameService.Content.DefaultFont12,
                TextColor = MaestroTheme.CreamWhite
            };

            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            _songPlayer.OnStarted += OnPlaybackStateChanged;
            _songPlayer.OnPaused += OnPlaybackStateChanged;
            _songPlayer.OnResumed += OnPlaybackStateChanged;
            _songPlayer.OnStopped += OnPlaybackStateChanged;
            _songPlayer.OnCompleted += OnPlaybackStateChanged;
        }

        private void OnPlaybackStateChanged(object sender, EventArgs e)
        {
            UpdatePlaybackState();
        }

        private void OnPauseClicked(object sender, MouseEventArgs e)
        {
            if (SongFilterBar.WasJustUnfocused)
            {
                SongFilterBar.WasJustUnfocused = false;
                return;
            }

            _songPlayer.TogglePause();
        }

        private void OnStopClicked(object sender, MouseEventArgs e)
        {
            ClearTextInputFocus();
            _songPlayer.Stop();
        }

        private static void ClearTextInputFocus()
        {
            if (Control.FocusedControl is TextInputBase textInput)
            {
                textInput.Focused = false;
            }
        }

        private void OnSpeedSliderChanged(object sender, ValueEventArgs<float> e)
        {
            var speed = e.Value / 10f;
            _songPlayer.PlaybackSpeed = speed;
            _speedValueLabel.Text = $"{speed:F1}x";
        }

        public void UpdatePlaybackState()
        {
            if (_songPlayer.IsPlaying)
            {
                var song = _songPlayer.CurrentSong;
                _nowPlayingLabel.Text = song?.DisplayName ?? "Unknown";
                _nowPlayingLabel.Location = new Point(Layout.LabelX, Layout.LabelYPlaying);

                if (_songPlayer.IsPaused)
                {
                    _pauseButton.Text = ">";
                    _nowPlayingLabel.TextColor = MaestroTheme.CreamWhite;
                    _progressLabel.Text = "Paused";
                    _progressLabel.TextColor = MaestroTheme.Paused;
                }
                else
                {
                    _pauseButton.Text = "||";
                    _nowPlayingLabel.TextColor = MaestroTheme.CreamWhite;
                    if (_songPlayer.IsAdjustingOctave)
                    {
                        _progressLabel.Text = "Adjusting...";
                        _progressLabel.TextColor = MaestroTheme.Paused;
                    }
                    else
                    {
                        var isComplete = song != null && _songPlayer.CurrentCommandIndex >= song.Commands.Count;
                        if (isComplete)
                        {
                            _progressLabel.Text = "Done!";
                        }
                        else
                        {
                            var progress = song != null && song.Commands.Count > 0
                                ? (float)_songPlayer.CurrentCommandIndex / song.Commands.Count * 100
                                : 0;
                            _progressLabel.Text = $"Playing... {progress:F0}%";
                        }
                        _progressLabel.TextColor = MaestroTheme.MutedCream;
                    }
                }

                _pauseButton.Enabled = true;
                _stopButton.Enabled = true;
            }
            else
            {
                var song = _songPlayer.CurrentSong;
                var isCompleted = song != null && _songPlayer.CurrentCommandIndex >= song.Commands.Count;

                if (isCompleted)
                {
                    _nowPlayingLabel.Text = song.DisplayName;
                    _nowPlayingLabel.Location = new Point(Layout.LabelX, Layout.LabelYPlaying);
                    _nowPlayingLabel.TextColor = MaestroTheme.CreamWhite;
                    _progressLabel.Text = "Done!";
                    _progressLabel.TextColor = MaestroTheme.MutedCream;
                }
                else
                {
                    _nowPlayingLabel.Text = "No song playing";
                    _nowPlayingLabel.Location = new Point(Layout.LabelX, Layout.LabelYCentered);
                    _nowPlayingLabel.TextColor = MaestroTheme.MutedCream;
                    _progressLabel.Text = "";
                }

                _pauseButton.Text = "||";
                _pauseButton.Enabled = false;
                _stopButton.Enabled = false;
            }
        }

        public override void UpdateContainer(GameTime gameTime)
        {
            base.UpdateContainer(gameTime);

            if (_songPlayer.IsPlaying && !_songPlayer.IsPaused)
            {
                if (_songPlayer.IsWaitingForInput)
                {
                    _pauseButton.Text = ">";
                    _progressLabel.Text = "Paused";
                    _progressLabel.TextColor = MaestroTheme.Paused;
                }
                else if (_songPlayer.IsAdjustingOctave)
                {
                    _progressLabel.Text = "Adjusting...";
                    _progressLabel.TextColor = MaestroTheme.Paused;
                }
                else
                {
                    _pauseButton.Text = "||";
                    var song = _songPlayer.CurrentSong;
                    var isComplete = song != null && _songPlayer.CurrentCommandIndex >= song.Commands.Count;
                    if (isComplete)
                    {
                        _progressLabel.Text = "Done!";
                    }
                    else
                    {
                        var progress = song != null && song.Commands.Count > 0
                            ? (float)_songPlayer.CurrentCommandIndex / song.Commands.Count * 100
                            : 0;
                        _progressLabel.Text = $"Playing... {progress:F0}%";
                    }
                    _progressLabel.TextColor = MaestroTheme.MutedCream;
                }
            }
        }

        protected override void DisposeControl()
        {
            _songPlayer.OnStarted -= OnPlaybackStateChanged;
            _songPlayer.OnPaused -= OnPlaybackStateChanged;
            _songPlayer.OnResumed -= OnPlaybackStateChanged;
            _songPlayer.OnStopped -= OnPlaybackStateChanged;
            _songPlayer.OnCompleted -= OnPlaybackStateChanged;

            _pauseButton?.Dispose();
            _stopButton?.Dispose();
            _nowPlayingLabel?.Dispose();
            _progressLabel?.Dispose();
            _speedLabel?.Dispose();
            _speedSlider?.Dispose();
            _speedValueLabel?.Dispose();

            base.DisposeControl();
        }
    }
}
