using Blish_HUD;
using Blish_HUD.Controls;
using Maestro.Models;
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

        // Tint for dark glyph icons drawn on light GW2 buttons (play/pause/stop)
        public static readonly Color IconGlyph = new Color(57, 50, 38);

        // Active/selected toggle buttons: dark espresso fill + light glyph (clearly visible on light buttons)
        public static readonly Color ToggleActiveFill = new Color(46, 34, 28);
        public static readonly Color ToggleActiveGlyph = CreamWhite;

        // State Colors
        public static readonly Color Playing = new Color(76, 175, 80);
        public static readonly Color Paused = new Color(255, 193, 7);
        public static readonly Color Error = new Color(244, 67, 54);
        public static readonly Color Disabled = new Color(85, 85, 85);

        // Panel Colors (semi-transparent)
        public static readonly Color PanelBackground = new Color(45, 45, 45, 180);
        public static readonly Color PanelHover = new Color(64, 64, 64, 200);
        public static readonly Color PanelSelected = new Color(51, 51, 51, 220);

        // Piano Key Colors
        public static readonly Color PianoWhiteKey = new Color(250, 250, 245);
        public static readonly Color PianoWhiteKeyHover = new Color(230, 230, 220);
        public static readonly Color PianoWhiteKeyPressed = new Color(200, 180, 140);
        public static readonly Color PianoBlackKey = new Color(30, 30, 30);
        public static readonly Color PianoBlackKeyHover = new Color(50, 50, 50);
        public static readonly Color PianoBlackKeyPressed = new Color(80, 70, 50);

        // Ghost button (neutral, unaccented buttons)
        public static readonly Color GhostButtonBackground = new Color(60, 55, 70);          // Solid dark, visible
        public static readonly Color GhostButtonBorder = new Color(80, 75, 90);
        public static readonly Color GhostButtonHover = new Color(80, 75, 95);
        public static readonly Color GhostButtonText = CreamWhite;                           // Full cream white

        // Subtle separators
        public static readonly Color SubtleBorder = new Color(255, 255, 255, 25);

        // Input field accent tinting
        public static readonly Color InputLabelColor = new Color(170, 158, 135);             // #aa9e87
        public static readonly Color OctaveLabelColor = new Color(200, 210, 220);            // #c8d2dc
        public static readonly Color HintTextColor = new Color(110, 105, 120);                // #6e6978

        // Note chip octave colors (warmed-up)
        public static readonly Color ChipRest = new Color(58, 53, 64);           // #3a3540
        public static readonly Color ChipLowerOctave = new Color(106, 90, 156);  // #6a5a9c - purple
        public static readonly Color ChipMiddleOctave = new Color(90, 138, 99);  // #5a8a63 - green
        public static readonly Color ChipUpperOctave = new Color(154, 74, 82);   // #9a4a52 - red

        // Explicit sharp colors (noticeably darker than base)
        public static readonly Color ChipLowerOctaveSharp = new Color(60, 50, 95);    // #3c325f
        public static readonly Color ChipMiddleOctaveSharp = new Color(48, 88, 56);   // #305838
        public static readonly Color ChipUpperOctaveSharp = new Color(100, 42, 48);   // #642a30
  
        // Action buttons (Import, Cancel, etc.)
        public const int ActionButtonWidth = 90;
        public const int ActionButtonHeight = 26;
        
        // Spacing
        public const int InputSpacing = 7;
        public const int PaddingContentBottom = 20;
        public const int PaddingContentTop = 2;
        public const int WindowContentTopPadding = 20;

        // Private
        // Per-window vertical gradients: one warm, low-value family so the windows feel
        // related, each with its own hue so they stay individually recognizable.
        private static readonly Color BgMainTop = new Color(28, 20, 34, 255);          // #1c1422 plum
        private static readonly Color BgMainBottom = new Color(42, 32, 28, 255);       // #2a201c espresso
        private static readonly Color BgImportTop = new Color(22, 21, 36, 255);        // #161524 indigo
        private static readonly Color BgImportBottom = new Color(34, 32, 52, 255);     // #222034
        private static readonly Color BgCreatorTop = new Color(30, 23, 30, 255);       // #1e171e umber
        private static readonly Color BgCreatorBottom = new Color(44, 34, 24, 255);    // #2c2218 amber-brown
        private static readonly Color BgCommunityTop = new Color(19, 26, 28, 255);     // #131a1c teal
        private static readonly Color BgCommunityBottom = new Color(28, 40, 38, 255);  // #1c2826
        private static readonly Color BgDrawerTop = new Color(26, 20, 25, 255);        // #1a1419 wine
        private static readonly Color BgDrawerBottom = new Color(38, 27, 33, 255);     // #261b21
        private const int BACKGROUND_X_OFFSET = 1;
        private const int BACKGROUND_Y_OFFSET = 13;

        public const int CornerRadius = 4;

        private static Texture2D _cornerMask;

        public static Texture2D GetCornerMask()
        {
            if (_cornerMask != null) return _cornerMask;

            var context = GameService.Graphics.LendGraphicsDeviceContext();
            try
            {
                var r = CornerRadius;
                _cornerMask = new Texture2D(context.GraphicsDevice, r, r);
                var data = new Color[r * r];

                for (int y = 0; y < r; y++)
                {
                    for (int x = 0; x < r; x++)
                    {
                        // Distance from outer corner (r-1, r-1 maps to the sharp corner)
                        float dx = (r - 1) - x;
                        float dy = (r - 1) - y;
                        float dist = (float)System.Math.Sqrt(dx * dx + dy * dy);
                        data[y * r + x] = dist <= r - 0.5f ? Color.White : Color.Transparent;
                    }
                }

                _cornerMask.SetData(data);
                return _cornerMask;
            }
            finally
            {
                context.Dispose();
            }
        }

        public static void DrawRoundedRect(SpriteBatch spriteBatch, Control ctrl, Rectangle bounds, Color color)
        {
            var pixel = ContentService.Textures.Pixel;
            var corner = GetCornerMask();
            var r = CornerRadius;

            // Center horizontal strip
            spriteBatch.DrawOnCtrl(ctrl, pixel, new Rectangle(r, 0, bounds.Width - r * 2, bounds.Height), color);
            // Left vertical strip (between corners)
            spriteBatch.DrawOnCtrl(ctrl, pixel, new Rectangle(0, r, r, bounds.Height - r * 2), color);
            // Right vertical strip (between corners)
            spriteBatch.DrawOnCtrl(ctrl, pixel, new Rectangle(bounds.Width - r, r, r, bounds.Height - r * 2), color);

            // Four corners
            spriteBatch.DrawOnCtrl(ctrl, corner, new Rectangle(0, 0, r, r), null, color, 0f, Vector2.Zero, SpriteEffects.None);
            spriteBatch.DrawOnCtrl(ctrl, corner, new Rectangle(bounds.Width - r, 0, r, r), null, color, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally);
            spriteBatch.DrawOnCtrl(ctrl, corner, new Rectangle(0, bounds.Height - r, r, r), null, color, 0f, Vector2.Zero, SpriteEffects.FlipVertically);
            spriteBatch.DrawOnCtrl(ctrl, corner, new Rectangle(bounds.Width - r, bounds.Height - r, r, r), null, color, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically);
        }

        public static void DrawBottomRoundedRect(SpriteBatch spriteBatch, Control ctrl, Rectangle bounds, Color color)
        {
            var pixel = ContentService.Textures.Pixel;
            var corner = GetCornerMask();
            var r = CornerRadius;

            // Top strip (no rounding)
            spriteBatch.DrawOnCtrl(ctrl, pixel, new Rectangle(0, 0, bounds.Width, bounds.Height - r), color);
            // Bottom center strip
            spriteBatch.DrawOnCtrl(ctrl, pixel, new Rectangle(r, bounds.Height - r, bounds.Width - r * 2, r), color);

            // Bottom corners only
            spriteBatch.DrawOnCtrl(ctrl, corner, new Rectangle(0, bounds.Height - r, r, r), null, color, 0f, Vector2.Zero, SpriteEffects.FlipVertically);
            spriteBatch.DrawOnCtrl(ctrl, corner, new Rectangle(bounds.Width - r, bounds.Height - r, r, r), null, color, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically);
        }

        public static Color WithAlpha(Color color, int alpha)
        {
            return new Color(color.R, color.G, color.B, alpha);
        }

        public static Color Darken(Color color, float amount)
        {
            return new Color((int)(color.R * amount), (int)(color.G * amount), (int)(color.B * amount), color.A);
        }

        public static Color Brighten(Color color, int amount = 30)
        {
            return new Color(
                System.Math.Min(color.R + amount, 255),
                System.Math.Min(color.G + amount, 255),
                System.Math.Min(color.B + amount, 255),
                255);
        }

        public static Color GetInstrumentAccent(InstrumentType instrument)
        {
            return InstrumentCatalog.Get(instrument).Accent;
        }

        public static Color GetInstrumentAccentDark(InstrumentType instrument)
        {
            return InstrumentCatalog.Get(instrument).AccentDark;
        }

        public static Color AccentTint(Color accent, float opacity)
        {
            return new Color(accent.R, accent.G, accent.B, (int)(255 * opacity));
        }

        public static Texture2D CreateVerticalGradient(int windowWidth, int windowHeight, Color top, Color bottom)
        {
            var width = windowWidth - BACKGROUND_X_OFFSET;
            var height = windowHeight - BACKGROUND_Y_OFFSET;
            var context = GameService.Graphics.LendGraphicsDeviceContext();
            try
            {
                var texture = new Texture2D(context.GraphicsDevice, width, height);
                var data = new Color[width * height];

                for (var y = 0; y < height; y++)
                {
                    var t = height > 1 ? (float)y / (height - 1) : 0f;
                    var row = Color.Lerp(top, bottom, t);
                    for (var x = 0; x < width; x++)
                    {
                        data[y * width + x] = row;
                    }
                }

                texture.SetData(data);
                return texture;
            }
            finally
            {
                context.Dispose();
            }
        }

        public static Texture2D CreateWindowBackground(int windowWidth, int windowHeight)
            => CreateVerticalGradient(windowWidth, windowHeight, BgMainTop, BgMainBottom);

        public static Texture2D CreateImportBackground(int windowWidth, int windowHeight)
            => CreateVerticalGradient(windowWidth, windowHeight, BgImportTop, BgImportBottom);

        public static Texture2D CreateCreatorBackground(int windowWidth, int windowHeight)
            => CreateVerticalGradient(windowWidth, windowHeight, BgCreatorTop, BgCreatorBottom);

        public static Texture2D CreateCommunityBackground(int windowWidth, int windowHeight)
            => CreateVerticalGradient(windowWidth, windowHeight, BgCommunityTop, BgCommunityBottom);

        public static Texture2D CreateDrawerBackground(int windowWidth, int windowHeight)
            => CreateVerticalGradient(windowWidth, windowHeight, BgDrawerTop, BgDrawerBottom);
    }
}
