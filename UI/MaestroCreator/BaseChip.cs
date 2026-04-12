using System;
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

        protected Color _currentColor;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; Invalidate(); }
        }

        private bool _isPlaying;
        public bool IsPlaying
        {
            get => _isPlaying;
            set { _isPlaying = value; Invalidate(); }
        }

        protected void FireChipClicked(MouseEventArgs e) => ChipClicked?.Invoke(this, e);
        protected void FireRemoveClicked() => RemoveClicked?.Invoke(this, EventArgs.Empty);

        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds)
        {
            if (_isPlaying || _isSelected)
            {
                MaestroTheme.DrawRoundedRect(spriteBatch, this, bounds, MaestroTheme.AmberGold);
                var inset = new Rectangle(BORDER_THICKNESS, BORDER_THICKNESS,
                    bounds.Width - BORDER_THICKNESS * 2, bounds.Height - BORDER_THICKNESS * 2);
                MaestroTheme.DrawRoundedRect(spriteBatch, this, inset, _currentColor);
            }
            else
            {
                MaestroTheme.DrawRoundedRect(spriteBatch, this, bounds, _currentColor);
            }
        }
    }
}
