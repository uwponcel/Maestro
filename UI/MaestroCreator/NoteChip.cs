using System;
using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;

namespace Maestro.UI.MaestroCreator
{
    public class NoteChip : Panel
    {
        public static class Layout
        {
            public const int Height = 24;
            public const int FixedWidth = 118;
            public const int CloseButtonSize = 16;
            public const int Padding = 4;
            public const int CloseButtonMargin = 2;
        }

        public event EventHandler RemoveClicked;

        public string NoteString { get; }
        public int Index { get; set; }

        private readonly Label _noteLabel;
        private readonly Label _closeButton;

        public NoteChip(string noteString, int index)
        {
            NoteString = noteString;
            Index = index;

            Size = new Point(Layout.FixedWidth, Layout.Height);
            BackgroundColor = GetNoteColor(noteString);

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
            MouseEntered += (s, e) => BackgroundColor = MaestroTheme.WithAlpha(GetNoteColor(noteString), 255);
            MouseLeft += (s, e) => BackgroundColor = GetNoteColor(noteString);
        }

        private Color GetNoteColor(string noteString)
        {
            if (noteString.StartsWith("R"))
                return MaestroTheme.ChipRest;

            if (noteString.Contains("-"))
                return MaestroTheme.ChipLowerOctave;

            if (noteString.Contains("+"))
                return MaestroTheme.ChipUpperOctave;

            return MaestroTheme.ChipMiddleOctave;
        }

        protected override void DisposeControl()
        {
            _noteLabel?.Dispose();
            _closeButton?.Dispose();
            base.DisposeControl();
        }
    }
}
