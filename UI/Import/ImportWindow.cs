using System;
using System.Collections.Generic;
using Blish_HUD;
using Blish_HUD.Controls;
using Maestro.Models;
using Maestro.Services.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maestro.UI.Import
{
    public class ImportWindow : StandardWindow
    {
        private static class Layout
        {
            public const int WindowWidth = 420;
            public const int WindowHeight = 272;
            public const int ContentWidth = 390;

            public const int PasteBarHeight = 36;
            public const int ChipHeight = 18;

            public const int InputX = 90;
            public const int InputWidth = 290;
            public const int RowHeight = 28;

            public const int FooterButtonWidth = 90;
            public const int FooterButtonGap = 10;
        }

        public event EventHandler<Song> SongImported;

        private readonly StandardButton _pasteButton;
        private readonly Label _statusChip;
        private readonly TextBox _titleInput;
        private readonly TextBox _artistInput;
        private readonly TextBox _transcriberInput;
        private readonly Dropdown _instrumentDropdown;
        private readonly StandardButton _importButton;
        private readonly StandardButton _cancelButton;
        private readonly Label _formatLink;

        private List<string> _parsedNotes;
        private bool _skipOctaveReset;

        private static Texture2D _backgroundTexture;

        private static Texture2D GetBackground()
        {
            return _backgroundTexture ?? (_backgroundTexture = MaestroTheme.CreateImportBackground(Layout.WindowWidth, Layout.WindowHeight));
        }

        public ImportWindow()
            : base(
                GetBackground(),
                new Rectangle(0, 0, Layout.WindowWidth, Layout.WindowHeight),
                new Rectangle(15, MaestroTheme.WindowContentTopPadding, Layout.ContentWidth, Layout.WindowHeight))
        {
            Title = "Import Song";
            Subtitle = "AHK or Maestro";
            Emblem = Module.Instance.ContentsManager.GetTexture("import-emblem.png");
            SavesPosition = true;
            Id = "ImportWindow_v1";
            CanResize = false;
            Parent = GameService.Graphics.SpriteScreen;

            var currentY = MaestroTheme.PaddingContentTop;

            // Step 1: prominent paste action
            _pasteButton = new StandardButton
            {
                Parent = this,
                Text = "Paste Song",
                Location = new Point(0, currentY),
                Size = new Point(Layout.ContentWidth, Layout.PasteBarHeight)
            };
            _pasteButton.Click += OnPasteClicked;
            currentY += Layout.PasteBarHeight + MaestroTheme.InputSpacing;

            // Parse result / hint chip
            _statusChip = new Label
            {
                Parent = this,
                Location = new Point(0, currentY),
                Width = Layout.ContentWidth,
                Height = Layout.ChipHeight,
                HorizontalAlignment = HorizontalAlignment.Center,
                Font = GameService.Content.DefaultFont12,
                Text = "Paste an AHK script or Maestro song",
                TextColor = MaestroTheme.HintTextColor
            };
            currentY += Layout.ChipHeight + MaestroTheme.InputSpacing;

            // Step 2: song details
            CreateLabel("Title:", 0, currentY);
            _titleInput = CreateTextBox(currentY, "Song title");
            _titleInput.TextChanged += (s, e) => UpdateImportEnabled();
            currentY += Layout.RowHeight + MaestroTheme.InputSpacing;

            CreateLabel("Artist:", 0, currentY);
            _artistInput = CreateTextBox(currentY, "Artist name");
            currentY += Layout.RowHeight + MaestroTheme.InputSpacing;

            CreateLabel("Transcriber:", 0, currentY);
            _transcriberInput = CreateTextBox(currentY, "Transcriber name");
            currentY += Layout.RowHeight + MaestroTheme.InputSpacing;

            CreateLabel("Instrument:", 0, currentY);
            _instrumentDropdown = new Dropdown
            {
                Parent = this,
                Location = new Point(Layout.InputX, currentY),
                Width = Layout.InputWidth
            };
            foreach (var info in InstrumentCatalog.Pickable)
            {
                _instrumentDropdown.Items.Add(info.DisplayName);
            }
            _instrumentDropdown.SelectedItem = "Harp";
            currentY += Layout.RowHeight + MaestroTheme.InputSpacing * 2;

            // Footer: primary Import + Cancel
            var cancelX = Layout.ContentWidth - Layout.FooterButtonWidth;
            var importX = cancelX - Layout.FooterButtonGap - Layout.FooterButtonWidth;

            _importButton = new StandardButton
            {
                Parent = this,
                Text = "Import",
                Location = new Point(importX, currentY),
                Size = new Point(Layout.FooterButtonWidth, MaestroTheme.ActionButtonHeight),
                Enabled = false
            };
            _importButton.Click += OnImportClicked;

            _cancelButton = new StandardButton
            {
                Parent = this,
                Text = "Cancel",
                Location = new Point(cancelX, currentY),
                Size = new Point(Layout.FooterButtonWidth, MaestroTheme.ActionButtonHeight)
            };
            _cancelButton.Click += OnCancelClicked;

            // Link to the song-format guide (README)
            _formatLink = new Label
            {
                Parent = this,
                Text = "Maestro format guide",
                Location = new Point(0, currentY + 6),
                AutoSizeWidth = true,
                Font = GameService.Content.DefaultFont12,
                TextColor = MaestroTheme.AmberGold,
                BasicTooltipText = "Open the Maestro song format guide in your browser"
            };
            _formatLink.Click += OnFormatLinkClicked;
        }

        private void OnFormatLinkClicked(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("https://blishhud.com/modules/?module=Aex.Maestro#profile");
            }
            catch
            {
                // ignore: failed to open the browser
            }
        }

        private Label CreateLabel(string text, int x, int y)
        {
            return new Label
            {
                Parent = this,
                Text = text,
                Location = new Point(x, y + 5),
                AutoSizeWidth = true,
                TextColor = MaestroTheme.CreamWhite
            };
        }

        private TextBox CreateTextBox(int y, string placeholder)
        {
            return new TextBox
            {
                Parent = this,
                Location = new Point(Layout.InputX, y),
                Width = Layout.InputWidth,
                PlaceholderText = placeholder
            };
        }

        private void OnPasteClicked(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            ClipboardUtil.WindowsClipboardService.GetTextAsync()
                .ContinueWith(task =>
                {
                    if (task.IsFaulted || string.IsNullOrEmpty(task.Result))
                    {
                        _parsedNotes = null;
                        SetStatus("Clipboard is empty", MaestroTheme.Error);
                        UpdateImportEnabled();
                        return;
                    }

                    ParseClipboard(task.Result);
                });
        }

        private void ParseClipboard(string text)
        {
            var trimmed = text.TrimStart();

            if (trimmed.StartsWith("["))
            {
                _parsedNotes = null;
                Subtitle = "Maestro format";
                SetStatus("Paste a single song, not a list", MaestroTheme.Error);
                UpdateImportEnabled();
                return;
            }

            if (trimmed.StartsWith("{"))
            {
                ParseJson(text);
            }
            else
            {
                ParseScript(text);
            }
        }

        private void ParseScript(string script)
        {
            Subtitle = "AHK v1 Format";
            _skipOctaveReset = false;

            try
            {
                _parsedNotes = AhkParser.ParseToCompact(script);

                if (_parsedNotes.Count == 0)
                {
                    SetStatus("No notes found in script", MaestroTheme.Error);
                }
                else
                {
                    var durationMs = NoteParser.CalculateDurationMs(_parsedNotes);
                    var duration = TimeSpan.FromMilliseconds(durationMs);
                    SetStatus($"✓ {_parsedNotes.Count} notes · {duration:m\\:ss}", MaestroTheme.Playing);
                }
            }
            catch
            {
                _parsedNotes = null;
                SetStatus("Could not parse clipboard content", MaestroTheme.Error);
            }

            UpdateImportEnabled();
        }

        private void FailParse(string message)
        {
            _parsedNotes = null;
            _skipOctaveReset = false;
            SetStatus(message, MaestroTheme.Error);
            UpdateImportEnabled();
        }

        private void ParseJson(string json)
        {
            Subtitle = "Maestro format";

            Song song;
            try
            {
                song = SongSerializer.DeserializeJsonContent(json);
            }
            catch
            {
                FailParse("Could not read Maestro song");
                return;
            }

            if (song?.Notes == null || song.Notes.Count == 0)
            {
                FailParse("No notes found");
                return;
            }

            var durationMs = NoteParser.CalculateDurationMs(song.Notes);
            if (durationMs <= 0)
            {
                FailParse("Notes are not in a valid format");
                return;
            }

            _parsedNotes = song.Notes;
            _skipOctaveReset = song.SkipOctaveReset;

            if (!string.IsNullOrWhiteSpace(song.Name))
                _titleInput.Text = song.Name;
            _artistInput.Text = string.IsNullOrWhiteSpace(song.Artist) ? string.Empty : song.Artist;
            _transcriberInput.Text = string.IsNullOrWhiteSpace(song.Transcriber) ? string.Empty : song.Transcriber;

            // InstrumentCatalog.Get never throws: every InstrumentType has a row.
            // DeserializeJsonContent falls back to the enum default (Piano) for an
            // unrecognized instrument string; the user can correct it via the dropdown.
            _instrumentDropdown.SelectedItem = InstrumentCatalog.Get(song.Instrument).DisplayName;

            var duration = TimeSpan.FromMilliseconds(durationMs);
            SetStatus($"✓ {_parsedNotes.Count} notes · {duration:m\\:ss}", MaestroTheme.Playing);
            UpdateImportEnabled();
        }

        private void SetStatus(string text, Color color)
        {
            _statusChip.Text = text;
            _statusChip.TextColor = color;
        }

        private void UpdateImportEnabled()
        {
            _importButton.Enabled =
                _parsedNotes != null &&
                _parsedNotes.Count > 0 &&
                !string.IsNullOrWhiteSpace(_titleInput.Text);
        }

        private void OnImportClicked(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            if (!ValidateInput())
                return;

            var song = BuildSong();
            if (song != null)
            {
                SongImported?.Invoke(this, song);
                Hide();
                ClearInputs();
            }
        }

        private void OnCancelClicked(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            Hide();
            ClearInputs();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(_titleInput.Text))
            {
                ScreenNotification.ShowNotification("Please enter a song title", ScreenNotification.NotificationType.Error);
                return false;
            }

            if (_parsedNotes == null || _parsedNotes.Count == 0)
            {
                ScreenNotification.ShowNotification("Please paste a valid song", ScreenNotification.NotificationType.Error);
                return false;
            }

            return true;
        }

        private Song BuildSong()
        {
            InstrumentCatalog.TryFromDisplayName(_instrumentDropdown.SelectedItem, out var instrument);

            var song = new Song
            {
                Name = _titleInput.Text.Trim(),
                Artist = string.IsNullOrWhiteSpace(_artistInput.Text) ? "Unknown" : _artistInput.Text.Trim(),
                Transcriber = string.IsNullOrWhiteSpace(_transcriberInput.Text) ? null : _transcriberInput.Text.Trim(),
                Instrument = instrument,
                SkipOctaveReset = _skipOctaveReset,
                IsUserImported = true
            };

            song.Notes.AddRange(_parsedNotes);
            var commands = NoteParser.Parse(_parsedNotes);
            song.Commands.AddRange(commands);

            ScreenNotification.ShowNotification($"Imported \"{song.Name}\" ({_parsedNotes.Count} notes)");
            return song;
        }

        private void ClearInputs()
        {
            _titleInput.Text = string.Empty;
            _artistInput.Text = string.Empty;
            _transcriberInput.Text = string.Empty;
            _instrumentDropdown.SelectedItem = "Harp";
            _parsedNotes = null;
            _skipOctaveReset = false;
            Subtitle = "AHK or Maestro";
            SetStatus("Paste an AHK script or Maestro song", MaestroTheme.HintTextColor);
            UpdateImportEnabled();
        }

        protected override void DisposeControl()
        {
            _importButton.Click -= OnImportClicked;
            _cancelButton.Click -= OnCancelClicked;
            _pasteButton.Click -= OnPasteClicked;
            _formatLink.Click -= OnFormatLinkClicked;

            _titleInput?.Dispose();
            _artistInput?.Dispose();
            _transcriberInput?.Dispose();
            _instrumentDropdown?.Dispose();
            _statusChip?.Dispose();
            _pasteButton?.Dispose();
            _importButton?.Dispose();
            _cancelButton?.Dispose();
            _formatLink?.Dispose();

            base.DisposeControl();
        }
    }
}
