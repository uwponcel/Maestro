using System;
using System.Collections.Generic;
using System.Text;
using Blish_HUD;
using Blish_HUD.Controls;
using Maestro.Models;
using Maestro.Services.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maestro.UI.MaestroCreator
{
    public class MaestroCreatorWindow : StandardWindow
    {
        private static readonly Logger Logger = Logger.GetLogger<MaestroCreatorWindow>();

        private static class Layout
        {
            public const int WindowWidth = 420;
            public const int WindowHeight = 565;
            public const int ContentWidth = 390;
            public const int ContentHeight = 530;

            public const int RowHeight = 28;
            public const int NoteSequenceHeight = 195;
            public const int ChordBarHeight = 30;

            public const int TitleInputX = 35;
            public const int TitleInputWidth = 95;
            public const int ArtistLabelX = 135;
            public const int ArtistInputX = 175;
            public const int ArtistInputWidth = 70;
            public const int TranscriberLabelX = 250;
            public const int TranscriberInputX = 275;
            public const int TranscriberInputWidth = 115;

            public const int ActionButtonWidth = 80;
            public const int ActionButtonSpacing = 10;
            public const int ChordPreviewMaxLength = 38;
            public const int LabelYOffset = 5;
        }

        public event EventHandler<Song> SongCreated;
        public event EventHandler WindowClosed;

        private readonly TextBox _titleInput;
        private readonly TextBox _artistInput;
        private readonly TextBox _transcriberInput;
        private readonly PianoKeyboard _pianoKeyboard;
        private readonly DurationSelector _durationSelector;
        private readonly NoteSequencePanel _noteSequencePanel;
        private readonly StandardButton _previewButton;
        private readonly StandardButton _saveButton;
        private readonly StandardButton _cancelButton;

        private readonly StandardButton _chordModeButton;
        private readonly Label _chordPreviewLabel;
        private readonly StandardButton _addChordButton;
        private bool _isChordMode;
        private readonly List<string> _pendingChordNotes = new List<string>();
        private readonly List<NoteEventArgs> _pendingChordEvents = new List<NoteEventArgs>();

        private InstrumentType _instrument = InstrumentType.Piano;

        // Instrument confirmation overlay
        private readonly Panel _confirmationOverlay;
        private readonly Label _confirmationLabel;
        private readonly StandardButton _readyButton;
        private bool _isWaitingForConfirmation;

        private static Texture2D _backgroundTexture;

        private static Texture2D GetBackground()
        {
            return _backgroundTexture ?? (_backgroundTexture = MaestroTheme.CreateWindowBackground(Layout.WindowWidth, Layout.WindowHeight));
        }

        public void SetInstrument(InstrumentType instrument)
        {
            _instrument = instrument;
            Subtitle = instrument.ToString();
            _pianoKeyboard.Configure(instrument);
        }

        public override void Show()
        {
            base.Show();

            // Configure keyboard for selected instrument
            _pianoKeyboard.Configure(_instrument);

            // Show confirmation overlay and wait for user to confirm instrument is equipped
            _isWaitingForConfirmation = true;
            _confirmationLabel.Text = $"Equip your {_instrument} and click Ready";
            _confirmationOverlay.Visible = true;
            _pianoKeyboard.SetOctaveButtonsEnabled(false);
        }

        private void OnReadyClicked(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            if (!_isWaitingForConfirmation)
                return;

            _isWaitingForConfirmation = false;
            _confirmationOverlay.Visible = false;

            // Now do the octave reset
            _chordPreviewLabel.Text = "Resetting octave...";

            // Reset in-game instrument to appropriate starting octave
            // Bass: stays at low octave, others: go to middle octave
            if (_instrument == InstrumentType.Bass)
            {
                ResetToLowOctave();
            }
            else
            {
                Module.Instance.ResetToMiddleOctave();
            }

            // Re-enable buttons and clear status
            _pianoKeyboard.SetOctaveButtonsEnabled(true);
            _chordPreviewLabel.Text = "";
        }

        private void ResetToLowOctave()
        {
            // For bass: just go to lowest octave (5x down ensures we're at bottom)
            // This is simpler than middle reset since we don't need to go back up
            var keyboardService = Module.Instance;
            for (var i = 0; i < 5; i++)
            {
                keyboardService.PlayOctaveChange(false);
                System.Threading.Thread.Sleep(GameTimings.OctaveChangeDelayMs);
            }
        }

        public MaestroCreatorWindow()
            : base(
                GetBackground(),
                new Rectangle(0, 0, Layout.WindowWidth, Layout.WindowHeight),
                new Rectangle(15, 30, Layout.ContentWidth, Layout.ContentHeight))
        {
            Title = "Maestro Creator";
            Emblem = Module.Instance.ContentsManager.GetTexture("creator-emblem.png");
            SavesPosition = true;
            Id = "MaestroCreatorWindow_v1";
            CanResize = false;
            Parent = GameService.Graphics.SpriteScreen;

            var currentY = MaestroTheme.PaddingContentTop;

            CreateLabel("Title:", 0, currentY);
            _titleInput = new TextBox
            {
                Parent = this,
                Location = new Point(Layout.TitleInputX, currentY),
                Width = Layout.TitleInputWidth,
                PlaceholderText = "Song title"
            };

            CreateLabel("Artist:", Layout.ArtistLabelX, currentY);
            _artistInput = new TextBox
            {
                Parent = this,
                Location = new Point(Layout.ArtistInputX, currentY),
                Width = Layout.ArtistInputWidth,
                PlaceholderText = "Artist"
            };

            CreateLabel("By:", Layout.TranscriberLabelX, currentY);
            _transcriberInput = new TextBox
            {
                Parent = this,
                Location = new Point(Layout.TranscriberInputX, currentY),
                Width = Layout.TranscriberInputWidth,
                PlaceholderText = "Your name"
            };
            currentY += Layout.RowHeight + MaestroTheme.InputSpacing;

            _pianoKeyboard = new PianoKeyboard(Layout.ContentWidth)
            {
                Parent = this,
                Location = new Point(0, currentY),
                ShowBorder = true
            };
            _pianoKeyboard.NotePressed += OnNotePressed;
            _pianoKeyboard.OctaveChanged += OnOctaveChanged;
            currentY += PianoKeyboard.Layout.TotalHeight + MaestroTheme.InputSpacing;

            // Duration selector
            _durationSelector = new DurationSelector(Layout.ContentWidth)
            {
                Parent = this,
                Location = new Point(0, currentY)
            };
            currentY += DurationSelector.Layout.Height + MaestroTheme.InputSpacing;

            // Chord mode bar
            _chordModeButton = new StandardButton
            {
                Parent = this,
                Text = "Chord: OFF",
                Location = new Point(0, currentY),
                Size = new Point(80, Layout.ChordBarHeight - 4),
                BasicTooltipText = "Toggle chord mode to add multiple notes at once"
            };
            _chordModeButton.Click += OnChordModeToggle;

            _chordPreviewLabel = new Label
            {
                Parent = this,
                Text = "",
                Location = new Point(85, currentY),
                Size = new Point(220, Layout.ChordBarHeight - 4),
                Font = GameService.Content.DefaultFont12,
                TextColor = MaestroTheme.AmberGold,
                VerticalAlignment = VerticalAlignment.Middle
            };

            _addChordButton = new StandardButton
            {
                Parent = this,
                Text = "Add Chord",
                Location = new Point(Layout.ContentWidth - 80, currentY),
                Size = new Point(80, Layout.ChordBarHeight - 4),
                Enabled = false,
                BasicTooltipText = "Add the current chord to the sequence"
            };
            _addChordButton.Click += OnAddChordClicked;
            currentY += Layout.ChordBarHeight + MaestroTheme.InputSpacing;

            _noteSequencePanel = new NoteSequencePanel(Layout.ContentWidth, Layout.NoteSequenceHeight)
            {
                Parent = this,
                Location = new Point(0, currentY),
                ShowBorder = true
            };
            currentY += Layout.NoteSequenceHeight + MaestroTheme.InputSpacing * 2;

            // Action buttons
            var buttonsStartX = (Layout.ContentWidth - (Layout.ActionButtonWidth * 3 + Layout.ActionButtonSpacing * 2)) / 2;

            _previewButton = new StandardButton
            {
                Parent = this,
                Text = "Preview",
                Location = new Point(buttonsStartX, currentY),
                Size = new Point(Layout.ActionButtonWidth, MaestroTheme.ActionButtonHeight)
            };
            _previewButton.Click += OnPreviewClicked;

            _saveButton = new StandardButton
            {
                Parent = this,
                Text = "Save",
                Location = new Point(buttonsStartX + Layout.ActionButtonWidth + Layout.ActionButtonSpacing, currentY),
                Size = new Point(Layout.ActionButtonWidth, MaestroTheme.ActionButtonHeight)
            };
            _saveButton.Click += OnSaveClicked;

            _cancelButton = new StandardButton
            {
                Parent = this,
                Text = "Cancel",
                Location = new Point(buttonsStartX + (Layout.ActionButtonWidth + Layout.ActionButtonSpacing) * 2, currentY),
                Size = new Point(Layout.ActionButtonWidth, MaestroTheme.ActionButtonHeight)
            };
            _cancelButton.Click += OnCancelClicked;

            // Confirmation overlay - shown when window opens, hidden after Ready is clicked
            _confirmationOverlay = new Panel
            {
                Parent = this,
                Location = new Point(0, MaestroTheme.PaddingContentTop + Layout.RowHeight + MaestroTheme.InputSpacing),
                Size = new Point(Layout.ContentWidth, PianoKeyboard.Layout.TotalHeight),
                BackgroundColor = new Color(0, 0, 0, 200),
                ZIndex = 100,
                Visible = false
            };

            _confirmationLabel = new Label
            {
                Parent = _confirmationOverlay,
                Text = "Equip your instrument and click Ready",
                Location = new Point(0, PianoKeyboard.Layout.TotalHeight / 2 - 30),
                Size = new Point(Layout.ContentWidth, 30),
                Font = GameService.Content.DefaultFont16,
                TextColor = MaestroTheme.CreamWhite,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            _readyButton = new StandardButton
            {
                Parent = _confirmationOverlay,
                Text = "Ready",
                Location = new Point((Layout.ContentWidth - 100) / 2, PianoKeyboard.Layout.TotalHeight / 2 + 5),
                Size = new Point(100, 30)
            };
            _readyButton.Click += OnReadyClicked;
        }

        private Label CreateLabel(string text, int x, int y)
        {
            return new Label
            {
                Parent = this,
                Text = text,
                Location = new Point(x, y + Layout.LabelYOffset),
                AutoSizeWidth = true,
                TextColor = MaestroTheme.CreamWhite
            };
        }

        private void OnChordModeToggle(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            _isChordMode = !_isChordMode;
            _chordModeButton.Text = _isChordMode ? "Chord: ON" : "Chord: OFF";

            if (!_isChordMode && _pendingChordNotes.Count > 0)
            {
                // When turning off chord mode, clear any pending chord (user must use Add Chord button)
                _pendingChordNotes.Clear();
                _pendingChordEvents.Clear();
            }

            UpdateChordPreview();
        }

        private void OnAddChordClicked(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            if (_pendingChordNotes.Count > 0)
            {
                AddPendingChord();
            }
        }

        private void AddPendingChord()
        {
            if (_pendingChordNotes.Count == 0) return;

            // Play all chord notes
            foreach (var noteEvent in _pendingChordEvents)
            {
                PlayNoteSound(noteEvent);
            }

            // Join notes with space for chord notation
            var chordString = string.Join(" ", _pendingChordNotes);
            _noteSequencePanel.AddNote(chordString);

            _pendingChordNotes.Clear();
            _pendingChordEvents.Clear();
            UpdateChordPreview();
        }

        private void UpdateChordPreview()
        {
            if (_pendingChordNotes.Count == 0)
            {
                _chordPreviewLabel.Text = _isChordMode ? "Click keys to build chord..." : "";
                _chordPreviewLabel.BasicTooltipText = null;
                _addChordButton.Enabled = false;
            }
            else
            {
                var chordText = string.Join(" ", _pendingChordNotes);
                var displayText = "Chord: " + chordText;

                if (displayText.Length > Layout.ChordPreviewMaxLength)
                {
                    displayText = displayText.Substring(0, Layout.ChordPreviewMaxLength - 3) + "...";
                }

                _chordPreviewLabel.Text = displayText;
                _chordPreviewLabel.BasicTooltipText = chordText;
                _addChordButton.Enabled = true;
            }
        }

        private void OnOctaveChanged(object sender, bool up)
        {
            // Send the octave change to in-game instrument
            Module.Instance.PlayOctaveChange(up);
        }

        private void OnNotePressed(object sender, NoteEventArgs e)
        {
            var noteString = BuildNoteString(e);

            if (_isChordMode)
            {
                // In chord mode, accumulate notes (sound plays when chord is added)
                _pendingChordNotes.Add(noteString);
                _pendingChordEvents.Add(e);
                UpdateChordPreview();
            }
            else
            {
                // Normal mode - add single note and play sound
                PlayNoteSound(e);
                _noteSequencePanel.AddNote(noteString);
            }
        }

        private void PlayNoteSound(NoteEventArgs e)
        {
            if (e.IsRest)
                return;

            Module.Instance.PlayNote(e.Note, e.IsSharp, e.IsHighC);
        }

        private string BuildNoteString(NoteEventArgs e)
        {
            var sb = new StringBuilder();

            if (e.IsRest)
            {
                sb.Append("R");
            }
            else
            {
                sb.Append(e.Note);

                if (e.IsHighC)
                    sb.Append("^");
                else if (e.IsSharp)
                    sb.Append("#");

                // Add octave modifier based on instrument
                var octave = _pianoKeyboard.CurrentOctave;
                if (_instrument == InstrumentType.Bass)
                {
                    // Bass: octave 0 = Low (no modifier), octave 1 = High (+)
                    if (octave > 0)
                        sb.Append("+");
                }
                else
                {
                    // Others: octave -1 = Lower (-), 0 = Middle (none), 1 = Upper (+)
                    if (octave > 0)
                        sb.Append("+");
                    else if (octave < 0)
                        sb.Append("-");
                }
            }

            sb.Append(":");
            sb.Append(_durationSelector.CurrentDurationMs);

            return sb.ToString();
        }

        private void OnPreviewClicked(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            // Add any pending chord first
            if (_pendingChordNotes.Count > 0)
            {
                AddPendingChord();
            }

            if (_noteSequencePanel.NoteCount == 0)
            {
                ScreenNotification.ShowNotification("Add some notes first!", ScreenNotification.NotificationType.Warning);
                return;
            }

            var song = BuildSong("Preview", "Preview", "");
            if (song != null)
            {
                Module.Instance.PreviewSong(song);
            }
        }

        private void OnSaveClicked(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            // Add any pending chord first
            if (_pendingChordNotes.Count > 0)
            {
                AddPendingChord();
            }

            if (!ValidateInput())
                return;

            var song = BuildSong(
                _titleInput.Text.Trim(),
                string.IsNullOrWhiteSpace(_artistInput.Text) ? "Unknown" : _artistInput.Text.Trim(),
                string.IsNullOrWhiteSpace(_transcriberInput.Text) ? "" : _transcriberInput.Text.Trim());

            if (song != null)
            {
                SongCreated?.Invoke(this, song);
                ScreenNotification.ShowNotification($"Song saved: {song.Name}");
                Hide();
                ClearInputs();
            }
        }

        private void OnCancelClicked(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            Hide();
            ClearInputs();
        }

        public override void Hide()
        {
            base.Hide();
            WindowClosed?.Invoke(this, EventArgs.Empty);
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(_titleInput.Text))
            {
                ScreenNotification.ShowNotification("Please enter a song title", ScreenNotification.NotificationType.Error);
                return false;
            }

            if (_noteSequencePanel.NoteCount == 0)
            {
                ScreenNotification.ShowNotification("Please add some notes", ScreenNotification.NotificationType.Error);
                return false;
            }

            return true;
        }

        private Song BuildSong(string title, string artist, string transcriber)
        {
            try
            {
                var song = new Song
                {
                    Name = title,
                    Artist = artist,
                    Transcriber = transcriber,
                    Instrument = _instrument,
                    IsUserImported = true
                };

                foreach (var note in _noteSequencePanel.Notes)
                {
                    song.Notes.Add(note);
                }

                var commands = NoteParser.Parse(song.Notes);
                song.Commands.AddRange(commands);

                return song;
            }
            catch (Exception ex)
            {
                ScreenNotification.ShowNotification($"Error building song: {ex.Message}", ScreenNotification.NotificationType.Error);
                return null;
            }
        }

        private void ClearInputs()
        {
            _titleInput.Text = string.Empty;
            _artistInput.Text = string.Empty;
            _transcriberInput.Text = string.Empty;
            _noteSequencePanel.Clear();
            _pianoKeyboard.CurrentOctave = 0;
            _pendingChordNotes.Clear();
            _pendingChordEvents.Clear();
            _isChordMode = false;
            _chordModeButton.Text = "Chord: OFF";
            UpdateChordPreview();
        }

        protected override void DisposeControl()
        {
            _pianoKeyboard.NotePressed -= OnNotePressed;
            _pianoKeyboard.OctaveChanged -= OnOctaveChanged;
            _previewButton.Click -= OnPreviewClicked;
            _saveButton.Click -= OnSaveClicked;
            _cancelButton.Click -= OnCancelClicked;
            _chordModeButton.Click -= OnChordModeToggle;
            _addChordButton.Click -= OnAddChordClicked;
            _readyButton.Click -= OnReadyClicked;

            _titleInput?.Dispose();
            _artistInput?.Dispose();
            _transcriberInput?.Dispose();
            _pianoKeyboard?.Dispose();
            _durationSelector?.Dispose();
            _noteSequencePanel?.Dispose();
            _previewButton?.Dispose();
            _saveButton?.Dispose();
            _cancelButton?.Dispose();
            _chordModeButton?.Dispose();
            _chordPreviewLabel?.Dispose();
            _addChordButton?.Dispose();
            _readyButton?.Dispose();
            _confirmationLabel?.Dispose();
            _confirmationOverlay?.Dispose();

            base.DisposeControl();
        }
    }
}
