using System;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Controls.Effects;
using Blish_HUD.Input;
using Maestro.Models;
using Maestro.Services.Community;
using Microsoft.Xna.Framework;

namespace Maestro.UI.Components
{
    public class CommunitySongCard : Panel
    {
        public static class Layout
        {
            public const int Height = 70;
            public const int IndicatorWidth = 4;
            public const int LabelsX = 12;
            public const int InstrumentY = 4;
            public const int TitleY = 22;
            public const int DetailsY = 40;
            public const int ButtonWidth = 50;
            public const int ButtonHeight = 26;
            public const int ButtonY = (Height - ButtonHeight) / 2;
            public const int ButtonRightMargin = 15;
            public static int LabelRightMargin => ButtonWidth + ButtonRightMargin + 10;
        }

        public event EventHandler<CommunitySong> DownloadRequested;
        public event EventHandler<CommunitySong> DeleteRequested;

        private readonly CommunitySong _song;
        private readonly Panel _indicator;
        private readonly Label _instrumentLabel;
        private readonly Label _titleLabel;
        private readonly Label _detailsLabel;
        private readonly StandardButton _actionButton;
        private readonly Label _progressLabel;
        private readonly ScrollingHighlightEffect _highlightEffect;
        private readonly ContextMenuStrip _contextMenu;

        private DownloadState _downloadState;
        private int _downloadProgress;

        public CommunitySong Song => _song;

        public bool IsDownloaded
        {
            get => _downloadState == DownloadState.Completed;
            set
            {
                if (value)
                {
                    _downloadState = DownloadState.Completed;
                    UpdateVisualState();
                }
            }
        }

        public CommunitySongCard(CommunitySong song, int width, bool isDownloaded = false)
        {
            _song = song;
            _downloadState = isDownloaded ? DownloadState.Completed : DownloadState.Idle;

            Size = new Point(width, Layout.Height);
            BackgroundColor = MaestroTheme.PanelBackground;

            _highlightEffect = new ScrollingHighlightEffect(this);
            EffectBehind = _highlightEffect;

            var instrumentColor = GetInstrumentColor(song.InstrumentType);

            _indicator = new Panel
            {
                Parent = this,
                Location = new Point(0, 0),
                Size = new Point(Layout.IndicatorWidth, Layout.Height),
                BackgroundColor = instrumentColor
            };

            _instrumentLabel = new Label
            {
                Parent = this,
                Text = $"[{song.Instrument}]",
                Location = new Point(Layout.LabelsX, Layout.InstrumentY),
                Font = GameService.Content.DefaultFont12,
                TextColor = instrumentColor
            };

            _titleLabel = new Label
            {
                Parent = this,
                Text = song.Name,
                Location = new Point(Layout.LabelsX, Layout.TitleY),
                Width = width - Layout.LabelRightMargin,
                Font = GameService.Content.DefaultFont14,
                TextColor = MaestroTheme.CreamWhite
            };

            var detailsText = $"{song.Artist} - {song.Transcriber}";
            if (!string.IsNullOrEmpty(song.DisplayDownloads))
                detailsText += $" | {song.DisplayDownloads} downloads";
            if (!string.IsNullOrEmpty(song.DisplayDuration))
                detailsText += $" | {song.DisplayDuration}";

            _detailsLabel = new Label
            {
                Parent = this,
                Text = detailsText,
                Location = new Point(Layout.LabelsX, Layout.DetailsY),
                Width = width - Layout.LabelRightMargin,
                Font = GameService.Content.DefaultFont12,
                TextColor = MaestroTheme.MutedCream
            };

            _actionButton = new StandardButton
            {
                Parent = this,
                Text = "DL",
                Location = new Point(width - Layout.ButtonWidth - Layout.ButtonRightMargin, Layout.ButtonY),
                Width = Layout.ButtonWidth
            };
            _actionButton.Click += OnActionButtonClicked;

            _progressLabel = new Label
            {
                Parent = this,
                Text = "",
                Location = new Point(width - Layout.ButtonWidth - Layout.ButtonRightMargin, Layout.ButtonY + 5),
                Width = Layout.ButtonWidth,
                Height = Layout.ButtonHeight,
                Font = GameService.Content.DefaultFont12,
                TextColor = MaestroTheme.CreamWhite,
                HorizontalAlignment = HorizontalAlignment.Center,
                Visible = false
            };

            _contextMenu = new ContextMenuStrip();
            var deleteItem = _contextMenu.AddMenuItem("Delete Song");
            deleteItem.Click += (s, e) => DeleteRequested?.Invoke(this, _song);

            UpdateVisualState();
        }

        private void OnActionButtonClicked(object sender, MouseEventArgs e)
        {
            if (_downloadState == DownloadState.Idle)
            {
                DownloadRequested?.Invoke(this, _song);
            }
        }

        public void UpdateDownloadProgress(int progress, DownloadState state)
        {
            _downloadProgress = progress;
            _downloadState = state;
            UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            var isDownloaded = _downloadState == DownloadState.Completed;
            Menu = isDownloaded ? _contextMenu : null;

            const string tooltip = "Right-click for options";
            BasicTooltipText = isDownloaded ? tooltip : null;
            _indicator.BasicTooltipText = isDownloaded ? tooltip : null;
            _instrumentLabel.BasicTooltipText = isDownloaded ? tooltip : null;
            _titleLabel.BasicTooltipText = isDownloaded ? tooltip : null;
            _detailsLabel.BasicTooltipText = isDownloaded ? tooltip : null;

            switch (_downloadState)
            {
                case DownloadState.Idle:
                    _actionButton.Text = "v";
                    _actionButton.Visible = true;
                    _actionButton.Enabled = true;
                    _progressLabel.Visible = false;
                    break;

                case DownloadState.Downloading:
                    _actionButton.Visible = false;
                    _progressLabel.Text = $"{_downloadProgress}%";
                    _progressLabel.Visible = true;
                    break;

                case DownloadState.Completed:
                    _actionButton.Text = "v";
                    _actionButton.Visible = true;
                    _actionButton.Enabled = false;
                    _progressLabel.Visible = false;
                    break;

                case DownloadState.Failed:
                    _actionButton.Text = "Retry";
                    _actionButton.Visible = true;
                    _actionButton.Enabled = true;
                    _progressLabel.Visible = false;
                    break;

                case DownloadState.Cancelled:
                    _actionButton.Text = "v";
                    _actionButton.Visible = true;
                    _actionButton.Enabled = true;
                    _progressLabel.Visible = false;
                    break;
            }
        }

        private static Color GetInstrumentColor(InstrumentType instrument)
        {
            switch (instrument)
            {
                case InstrumentType.Piano: return MaestroTheme.Piano;
                case InstrumentType.Harp: return MaestroTheme.Harp;
                case InstrumentType.Lute: return MaestroTheme.Lute;
                case InstrumentType.Bass: return MaestroTheme.Bass;
                default: return MaestroTheme.AmberGold;
            }
        }

        protected override void DisposeControl()
        {
            _indicator?.Dispose();
            _instrumentLabel?.Dispose();
            _titleLabel?.Dispose();
            _detailsLabel?.Dispose();
            _actionButton?.Dispose();
            _progressLabel?.Dispose();
            _contextMenu?.Dispose();

            base.DisposeControl();
        }
    }
}
