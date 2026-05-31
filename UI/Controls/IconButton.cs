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

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
        {
            base.Paint(spriteBatch, bounds);

            if (_iconTexture == null) return;

            var iconBounds = new Rectangle(
                _size.X / 2 - IconSize / 2,
                _size.Y / 2 - IconSize / 2,
                IconSize,
                IconSize);

            spriteBatch.DrawOnCtrl(this, _iconTexture, iconBounds, _tint);
        }
    }
}
