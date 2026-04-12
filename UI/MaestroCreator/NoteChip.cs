using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maestro.UI.MaestroCreator
{
    public class NoteChip : BaseChip
    {
        public static class Layout
        {
            public const int Height = 26;
            public const int FixedWidth = 114;
            public const int CloseButtonSize = 16;
            public const int Padding = 4;
            public const int CloseButtonMargin = 2;
        }

        public string NoteString { get; }

        private readonly Label _noteLabel;
        private readonly Label _closeButton;
        private readonly Color _chipColor;
        private readonly Color _hoverColor;

        public NoteChip(string noteString, int index)
        {
            NoteString = noteString;
            Index = index;

            _chipColor = GetNoteColor(noteString);
            _hoverColor = MaestroTheme.Brighten(_chipColor);
            _currentColor = _chipColor;

            Size = new Point(Layout.FixedWidth, Layout.Height);
            BackgroundColor = Color.Transparent; // Drawn via rounded rect

            var maxTextWidth = Layout.FixedWidth - Layout.CloseButtonSize - Layout.Padding * 2 - Layout.CloseButtonMargin;
            var maxChars = maxTextWidth / 7;
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
                Font = GameService.Content.DefaultFont14,
                TextColor = MaestroTheme.CreamWhite,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Middle,
                BasicTooltipText = needsTooltip ? noteString : null
            };

            _closeButton = new Label
            {
                Parent = this,
                Text = "\u00d7",
                Location = new Point(Layout.FixedWidth - Layout.CloseButtonSize - Layout.CloseButtonMargin, Layout.CloseButtonMargin),
                Size = new Point(Layout.CloseButtonSize, Layout.Height - Layout.Padding),
                Font = GameService.Content.DefaultFont14,
                TextColor = MaestroTheme.MutedCream,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Middle
            };

            _closeButton.MouseEntered += (s, e) => _closeButton.TextColor = MaestroTheme.Error;
            _closeButton.MouseLeft += (s, e) => _closeButton.TextColor = MaestroTheme.MutedCream;
            _closeButton.LeftMouseButtonReleased += (s, e) => FireRemoveClicked();

            MouseEntered += (s, e) => { _currentColor = _hoverColor; Invalidate(); };
            MouseLeft += (s, e) => { _currentColor = _chipColor; Invalidate(); };
        }

        protected override void OnClick(MouseEventArgs e)
        {
            const int closeButtonX = Layout.FixedWidth - Layout.CloseButtonSize - Layout.CloseButtonMargin;
            if (RelativeMousePosition.X < closeButtonX)
            {
                FireChipClicked(e);
            }

            base.OnClick(e);
        }

        private static Color GetNoteColor(string noteString)
        {
            if (noteString.StartsWith("R"))
                return MaestroTheme.ChipRest;

            var isSharp = noteString.Contains("#");

            if (noteString.Contains("-"))
                return isSharp ? MaestroTheme.ChipLowerOctaveSharp : MaestroTheme.ChipLowerOctave;
            if (noteString.Contains("+"))
                return isSharp ? MaestroTheme.ChipUpperOctaveSharp : MaestroTheme.ChipUpperOctave;

            return isSharp ? MaestroTheme.ChipMiddleOctaveSharp : MaestroTheme.ChipMiddleOctave;
        }

        protected override void DisposeControl()
        {
            _noteLabel?.Dispose();
            _closeButton?.Dispose();
            base.DisposeControl();
        }
    }
}
