using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maestro.UI.Controls
{
    public class StarButton : Control
    {
        private const int TEXTURE_SIZE = 16;
        private const int CONTROL_SIZE = 20;

        private bool _isFavorite;

        public bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                _isFavorite = value;
                BasicTooltipText = value ? "Remove from Favorites" : "Add to Favorites";
            }
        }

        public StarButton()
        {
            _size = new Point(CONTROL_SIZE, CONTROL_SIZE);
            BasicTooltipText = "Add to Favorites";
        }

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
        {
            var texture = _isFavorite ? MaestroIcons.StarFilled : MaestroIcons.StarOutline;

            var color = _isFavorite
                ? MaestroTheme.AmberGold
                : MouseOver
                    ? MaestroTheme.MutedCream
                    : MaestroTheme.MediumGray;

            var offset = (CONTROL_SIZE - TEXTURE_SIZE) / 2;

            spriteBatch.DrawOnCtrl(this, texture,
                new Rectangle(offset, offset, TEXTURE_SIZE, TEXTURE_SIZE),
                color);
        }
    }
}
