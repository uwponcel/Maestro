using System;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Maestro.Models;
using Maestro.Services.Playback;
using Microsoft.Xna.Framework;

namespace Maestro.UI.Main
{
    public class NowPlayingPanel : Panel
    {
        public static class Layout
        {
            public const int Height = 105;

            public const int ButtonY = 18;
            public const int ButtonWidth = 40;
            public const int PauseButtonX = 8;
            public const int StopButtonX = 52;

            public const int LabelX = 100;
            public const int LabelYPlaying = 8;
            public const int LabelYCentered = 21;
            public const int ProgressLabelY = 28;
            public const int InstrumentLabelY = 45;
            public const int LabelWidth = 300;

            public const int SpeedLabelX = 8;
            public const int SpeedLabelY = 62;
            public const int SpeedSliderX = 60;
            public const int SpeedSliderY = 65;
            public const int SpeedSliderWidth = 180;
            public const int SpeedValueX = 248;
        }

        public event EventHandler StopRequested;
        public event EventHandler<Song> PlayPendingRequested;

        private readonly SongPlayer _songPlayer;
        private readonly StandardButton _pauseButton;
        private readonly StandardButton _stopButton;
        private readonly Label _nowPlayingLabel;
        private readonly Label _progressLabel;
        private readonly Label _instrumentLabel;
        private readonly Label _speedLabel;
        private readonly TrackBar _speedSlider;
        private readonly Label _speedValueLabel;

        private bool _isPlayingFromQueue;
        private Song _pendingSong;
        private InstrumentType? _currentInstrument;

        public NowPlayingPanel(SongPlayer songPlayer, int width)
        {
            _songPlayer = songPlayer;

            Size = new Point(width, Layout.Height);
            BackgroundColor = MaestroTheme.WithAlpha(MaestroTheme.SlateGray, 150);
            ShowBorder = true;

            _pauseButton = CreatePauseButton();
            _stopButton = CreateStopButton();
            _nowPlayingLabel = CreateNowPlayingLabel();
            _progressLabel = CreateProgressLabel();
            _instrumentLabel = CreateInstrumentLabel();
            _speedLabel = CreateSpeedLabel();
            _speedSlider = CreateSpeedSlider();
            _speedValueLabel = CreateSpeedValueLabel();

            SubscribeToEvents();
        }

        public void SetQueuePlaybackMode(bool isPlaying)
        {
            _isPlayingFromQueue = isPlaying;
            UpdatePlaybackState();
        }

        public void SetPendingSong(Song song)
        {
            _pendingSong = song;
            ShowPendingState();
        }

        public void ClearPendingSong()
        {
            _pendingSong = null;
            UpdatePlaybackState();
        }

        public void SetCurrentInstrument(InstrumentType? instrument)
        {
            _currentInstrument = instrument;
            _instrumentLabel.Text = instrument?.ToString() ?? "";
        }

        public void UpdatePlaybackState()
        {
            if (_songPlayer.IsPlaying)
            {
                UpdatePlayingState();
            }
            else
            {
                UpdateStoppedState();
            }
        }

        public override void UpdateContainer(GameTime gameTime)
        {
            base.UpdateContainer(gameTime);

            if (_songPlayer.IsPlaying && !_songPlayer.IsPaused)
            {
                UpdateLiveProgress();
            }
        }

        protected override void DisposeControl()
        {
            UnsubscribeFromEvents();
            DisposeControls();

            base.DisposeControl();
        }

        private StandardButton CreatePauseButton()
        {
            var button = new StandardButton
            {
                Parent = this,
                Text = "||",
                Location = new Point(Layout.PauseButtonX, Layout.ButtonY),
                Width = Layout.ButtonWidth,
                Enabled = false
            };
            button.Click += OnPauseClicked;
            return button;
        }

        private StandardButton CreateStopButton()
        {
            var button = new StandardButton
            {
                Parent = this,
                Text = "X",
                Location = new Point(Layout.StopButtonX, Layout.ButtonY),
                Width = Layout.ButtonWidth,
                Enabled = false
            };
            button.Click += OnStopClicked;
            return button;
        }

        private Label CreateNowPlayingLabel()
        {
            return new Label
            {
                Parent = this,
                Text = "No song playing",
                Location = new Point(Layout.LabelX, Layout.LabelYCentered),
                Width = Layout.LabelWidth,
                Font = GameService.Content.DefaultFont14,
                TextColor = MaestroTheme.MutedCream
            };
        }

        private Label CreateProgressLabel()
        {
            return new Label
            {
                Parent = this,
                Text = "",
                Location = new Point(Layout.LabelX, Layout.ProgressLabelY),
                Width = Layout.LabelWidth,
                Font = GameService.Content.DefaultFont12,
                TextColor = MaestroTheme.MutedCream
            };
        }

        private Label CreateInstrumentLabel()
        {
            return new Label
            {
                Parent = this,
                Text = "",
                Location = new Point(Layout.LabelX, Layout.InstrumentLabelY),
                Width = Layout.LabelWidth,
                Font = GameService.Content.DefaultFont12,
                TextColor = MaestroTheme.MutedCream
            };
        }

        private Label CreateSpeedLabel()
        {
            return new Label
            {
                Parent = this,
                Text = "Speed:",
                Location = new Point(Layout.SpeedLabelX, Layout.SpeedLabelY),
                AutoSizeWidth = true,
                Font = GameService.Content.DefaultFont12,
                TextColor = MaestroTheme.MutedCream
            };
        }

        private TrackBar CreateSpeedSlider()
        {
            var slider = new TrackBar
            {
                Parent = this,
                Location = new Point(Layout.SpeedSliderX, Layout.SpeedSliderY),
                Width = Layout.SpeedSliderWidth,
                MinValue = 1,
                MaxValue = 20,
                Value = 10,
                SmallStep = true
            };
            slider.ValueChanged += OnSpeedSliderChanged;
            return slider;
        }

        private Label CreateSpeedValueLabel()
        {
            return new Label
            {
                Parent = this,
                Text = "1.0x",
                Location = new Point(Layout.SpeedValueX, Layout.SpeedLabelY),
                AutoSizeWidth = true,
                Font = GameService.Content.DefaultFont12,
                TextColor = MaestroTheme.CreamWhite
            };
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
            if (_pendingSong != null)
            {
                var song = _pendingSong;
                _pendingSong = null;
                PlayPendingRequested?.Invoke(this, song);
                return;
            }

            // If we just came from search bar, don't toggle
            // Song will auto-resume when playback loop detects focus changed
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
            StopRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnSpeedSliderChanged(object sender, ValueEventArgs<float> e)
        {
            var speed = e.Value / 10f;
            _songPlayer.PlaybackSpeed = speed;
            _speedValueLabel.Text = $"{speed:F1}x";
        }

        private void UpdatePlayingState()
        {
            var song = _songPlayer.CurrentSong;
            _nowPlayingLabel.Text = song?.DisplayName ?? "Unknown";
            _nowPlayingLabel.Location = new Point(Layout.LabelX, Layout.LabelYPlaying);
            _instrumentLabel.Text = song?.Instrument.ToString() ?? "";

            if (_songPlayer.IsPaused)
            {
                UpdatePausedState();
            }
            else
            {
                UpdateActivePlayingState(song);
            }

            _pauseButton.Enabled = true;
            _stopButton.Enabled = true;
        }

        private void UpdatePausedState()
        {
            _pauseButton.Text = ">";
            _nowPlayingLabel.TextColor = MaestroTheme.CreamWhite;
            _progressLabel.Text = "Paused" + GetQueueSuffix();
            _progressLabel.TextColor = MaestroTheme.Paused;
        }

        private void UpdateActivePlayingState(Song song)
        {
            _pauseButton.Text = "||";
            _nowPlayingLabel.TextColor = MaestroTheme.CreamWhite;

            if (_songPlayer.IsAdjustingOctave)
            {
                _progressLabel.Text = "Adjusting..." + GetQueueSuffix();
                _progressLabel.TextColor = MaestroTheme.Paused;
            }
            else
            {
                UpdateProgressText(song);
                _progressLabel.TextColor = MaestroTheme.MutedCream;
            }
        }

        private void UpdateProgressText(Song song)
        {
            var isComplete = song != null && _songPlayer.CurrentCommandIndex >= song.Commands.Count;
            if (isComplete)
            {
                _progressLabel.Text = "Done!" + GetQueueSuffix();
            }
            else
            {
                var progress = CalculateProgress(song);
                _progressLabel.Text = $"Playing... {progress:F0}%" + GetQueueSuffix();
            }
        }

        private void UpdateStoppedState()
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

        private void ShowPendingState()
        {
            _nowPlayingLabel.Text = _pendingSong?.DisplayName ?? "Unknown";
            _nowPlayingLabel.Location = new Point(Layout.LabelX, Layout.LabelYPlaying);
            _nowPlayingLabel.TextColor = MaestroTheme.CreamWhite;
            _instrumentLabel.Text = _pendingSong?.Instrument.ToString() ?? "";

            _progressLabel.Text = "Ready" + GetQueueSuffix();
            _progressLabel.TextColor = MaestroTheme.Paused;

            _pauseButton.Text = ">";
            _pauseButton.Enabled = true;
            _stopButton.Enabled = false;
        }

        private void UpdateLiveProgress()
        {
            if (_songPlayer.IsWaitingForInput)
            {
                _pauseButton.Text = ">";
                _progressLabel.Text = "Paused" + GetQueueSuffix();
                _progressLabel.TextColor = MaestroTheme.Paused;
            }
            else if (_songPlayer.IsAdjustingOctave)
            {
                _progressLabel.Text = "Adjusting..." + GetQueueSuffix();
                _progressLabel.TextColor = MaestroTheme.Paused;
            }
            else
            {
                _pauseButton.Text = "||";
                var song = _songPlayer.CurrentSong;
                UpdateProgressText(song);
                _progressLabel.TextColor = MaestroTheme.MutedCream;
            }
        }

        private string GetQueueSuffix() => _isPlayingFromQueue ? " - Queue" : "";

        private float CalculateProgress(Song song)
        {
            return song != null && song.Commands.Count > 0
                ? (float)_songPlayer.CurrentCommandIndex / song.Commands.Count * 100
                : 0;
        }

        private static void ClearTextInputFocus()
        {
            if (FocusedControl is TextInputBase textInput)
            {
                textInput.Focused = false;
            }
        }

        private void UnsubscribeFromEvents()
        {
            _songPlayer.OnStarted -= OnPlaybackStateChanged;
            _songPlayer.OnPaused -= OnPlaybackStateChanged;
            _songPlayer.OnResumed -= OnPlaybackStateChanged;
            _songPlayer.OnStopped -= OnPlaybackStateChanged;
            _songPlayer.OnCompleted -= OnPlaybackStateChanged;
        }

        private void DisposeControls()
        {
            _pauseButton?.Dispose();
            _stopButton?.Dispose();
            _nowPlayingLabel?.Dispose();
            _progressLabel?.Dispose();
            _instrumentLabel?.Dispose();
            _speedLabel?.Dispose();
            _speedSlider?.Dispose();
            _speedValueLabel?.Dispose();
        }
    }
}
