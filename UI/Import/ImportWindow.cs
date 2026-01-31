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
            public const int WindowHeight = 250;
            public const int ContentWidth = 390;

            public const int InputX = 90;
            public const int InputWidth = 290;
            public const int RowHeight = 28;
        }

        public event EventHandler<Song> SongImported;

        private readonly TextBox _titleInput;
        private readonly TextBox _artistInput;
        private readonly TextBox _transcriberInput;
        private readonly Dropdown _instrumentDropdown;
        private readonly StandardButton _pasteButton;
        private readonly Label _parseStatusLabel;
        private readonly StandardButton _importButton;
        private readonly StandardButton _cancelButton;

        private List<string> _parsedNotes;

        private static Texture2D _backgroundTexture;

        private static Texture2D GetBackground()
        {
            return _backgroundTexture ?? (_backgroundTexture = MaestroTheme.CreateWindowBackground(Layout.WindowWidth, Layout.WindowHeight));
        }

        public ImportWindow()
            : base(
                GetBackground(),
                new Rectangle(0, 0, Layout.WindowWidth, Layout.WindowHeight),
                new Rectangle(15, MaestroTheme.WindowContentTopPadding, Layout.ContentWidth, Layout.WindowHeight))
        {
            Title = "Import Song";
            Subtitle = "AHK v1 Format";
            Emblem = Module.Instance.ContentsManager.GetTexture("import-emblem.png");
            SavesPosition = true;
            Id = "ImportWindow_v1";
            CanResize = false;
            Parent = GameService.Graphics.SpriteScreen;

            var currentY = MaestroTheme.PaddingContentTop;

            CreateLabel("Title:", 0, currentY);
            _titleInput = CreateTextBox(Layout.InputX, currentY, "Song title");
            currentY += Layout.RowHeight + MaestroTheme.InputSpacing;

            CreateLabel("Artist:", 0, currentY);
            _artistInput = CreateTextBox(Layout.InputX, currentY, "Artist name");
            currentY += Layout.RowHeight + MaestroTheme.InputSpacing;

            CreateLabel("Transcriber:", 0, currentY);
            _transcriberInput = CreateTextBox(Layout.InputX, currentY, "Transcriber name");
            currentY += Layout.RowHeight + MaestroTheme.InputSpacing;

            CreateLabel("Instrument:", 0, currentY);
            _instrumentDropdown = new Dropdown
            {
                Parent = this,
                Location = new Point(Layout.InputX, currentY),
                Width = Layout.InputWidth
            };
            foreach (var instrument in Enum.GetNames(typeof(InstrumentType)))
            {
                _instrumentDropdown.Items.Add(instrument);
            }
            _instrumentDropdown.SelectedItem = "Harp";
            currentY += Layout.RowHeight + MaestroTheme.InputSpacing * 2;

            CreateLabel("AHK Script:", 0, currentY);
            _pasteButton = new StandardButton
            {
                Parent = this,
                Text = "Paste from Clipboard",
                Location = new Point(Layout.InputX, currentY),
                Size = new Point(Layout.InputWidth, MaestroTheme.ActionButtonHeight)
            };
            _pasteButton.Click += OnPasteClicked;
            currentY += Layout.RowHeight + MaestroTheme.InputSpacing;

            _parseStatusLabel = new Label
            {
                Parent = this,
                Text = "",
                Location = new Point(Layout.InputX, currentY),
                Width = Layout.InputWidth,
                AutoSizeHeight = true,
                TextColor = MaestroTheme.MutedCream
            };
            currentY += 10 + MaestroTheme.InputSpacing;

            _importButton = new StandardButton
            {
                Parent = this,
                Text = "Import",
                Location = new Point(Layout.ContentWidth - MaestroTheme.ActionButtonWidth * 2 - 10, currentY),
                Size = new Point(MaestroTheme.ActionButtonWidth, MaestroTheme.ActionButtonHeight)
            };
            _importButton.Click += OnImportClicked;

            _cancelButton = new StandardButton
            {
                Parent = this,
                Text = "Cancel",
                Location = new Point(Layout.ContentWidth - MaestroTheme.ActionButtonWidth, currentY),
                Size = new Point(MaestroTheme.ActionButtonWidth, MaestroTheme.ActionButtonHeight)
            };
            _cancelButton.Click += OnCancelClicked;
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

        private TextBox CreateTextBox(int x, int y, string placeholder)
        {
            return new TextBox
            {
                Parent = this,
                Location = new Point(x, y),
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
                        _parseStatusLabel.Text = "Clipboard is empty";
                        _parseStatusLabel.TextColor = MaestroTheme.Error;
                        return;
                    }

                    ParseScript(task.Result);
                });
        }

        private void ParseScript(string script)
        {
            try
            {
                _parsedNotes = AhkParser.ParseToCompact(script);

                if (_parsedNotes.Count == 0)
                {
                    _parseStatusLabel.Text = "No notes found in script";
                    _parseStatusLabel.TextColor = MaestroTheme.Error;
                }
                else
                {
                    var durationMs = NoteParser.CalculateDurationMs(_parsedNotes);
                    var duration = TimeSpan.FromMilliseconds(durationMs);
                    _parseStatusLabel.Text = $"{_parsedNotes.Count} notes \u00b7 {duration:m\\:ss}";
                    _parseStatusLabel.TextColor = MaestroTheme.Playing;
                }
            }
            catch
            {
                _parsedNotes = null;
                _parseStatusLabel.Text = "Could not parse clipboard content";
                _parseStatusLabel.TextColor = MaestroTheme.Error;
            }
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
                ScreenNotification.ShowNotification("Please paste a valid AHK script", ScreenNotification.NotificationType.Error);
                return false;
            }

            return true;
        }

        private Song BuildSong()
        {
            Enum.TryParse<InstrumentType>(_instrumentDropdown.SelectedItem, out var instrument);

            var song = new Song
            {
                Name = _titleInput.Text.Trim(),
                Artist = string.IsNullOrWhiteSpace(_artistInput.Text) ? "Unknown" : _artistInput.Text.Trim(),
                Transcriber = string.IsNullOrWhiteSpace(_transcriberInput.Text) ? null : _transcriberInput.Text.Trim(),
                Instrument = instrument,
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
            _parseStatusLabel.Text = "";
        }

        protected override void DisposeControl()
        {
            _importButton.Click -= OnImportClicked;
            _cancelButton.Click -= OnCancelClicked;
            _pasteButton.Click -= OnPasteClicked;

            _titleInput?.Dispose();
            _artistInput?.Dispose();
            _transcriberInput?.Dispose();
            _instrumentDropdown?.Dispose();
            _pasteButton?.Dispose();
            _importButton?.Dispose();
            _cancelButton?.Dispose();

            base.DisposeControl();
        }
    }
}
