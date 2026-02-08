using System;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maestro.UI.MaestroCreator
{
    public class NoteChip : Panel
    {
        private static class Layout
        {
            public const int Height = 24;
            public const int FixedWidth = 118;
            public const int CloseButtonSize = 16;
            public const int Padding = 4;
            public const int CloseButtonMargin = 2;
        }

        private const int BORDER_THICKNESS = 2;

        public event EventHandler RemoveClicked;
        public event EventHandler<MouseEventArgs> ChipClicked;

        public string NoteString { get; }
        public int Index { get; set; }

        private readonly Label _noteLabel;
        private readonly Label _closeButton;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; UpdateVisualState(); }
        }

        public NoteChip(string noteString, int index)
        {
            NoteString = noteString;
            Index = index;

            var baseColor = GetNoteColor(noteString);

            Size = new Point(Layout.FixedWidth, Layout.Height);
            BackgroundColor = baseColor;

            // Calculate max text width and truncate if needed
            var maxTextWidth = Layout.FixedWidth - Layout.CloseButtonSize - Layout.Padding * 2 - Layout.CloseButtonMargin;
            var maxChars = maxTextWidth / 7;  // ~7px per char
            var displayText = noteString;
            var needsTooltip = false;

            if (noteString.Length > maxChars)
            {
                displayText = noteString.Substring(0, maxChars - 2) + "..";
                needsTooltip = true;
            }

            _noteLabel = new Label
            {
                Parent = this,
                Text = displayText,
                Location = new Point(Layout.Padding, 2),
                Size = new Point(maxTextWidth, Layout.Height - 4),
                Font = GameService.Content.DefaultFont12,
                TextColor = MaestroTheme.CreamWhite,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Middle,
                BasicTooltipText = needsTooltip ? noteString : null
            };

            _closeButton = new Label
            {
                Parent = this,
                Text = "×",
                Location = new Point(Layout.FixedWidth - Layout.CloseButtonSize - Layout.CloseButtonMargin, Layout.CloseButtonMargin),
                Size = new Point(Layout.CloseButtonSize, Layout.Height - Layout.Padding),
                Font = GameService.Content.DefaultFont14,
                TextColor = MaestroTheme.MutedCream,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Middle
            };

            _closeButton.MouseEntered += (s, e) => _closeButton.TextColor = MaestroTheme.Error;
            _closeButton.MouseLeft += (s, e) => _closeButton.TextColor = MaestroTheme.MutedCream;
            _closeButton.LeftMouseButtonReleased += (s, e) => RemoveClicked?.Invoke(this, EventArgs.Empty);

            // Hover effect for the whole chip
            MouseEntered += (s, e) => BackgroundColor = MaestroTheme.WithAlpha(baseColor, 255);
            MouseLeft += (s, e) => BackgroundColor = baseColor;
        }

        protected override void OnClick(MouseEventArgs e)
        {
            // Only fire ChipClicked if click is outside the close button area
            const int closeButtonX = Layout.FixedWidth - Layout.CloseButtonSize - Layout.CloseButtonMargin;
            if (RelativeMousePosition.X < closeButtonX)
            {
                ChipClicked?.Invoke(this, e);
            }

            base.OnClick(e);
        }

        private void UpdateVisualState()
        {
            Invalidate();
        }

        private static Color GetNoteColor(string noteString)
        {
            if (noteString.StartsWith("R"))
                return MaestroTheme.ChipRest;

            if (noteString.Contains("-"))
                return MaestroTheme.ChipLowerOctave;

            if (noteString.Contains("+"))
                return MaestroTheme.ChipUpperOctave;

            return MaestroTheme.ChipMiddleOctave;
        }

        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds)
        {
            base.PaintBeforeChildren(spriteBatch, bounds);

            if (!_isSelected) return;

            var borderColor = MaestroTheme.AmberGold;
            var pixel = ContentService.Textures.Pixel;

            // Top
            spriteBatch.DrawOnCtrl(this, pixel, new Rectangle(0, 0, bounds.Width, BORDER_THICKNESS), borderColor);
            // Bottom
            spriteBatch.DrawOnCtrl(this, pixel, new Rectangle(0, bounds.Height - BORDER_THICKNESS, bounds.Width, BORDER_THICKNESS), borderColor);
            // Left
            spriteBatch.DrawOnCtrl(this, pixel, new Rectangle(0, 0, BORDER_THICKNESS, bounds.Height), borderColor);
            // Right
            spriteBatch.DrawOnCtrl(this, pixel, new Rectangle(bounds.Width - BORDER_THICKNESS, 0, BORDER_THICKNESS, bounds.Height), borderColor);
        }

        protected override void DisposeControl()
        {
            _noteLabel?.Dispose();
            _closeButton?.Dispose();
            base.DisposeControl();
        }
    }
}
