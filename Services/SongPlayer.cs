using System;
using System.Threading;
using System.Threading.Tasks;
using Blish_HUD;
using Maestro.Models;
using Microsoft.Xna.Framework.Input;

namespace Maestro.Services
{
    public class SongPlayer
    {
        private static readonly Logger Logger = Logger.GetLogger<SongPlayer>();

        private readonly KeyboardService _keyboardService;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _playbackTask;
        private bool _isPaused;
        private readonly object _pauseLock = new object();

        public Song CurrentSong { get; private set; }
        public int CurrentCommandIndex { get; private set; }
        public bool IsPlaying => _playbackTask != null && !_playbackTask.IsCompleted;
        public bool IsPaused => _isPaused;
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
            _isPaused = false;
            _cancellationTokenSource = new CancellationTokenSource();

            _playbackTask = Task.Run(() => PlaybackLoop(_cancellationTokenSource.Token));
            OnStarted?.Invoke(this, EventArgs.Empty);

            Logger.Info($"Started playing: {song.DisplayName} ({song.Commands.Count} commands)");
        }

        public void Pause()
        {
            if (!IsPlaying || _isPaused) return;

            lock (_pauseLock)
            {
                _isPaused = true;
            }

            OnPaused?.Invoke(this, EventArgs.Empty);
            Logger.Info("Playback paused");
        }

        public void Resume()
        {
            if (!IsPlaying || !_isPaused) return;

            lock (_pauseLock)
            {
                _isPaused = false;
                Monitor.Pulse(_pauseLock);
            }

            OnResumed?.Invoke(this, EventArgs.Empty);
            Logger.Info("Playback resumed");
        }

        public void TogglePause()
        {
            if (_isPaused)
                Resume();
            else
                Pause();
        }

        public void Stop()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();

                lock (_pauseLock)
                {
                    _isPaused = false;
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

            CurrentSong = null;
            CurrentCommandIndex = 0;

            OnStopped?.Invoke(this, EventArgs.Empty);
            Logger.Info("Playback stopped");
        }

        private async Task PlaybackLoop(CancellationToken cancellationToken)
        {
            try
            {
                ResetToMiddleOctave();

                await Task.Delay(300, cancellationToken);

                for (var i = 0; i < CurrentSong.Commands.Count; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    lock (_pauseLock)
                    {
                        while (_isPaused && !cancellationToken.IsCancellationRequested)
                        {
                            Monitor.Wait(_pauseLock, 100);
                        }
                    }

                    if (cancellationToken.IsCancellationRequested)
                        break;

                    CurrentCommandIndex = i;
                    var command = CurrentSong.Commands[i];

                    ExecuteCommand(command);

                    if (command.Type == CommandType.Wait && command.Duration > 0)
                    {
                        await Task.Delay(command.Duration, cancellationToken);
                    }
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

        private void ResetToMiddleOctave()
        {
            IsAdjustingOctave = true;
            Logger.Debug("Resetting octave...");

            // Go to lowest octave (3x down ensures we're at bottom)
            for (var i = 0; i < 3; i++)
            {
                _keyboardService.KeyDown(Keys.NumPad0);
                _keyboardService.KeyUp(Keys.NumPad0);
                Thread.Sleep(150);
            }

            // Bass only has Low/High octaves - stay at Low, songs handle their own octave
            // Other instruments: go up one to reach middle octave
            if (CurrentSong?.Instrument != InstrumentType.Bass)
            {
                _keyboardService.KeyDown(Keys.NumPad9);
                _keyboardService.KeyUp(Keys.NumPad9);
                Thread.Sleep(150);
            }

            // Small delay to let the instrument settle before playing
            Thread.Sleep(200);

            IsAdjustingOctave = false;
            Logger.Debug(CurrentSong?.Instrument == InstrumentType.Bass
                ? "Octave reset complete - Bass at Low octave"
                : "Octave reset complete - now at middle octave");
        }
    }
}
