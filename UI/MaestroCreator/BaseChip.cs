using System;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maestro.UI.MaestroCreator
{
    public abstract class BaseChip : Panel
    {
        protected const int BORDER_THICKNESS = 2;

        public event EventHandler RemoveClicked;
        public event EventHandler<MouseEventArgs> ChipClicked;

        public int Index { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; Invalidate(); }
        }

        protected void FireChipClicked(MouseEventArgs e) => ChipClicked?.Invoke(this, e);
        protected void FireRemoveClicked() => RemoveClicked?.Invoke(this, EventArgs.Empty);

        protected void DrawBorder(SpriteBatch spriteBatch, Rectangle bounds, Color borderColor)
        {
            var pixel = ContentService.Textures.Pixel;
            spriteBatch.DrawOnCtrl(this, pixel, new Rectangle(0, 0, bounds.Width, BORDER_THICKNESS), borderColor);
            spriteBatch.DrawOnCtrl(this, pixel, new Rectangle(0, bounds.Height - BORDER_THICKNESS, bounds.Width, BORDER_THICKNESS), borderColor);
            spriteBatch.DrawOnCtrl(this, pixel, new Rectangle(0, 0, BORDER_THICKNESS, bounds.Height), borderColor);
            spriteBatch.DrawOnCtrl(this, pixel, new Rectangle(bounds.Width - BORDER_THICKNESS, 0, BORDER_THICKNESS, bounds.Height), borderColor);
        }

        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds)
        {
            base.PaintBeforeChildren(spriteBatch, bounds);

            if (_isSelected)
                DrawBorder(spriteBatch, bounds, MaestroTheme.AmberGold);
        }
    }
}
