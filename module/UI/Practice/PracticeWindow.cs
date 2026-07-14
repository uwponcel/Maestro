using System;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Maestro.Models;
using Maestro.Services.Playback;
using Maestro.Services.Practice;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maestro.UI.Practice
{
    /// <summary>
    /// Top-level Practice Mode window. Owns a <see cref="PracticeSession"/>, a
    /// <see cref="PracticeInputListener"/>, and assembles the three practice controls
    /// (<see cref="PracticeHud"/>, <see cref="NoteHighway"/>, <see cref="SectionLoopBar"/>).
    /// </summary>
    public class PracticeWindow : StandardWindow
    {
        private static class Layout
        {
            public const int WindowWidth = 520;
            public const int WindowHeight = 720;

            // Match the shared window convention: content starts at x=15, so
            // WindowWidth - ContentWidth = 30 keeps the right padding equal.
            public const int ContentWidth = 490;
            public const int HudHeight = 64;
            public const int LoopBarHeight = 40;
            public const int BottomPadding = 60;
        }

        private static Texture2D _backgroundTexture;
        private static Texture2D GetBackground()
        {
            return _backgroundTexture ?? (_backgroundTexture = MaestroTheme.CreateWindowBackground(Layout.WindowWidth, Layout.WindowHeight));
        }

        public PracticeSession Session { get; }
        public Song Song { get; }

        private readonly Song _song;
        private readonly PracticeSettings _settings;
        private readonly PracticeInputListener _inputListener;
        private readonly PracticeHud _hud;
        private readonly NoteHighway _highway;
        private readonly SectionLoopBar _loopBar;
        private readonly Panel _confirmationOverlay;
        private readonly Label _confirmationLabel;
        private readonly Label _octaveNoteLabel;
        private readonly StandardButton _readyButton;
        private readonly Panel _resultOverlay;
        private readonly Label _resultTitleLabel;
        private readonly Label _resultScoreLabel;
        private readonly Label _resultComboLabel;
        private readonly Label _resultBreakdownLabel;
        private readonly StandardButton _resultRestartButton;
        private readonly StandardButton _resultCloseButton;
        private bool _isWaitingForConfirmation;
        private bool _isClosed;
        private readonly bool _isConstructed;

        public PracticeWindow(Song song, KeyboardService keyboardService, PracticeSettings settings)
            : base(
                GetBackground(),
                new Rectangle(0, 0, Layout.WindowWidth, Layout.WindowHeight),
                new Rectangle(15, MaestroTheme.WindowContentTopPadding, Layout.ContentWidth, Layout.WindowHeight))
        {
            if (song == null) throw new ArgumentNullException(nameof(song));
            if (keyboardService == null) throw new ArgumentNullException(nameof(keyboardService));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            _song = song;
            Song = song;
            _settings = settings;

            Title = "Practice";
            Subtitle = $"{song.Name} - {song.Artist}";
            SavesPosition = true;
            Id = "PracticeWindow_v1";
            CanResize = false;
            Parent = GameService.Graphics.SpriteScreen;

            var timeline = NoteTimeline.Build(song.Notes);
            var sender = new KeyboardServiceKeySender(keyboardService);
            var instrumentInfo = InstrumentCatalog.Get(song.Instrument);
            Session = new PracticeSession(
                timeline,
                sender,
                autoOctave: true,
                settings.CountdownLengthMs.Value,
                instrumentInfo.MinOctave,
                instrumentInfo.MaxOctave);
            Session.Clock.Speed = settings.LastUsedSpeed.Value;

            _inputListener = new PracticeInputListener(keyboardService, Module.Instance.Settings);
            _inputListener.Attach(Session);

            _hud = new PracticeHud(Session, settings)
            {
                Parent = this,
                Location = new Point(0, 0),
                Width = Layout.ContentWidth,
                Height = Layout.HudHeight,
            };
            _hud.PauseRequested += OnPauseRequested;
            _hud.RestartRequested += OnRestartRequested;
            _hud.CloseRequested += OnCloseRequested;

            _highway = new NoteHighway(Session, settings.LookaheadSeconds.Value, Module.Instance.Settings)
            {
                Parent = this,
                Location = new Point(0, Layout.HudHeight),
                Width = Layout.ContentWidth,
                Height = Layout.WindowHeight - Layout.HudHeight - Layout.LoopBarHeight - Layout.BottomPadding,
            };

            _loopBar = new SectionLoopBar(Session)
            {
                Parent = this,
                Location = new Point(0, Layout.WindowHeight - Layout.LoopBarHeight - Layout.BottomPadding),
                Width = Layout.ContentWidth,
                Height = Layout.LoopBarHeight,
            };

            _highway.OnAfterTick = OnHighwayAfterTick;

            // Session countdown begins only after the user confirms the instrument
            // is equipped. Pause the clock immediately and cover the highway with
            // a confirmation overlay; clicking Ready triggers the octave reset and
            // then starts the countdown.
            Session.Clock.Pause();
            _isWaitingForConfirmation = true;

            int overlayTop = Layout.HudHeight;
            int overlayHeight = Layout.WindowHeight - Layout.HudHeight - Layout.LoopBarHeight - Layout.BottomPadding;
            _confirmationOverlay = new Panel
            {
                Parent = this,
                Location = new Point(0, overlayTop),
                Size = new Point(Layout.ContentWidth, overlayHeight),
                BackgroundColor = new Color(0, 0, 0, 220),
                ZIndex = 100,
                Visible = true,
            };

            // Stacked top-down from explicit heights/gaps (not independently-guessed
            // absolute offsets) so the visual gap between blocks is exactly what's
            // written here, regardless of each font's actual line metrics.
            const int titleHeight = 26;
            const int titleToNoteGap = 14;
            const int noteHeight = 100;
            const int noteToButtonGap = 4;
            const int buttonHeight = 30;
            int blockHeight = titleHeight + titleToNoteGap + noteHeight + noteToButtonGap + buttonHeight;
            int blockTop = overlayHeight / 2 - blockHeight / 2;

            _confirmationLabel = new Label
            {
                Parent = _confirmationOverlay,
                Text = $"Equip your {_song.Instrument} and click Ready",
                Location = new Point(0, blockTop),
                Size = new Point(Layout.ContentWidth, titleHeight),
                Font = GameService.Content.DefaultFont18,
                TextColor = MaestroTheme.CreamWhite,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
            };

            int noteTop = blockTop + titleHeight + titleToNoteGap;
            _octaveNoteLabel = new Label
            {
                Parent = _confirmationOverlay,
                Text = "Octaves are switched automatically for now.\n" +
                       "An upcoming release will let you handle them yourself.\n" +
                       "Tiles marked # are sharp notes (a piano's black keys).\n" +
                       "Play them like in game: Alt + skill slots 1-5 (C# D# F# G# A#).",
                Location = new Point(0, noteTop),
                Size = new Point(Layout.ContentWidth, noteHeight),
                Font = GameService.Content.DefaultFont14,
                TextColor = MaestroTheme.MutedCream,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
            };

            _readyButton = new StandardButton
            {
                Parent = _confirmationOverlay,
                Text = "Ready",
                Location = new Point((Layout.ContentWidth - 100) / 2, noteTop + noteHeight + noteToButtonGap),
                Size = new Point(100, buttonHeight),
            };
            _readyButton.Click += OnReadyClicked;

            // Results overlay: shown when the session completes, same cover style as
            // the confirmation overlay.
            _resultOverlay = new Panel
            {
                Parent = this,
                Location = new Point(0, overlayTop),
                Size = new Point(Layout.ContentWidth, overlayHeight),
                BackgroundColor = new Color(0, 0, 0, 220),
                ZIndex = 100,
                Visible = false,
            };

            int centerY = overlayHeight / 2;
            _resultTitleLabel = new Label
            {
                Parent = _resultOverlay,
                Text = "SONG COMPLETE",
                Location = new Point(0, centerY - 110),
                Size = new Point(Layout.ContentWidth, 30),
                Font = GameService.Content.DefaultFont18,
                TextColor = MaestroTheme.AmberGold,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            _resultScoreLabel = new Label
            {
                Parent = _resultOverlay,
                Text = "SCORE 0",
                Location = new Point(0, centerY - 70),
                Size = new Point(Layout.ContentWidth, 42),
                Font = GameService.Content.DefaultFont32 ?? GameService.Content.DefaultFont18,
                TextColor = MaestroTheme.CreamWhite,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            _resultComboLabel = new Label
            {
                Parent = _resultOverlay,
                Text = "MAX COMBO x0",
                Location = new Point(0, centerY - 20),
                Size = new Point(Layout.ContentWidth, 26),
                Font = GameService.Content.DefaultFont16,
                TextColor = MaestroTheme.CreamWhite,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            _resultBreakdownLabel = new Label
            {
                Parent = _resultOverlay,
                Text = "",
                Location = new Point(0, centerY + 12),
                Size = new Point(Layout.ContentWidth, 24),
                Font = GameService.Content.DefaultFont14,
                TextColor = MaestroTheme.MutedCream,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            _resultRestartButton = new StandardButton
            {
                Parent = _resultOverlay,
                Text = "Restart",
                Location = new Point(Layout.ContentWidth / 2 - 108, centerY + 52),
                Size = new Point(100, 30),
            };
            _resultRestartButton.Click += OnResultRestartClicked;

            _resultCloseButton = new StandardButton
            {
                Parent = _resultOverlay,
                Text = "Close",
                Location = new Point(Layout.ContentWidth / 2 + 8, centerY + 52),
                Size = new Point(100, 30),
            };
            _resultCloseButton.Click += (s, e) => Hide();

            Session.OnCompleted += OnSessionCompleted;
            _isConstructed = true;
        }

        private void OnSessionCompleted(PracticeResult result)
        {
            int score = result.PerfectCount * 100 + result.GoodCount * 50;
            _resultScoreLabel.Text = $"SCORE {score}";
            _resultComboLabel.Text = $"MAX COMBO x{result.MaxCombo}";
            _resultBreakdownLabel.Text =
                $"Perfect {result.PerfectCount}   Good {result.GoodCount}   Miss {result.MissCount}   Wrong {result.WrongCount}";
            _resultOverlay.Visible = true;
        }

        private void OnResultRestartClicked(object sender, MouseEventArgs e)
        {
            _resultOverlay.Visible = false;
            OnRestartRequested();
        }

        private void OnReadyClicked(object sender, MouseEventArgs e)
        {
            if (!_isWaitingForConfirmation) return;
            _isWaitingForConfirmation = false;
            _confirmationOverlay.Visible = false;

            // Blocks the UI thread for ~600 ms. The session clock is paused during
            // the reset, so no time elapses on the countdown while keys are sent.
            ResetOctaveForInstrument();

            Session.Clock.Resume();
        }

        private void ResetOctaveForInstrument()
        {
            // Walk to the bottom octave, then step up to the timeline's octave 0. The
            // number of ups is -MinOctave: 0 for instruments whose lowest octave is
            // already 0 (Bass, 2-octave Bell), 1 for instruments that reach octave -1.
            var info = InstrumentCatalog.Get(_song.Instrument);
            for (var i = 0; i < 5; i++)
            {
                Module.Instance.PlayOctaveChange(false);
                System.Threading.Thread.Sleep(GameTimings.OctaveResetDelayMs);
            }
            for (var i = 0; i < -info.MinOctave; i++)
            {
                Module.Instance.PlayOctaveChange(true);
                System.Threading.Thread.Sleep(GameTimings.OctaveResetDelayMs);
            }
        }

        private void OnHighwayAfterTick()
        {
            _loopBar.ApplyLoopIfNeeded();
            _hud.RefreshStats();
        }

        private void OnPauseRequested()
        {
            // Ignore pause toggles while the confirmation overlay is showing —
            // Ready is the only legal input at that stage.
            if (_isWaitingForConfirmation) return;

            if (Session.Clock.IsPaused) Session.Clock.Resume();
            else Session.Clock.Pause();
        }

        private void OnRestartRequested()
        {
            if (_isWaitingForConfirmation) return;

            _resultOverlay.Visible = false;
            Session.Clock.Pause();
            Session.Restart(countdownMs: _settings.CountdownLengthMs.Value);
            ResetOctaveForInstrument();
            Session.Clock.Resume();
        }

        private void OnCloseRequested()
        {
            Hide();
        }

        /// <summary>
        /// A practice window never survives being hidden: whether closed via the HUD
        /// button or the title-bar X (which only hides StandardWindows), dispose so
        /// <c>Module.IsPracticeActive</c> clears and main playback unblocks. The base
        /// WindowBase2 constructor sets Visible = false before this class is fully
        /// constructed, so bail until construction has finished.
        /// </summary>
        protected override void OnHidden(EventArgs e)
        {
            base.OnHidden(e);
            if (!_isConstructed || _isClosed) return;
            _isClosed = true;
            Dispose();
        }

        protected override void DisposeControl()
        {
            _isClosed = true;
            if (_hud != null)
            {
                _hud.PauseRequested -= OnPauseRequested;
                _hud.RestartRequested -= OnRestartRequested;
                _hud.CloseRequested -= OnCloseRequested;
            }
            if (_highway != null) _highway.OnAfterTick = null;
            if (_readyButton != null) _readyButton.Click -= OnReadyClicked;
            if (_resultRestartButton != null) _resultRestartButton.Click -= OnResultRestartClicked;
            if (Session != null) Session.OnCompleted -= OnSessionCompleted;
            _inputListener?.Detach();

            base.DisposeControl();
        }
    }
}
