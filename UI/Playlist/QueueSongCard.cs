using System;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Maestro.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;

namespace Maestro.UI.Playlist
{
    public class QueueSongCard : Control
    {
        public static class Layout
        {
            public const int Height = 40;
            public const int DragHandleX = 12;
            public const int TitleX = 32;
            public const int TitleY = 4;
            public const int ArtistY = 21;
            public const int RemoveButtonSize = 24;
            public const int RemoveButtonMargin = 22;
        }

        public event EventHandler RemoveRequested;
        public event EventHandler DragStarted;
        public event EventHandler DragEnded;

        private readonly Song _song;
        private readonly int _index;

        private bool _isDragging;
        private bool _isHoveringRemove;
        private bool _isHoveringDragHandle;
        private Rectangle _dragHandleBounds;
        private Rectangle _removeButtonBounds;

        public Song Song => _song;
        public int Index => _index;
        public bool IsDragging => _isDragging;
        public bool IsGhost { get; set; }

        public void EndDrag()
        {
            _isDragging = false;
            _isHoveringDragHandle = false;
        }

        public QueueSongCard(Song song, int index, int width)
        {
            _song = song;
            _index = index;

            _size = new Point(width, Layout.Height);

            _dragHandleBounds = new Rectangle(Layout.DragHandleX, (Layout.Height - 16) / 2, 12, 16);
            _removeButtonBounds = new Rectangle(
                width - Layout.RemoveButtonSize - Layout.RemoveButtonMargin,
                (Layout.Height - Layout.RemoveButtonSize) / 2,
                Layout.RemoveButtonSize,
                Layout.RemoveButtonSize);
        }

        protected override void OnMouseMoved(MouseEventArgs e)
        {
            var pos = RelativeMousePosition;
            _isHoveringDragHandle = _dragHandleBounds.Contains(pos);
            _isHoveringRemove = _removeButtonBounds.Contains(pos);

            // Update tooltip based on what's being hovered (hide during drag and on ghost)
            if (_isDragging || IsGhost)
                BasicTooltipText = null;
            else if (_isHoveringDragHandle)
                BasicTooltipText = "Drag to reorder";
            else if (_isHoveringRemove)
                BasicTooltipText = "Remove from queue";
            else
                BasicTooltipText = null;

            base.OnMouseMoved(e);
        }

        protected override void OnMouseLeft(MouseEventArgs e)
        {
            _isHoveringDragHandle = false;
            _isHoveringRemove = false;
            base.OnMouseLeft(e);
        }

        protected override void OnLeftMouseButtonPressed(MouseEventArgs e)
        {
            if (_isHoveringDragHandle)
            {
                _isDragging = true;
                BasicTooltipText = null;
                DragStarted?.Invoke(this, EventArgs.Empty);
            }
            base.OnLeftMouseButtonPressed(e);
        }

        protected override void OnLeftMouseButtonReleased(MouseEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                DragEnded?.Invoke(this, EventArgs.Empty);
            }
            base.OnLeftMouseButtonReleased(e);
        }

        protected override void OnClick(MouseEventArgs e)
        {
            if (_isHoveringRemove)
            {
                RemoveRequested?.Invoke(this, EventArgs.Empty);
            }
            base.OnClick(e);
        }

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
        {
            // Apply opacity for dimming during drag (uses inherited Control.Opacity)
            var opacity = Opacity;

            // Background
            var bgColor = MouseOver ? MaestroTheme.PanelHover : MaestroTheme.PanelBackground;
            bgColor = new Color(bgColor.R, bgColor.G, bgColor.B, (int)(bgColor.A * opacity));
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, bounds, bgColor);

            // Left accent bar
            var instrumentColor = GetInstrumentColor(_song.Instrument);
            instrumentColor = new Color(instrumentColor.R, instrumentColor.G, instrumentColor.B, (int)(255 * opacity));
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel,
                new Rectangle(0, 0, 3, Layout.Height), instrumentColor);

            // Drag handle (3 horizontal lines)
            var handleColor = _isHoveringDragHandle || _isDragging
                ? MaestroTheme.AmberGold
                : MaestroTheme.MutedCream;
            handleColor = new Color(handleColor.R, handleColor.G, handleColor.B, (int)(255 * opacity));
            const int handleY = (Layout.Height - 12) / 2;

            for (var i = 0; i < 3; i++)
            {
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel,
                    new Rectangle(Layout.DragHandleX, handleY + i * 5, 10, 2), handleColor);
            }

            // Song title (truncated if needed)
            var titleWidth = _size.X - Layout.TitleX - Layout.RemoveButtonSize - Layout.RemoveButtonMargin - 8;
            var title = TruncateText(_song.Name, Content.DefaultFont14, titleWidth);
            var titleColor = new Color(MaestroTheme.CreamWhite.R, MaestroTheme.CreamWhite.G, MaestroTheme.CreamWhite.B, (int)(255 * opacity));
            spriteBatch.DrawStringOnCtrl(this, title, Content.DefaultFont14,
                new Rectangle(Layout.TitleX, Layout.TitleY, titleWidth, 16),
                titleColor);

            // Artist
            var artistText = _song.Artist;
            var artistTruncated = TruncateText(artistText, Content.DefaultFont12, titleWidth);
            var artistColor = new Color(MaestroTheme.MutedCream.R, MaestroTheme.MutedCream.G, MaestroTheme.MutedCream.B, (int)(255 * opacity));
            spriteBatch.DrawStringOnCtrl(this, artistTruncated, Content.DefaultFont12,
                new Rectangle(Layout.TitleX, Layout.ArtistY, titleWidth, 14),
                artistColor);

            // Remove button (X)
            var removeColor = _isHoveringRemove ? MaestroTheme.Error : MaestroTheme.MutedCream;
            removeColor = new Color(removeColor.R, removeColor.G, removeColor.B, (int)(255 * opacity));
            DrawX(spriteBatch, _removeButtonBounds, removeColor);
        }

        private void DrawX(SpriteBatch spriteBatch, Rectangle bounds, Color color)
        {
            var centerX = bounds.X + bounds.Width / 2;
            var centerY = bounds.Y + bounds.Height / 2;
            const int size = 4;

            // Draw X as two crossing lines using small rectangles
            for (var i = -size; i <= size; i++)
            {
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel,
                    new Rectangle(centerX + i - 1, centerY + i - 1, 2, 2), color);
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel,
                    new Rectangle(centerX - i - 1, centerY + i - 1, 2, 2), color);
            }
        }

        private static string TruncateText(string text, BitmapFont font, int maxWidth)
        {
            if (string.IsNullOrEmpty(text)) return text;

            var measurement = font.MeasureString(text);
            if (measurement.Width <= maxWidth) return text;

            const string ellipsis = "...";
            var truncated = text;

            while (truncated.Length > 0)
            {
                truncated = truncated.Substring(0, truncated.Length - 1);
                measurement = font.MeasureString(truncated + ellipsis);
                if (measurement.Width <= maxWidth)
                {
                    return truncated + ellipsis;
                }
            }

            return ellipsis;
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
    }
}
