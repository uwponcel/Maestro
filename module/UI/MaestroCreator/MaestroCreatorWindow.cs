using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blish_HUD;
using Blish_HUD.Controls;
using Maestro.Models;
using Maestro.Services.Data;
using Maestro.Services.Playback;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maestro.UI.MaestroCreator
{
    public class MaestroCreatorWindow : StandardWindow
    {
        private static class Layout
        {
            public const int WindowWidth = 420;
            public const int WindowHeight = 408;
            public const int ContentWidth = 390;
            // Left inset that horizontally centers the content region inside the window.
            public const int ContentLeftInset = (WindowWidth - ContentWidth) / 2;
            public const int ContentHeight = 398;

            public const int RowHeight = 28;
            public const int ChordBarHeight = 30;

            // Metadata: Title on a full-width row, Artist + By split on the next row.
            public const int TitleInputX = 45;
            public const int TitleInputWidth = ContentWidth - TitleInputX;   // 345
            public const int ArtistInputX = 48;
            public const int ArtistInputWidth = 140;
            public const int ByLabelX = 200;
            public const int ByInputX = 228;
            public const int ByInputWidth = ContentWidth - ByInputX;         // 162

            public const int ActionButtonWidth = 90;
            public const int ActionButtonHeight = 30;
            public const int ActionButtonSpacing = 12;
            public const int ChordPreviewMaxLength = 38;
            public const int LabelYOffset = 5;
            public const int MaxChordNotes = 7;
        }

        public event EventHandler<Song> SongCreated;
        public event EventHandler<Song> SongEdited;
        public event EventHandler WindowClosed;

        private readonly TextBox _titleInput;
        private readonly TextBox _artistInput;
        private readonly TextBox _transcriberInput;
        private readonly PianoKeyboard _pianoKeyboard;
        private readonly DrumPadPanel _drumPadPanel;
        private readonly DurationSelector _durationSelector;
        private readonly NoteSequencePanel _noteSequencePanel;
        private readonly StandardButton _saveButton;
        private readonly StandardButton _cancelButton;

        private readonly StandardButton _chordModeButton;
        private readonly Label _chordPreviewLabel;
        private readonly StandardButton _addChordButton;
        private bool _isChordMode;
        private readonly List<string> _pendingChordNotes = new List<string>();
        private readonly List<Action> _pendingChordPreviews = new List<Action>();

        private InstrumentType _instrument = InstrumentType.Piano;
        private Song _editingSong;

        // Notes window (always used, no embedded panel)
        private NoteSequenceWindow _noteSequenceWindow;

        // Creator's own playback (decoupled from main window)
        private readonly SongPlayer _creatorPlayer;
        private bool _isPreviewActive;

        // Instrument confirmation overlay
        private readonly Panel _confirmationOverlay;
        private readonly Label _confirmationLabel;
        private readonly StandardButton _readyButton;
        private bool _isWaitingForConfirmation;

        private static Texture2D _backgroundTexture;

        private static Texture2D GetBackground()
        {
            return _backgroundTexture ?? (_backgroundTexture = MaestroTheme.CreateCreatorBackground(Layout.WindowWidth, Layout.WindowHeight));
        }

        private static string ShortInstrumentName(InstrumentType instrument)
        {
            // The Bell variants ("Bell (2 octaves)" / "Bell (3 octaves)") are just
            // shown as "Bell" in the Creator's subtitle and prompts.
            return instrument == InstrumentType.Bell || instrument == InstrumentType.BellMagnanimous
                ? "Bell"
                : InstrumentCatalog.Get(instrument).DisplayName;
        }

        private bool IsPercussion => InstrumentCatalog.Get(_instrument).IsPercussion;

        private void ConfigureInputPanel()
        {
            if (IsPercussion)
            {
                _pianoKeyboard.Visible = false;
                _drumPadPanel.Visible = true;
                _drumPadPanel.Configure(_instrument);
            }
            else
            {
                _drumPadPanel.Visible = false;
                _pianoKeyboard.Visible = true;
                _pianoKeyboard.Configure(_instrument);
            }
        }

        public void SetInstrument(InstrumentType instrument)
        {
            _instrument = instrument;
            Subtitle = ShortInstrumentName(instrument);
            ConfigureInputPanel();
            _durationSelector.SetAccentColor(instrument);

            // Update chord mode button accent
            if (_isChordMode)
                _chordModeButton.BackgroundColor = MaestroTheme.GetInstrumentAccent(instrument);
        }

        public void LoadSong(Song song)
        {
            _editingSong = song;
            _instrument = song.Instrument;
            SetInstrument(song.Instrument);

            _titleInput.Text = song.Name ?? string.Empty;
            _artistInput.Text = song.Artist ?? string.Empty;
            _transcriberInput.Text = song.Transcriber ?? string.Empty;

            if (song.Bpm.HasValue)
                _durationSelector.Bpm = song.Bpm.Value;

            _noteSequencePanel.Clear();
            foreach (var note in song.Notes)
            {
                _noteSequencePanel.AddNote(note);
            }
        }

        public override void Show()
        {
            base.Show();

            // Always open the Notes window alongside the Creator
            OpenNotesWindow();

            // Configure keyboard for selected instrument
            ConfigureInputPanel();

            // Skip confirmation overlay when editing (instrument is already known)
            if (_editingSong != null)
            {
                _isWaitingForConfirmation = false;
                _confirmationOverlay.Visible = false;
                if (!IsPercussion)
                    _pianoKeyboard.SetOctaveButtonsEnabled(true);
                return;
            }

            // Show confirmation overlay and wait for user to confirm instrument is equipped
            _isWaitingForConfirmation = true;
            _confirmationLabel.Text = $"Equip your {ShortInstrumentName(_instrument)} and click Ready";
            _confirmationOverlay.Visible = true;
            if (!IsPercussion)
                _pianoKeyboard.SetOctaveButtonsEnabled(false);
        }

        private void OpenNotesWindow()
        {
            if (_noteSequenceWindow != null) return;

            _noteSequencePanel.Parent = null;
            _noteSequenceWindow = new NoteSequenceWindow(_noteSequencePanel);
            _noteSequenceWindow.Show();
        }

        private void CloseNotesWindow()
        {
            if (_noteSequenceWindow == null) return;

            _noteSequenceWindow.DetachPanel();
            _noteSequenceWindow.CloseProgrammatic();
            _noteSequenceWindow.Dispose();
            _noteSequenceWindow = null;
        }

        private void OnReadyClicked(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            if (!_isWaitingForConfirmation)
                return;

            _isWaitingForConfirmation = false;
            _confirmationOverlay.Visible = false;

            // Now do the octave reset
            _chordPreviewLabel.Text = "Resetting octave...";

            // Instruments whose lowest octave is 0 (Bass, 2-octave Bell) reset to their
            // bottom; instruments that reach octave -1 step back up to octave 0 (middle).
            if (IsPercussion)
            {
                // Drums have no octave; nothing to reset.
            }
            else if (InstrumentCatalog.Get(_instrument).MinOctave == 0)
            {
                ResetToLowOctave();
            }
            else
            {
                Module.Instance.ResetToMiddleOctave();
            }

            // Re-enable buttons and clear status
            if (!IsPercussion)
                _pianoKeyboard.SetOctaveButtonsEnabled(true);
            _chordPreviewLabel.Text = "";
        }

        private void ResetToLowOctave()
        {
            // For bass: just go to lowest octave (5x down ensures we're at bottom)
            var keyboardService = Module.Instance;
            for (var i = 0; i < 5; i++)
            {
                keyboardService.PlayOctaveChange(false);
                System.Threading.Thread.Sleep(GameTimings.OctaveResetDelayMs);
            }
        }

        public MaestroCreatorWindow()
            : base(
                GetBackground(),
                new Rectangle(0, 0, Layout.WindowWidth, Layout.WindowHeight),
                new Rectangle(Layout.ContentLeftInset, MaestroTheme.WindowContentTopPadding, Layout.ContentWidth, Layout.ContentHeight))
        {
            Title = "Maestro Creator";
            Emblem = Module.Instance.ContentsManager.GetTexture("creator-emblem.png");
            SavesPosition = true;
            Id = "MaestroCreatorWindow_v2";
            CanResize = false;
            Parent = GameService.Graphics.SpriteScreen;

            var currentY = MaestroTheme.PaddingContentTop;

            // --- Title (full-width row) ---
            CreateLabel("Title:", 0, currentY);
            _titleInput = new TextBox
            {
                Parent = this,
                Location = new Point(Layout.TitleInputX, currentY),
                Width = Layout.TitleInputWidth,
                PlaceholderText = "Song title"
            };
            currentY += Layout.RowHeight + MaestroTheme.InputSpacing;

            // --- Artist + By (split row) ---
            CreateLabel("Artist:", 0, currentY);
            _artistInput = new TextBox
            {
                Parent = this,
                Location = new Point(Layout.ArtistInputX, currentY),
                Width = Layout.ArtistInputWidth,
                PlaceholderText = "Artist"
            };

            CreateLabel("By:", Layout.ByLabelX, currentY);
            _transcriberInput = new TextBox
            {
                Parent = this,
                Location = new Point(Layout.ByInputX, currentY),
                Width = Layout.ByInputWidth,
                PlaceholderText = "Your name"
            };
            currentY += Layout.RowHeight + MaestroTheme.InputSpacing * 2;

            // --- Piano keyboard ---
            _pianoKeyboard = new PianoKeyboard(Layout.ContentWidth)
            {
                Parent = this,
                Location = new Point(0, currentY),
                ShowBorder = true
            };
            _pianoKeyboard.NotePressed += OnNotePressed;
            _pianoKeyboard.OctaveChanged += OnOctaveChanged;

            _drumPadPanel = new DrumPadPanel(Layout.ContentWidth)
            {
                Parent = this,
                Location = new Point(0, currentY),
                Visible = false,
                ShowBorder = true
            };
            _drumPadPanel.PadPressed += OnDrumPadPressed;
            _drumPadPanel.RestPressed += OnDrumRestPressed;

            currentY += PianoKeyboard.Layout.TotalHeight + MaestroTheme.InputSpacing;

            // --- Duration selector (BPM + note types + dot) ---
            _durationSelector = new DurationSelector(Layout.ContentWidth)
            {
                Parent = this,
                Location = new Point(0, currentY)
            };
            currentY += DurationSelector.Layout.Height + MaestroTheme.InputSpacing;

            // --- Chord mode bar ---
            _chordModeButton = new StandardButton
            {
                Parent = this,
                Text = "Chord",
                Location = new Point(0, currentY),
                Size = new Point(65, Layout.ChordBarHeight - 4),
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
            currentY += Layout.ChordBarHeight + MaestroTheme.InputSpacing * 2;

            // --- Action buttons (Save + Cancel, centered) ---
            var totalButtonsWidth = Layout.ActionButtonWidth * 2 + Layout.ActionButtonSpacing;
            var buttonsStartX = (Layout.ContentWidth - totalButtonsWidth) / 2;

            _saveButton = new StandardButton
            {
                Parent = this,
                Text = "Save",
                Location = new Point(buttonsStartX, currentY),
                Size = new Point(Layout.ActionButtonWidth, Layout.ActionButtonHeight)
            };
            _saveButton.Click += OnSaveClicked;

            _cancelButton = new StandardButton
            {
                Parent = this,
                Text = "Cancel",
                Location = new Point(buttonsStartX + Layout.ActionButtonWidth + Layout.ActionButtonSpacing, currentY),
                Size = new Point(Layout.ActionButtonWidth, Layout.ActionButtonHeight)
            };
            _cancelButton.Click += OnCancelClicked;

            // --- Creator's own song player (decoupled from main window) ---
            _creatorPlayer = new SongPlayer(Module.Instance.KeyboardService);

            // --- Note sequence panel (lives in the separate Notes window, never embedded) ---
            _noteSequencePanel = new NoteSequencePanel(NoteSequenceWindow.Layout.DefaultWidth - 30, 400);
            _noteSequencePanel.PreviewAllRequested += OnPreviewClicked;
            _noteSequencePanel.PreviewSelectionRequested += OnPreviewSelectionClicked;
            _noteSequencePanel.PauseRequested += OnPauseClicked;
            _noteSequencePanel.StopRequested += OnStopClicked;
            _noteSequencePanel.InsertModeChanged += OnNoteSequenceStateChanged;
            _noteSequencePanel.ReplaceModeChanged += OnNoteSequenceStateChanged;
            _noteSequencePanel.SelectionChanged += OnNoteSequenceStateChanged;

            // --- Confirmation overlay ---
            _confirmationOverlay = new Panel
            {
                Parent = this,
                // Cover exactly the piano keyboard, not the metadata rows above it.
                Location = new Point(0, _pianoKeyboard.Location.Y),
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
                TextColor = MaestroTheme.InputLabelColor
            };
        }

        private void OnChordModeToggle(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            _isChordMode = !_isChordMode;
            var accent = MaestroTheme.GetInstrumentAccent(_instrument);
            _chordModeButton.BackgroundColor = _isChordMode ? accent : Color.Transparent;

            if (!_isChordMode && _pendingChordNotes.Count > 0)
            {
                _pendingChordNotes.Clear();
                _pendingChordPreviews.Clear();
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

            foreach (var preview in _pendingChordPreviews)
                preview?.Invoke();

            var chordString = string.Join(" ", _pendingChordNotes);

            if (_noteSequencePanel.IsReplaceMode)
            {
                _noteSequencePanel.ReplaceAt(_noteSequencePanel.ReplaceTargetIndex, chordString);
                _noteSequencePanel.ExitReplaceMode();
            }
            else if (_noteSequencePanel.IsInsertMode && _noteSequencePanel.HasSelection)
            {
                var insertIndex = _noteSequencePanel.GetLastSelectedIndex() + 1;
                _noteSequencePanel.InsertAt(insertIndex, chordString);
                _noteSequencePanel.SelectSingle(insertIndex);
            }
            else
            {
                _noteSequencePanel.AddNote(chordString);
            }

            _pendingChordNotes.Clear();
            _pendingChordPreviews.Clear();
            UpdateChordPreview();
        }

        private void UpdateChordPreview(bool showFullMessage = false)
        {
            if (_pendingChordNotes.Count == 0)
            {
                _chordPreviewLabel.Text = _isChordMode ? "Click keys to build chord..." : "";
                _chordPreviewLabel.BasicTooltipText = null;
                _addChordButton.Enabled = false;
                return;
            }

            var chordText = string.Join(" ", _pendingChordNotes);
            var displayText = showFullMessage
                ? $"Chord full ({Layout.MaxChordNotes}/{Layout.MaxChordNotes})"
                : $"Chord ({_pendingChordNotes.Count}/{Layout.MaxChordNotes}): {chordText}";

            if (displayText.Length > Layout.ChordPreviewMaxLength)
            {
                displayText = displayText.Substring(0, Layout.ChordPreviewMaxLength - 3) + "...";
            }

            _chordPreviewLabel.Text = displayText;
            _chordPreviewLabel.BasicTooltipText = chordText;
            _addChordButton.Enabled = true;
        }

        private void OnNoteSequenceStateChanged(object sender, EventArgs e) => UpdateStatusLabel();

        private void UpdateStatusLabel()
        {
            if (!_isChordMode)
            {
                _chordPreviewLabel.Text = "";
            }
        }

        private void OnOctaveChanged(object sender, bool up)
        {
            Module.Instance.PauseIfPlaying();
            Module.Instance.PlayOctaveChange(up);
        }

        private void OnNotePressed(object sender, NoteEventArgs e)
        {
            var noteString = BuildNoteString(e);

            if (_isChordMode)
            {
                AddToPendingChord(noteString, () => PlayNoteSound(e));
                return;
            }

            CommitToken(noteString, () => PlayNoteSound(e));
        }

        private void OnDrumPadPressed(object sender, DrumSound sound)
        {
            var token = DrumMapping.Get(sound).Code + ":" + _durationSelector.CurrentDurationMs;

            if (_isChordMode)
            {
                AddToPendingChord(token, () => Module.Instance.PlayDrum(sound));
                return;
            }

            CommitToken(token, () => Module.Instance.PlayDrum(sound));
        }

        private void OnDrumRestPressed(object sender, EventArgs e)
        {
            var token = "R:" + _durationSelector.CurrentDurationMs;
            CommitToken(token, null);
        }

        private void AddToPendingChord(string token, Action preview)
        {
            if (_pendingChordNotes.Count >= Layout.MaxChordNotes)
            {
                UpdateChordPreview(showFullMessage: true);
                return;
            }
            if (_pendingChordNotes.Contains(token))
                return;

            _pendingChordNotes.Add(token);
            _pendingChordPreviews.Add(preview);
            UpdateChordPreview();
        }

        private void CommitToken(string token, Action preview)
        {
            if (_noteSequencePanel.IsReplaceMode)
            {
                preview?.Invoke();
                _noteSequencePanel.ReplaceAt(_noteSequencePanel.ReplaceTargetIndex, token);
                _noteSequencePanel.ExitReplaceMode();
            }
            else if (_noteSequencePanel.IsInsertMode && _noteSequencePanel.HasSelection)
            {
                preview?.Invoke();
                var insertIndex = _noteSequencePanel.GetLastSelectedIndex() + 1;
                _noteSequencePanel.InsertAt(insertIndex, token);
                _noteSequencePanel.SelectSingle(insertIndex);
            }
            else
            {
                preview?.Invoke();
                _noteSequencePanel.AddNote(token);
            }
        }

        private void PlayNoteSound(NoteEventArgs e)
        {
            if (e.IsRest)
                return;

            Module.Instance.PauseIfPlaying();
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

                // Compact note format encodes a single octave offset: "+" high, "-" low,
                // nothing for middle. Instruments whose lowest octave is 0 never produce a
                // negative offset, so the general rule covers every instrument.
                var octave = _pianoKeyboard.CurrentOctave;
                if (octave > 0)
                    sb.Append("+");
                else if (octave < 0)
                    sb.Append("-");
            }

            sb.Append(":");
            sb.Append(_durationSelector.CurrentDurationMs);

            return sb.ToString();
        }

        private void OnPreviewClicked(object sender, EventArgs e)
        {
            if (_pendingChordNotes.Count > 0)
            {
                AddPendingChord();
            }

            if (_noteSequencePanel.NoteCount == 0)
            {
                ScreenNotification.ShowNotification("Add some notes first!", ScreenNotification.NotificationType.Warning);
                return;
            }

            var notes = _noteSequencePanel.Notes.ToList();
            var parseResult = SongCompiler.ParseWithMapping(notes, _instrument);

            var song = new Song
            {
                Name = "Preview",
                Artist = "Preview",
                Instrument = _instrument,
                IsCreated = true
            };
            song.Notes.AddRange(notes);
            song.Commands.AddRange(parseResult.Commands);

            Module.Instance.SongPlayer.Stop();
            _creatorPlayer.Play(song);
            StartPreviewHighlight(parseResult.CommandToNoteLineIndex, null);
        }

        private void OnPreviewSelectionClicked(object sender, EventArgs e)
        {
            var selectedNotes = _noteSequencePanel.GetSelectedNotes();
            if (selectedNotes.Count == 0) return;

            try
            {
                var selectedIndices = _noteSequencePanel.GetSelectedIndices();
                var parseResult = SongCompiler.ParseWithMapping(selectedNotes.ToList(), _instrument);

                var song = new Song
                {
                    Name = "Preview",
                    Artist = "Preview",
                    Instrument = _instrument,
                    IsCreated = true
                };
                song.Notes.AddRange(selectedNotes);
                song.Commands.AddRange(parseResult.Commands);

                Module.Instance.SongPlayer.Stop();
                _creatorPlayer.Play(song);
                StartPreviewHighlight(parseResult.CommandToNoteLineIndex, selectedIndices.ToArray());
            }
            catch (Exception ex)
            {
                ScreenNotification.ShowNotification($"Error previewing selection: {ex.Message}", ScreenNotification.NotificationType.Error);
            }
        }

        private void StartPreviewHighlight(int[] mapping, int[] noteIndices)
        {
            if (_isPreviewActive)
                StopPreviewHighlight();

            _isPreviewActive = true;
            _noteSequencePanel.StartPlaybackHighlight(_creatorPlayer, mapping, noteIndices);
            _noteSequencePanel.SetControlsEnabled(false);

            _creatorPlayer.OnStopped += OnPreviewEnded;
            _creatorPlayer.OnCompleted += OnPreviewEnded;
        }

        private void StopPreviewHighlight()
        {
            _creatorPlayer.OnStopped -= OnPreviewEnded;
            _creatorPlayer.OnCompleted -= OnPreviewEnded;

            _isPreviewActive = false;
            _noteSequencePanel.StopPlaybackHighlight();
            _noteSequencePanel.SetControlsEnabled(true);
        }

        private void OnPauseClicked(object sender, EventArgs e)
        {
            var player = _creatorPlayer;
            if (!player.IsPlaying) return;

            player.TogglePause();
            _noteSequencePanel.SetPlaybackPaused(player.IsPaused);
        }

        private void OnStopClicked(object sender, EventArgs e)
        {
            _creatorPlayer.Stop();
        }

        private void OnPreviewEnded(object sender, EventArgs e)
        {
            StopPreviewHighlight();
        }

        private void OnSaveClicked(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
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
                if (_editingSong != null)
                {
                    song.IsCreated = _editingSong.IsCreated;
                    song.IsUserImported = _editingSong.IsUserImported;
                    song.CommunityId = _editingSong.CommunityId;
                    song.IsUploaded = _editingSong.IsUploaded && !HasSongChanged(_editingSong, song);
                    SongEdited?.Invoke(this, song);
                    ScreenNotification.ShowNotification($"Song updated: {song.Name}");
                }
                else
                {
                    SongCreated?.Invoke(this, song);
                    ScreenNotification.ShowNotification($"Song saved: {song.Name}");
                }

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
            if (_isPreviewActive)
                StopPreviewHighlight();
            _creatorPlayer.Stop();

            CloseNotesWindow();

            base.Hide();
            WindowClosed?.Invoke(this, EventArgs.Empty);

            // Reset edit/input state on every close path, including the title-bar X
            // (which bypasses Save/Cancel). Otherwise a stale _editingSong makes the
            // next "create" skip the equip-confirmation overlay and reload old data.
            // Save/Cancel also call ClearInputs() after Hide(); the repeat is a no-op.
            ClearInputs();
        }

        private bool HasSongChanged(Song original, Song edited)
        {
            if (original.Name != edited.Name) return true;
            if (original.Artist != edited.Artist) return true;
            if (original.Transcriber != edited.Transcriber) return true;
            if (original.Instrument != edited.Instrument) return true;
            if (!original.Notes.SequenceEqual(edited.Notes)) return true;
            return false;
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
                    IsCreated = true,
                    Bpm = _durationSelector.Bpm
                };

                foreach (var note in _noteSequencePanel.Notes)
                {
                    song.Notes.Add(note);
                }

                var commands = SongCompiler.Parse(song.Notes, _instrument);
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
            _editingSong = null;
            _titleInput.Text = string.Empty;
            _artistInput.Text = string.Empty;
            _transcriberInput.Text = string.Empty;
            _noteSequencePanel.ResetModes();
            _noteSequencePanel.ClearSelection();
            _noteSequencePanel.Clear();
            _noteSequencePanel.ClearUndoStack();
            _pianoKeyboard.CurrentOctave = 0;
            _pendingChordNotes.Clear();
            _pendingChordPreviews.Clear();
            _isChordMode = false;
            _chordModeButton.BackgroundColor = Color.Transparent;
            UpdateChordPreview();
        }

        protected override void DisposeControl()
        {
            _creatorPlayer.Stop();
            CloseNotesWindow();
            _noteSequencePanel.PreviewSelectionRequested -= OnPreviewSelectionClicked;
            _noteSequencePanel.InsertModeChanged -= OnNoteSequenceStateChanged;
            _noteSequencePanel.ReplaceModeChanged -= OnNoteSequenceStateChanged;
            _noteSequencePanel.SelectionChanged -= OnNoteSequenceStateChanged;
            _pianoKeyboard.NotePressed -= OnNotePressed;
            _pianoKeyboard.OctaveChanged -= OnOctaveChanged;
            _drumPadPanel.PadPressed -= OnDrumPadPressed;
            _drumPadPanel.RestPressed -= OnDrumRestPressed;
            _noteSequencePanel.PreviewAllRequested -= OnPreviewClicked;
            _noteSequencePanel.PauseRequested -= OnPauseClicked;
            _noteSequencePanel.StopRequested -= OnStopClicked;
            _saveButton.Click -= OnSaveClicked;
            _cancelButton.Click -= OnCancelClicked;
            _chordModeButton.Click -= OnChordModeToggle;
            _addChordButton.Click -= OnAddChordClicked;
            _readyButton.Click -= OnReadyClicked;

            _titleInput?.Dispose();
            _artistInput?.Dispose();
            _transcriberInput?.Dispose();
            _pianoKeyboard?.Dispose();
            _drumPadPanel?.Dispose();
            _durationSelector?.Dispose();
            _noteSequencePanel?.Dispose();
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
