using System;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Maestro.Models;
using Maestro.Services.Playback;
using Maestro.UI.Controls;
using Microsoft.Xna.Framework;

namespace Maestro.UI.Main
{
    public class NowPlayingPanel : Panel
    {
        public static class Layout
        {
            public const int Height = 130;

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

            public const int SeekElapsedX = 8;
            public const int SeekLabelY = 87;
            public const int SeekSliderX = 42;
            public const int SeekSliderY = 90;
            public const int SeekSliderWidth = 200;
            public const int SeekTotalX = 248;

            public const int QueueButtonWidth = 30;
            public const int QueueButtonRightPadding = 8;
            public const int QueueButtonY = 8;
        }

        public event EventHandler StopRequested;
        public event EventHandler<Song> PlayPendingRequested;
        public event EventHandler QueueToggleClicked;

        private readonly SongPlayer _songPlayer;
        private readonly StandardButton _pauseButton;
        private readonly StandardButton _stopButton;
        private readonly MarqueeLabel _nowPlayingLabel;
        private readonly Label _progressLabel;
        private readonly Label _instrumentLabel;
        private readonly Label _speedLabel;
        private readonly TrackBar _speedSlider;
        private readonly Label _speedValueLabel;
        private readonly Label _elapsedLabel;
        private readonly TrackBar _seekSlider;
        private readonly Label _totalLabel;
        private readonly StandardButton _queueButton;

        private bool _isPlayingFromQueue;
        private Song _pendingSong;
        private InstrumentType? _currentInstrument;
        private bool _wasPlayingBeforeSeek;

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
            _elapsedLabel = CreateElapsedLabel();
            _seekSlider = CreateSeekSlider();
            _totalLabel = CreateTotalLabel();
            _queueButton = CreateQueueButton(width);

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

            if (_songPlayer.IsPlaying && !_seekSlider.Dragging)
            {
                UpdateSeekSlider();
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

        private MarqueeLabel CreateNowPlayingLabel()
        {
            var availableWidth = Width - Layout.LabelX - Layout.QueueButtonWidth - Layout.QueueButtonRightPadding - 5;
            return new MarqueeLabel
            {
                Parent = this,
                Text = "No song playing",
                Location = new Point(Layout.LabelX, Layout.LabelYCentered),
                Width = availableWidth,
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

        private Label CreateElapsedLabel()
        {
            return new Label
            {
                Parent = this,
                Text = "0:00",
                Location = new Point(Layout.SeekElapsedX, Layout.SeekLabelY),
                AutoSizeWidth = true,
                Font = GameService.Content.DefaultFont12,
                TextColor = MaestroTheme.MutedCream
            };
        }

        private TrackBar CreateSeekSlider()
        {
            var slider = new TrackBar
            {
                Parent = this,
                Location = new Point(Layout.SeekSliderX, Layout.SeekSliderY),
                Width = Layout.SeekSliderWidth,
                MinValue = 0,
                MaxValue = 1000,
                Value = 0,
                SmallStep = true,
                Enabled = false
            };
            slider.IsDraggingChanged += OnSeekDraggingChanged;
            slider.ValueChanged += OnSeekValueChanged;
            return slider;
        }

        private Label CreateTotalLabel()
        {
            return new Label
            {
                Parent = this,
                Text = "0:00",
                Location = new Point(Layout.SeekTotalX, Layout.SeekLabelY),
                AutoSizeWidth = true,
                Font = GameService.Content.DefaultFont12,
                TextColor = MaestroTheme.MutedCream
            };
        }

        private StandardButton CreateQueueButton(int panelWidth)
        {
            var button = new StandardButton
            {
                Parent = this,
                Text = ">>",
                Location = new Point(panelWidth - Layout.QueueButtonWidth - Layout.QueueButtonRightPadding, Layout.QueueButtonY),
                Size = new Point(Layout.QueueButtonWidth, MaestroTheme.ActionButtonHeight),
                BasicTooltipText = "Toggle Queue"
            };
            button.Click += (s, e) => QueueToggleClicked?.Invoke(this, EventArgs.Empty);
            return button;
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

        private void OnSeekDraggingChanged(object sender, ValueEventArgs<bool> e)
        {
            if (e.Value)
            {
                _wasPlayingBeforeSeek = _songPlayer.IsPlaying && !_songPlayer.IsPaused;
                if (_wasPlayingBeforeSeek)
                    _songPlayer.Pause();
            }
            else
            {
                _songPlayer.SeekTo(_seekSlider.Value / 1000f);
                if (_wasPlayingBeforeSeek)
                    _songPlayer.Resume();
            }
        }

        private void OnSeekValueChanged(object sender, ValueEventArgs<float> e)
        {
            if (!_seekSlider.Dragging) return;

            var song = _songPlayer.CurrentSong;
            if (song?.SeekData == null) return;

            var targetMs = (long)(e.Value / 1000f * song.SeekData.TotalDurationMs);
            _elapsedLabel.Text = FormatTime(targetMs);
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
            _seekSlider.Enabled = true;

            if (song?.SeekData != null)
            {
                _totalLabel.Text = FormatTime(song.SeekData.TotalDurationMs);
            }
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
            _seekSlider.Enabled = false;
            _seekSlider.Value = 0;
            _elapsedLabel.Text = "0:00";
            _totalLabel.Text = "0:00";
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
            _seekSlider.Enabled = false;
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

        private void UpdateSeekSlider()
        {
            var song = _songPlayer.CurrentSong;
            if (song?.SeekData == null) return;

            var seekData = song.SeekData;
            var index = _songPlayer.CurrentCommandIndex;
            if (index >= seekData.CumulativeTimeMs.Length)
                index = seekData.CumulativeTimeMs.Length - 1;
            if (index < 0) return;

            var elapsedMs = seekData.CumulativeTimeMs[index];
            var progress = seekData.TotalDurationMs > 0
                ? (float)elapsedMs / seekData.TotalDurationMs * 1000f
                : 0;
            _seekSlider.Value = progress;
            _elapsedLabel.Text = FormatTime(elapsedMs);
        }

        private string GetQueueSuffix() => _isPlayingFromQueue ? " - Queue" : "";

        private float CalculateProgress(Song song)
        {
            return song != null && song.Commands.Count > 0
                ? (float)_songPlayer.CurrentCommandIndex / song.Commands.Count * 100
                : 0;
        }

        private static string FormatTime(long ms)
        {
            var span = TimeSpan.FromMilliseconds(Math.Max(0, ms));
            return span.TotalHours >= 1
                ? span.ToString(@"h\:mm\:ss")
                : span.ToString(@"m\:ss");
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
            _elapsedLabel?.Dispose();
            _seekSlider?.Dispose();
            _totalLabel?.Dispose();
            _queueButton?.Dispose();
        }
    }
}
