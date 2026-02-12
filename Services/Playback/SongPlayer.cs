using System;
using System.Threading;
using System.Threading.Tasks;
using Blish_HUD;
using Maestro.Models;
using Maestro.Services.Data;
using Maestro.UI.Main;
using Microsoft.Xna.Framework.Input;

namespace Maestro.Services.Playback
{
    public class SongPlayer
    {
        private static readonly Logger Logger = Logger.GetLogger<SongPlayer>();
        private readonly KeyboardService _keyboardService;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _playbackTask;
        private readonly object _pauseLock = new object();
        private float _playbackSpeed = 1.0f;

        private volatile bool _seekRequested;
        private int _seekTargetIndex;
        private int _seekTargetOctave;

        private static bool Gw2HasFocus => GameService.GameIntegration.Gw2Instance.Gw2HasFocus;
        private static bool IsGw2TextInputFocused => GameService.Gw2Mumble.UI.IsTextInputFocused;
        private static bool IsOverlayTextInputFocused => SongFilterBar.IsTextInputFocused;
        private static bool ShouldPauseForInput => !Gw2HasFocus || IsGw2TextInputFocused || IsOverlayTextInputFocused;

        public Song CurrentSong { get; private set; }
        public float PlaybackSpeed
        {
            get => _playbackSpeed;
            set => _playbackSpeed = Math.Max(0.1f, Math.Min(2.0f, value));
        }
        public int CurrentCommandIndex { get; private set; }
        public bool IsPlaying => _playbackTask != null && !_playbackTask.IsCompleted;
        public bool IsPaused { get; private set; }
        public bool IsWaitingForInput => IsPlaying && !IsPaused && ShouldPauseForInput;
        public bool IsAdjustingOctave { get; private set; }

        public event EventHandler OnStarted;
        public event EventHandler OnPaused;
        public event EventHandler OnResumed;
        public event EventHandler OnStopped;
        public event EventHandler OnCompleted;

        public SongPlayer(KeyboardService keyboardService)
        {
            _keyboardService = keyboardService;
        }

        public void Play(Song song)
        {
            Stop();

            CurrentSong = song;
            CurrentCommandIndex = 0;
            IsPaused = false;
            _seekRequested = false;
            _cancellationTokenSource = new CancellationTokenSource();

            if (song.SeekData == null && song.Commands.Count > 0)
            {
                song.SeekData = NoteParser.ComputeSeekData(song.Commands);
            }

            _keyboardService.StartDebugLog(song.DisplayName);
            _playbackTask = Task.Run(() => PlaybackLoop(_cancellationTokenSource.Token));
            OnStarted?.Invoke(this, EventArgs.Empty);

            Logger.Info($"Started playing: {song.DisplayName} ({song.Commands.Count} commands)");
        }

        public void Pause()
        {
            if (!IsPlaying || IsPaused) return;

            lock (_pauseLock)
            {
                IsPaused = true;
            }

            _keyboardService.ReleaseAllKeys();
            OnPaused?.Invoke(this, EventArgs.Empty);
            Logger.Info("Playback paused");
        }

        public void Resume()
        {
            if (!IsPlaying || !IsPaused) return;

            lock (_pauseLock)
            {
                IsPaused = false;
                Monitor.Pulse(_pauseLock);
            }

            OnResumed?.Invoke(this, EventArgs.Empty);
            Logger.Info("Playback resumed");
        }

        public void TogglePause()
        {
            if (IsPaused)
                Resume();
            else
                Pause();
        }

        public void SeekTo(float progress)
        {
            if (CurrentSong?.SeekData == null) return;

            var seekData = CurrentSong.SeekData;
            var targetMs = (long)(progress * seekData.TotalDurationMs);

            var index = Array.BinarySearch(seekData.CumulativeTimeMs, targetMs);
            if (index < 0) index = ~index;
            index = Math.Min(index, CurrentSong.Commands.Count - 1);
            index = Math.Max(index, 0);

            _seekTargetIndex = index;
            _seekTargetOctave = seekData.OctaveAtCommand[index];
            _seekRequested = true;

            Logger.Info($"Seek requested to {progress:P0} (command {index}, octave {_seekTargetOctave})");
        }

        public void Stop()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();

                lock (_pauseLock)
                {
                    IsPaused = false;
                    Monitor.Pulse(_pauseLock);
                }

                try
                {
                    _playbackTask?.Wait(1000);
                }
                catch (AggregateException) { }

                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

            _keyboardService.ReleaseAllKeys();
            _keyboardService.StopDebugLog();

            CurrentSong = null;
            CurrentCommandIndex = 0;
            _seekRequested = false;

            OnStopped?.Invoke(this, EventArgs.Empty);
            Logger.Info("Playback stopped");
        }

        private async Task PlaybackLoop(CancellationToken cancellationToken)
        {
            try
            {
                if (!CurrentSong.SkipOctaveReset)
                {
                    ResetToMiddleOctave();
                }

                await Task.Delay(GameTimings.PlaybackStartDelayMs, cancellationToken);

                CurrentCommandIndex = 0;

                while (CurrentCommandIndex < CurrentSong.Commands.Count)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    lock (_pauseLock)
                    {
                        while ((IsPaused || ShouldPauseForInput) && !cancellationToken.IsCancellationRequested)
                        {
                            Monitor.Wait(_pauseLock, 100);
                        }
                    }

                    if (cancellationToken.IsCancellationRequested)
                        break;

                    if (_seekRequested)
                    {
                        _seekRequested = false;
                        CurrentCommandIndex = _seekTargetIndex;
                        _keyboardService.ReleaseAllKeys();
                        ResetToTargetOctave(_seekTargetOctave);
                        continue;
                    }

                    var command = CurrentSong.Commands[CurrentCommandIndex];

                    ExecuteCommand(command);

                    if (command.Type == CommandType.Wait && command.Duration > 0)
                    {
                        var adjustedDuration = (int)(command.Duration / _playbackSpeed);
                        await Task.Delay(adjustedDuration, cancellationToken);
                    }

                    CurrentCommandIndex++;
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    CurrentCommandIndex = CurrentSong.Commands.Count;
                    OnCompleted?.Invoke(this, EventArgs.Empty);
                    Logger.Info($"Completed playing: {CurrentSong?.DisplayName}");
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when stopped
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Error during playback - song stopped");
            }
        }

        private void ExecuteCommand(SongCommand command)
        {
            switch (command.Type)
            {
                case CommandType.KeyDown:
                    _keyboardService.KeyDown(command.Key);
                    break;
                case CommandType.KeyUp:
                    _keyboardService.KeyUp(command.Key);
                    break;
                case CommandType.Wait:
                    // Handled in PlaybackLoop with Task.Delay
                    break;
            }
        }

        /// <summary>
        /// Resets the instrument to middle octave (or low for Bass) before playback.
        /// </summary>
        private void ResetToMiddleOctave()
        {
            IsAdjustingOctave = true;
            Logger.Debug("Resetting octave...");

            for (var i = 0; i < 5; i++)
            {
                _keyboardService.KeyDown(Keys.NumPad0);
                _keyboardService.KeyUp(Keys.NumPad0);
                Thread.Sleep(GameTimings.OctaveResetDelayMs);
            }

            if (CurrentSong?.Instrument != InstrumentType.Bass)
            {
                _keyboardService.KeyDown(Keys.NumPad9);
                _keyboardService.KeyUp(Keys.NumPad9);
                Thread.Sleep(GameTimings.OctaveResetDelayMs);
            }

            IsAdjustingOctave = false;
            Logger.Debug(CurrentSong?.Instrument == InstrumentType.Bass
                ? "Octave reset complete - Bass at Low octave"
                : "Octave reset complete - now at middle octave");
        }

        /// <summary>
        /// Resets the instrument to a specific target octave for seeking.
        /// Goes to absolute bottom (5x down), then steps up to the target.
        /// </summary>
        private void ResetToTargetOctave(int targetOctave)
        {
            IsAdjustingOctave = true;
            Logger.Debug($"Seek octave reset to {targetOctave}...");

            for (var i = 0; i < 5; i++)
            {
                _keyboardService.KeyDown(Keys.NumPad0);
                _keyboardService.KeyUp(Keys.NumPad0);
                Thread.Sleep(GameTimings.OctaveResetDelayMs);
            }

            // From bottom: non-Bass needs (targetOctave + 1) ups, Bass needs targetOctave ups
            int upsNeeded = CurrentSong?.Instrument == InstrumentType.Bass
                ? targetOctave
                : targetOctave + 1;
            upsNeeded = Math.Max(0, upsNeeded);

            for (var i = 0; i < upsNeeded; i++)
            {
                _keyboardService.KeyDown(Keys.NumPad9);
                _keyboardService.KeyUp(Keys.NumPad9);
                Thread.Sleep(GameTimings.OctaveResetDelayMs);
            }

            IsAdjustingOctave = false;
            Logger.Debug($"Seek octave reset complete - target octave {targetOctave}");
        }
    }
}
