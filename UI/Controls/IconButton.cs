using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maestro.UI.Controls
{
    /// <summary>
    /// A <see cref="StandardButton"/> that draws a single glyph icon centered in the
    /// button, tinted with a theme color. Use this instead of <see cref="StandardButton.Icon"/>
    /// for icon-only buttons: the built-in Icon layout left-aligns the glyph (it reserves
    /// room for text), so an icon-only StandardButton ends up off-center and untinted.
    /// </summary>
    public class IconButton : StandardButton
    {
        private const int IconSize = 16;

        private Color _tint;
        private Texture2D _iconTexture;
        private bool _selected;

        public IconButton(Texture2D icon, Color tint)
        {
            _iconTexture = icon;
            _tint = tint;
        }

        /// <summary>Color the glyph is drawn with. Change it to reflect state (e.g. active).</summary>
        public Color Tint
        {
            get => _tint;
            set
            {
                if (_tint == value) return;
                _tint = value;
                Invalidate();
            }
        }

        /// <summary>The glyph to draw. Swap this to change the icon (e.g. play vs pause).</summary>
        public Texture2D IconTexture
        {
            get => _iconTexture;
            set
            {
                if (_iconTexture == value) return;
                _iconTexture = value;
                Invalidate();
            }
        }

        /// <summary>When true, the button renders a filled "selected" state (dark fill + light glyph).</summary>
        public bool Selected
        {
            get => _selected;
            set
            {
                if (_selected == value) return;
                _selected = value;
                Invalidate();
            }
        }

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
        {
            base.Paint(spriteBatch, bounds);

            if (_iconTexture == null) return;

            if (_selected)
            {
                // Dark espresso inner fill so an active toggle reads clearly against the light button.
                var fill = new Rectangle(3, 3, _size.X - 6, _size.Y - 6);
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, fill, MaestroTheme.ToggleActiveFill);
            }

            var glyphColor = _selected ? MaestroTheme.ToggleActiveGlyph : _tint;

            var iconBounds = new Rectangle(
                _size.X / 2 - IconSize / 2,
                _size.Y / 2 - IconSize / 2,
                IconSize,
                IconSize);

            spriteBatch.DrawOnCtrl(this, _iconTexture, iconBounds, glyphColor);
        }
    }
}
