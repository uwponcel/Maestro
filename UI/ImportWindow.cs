using System;
using System.Collections.Generic;
using Blish_HUD;
using Blish_HUD.Controls;
using Maestro.Models;
using Maestro.Services.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maestro.UI
{
    public class ImportWindow : StandardWindow
    {
        private static class Layout
        {
            public const int WindowWidth = 420;
            public const int WindowHeight = 380;

            public const int ContentWidth = 390;
            public const int ContentHeight = 380;
            
            public const int InputX = 90;
            public const int InputWidth = 290;
            public const int RowHeight = 28;

            public const int ScriptAreaHeight = 140;
        }

        public event EventHandler<Song> SongImported;

        private readonly TextBox _titleInput;
        private readonly TextBox _artistInput;
        private readonly TextBox _transcriberInput;
        private readonly Dropdown _instrumentDropdown;
        private readonly Panel _scriptContainer;
        private readonly MultilineTextBox _scriptInput;
        private readonly StandardButton _importButton;
        private readonly StandardButton _cancelButton;

        private static Texture2D _backgroundTexture;

        private static Texture2D GetBackground()
        {
            return _backgroundTexture ?? (_backgroundTexture = MaestroTheme.CreateWindowBackground(Layout.WindowWidth, Layout.WindowHeight));
        }

        public ImportWindow()
            : base(
                GetBackground(),
                new Rectangle(0, 0, Layout.WindowWidth, Layout.WindowHeight),
                new Rectangle(15, 30, Layout.ContentWidth, Layout.ContentHeight))
        {
            Title = "Import Song";
            Subtitle = "AHK v1 Format";
            Emblem = Module.Instance.ContentsManager.GetTexture("import.png");
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
            currentY += Layout.RowHeight + MaestroTheme.InputSpacing;

            CreateLabel("AHK Script:", 0, currentY);
            currentY += 28;

            _scriptContainer = new Panel
            {
                Parent = this,
                Location = new Point(0, currentY),
                Size = new Point(Layout.ContentWidth, Layout.ScriptAreaHeight),
                CanScroll = true,
                ShowBorder = true
            };

            _scriptInput = new MultilineTextBox
            {
                Parent = _scriptContainer,
                Location = new Point(0, 0),
                Size = new Point(Layout.ContentWidth - 20, 600),
                PlaceholderText = "Paste AHK v1 script here...",
                HideBackground = true
            };
            currentY += Layout.ScriptAreaHeight + MaestroTheme.InputSpacing;

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

        private void OnImportClicked(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            if (!ValidateInput())
                return;

            var song = ParseSong();
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

            if (string.IsNullOrWhiteSpace(_scriptInput.Text))
            {
                ScreenNotification.ShowNotification("Please paste the AHK script", ScreenNotification.NotificationType.Error);
                return false;
            }

            return true;
        }

        private Song ParseSong()
        {
            try
            {
                var notes = AhkParser.ParseToCompact(_scriptInput.Text);

                if (notes.Count == 0)
                {
                    ScreenNotification.ShowNotification("No notes found in AHK script", ScreenNotification.NotificationType.Error);
                    return null;
                }

                Enum.TryParse<InstrumentType>(_instrumentDropdown.SelectedItem, out var instrument);

                var song = new Song
                {
                    Name = _titleInput.Text.Trim(),
                    Artist = string.IsNullOrWhiteSpace(_artistInput.Text) ? "Unknown" : _artistInput.Text.Trim(),
                    Transcriber = string.IsNullOrWhiteSpace(_transcriberInput.Text) ? null : _transcriberInput.Text.Trim(),
                    Instrument = instrument,
                    IsUserImported = true
                };

                song.Notes.AddRange(notes);
                var commands = NoteParser.Parse(notes);
                song.Commands.AddRange(commands);

                ScreenNotification.ShowNotification($"Imported {notes.Count} notes, {song.Commands.Count} commands", ScreenNotification.NotificationType.Info);
                return song;
            }
            catch (Exception ex)
            {
                ScreenNotification.ShowNotification($"Parse error: {ex.Message}", ScreenNotification.NotificationType.Error);
                return null;
            }
        }

        private void ClearInputs()
        {
            _titleInput.Text = string.Empty;
            _artistInput.Text = string.Empty;
            _transcriberInput.Text = string.Empty;
            _scriptInput.Text = string.Empty;
            _instrumentDropdown.SelectedItem = "Harp";
        }

        protected override void DisposeControl()
        {
            _importButton.Click -= OnImportClicked;
            _cancelButton.Click -= OnCancelClicked;

            _titleInput?.Dispose();
            _artistInput?.Dispose();
            _transcriberInput?.Dispose();
            _instrumentDropdown?.Dispose();
            _scriptInput?.Dispose();
            _scriptContainer?.Dispose();
            _importButton?.Dispose();
            _cancelButton?.Dispose();

            base.DisposeControl();
        }
    }
}
