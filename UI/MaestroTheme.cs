using Blish_HUD;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maestro.UI
{
    public static class MaestroTheme
    {
        // Primary Accent - Amber/Gold
        public static readonly Color AmberGold = new Color(212, 166, 86);
        public static readonly Color DeepAmber = new Color(232, 184, 74);
        public static readonly Color WarmBronze = new Color(139, 105, 20);

        // Backgrounds
        public static readonly Color DarkCharcoal = new Color(26, 26, 26);
        public static readonly Color SlateGray = new Color(45, 45, 45);
        public static readonly Color MediumGray = new Color(64, 64, 64);

        // Text Colors
        public static readonly Color CreamWhite = new Color(240, 230, 210);
        public static readonly Color MutedCream = new Color(200, 191, 169);
        public static readonly Color LightGray = new Color(128, 128, 128);

        // State Colors
        public static readonly Color Playing = new Color(76, 175, 80);
        public static readonly Color Paused = new Color(255, 193, 7);
        public static readonly Color Error = new Color(244, 67, 54);
        public static readonly Color Disabled = new Color(85, 85, 85);

        // Instrument Accent Colors (for icons/badges)
        public static readonly Color Piano = new Color(126, 200, 227);
        public static readonly Color Harp = new Color(184, 212, 168);
        public static readonly Color Lute = new Color(232, 193, 112);
        public static readonly Color Bass = new Color(212, 132, 140);

        // Panel Colors (semi-transparent)
        public static readonly Color PanelBackground = new Color(45, 45, 45, 180);
        public static readonly Color PanelHover = new Color(64, 64, 64, 200);
        public static readonly Color PanelSelected = new Color(51, 51, 51, 220);
  
        // Action buttons (Import, Cancel, etc.)
        public const int ActionButtonWidth = 90;
        public const int ActionButtonHeight = 26;
        
        // Spacing
        public const int InputSpacing = 7;
        public const int PaddingContentBottom = 20;
        public const int PaddingContentTop = 2;

        // Private
        private static readonly Color WindowBackground = new Color(30, 30, 30, 255);
        private const int BACKGROUND_X_OFFSET = 1;
        private const int BACKGROUND_Y_OFFSET = 13;

        public static Color WithAlpha(Color color, int alpha)
        {
            return new Color(color.R, color.G, color.B, alpha);
        }

        public static Texture2D CreateWindowBackground(int windowWidth, int windowHeight)
        {
            var width = windowWidth - BACKGROUND_X_OFFSET;
            var height = windowHeight - BACKGROUND_Y_OFFSET;
            var context = GameService.Graphics.LendGraphicsDeviceContext();
            try
            {
                var texture = new Texture2D(context.GraphicsDevice, width, height);
                var data = new Color[width * height];

                for (var i = 0; i < data.Length; i++)
                {
                    data[i] = WindowBackground;
                }

                texture.SetData(data);
                return texture;
            }
            finally
            {
                context.Dispose();
            }
        }
    }
}
