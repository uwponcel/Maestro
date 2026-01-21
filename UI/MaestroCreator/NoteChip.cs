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
            public const int MinWidth = 50;
            public const int MaxWidth = 155;
            public const int CloseButtonSize = 16;
            public const int Padding = 4;
            public const int CharWidthEstimate = 8;
            public const int CharWidthTruncate = 7;
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

            var textWidth = noteString.Length * Layout.CharWidthEstimate + Layout.Padding * 2 + Layout.CloseButtonSize;
            var width = Math.Max(Layout.MinWidth, Math.Min(textWidth, Layout.MaxWidth));

            Size = new Point(width, Layout.Height);
            BackgroundColor = GetNoteColor(noteString);

            var displayText = noteString;
            var maxTextWidth = width - Layout.CloseButtonSize - Layout.Padding * 2;
            var maxChars = maxTextWidth / Layout.CharWidthTruncate;
            if (noteString.Length > maxChars && maxChars > 3)
            {
                displayText = noteString.Substring(0, maxChars - 2) + "..";
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
                BasicTooltipText = noteString // Show full text on hover
            };

            _closeButton = new Label
            {
                Parent = this,
                Text = "×",
                Location = new Point(width - Layout.CloseButtonSize - Layout.CloseButtonMargin, Layout.CloseButtonMargin),
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
