using System;
using System.Collections.Generic;
using System.Linq;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Maestro.Models;
using Maestro.Services.Community;
using Maestro.UI.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maestro.UI.Community
{
    public class UploadWindow : StandardWindow
    {
        private static class Layout
        {
            public const int WindowWidth = 420;
            public const int WindowHeight = 355;
            public const int ContentWidth = 390;
            public const int LabelWidth = 90;
            public const int ValueWidth = 280;
            public const int RowHeight = 24;
        }

        public event EventHandler<UploadResponse> UploadCompleted;

        private readonly CommunityUploadService _uploadService;
        private readonly List<Song> _allSongs;
        private List<Song> _uploadableSongs;

        private CustomDropdown _songSelector;
        private Label _nameLabel;
        private Label _artistLabel;
        private Label _transcriberLabel;
        private Label _instrumentLabel;
        private Label _noteCountLabel;
        private Label _remainingUploadsLabel;

        private Panel _validationPanel;
        private Label _nameValidation;
        private Label _transcriberValidation;
        private Label _instrumentValidation;
        private Label _notesValidation;
        private Label _duplicateValidation;
        private Label _rateLimitValidation;

        private StandardButton _uploadButton;
        private StandardButton _cancelButton;
        private LoadingSpinner _loadingSpinner;
        private Label _statusIcon;
        private Label _statusLabel;

        private Song _selectedSong;
        private bool _isUploading;

        private static Texture2D _backgroundTexture;

        private static Texture2D GetBackground()
        {
            return _backgroundTexture ?? (_backgroundTexture = MaestroTheme.CreateWindowBackground(Layout.WindowWidth, Layout.WindowHeight));
        }

        public UploadWindow(CommunityUploadService uploadService, List<Song> songs)
            : base(
                GetBackground(),
                new Rectangle(0, 0, Layout.WindowWidth, Layout.WindowHeight),
                new Rectangle(15, MaestroTheme.WindowContentTopPadding, Layout.ContentWidth, Layout.WindowHeight))
        {
            _uploadService = uploadService;
            _allSongs = songs;
            _uploadableSongs = FilterUploadableSongs();

            Title = "Upload to Community";
            Emblem = Module.Instance.ContentsManager.GetTexture("upload-emblem.png");
            SavesPosition = true;
            Id = "UploadWindow_v1";
            CanResize = false;
            Parent = GameService.Graphics.SpriteScreen;

            BuildUi();
            SubscribeToEvents();
        }

        private void BuildUi()
        {
            var currentY = MaestroTheme.PaddingContentTop;

            // Song selector
            CreateLabel("Select Song:", 0, currentY);
            _songSelector = new CustomDropdown
            {
                Parent = this,
                Location = new Point(Layout.LabelWidth, currentY),
                Size = new Point(Layout.ValueWidth, 27)
            };

            if (_uploadableSongs.Count == 0)
            {
                _songSelector.AddItem("No songs available");
                _songSelector.Enabled = false;
            }
            else
            {
                foreach (var song in _uploadableSongs)
                {
                    var fullText = $"{song.Name} - {song.Transcriber ?? "Unknown"}";
                    _songSelector.AddItem(fullText, fullText, song);
                }
            }
            _songSelector.ValueChanged += OnSongSelected;
            currentY += Layout.RowHeight + MaestroTheme.InputSpacing + 5;

            // Song details panel
            var detailsPanel = new Panel
            {
                Parent = this,
                Location = new Point(0, currentY),
                Size = new Point(Layout.ContentWidth, 95),
                ShowBorder = true,
                CanScroll = true
            };

            var detailY = 8;
            _nameLabel = CreateDetailRow(detailsPanel, "Name:", ref detailY);
            _artistLabel = CreateDetailRow(detailsPanel, "Artist:", ref detailY);
            _transcriberLabel = CreateDetailRow(detailsPanel, "Transcriber:", ref detailY);
            _instrumentLabel = CreateDetailRow(detailsPanel, "Instrument:", ref detailY);
            _noteCountLabel = CreateDetailRow(detailsPanel, "Notes:", ref detailY);

            currentY += 95 + MaestroTheme.InputSpacing;

            // Validation panel
            CreateLabel("Validation:", 0, currentY);
            currentY += Layout.RowHeight;

            _validationPanel = new Panel
            {
                Parent = this,
                Location = new Point(0, currentY),
                Size = new Point(Layout.ContentWidth, 95),
                ShowBorder = true,
                CanScroll = true
            };

            var valY = 5;
            _nameValidation = CreateValidationRow(_validationPanel, "Name (min 3 chars)", ref valY);
            _transcriberValidation = CreateValidationRow(_validationPanel, "Transcriber (min 2 chars)", ref valY);
            _instrumentValidation = CreateValidationRow(_validationPanel, "Instrument selected", ref valY);
            _notesValidation = CreateValidationRow(_validationPanel, "At least 10 notes", ref valY);
            _duplicateValidation = CreateValidationRow(_validationPanel, "Not a duplicate", ref valY);
            _rateLimitValidation = CreateValidationRow(_validationPanel, "Upload limit OK", ref valY);

            currentY += 95 + MaestroTheme.InputSpacing;

            // Remaining uploads indicator
            _remainingUploadsLabel = new Label
            {
                Parent = this,
                Location = new Point(0, currentY),
                Width = Layout.ContentWidth,
                Text = $"Uploads remaining today: {_uploadService.GetRemainingUploads()}/3",
                TextColor = MaestroTheme.MutedCream
            };
            currentY += Layout.RowHeight + MaestroTheme.InputSpacing;

            // Status and loading
            _loadingSpinner = new LoadingSpinner
            {
                Parent = this,
                Location = new Point(0, currentY),
                Size = new Point(20, 20),
                Visible = false
            };

            _statusIcon = new Label
            {
                Parent = this,
                Location = new Point(2, currentY),
                AutoSizeWidth = true,
                Text = "",
                Visible = false
            };

            _statusLabel = new Label
            {
                Parent = this,
                Location = new Point(25, currentY),
                Width = Layout.ContentWidth - 25,
                Text = "",
                TextColor = MaestroTheme.LightGray
            };
            currentY += MaestroTheme.InputSpacing;

            // Buttons
            _uploadButton = new StandardButton
            {
                Parent = this,
                Text = "Upload",
                Location = new Point(Layout.ContentWidth - MaestroTheme.ActionButtonWidth * 2 - 10, currentY),
                Size = new Point(MaestroTheme.ActionButtonWidth, MaestroTheme.ActionButtonHeight),
                Enabled = false
            };
            _uploadButton.Click += OnUploadClicked;

            _cancelButton = new StandardButton
            {
                Parent = this,
                Text = "Cancel",
                Location = new Point(Layout.ContentWidth - MaestroTheme.ActionButtonWidth, currentY),
                Size = new Point(MaestroTheme.ActionButtonWidth, MaestroTheme.ActionButtonHeight)
            };
            _cancelButton.Click += OnCancelClicked;

            // Initial update if songs available
            if (_uploadableSongs.Count > 0)
            {
                _selectedSong = _uploadableSongs[0];
                UpdateSongDetails();
                UpdateValidation();
            }
        }

        private Label CreateLabel(string text, int x, int y)
        {
            return new Label
            {
                Parent = this,
                Text = text,
                Location = new Point(x, y + 3),
                AutoSizeWidth = true,
                TextColor = MaestroTheme.CreamWhite
            };
        }

        private Label CreateDetailRow(Panel parent, string labelText, ref int y)
        {
            new Label
            {
                Parent = parent,
                Text = labelText,
                Location = new Point(10, y),
                Width = 80,
                TextColor = MaestroTheme.LightGray
            };

            var valueLabel = new Label
            {
                Parent = parent,
                Text = "-",
                Location = new Point(90, y),
                Width = Layout.ContentWidth - 100,
                TextColor = MaestroTheme.CreamWhite
            };

            y += 17;
            return valueLabel;
        }

        private Label CreateValidationRow(Panel parent, string text, ref int y)
        {
            var label = new Label
            {
                Parent = parent,
                Text = $"[ ] {text}",
                Location = new Point(10, y),
                Width = Layout.ContentWidth - 20,
                TextColor = MaestroTheme.LightGray
            };

            y += 15;
            return label;
        }

        private void SubscribeToEvents()
        {
            _uploadService.UploadProgressChanged += OnUploadProgressChanged;
        }

        private void OnSongSelected(object sender, ValueChangedEventArgs e)
        {
            var selectedItem = _songSelector.SelectedItem;
            if (selectedItem?.Value is Song song)
            {
                _selectedSong = song;
                UpdateSongDetails();
                UpdateValidation();
            }
        }


        private void UpdateSongDetails()
        {
            if (_selectedSong == null)
            {
                _nameLabel.Text = "-";
                _artistLabel.Text = "-";
                _transcriberLabel.Text = "-";
                _instrumentLabel.Text = "-";
                _noteCountLabel.Text = "-";
                return;
            }

            _nameLabel.Text = _selectedSong.Name ?? "-";
            _artistLabel.Text = _selectedSong.Artist ?? "-";
            _transcriberLabel.Text = _selectedSong.Transcriber ?? "(not set)";
            _instrumentLabel.Text = _selectedSong.Instrument.ToString();
            _noteCountLabel.Text = GetNoteCount(_selectedSong).ToString();
        }

        private void UpdateValidation()
        {
            _remainingUploadsLabel.Text = $"Uploads remaining today: {_uploadService.GetRemainingUploads()}/3";

            if (_selectedSong == null)
            {
                _uploadButton.Enabled = false;
                return;
            }

            var validation = _uploadService.ValidateSong(_selectedSong);

            UpdateValidationLabel(_nameValidation, "Name (min 3 chars)", validation.NameValid, validation.NameError);
            UpdateValidationLabel(_transcriberValidation, "Transcriber (min 2 chars)", validation.TranscriberValid, validation.TranscriberError);
            UpdateValidationLabel(_instrumentValidation, "Instrument selected", validation.InstrumentValid, validation.InstrumentError);
            UpdateValidationLabel(_notesValidation, "At least 10 notes", validation.NotesValid, validation.NotesError);
            UpdateValidationLabel(_duplicateValidation, "Not a duplicate", !validation.IsDuplicate, validation.DuplicateError);
            UpdateValidationLabel(_rateLimitValidation, "Upload limit OK", !validation.RateLimitExceeded, validation.RateLimitError);

            _uploadButton.Enabled = validation.IsValid && !_isUploading;
        }

        private void UpdateValidationLabel(Label label, string text, bool isValid, string error)
        {
            var checkmark = isValid ? "[X]" : "[ ]";
            label.Text = $"{checkmark} {text}";
            label.TextColor = isValid ? MaestroTheme.Playing : MaestroTheme.Error;

            if (!isValid && !string.IsNullOrEmpty(error))
            {
                label.BasicTooltipText = error;
            }
            else
            {
                label.BasicTooltipText = null;
            }
        }

        private int GetNoteCount(Song song)
        {
            if (song.Notes != null && song.Notes.Count > 0)
            {
                return song.Notes.Count(n => !n.StartsWith("R:"));
            }

            return song.Commands.Count(c => c.Type != CommandType.Wait);
        }

        private async void OnUploadClicked(object sender, MouseEventArgs e)
        {
            if (_selectedSong == null || _isUploading) return;

            _isUploading = true;
            _uploadButton.Enabled = false;
            _songSelector.Enabled = false;
            _loadingSpinner.Visible = true;
            _statusIcon.Visible = false;

            try
            {
                var response = await _uploadService.UploadSongAsync(_selectedSong);

                if (response.Success)
                {
                    _loadingSpinner.Visible = false;
                    _statusIcon.Text = "[X]";
                    _statusIcon.TextColor = MaestroTheme.Playing;
                    _statusIcon.Visible = true;

                    ScreenNotification.ShowNotification("Song submitted successfully!", ScreenNotification.NotificationType.Info);
                    UploadCompleted?.Invoke(this, response);
                    RefreshSongList();
                }
                else
                {
                    _loadingSpinner.Visible = false;
                    _statusIcon.Text = "[X]";
                    _statusIcon.TextColor = MaestroTheme.Error;
                    _statusIcon.Visible = true;
                    _statusLabel.Text = response.Error ?? "Upload failed";
                    _statusLabel.TextColor = MaestroTheme.Error;
                    ScreenNotification.ShowNotification($"Upload failed: {response.Error}", ScreenNotification.NotificationType.Error);
                }
            }
            finally
            {
                _isUploading = false;
                _loadingSpinner.Visible = false;
                _songSelector.Enabled = true;
                UpdateValidation();
            }
        }

        private void OnUploadProgressChanged(object sender, UploadProgressEventArgs e)
        {
            _statusLabel.Text = e.Message;

            switch (e.State)
            {
                case UploadState.Completed:
                    _statusLabel.TextColor = MaestroTheme.Playing;
                    _loadingSpinner.Visible = false;
                    _statusIcon.Text = "[X]";
                    _statusIcon.TextColor = MaestroTheme.Playing;
                    _statusIcon.Visible = true;
                    break;
                case UploadState.Failed:
                case UploadState.Cancelled:
                    _statusLabel.TextColor = MaestroTheme.Error;
                    _loadingSpinner.Visible = false;
                    _statusIcon.Visible = false;
                    break;
                default:
                    _statusLabel.TextColor = MaestroTheme.LightGray;
                    break;
            }
        }

        private void OnCancelClicked(object sender, MouseEventArgs e)
        {
            Hide();
        }

        public override void Show()
        {
            RefreshSongList();
            ClearStatus();
            base.Show();
        }

        private List<Song> FilterUploadableSongs()
        {
            return _allSongs
                .Where(s => (s.IsUserImported || s.IsCreated) && !s.IsCommunityDownloaded && !s.IsUploaded)
                .ToList();
        }

        internal void RefreshSongList()
        {
            _uploadableSongs = FilterUploadableSongs();

            _songSelector.ValueChanged -= OnSongSelected;
            _songSelector.ClearItems();

            if (_uploadableSongs.Count == 0)
            {
                _songSelector.AddItem("No songs available");
                _songSelector.Enabled = false;
                _selectedSong = null;
            }
            else
            {
                foreach (var song in _uploadableSongs)
                {
                    var fullText = $"{song.Name} - {song.Transcriber ?? "Unknown"}";
                    _songSelector.AddItem(fullText, fullText, song);
                }
                _songSelector.Enabled = true;
                _selectedSong = _uploadableSongs[0];
            }

            _songSelector.ValueChanged += OnSongSelected;
            UpdateSongDetails();
            UpdateValidation();
        }

        private void ClearStatus()
        {
            _statusLabel.Text = "";
            _statusLabel.TextColor = MaestroTheme.LightGray;
            _statusIcon.Visible = false;
            _loadingSpinner.Visible = false;
        }

        protected override void DisposeControl()
        {
            _uploadService.UploadProgressChanged -= OnUploadProgressChanged;

            _songSelector.ValueChanged -= OnSongSelected;
            _uploadButton.Click -= OnUploadClicked;
            _cancelButton.Click -= OnCancelClicked;

            _songSelector?.Dispose();
            _validationPanel?.Dispose();
            _uploadButton?.Dispose();
            _cancelButton?.Dispose();
            _loadingSpinner?.Dispose();
            _statusIcon?.Dispose();
            _statusLabel?.Dispose();
            _remainingUploadsLabel?.Dispose();

            base.DisposeControl();
        }
    }
}
