using System;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Controls.Effects;
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

            // Play button (right side, vertically centered)
            public const int PlayButtonWidth = 40;
            public const int PlayButtonHeight = 26;
            public const int PlayButtonY = (Height - PlayButtonHeight) / 2;
            public const int PlayButtonRightMargin = 15;

            // Labels width = card width - LabelRightMargin
            public static int LabelRightMargin => PlayButtonWidth + PlayButtonRightMargin;
        }

        public event EventHandler<MouseEventArgs> PlayClicked;
        public event EventHandler<MouseEventArgs> CardClicked;
        public event EventHandler DeleteRequested;

        private readonly Song _song;
        private readonly Panel _indicator;
        private readonly Label _instrumentLabel;
        private readonly Label _titleLabel;
        private readonly Label _artistLabel;
        private readonly StandardButton _playButton;
        private readonly ScrollingHighlightEffect _highlightEffect;

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
            BackgroundColor = MaestroTheme.PanelBackground;

            _highlightEffect = new ScrollingHighlightEffect(this);
            EffectBehind = _highlightEffect;

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
                TextColor = MaestroTheme.CreamWhite
            };

            _artistLabel = new Label
            {
                Parent = this,
                Text = song.Artist,
                Location = new Point(Layout.LabelsX, Layout.ArtistY),
                Width = width - Layout.LabelRightMargin,
                Font = GameService.Content.DefaultFont12,
                TextColor = MaestroTheme.MutedCream
            };

            _playButton = new StandardButton
            {
                Parent = this,
                Text = ">",
                Location = new Point(width - Layout.PlayButtonWidth - Layout.PlayButtonRightMargin, Layout.PlayButtonY),
                Width = Layout.PlayButtonWidth
            };
            _playButton.Click += (s, e) => PlayClicked?.Invoke(this, e);

            if (song.IsUserImported)
            {
                var contextMenu = new ContextMenuStrip();
                var deleteItem = contextMenu.AddMenuItem("Delete Song");
                deleteItem.Click += (s, e) => DeleteRequested?.Invoke(this, EventArgs.Empty);
                Menu = contextMenu;

                const string tooltip = "Right-click for options";
                BasicTooltipText = tooltip;
                _indicator.BasicTooltipText = tooltip;
                _instrumentLabel.BasicTooltipText = tooltip;
                _titleLabel.BasicTooltipText = tooltip;
                _artistLabel.BasicTooltipText = tooltip;
            }

            Click += (s, e) => CardClicked?.Invoke(this, e);
        }

        private void UpdateVisualState()
        {
            _highlightEffect.ForceActive = _isSelected;
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
            _artistLabel?.Dispose();
            _playButton?.Dispose();

            base.DisposeControl();
        }
    }
}
