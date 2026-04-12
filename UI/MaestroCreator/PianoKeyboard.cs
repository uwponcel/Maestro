using System;
using Blish_HUD;
using Blish_HUD.Controls;
using Maestro.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maestro.UI.MaestroCreator
{
    /// <summary>
    /// Event arguments for piano key press events.
    /// </summary>
    public class NoteEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the note name (C, D, E, F, G, A, B).
        /// </summary>
        public string Note { get; }

        /// <summary>
        /// Gets whether this is a sharp note (requires Alt modifier).
        /// </summary>
        public bool IsSharp { get; }

        /// <summary>
        /// Gets whether this is high C (octave above middle C).
        /// </summary>
        public bool IsHighC { get; }

        /// <summary>
        /// Gets whether this is a rest (silence).
        /// </summary>
        public bool IsRest { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NoteEventArgs"/> class.
        /// </summary>
        /// <param name="note">The note name.</param>
        /// <param name="isSharp">Whether this is a sharp note.</param>
        /// <param name="isHighC">Whether this is high C.</param>
        /// <param name="isRest">Whether this is a rest.</param>
        public NoteEventArgs(string note, bool isSharp = false, bool isHighC = false, bool isRest = false)
        {
            Note = note;
            IsSharp = isSharp;
            IsHighC = isHighC;
            IsRest = isRest;
        }
    }

    public class PianoKeyboard : Panel
    {
        public static class Layout
        {
            public const int WhiteKeyWidth = 45;
            public const int WhiteKeyHeight = 90;
            public const int BlackKeyWidth = 30;
            public const int BlackKeyHeight = 55;
            public const int OctaveControlHeight = 30;
            public const int RestButtonHeight = 26;
            public const int Spacing = 5;
            public const int RestSpacing = 12;
            public const int BottomPadding = 16;
            public const int OctaveButtonWidth = 30;
            public const int OctaveButtonOffset = 110;
            public const int OctaveLabelWidth = 150;
            public const int OctaveLabelOffset = 75;
            public const int WhiteKeyGap = 2;

            public static int KeysWidth => WhiteKeyWidth * 8 + Spacing * 2;
            public static int TotalHeight => OctaveControlHeight + WhiteKeyHeight + RestSpacing + RestButtonHeight + Spacing + BottomPadding;
        }

        public event EventHandler<NoteEventArgs> NotePressed;
        public event EventHandler<bool> OctaveChanged; // true = up, false = down

        private InstrumentType _instrument = InstrumentType.Piano;
        private int _minOctave = -1;
        private int _maxOctave = 1;
        private bool _sharpsEnabled = true;

        private int _currentOctave;
        public int CurrentOctave
        {
            get => _currentOctave;
            set
            {
                _currentOctave = value < _minOctave ? _minOctave : (value > _maxOctave ? _maxOctave : value);
                UpdateOctaveDisplay();
            }
        }

        private readonly Label _octaveLabel;
        private readonly Panel _octaveDownButton;
        private readonly Label _octaveDownLabel;
        private readonly Panel _octaveUpButton;
        private readonly Label _octaveUpLabel;
        private readonly Panel _restButton;
        private readonly Label _restLabel;

        private readonly string[] _whiteNotes = { "C", "D", "E", "F", "G", "A", "B", "C^" };

        private readonly (string note, int afterWhiteKey)[] _blackKeys =
        {
            ("C#", 0),
            ("D#", 1),
            ("F#", 3),
            ("G#", 4),
            ("A#", 5),
        };

        private readonly Panel[] _whiteKeyPanels;
        private readonly Panel[] _blackKeyPanels;

        private Color _accentColor = MaestroTheme.AmberGold;

        private readonly int _keysY;
        private readonly int _keysOffsetX;

        public PianoKeyboard(int containerWidth)
        {
            Size = new Point(containerWidth, Layout.TotalHeight);
            BackgroundColor = new Color(0, 0, 0, 65);  // Dark container for piano keys

            _whiteKeyPanels = new Panel[8];
            _blackKeyPanels = new Panel[5];

            // Center the keys within the container
            _keysOffsetX = (containerWidth - Layout.KeysWidth) / 2;
            var centerX = containerWidth / 2;

            var octaveY = Layout.Spacing;
            _octaveDownButton = new Panel
            {
                Parent = this,
                Location = new Point(centerX - Layout.OctaveButtonOffset, octaveY),
                Size = new Point(Layout.OctaveButtonWidth, Layout.OctaveControlHeight - 4),
                BackgroundColor = MaestroTheme.GhostButtonBackground
            };
            _octaveDownLabel = new Label
            {
                Parent = _octaveDownButton,
                Text = "-",
                Location = new Point(0, 0),
                Size = new Point(Layout.OctaveButtonWidth, Layout.OctaveControlHeight - 4),
                Font = GameService.Content.DefaultFont16,
                TextColor = _accentColor,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Middle
            };
            _octaveDownButton.MouseEntered += (s, e) => _octaveDownButton.BackgroundColor = MaestroTheme.GhostButtonHover;
            _octaveDownButton.MouseLeft += (s, e) => _octaveDownButton.BackgroundColor = MaestroTheme.GhostButtonBackground;
            _octaveDownButton.LeftMouseButtonReleased += (s, e) =>
            {
                if (_octaveButtonsEnabled && _currentOctave > _minOctave)
                {
                    CurrentOctave--;
                    OctaveChanged?.Invoke(this, false);
                }
            };

            _octaveLabel = new Label
            {
                Parent = this,
                Text = "Octave: Middle",
                Location = new Point(centerX - Layout.OctaveLabelOffset, octaveY),
                Size = new Point(Layout.OctaveLabelWidth, Layout.OctaveControlHeight),
                Font = GameService.Content.DefaultFont14,
                TextColor = MaestroTheme.OctaveLabelColor,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Middle
            };

            _octaveUpButton = new Panel
            {
                Parent = this,
                Location = new Point(centerX + Layout.OctaveButtonOffset - Layout.OctaveButtonWidth, octaveY),
                Size = new Point(Layout.OctaveButtonWidth, Layout.OctaveControlHeight - 4),
                BackgroundColor = MaestroTheme.GhostButtonBackground
            };
            _octaveUpLabel = new Label
            {
                Parent = _octaveUpButton,
                Text = "+",
                Location = new Point(0, 0),
                Size = new Point(Layout.OctaveButtonWidth, Layout.OctaveControlHeight - 4),
                Font = GameService.Content.DefaultFont16,
                TextColor = _accentColor,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Middle
            };
            _octaveUpButton.MouseEntered += (s, e) => _octaveUpButton.BackgroundColor = MaestroTheme.GhostButtonHover;
            _octaveUpButton.MouseLeft += (s, e) => _octaveUpButton.BackgroundColor = MaestroTheme.GhostButtonBackground;
            _octaveUpButton.LeftMouseButtonReleased += (s, e) =>
            {
                if (_octaveButtonsEnabled && _currentOctave < _maxOctave)
                {
                    CurrentOctave++;
                    OctaveChanged?.Invoke(this, true);
                }
            };

            _keysY = octaveY + Layout.OctaveControlHeight + Layout.Spacing;

            for (int i = 0; i < _whiteNotes.Length; i++)
            {
                var note = _whiteNotes[i];
                var isHighC = note == "C^";
                var keyPanel = CreateWhiteKey(i, note, isHighC);
                _whiteKeyPanels[i] = keyPanel;
            }

            for (int i = 0; i < _blackKeys.Length; i++)
            {
                var (note, afterWhiteKey) = _blackKeys[i];
                var keyPanel = CreateBlackKey(i, note, afterWhiteKey);
                _blackKeyPanels[i] = keyPanel;
            }

            var restY = _keysY + Layout.WhiteKeyHeight + Layout.RestSpacing;
            _restButton = new Panel
            {
                Parent = this,
                Location = new Point(_keysOffsetX + Layout.Spacing, restY),
                Size = new Point(60, Layout.RestButtonHeight),
                BackgroundColor = MaestroTheme.GhostButtonBackground
            };
            _restLabel = new Label
            {
                Parent = _restButton,
                Text = "REST",
                Location = new Point(0, -2),
                Size = new Point(60, Layout.RestButtonHeight),
                Font = GameService.Content.DefaultFont12,
                TextColor = MaestroTheme.GhostButtonText,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Middle
            };
            _restButton.MouseEntered += (s, e) => _restButton.BackgroundColor = MaestroTheme.GhostButtonHover;
            _restButton.MouseLeft += (s, e) => _restButton.BackgroundColor = MaestroTheme.GhostButtonBackground;
            _restButton.LeftMouseButtonReleased += (s, e) => NotePressed?.Invoke(this, new NoteEventArgs("R", isRest: true));
        }

        private Panel CreateWhiteKey(int index, string note, bool isHighC)
        {
            var keyPanel = new RoundedKeyPanel
            {
                Parent = this,
                Location = new Point(_keysOffsetX + Layout.Spacing + index * Layout.WhiteKeyWidth, _keysY),
                Size = new Point(Layout.WhiteKeyWidth - Layout.WhiteKeyGap, Layout.WhiteKeyHeight),
                BackgroundColor = Color.Transparent,
                FillColor = MaestroTheme.PianoWhiteKey,
                ZIndex = 0
            };

            var label = new Label
            {
                Parent = keyPanel,
                Text = isHighC ? "C^" : note,
                Location = new Point(0, Layout.WhiteKeyHeight - 25),
                Size = new Point(Layout.WhiteKeyWidth - 2, 20),
                Font = GameService.Content.DefaultFont12,
                TextColor = MaestroTheme.DarkCharcoal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            keyPanel.MouseEntered += (s, e) => keyPanel.FillColor = MaestroTheme.PianoWhiteKeyHover;
            keyPanel.MouseLeft += (s, e) => keyPanel.FillColor = MaestroTheme.PianoWhiteKey;
            keyPanel.LeftMouseButtonPressed += (s, e) =>
            {
                keyPanel.FillColor = MaestroTheme.PianoWhiteKeyPressed;
            };
            keyPanel.LeftMouseButtonReleased += (s, e) =>
            {
                keyPanel.FillColor = MaestroTheme.PianoWhiteKeyHover;
                NotePressed?.Invoke(this, new NoteEventArgs(note.Replace("^", ""), isHighC: isHighC));
            };

            return keyPanel;
        }

        private Panel CreateBlackKey(int index, string note, int afterWhiteKey)
        {
            var xPos = _keysOffsetX + Layout.Spacing + (afterWhiteKey + 1) * Layout.WhiteKeyWidth - Layout.BlackKeyWidth / 2 - 1;

            var keyPanel = new RoundedKeyPanel
            {
                Parent = this,
                Location = new Point(xPos, _keysY),
                Size = new Point(Layout.BlackKeyWidth, Layout.BlackKeyHeight),
                BackgroundColor = Color.Transparent,
                FillColor = MaestroTheme.PianoBlackKey,
                ZIndex = 10
            };

            var baseNote = note.Replace("#", "");
            var label = new Label
            {
                Parent = keyPanel,
                Text = note,
                Location = new Point(0, Layout.BlackKeyHeight - 20),
                Size = new Point(Layout.BlackKeyWidth, 18),
                Font = GameService.Content.DefaultFont12,
                TextColor = MaestroTheme.CreamWhite,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            keyPanel.MouseEntered += (s, e) => keyPanel.FillColor = MaestroTheme.PianoBlackKeyHover;
            keyPanel.MouseLeft += (s, e) => keyPanel.FillColor = MaestroTheme.PianoBlackKey;
            keyPanel.LeftMouseButtonPressed += (s, e) =>
            {
                keyPanel.FillColor = MaestroTheme.PianoBlackKeyPressed;
            };
            keyPanel.LeftMouseButtonReleased += (s, e) =>
            {
                keyPanel.FillColor = MaestroTheme.PianoBlackKeyHover;
                NotePressed?.Invoke(this, new NoteEventArgs(baseNote, isSharp: true));
            };

            return keyPanel;
        }

        private bool _octaveButtonsEnabled = true;

        public void SetOctaveButtonsEnabled(bool enabled)
        {
            _octaveButtonsEnabled = enabled;
            UpdateOctaveDisplay();
        }

        public void Configure(InstrumentType instrument)
        {
            _instrument = instrument;

            switch (instrument)
            {
                case InstrumentType.Piano:
                    _sharpsEnabled = true;
                    _minOctave = -1;  // Lower
                    _maxOctave = 1;   // Upper
                    break;
                case InstrumentType.Harp:
                case InstrumentType.Lute:
                    _sharpsEnabled = false;
                    _minOctave = -1;  // Lower
                    _maxOctave = 1;   // Upper
                    break;
                case InstrumentType.Bass:
                    _sharpsEnabled = false;
                    _minOctave = 0;   // Low
                    _maxOctave = 1;   // High
                    break;
                default:
                    _sharpsEnabled = true;
                    _minOctave = -1;
                    _maxOctave = 1;
                    break;
            }

            // Update black keys visibility
            foreach (var key in _blackKeyPanels)
            {
                if (key != null)
                    key.Visible = _sharpsEnabled;
            }

            // Clamp current octave to new bounds and reset to starting position
            // Bass starts at low (0), others at middle (0)
            _currentOctave = _instrument == InstrumentType.Bass ? 0 : 0;
            _accentColor = MaestroTheme.GetInstrumentAccent(instrument);
            UpdateOctaveDisplay();
        }

        private void UpdateOctaveDisplay()
        {
            if (_octaveLabel != null)
            {
                string octaveName;
                if (_instrument == InstrumentType.Bass)
                {
                    // Bass: 0 = Low, 1 = High
                    octaveName = _currentOctave == 0 ? "Low" : "High";
                }
                else
                {
                    // Others: -1 = Lower, 0 = Middle, 1 = Upper
                    switch (_currentOctave)
                    {
                        case -1:
                            octaveName = "Lower (-)";
                            break;
                        case 1:
                            octaveName = "Upper (+)";
                            break;
                        default:
                            octaveName = "Middle";
                            break;
                    }
                }
                _octaveLabel.Text = $"Octave: {octaveName}";
            }

            if (_octaveDownButton != null)
            {
                var downEnabled = _octaveButtonsEnabled && _currentOctave > _minOctave;
                _octaveDownButton.Opacity = downEnabled ? 1f : 0.3f;
                _octaveDownLabel.TextColor = downEnabled ? _accentColor : MaestroTheme.Disabled;
            }

            if (_octaveUpButton != null)
            {
                var upEnabled = _octaveButtonsEnabled && _currentOctave < _maxOctave;
                _octaveUpButton.Opacity = upEnabled ? 1f : 0.3f;
                _octaveUpLabel.TextColor = upEnabled ? _accentColor : MaestroTheme.Disabled;
            }
        }

        protected override void DisposeControl()
        {
            _octaveLabel?.Dispose();
            _octaveDownLabel?.Dispose();
            _octaveDownButton?.Dispose();
            _octaveUpLabel?.Dispose();
            _octaveUpButton?.Dispose();
            _restLabel?.Dispose();
            _restButton?.Dispose();

            foreach (var key in _whiteKeyPanels)
                key?.Dispose();

            foreach (var key in _blackKeyPanels)
                key?.Dispose();

            base.DisposeControl();
        }

        private class RoundedKeyPanel : Panel
        {
            private Color _fillColor;
            public Color FillColor
            {
                get => _fillColor;
                set { _fillColor = value; Invalidate(); }
            }

            public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds)
            {
                MaestroTheme.DrawBottomRoundedRect(spriteBatch, this, bounds, _fillColor);
            }
        }
    }
}
