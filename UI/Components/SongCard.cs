using System;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Maestro.Models;
using Microsoft.Xna.Framework;

namespace Maestro.UI.Components
{
    public class SongCard : Panel
    {
        public static class Layout
        {
            public const int Height = 70;

            // Indicator (left color bar)
            public const int IndicatorWidth = 4;

            // Content positioning
            public const int LabelsX = 12;
            public const int InstrumentY = 4;
            public const int TitleY = 22;
            public const int ArtistY = 40;

            // Play button (right side)
            public const int PlayButtonWidth = 40;
            public const int PlayButtonY = 14;
            public const int PlayButtonRightMargin = 15;

            // Labels width = card width - LabelRightMargin
            public static int LabelRightMargin => PlayButtonWidth + PlayButtonRightMargin;
        }

        public event EventHandler<MouseEventArgs> PlayClicked;
        public event EventHandler<MouseEventArgs> CardClicked;

        private readonly Song _song;
        private readonly Panel _indicator;
        private readonly Label _instrumentLabel;
        private readonly Label _titleLabel;
        private readonly Label _artistLabel;
        private readonly StandardButton _playButton;

        private bool _isSelected;
        private bool _isPlaying;

        public Song Song => _song;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                UpdateVisualState();
            }
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                _isPlaying = value;
                UpdateVisualState();
            }
        }

        public SongCard(Song song, int width)
        {
            _song = song;

            Size = new Point(width, Layout.Height);
            BackgroundColor = MaestroColors.PanelBackground;

            var instrumentColor = GetInstrumentColor(song.Instrument);

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
                TextColor = MaestroColors.CreamWhite
            };

            _artistLabel = new Label
            {
                Parent = this,
                Text = song.Artist,
                Location = new Point(Layout.LabelsX, Layout.ArtistY),
                Width = width - Layout.LabelRightMargin,
                Font = GameService.Content.DefaultFont12,
                TextColor = MaestroColors.MutedCream
            };

            _playButton = new StandardButton
            {
                Parent = this,
                Text = ">",
                Location = new Point(width - Layout.PlayButtonWidth - Layout.PlayButtonRightMargin, Layout.PlayButtonY),
                Width = Layout.PlayButtonWidth
            };
            _playButton.Click += (s, e) => PlayClicked?.Invoke(this, e);

            Click += (s, e) => CardClicked?.Invoke(this, e);

            MouseEntered += OnMouseEntered;
            MouseLeft += OnMouseLeft;
        }

        private void OnMouseEntered(object sender, MouseEventArgs e)
        {
            if (!_isSelected)
                BackgroundColor = MaestroColors.PanelHover;
        }

        private void OnMouseLeft(object sender, MouseEventArgs e)
        {
            if (!_isSelected)
                BackgroundColor = MaestroColors.PanelBackground;
        }

        private void UpdateVisualState()
        {
            if (_isSelected)
                BackgroundColor = MaestroColors.PanelSelected;
            else
                BackgroundColor = MaestroColors.PanelBackground;
        }

        private static Color GetInstrumentColor(InstrumentType instrument)
        {
            switch (instrument)
            {
                case InstrumentType.Piano: return MaestroColors.Piano;
                case InstrumentType.Harp: return MaestroColors.Harp;
                case InstrumentType.Lute: return MaestroColors.Lute;
                case InstrumentType.Bass: return MaestroColors.Bass;
                default: return MaestroColors.AmberGold;
            }
        }

        protected override void DisposeControl()
        {
            MouseEntered -= OnMouseEntered;
            MouseLeft -= OnMouseLeft;

            _indicator?.Dispose();
            _instrumentLabel?.Dispose();
            _titleLabel?.Dispose();
            _artistLabel?.Dispose();
            _playButton?.Dispose();

            base.DisposeControl();
        }
    }
}
