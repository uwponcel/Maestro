using System;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maestro.UI.Controls
{
    public class StarButton : Control
    {
        private const int TEXTURE_SIZE = 16;
        private const int CONTROL_SIZE = 20;

        private static Texture2D _starTexture;

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

        protected override void OnClick(MouseEventArgs e)
        {
            base.OnClick(e);
        }

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
        {
            var color = _isFavorite
                ? MaestroTheme.AmberGold
                : MouseOver
                    ? MaestroTheme.MutedCream
                    : MaestroTheme.MediumGray;

            var texture = GetStarTexture();
            var ox = (CONTROL_SIZE - TEXTURE_SIZE) / 2;
            var oy = (CONTROL_SIZE - TEXTURE_SIZE) / 2;

            spriteBatch.DrawOnCtrl(this, texture,
                new Rectangle(ox, oy, TEXTURE_SIZE, TEXTURE_SIZE),
                color);
        }

        private static Texture2D GetStarTexture()
        {
            if (_starTexture != null)
                return _starTexture;

            var context = GameService.Graphics.LendGraphicsDeviceContext();
            try
            {
                _starTexture = CreateStarTexture(context.GraphicsDevice, TEXTURE_SIZE);
            }
            finally
            {
                context.Dispose();
            }

            return _starTexture;
        }

        private static Texture2D CreateStarTexture(GraphicsDevice device, int size)
        {
            var texture = new Texture2D(device, size, size);
            var data = new Color[size * size];

            // 5-pointed star vertices
            const int points = 5;
            var outerRadius = size / 2f - 0.5f;
            var innerRadius = outerRadius * 0.38f;
            var cx = size / 2f;
            var cy = size / 2f;

            // Compute star polygon vertices (10 points: alternating outer/inner)
            var vertices = new Vector2[points * 2];
            for (var i = 0; i < points * 2; i++)
            {
                var angle = -Math.PI / 2 + i * Math.PI / points;
                var radius = i % 2 == 0 ? outerRadius : innerRadius;
                vertices[i] = new Vector2(
                    cx + (float)(radius * Math.Cos(angle)),
                    cy + (float)(radius * Math.Sin(angle)));
            }

            // Fill pixels using point-in-polygon + edge anti-aliasing
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var coverage = GetPixelCoverage(x, y, vertices);
                    if (coverage > 0f)
                    {
                        var alpha = (byte)(coverage * 255);
                        data[y * size + x] = new Color(alpha, alpha, alpha, alpha);
                    }
                }
            }

            texture.SetData(data);
            return texture;
        }

        private static float GetPixelCoverage(int px, int py, Vector2[] vertices)
        {
            // 4x4 supersampling for smooth edges
            const int samples = 4;
            const float step = 1f / samples;
            const float offset = step / 2f;
            var inside = 0;

            for (var sy = 0; sy < samples; sy++)
            {
                for (var sx = 0; sx < samples; sx++)
                {
                    var x = px + offset + sx * step;
                    var y = py + offset + sy * step;

                    if (IsPointInPolygon(x, y, vertices))
                        inside++;
                }
            }

            return inside / (float)(samples * samples);
        }

        private static bool IsPointInPolygon(float px, float py, Vector2[] vertices)
        {
            var inside = false;
            var n = vertices.Length;

            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                var vi = vertices[i];
                var vj = vertices[j];

                if ((vi.Y > py) != (vj.Y > py) &&
                    px < (vj.X - vi.X) * (py - vi.Y) / (vj.Y - vi.Y) + vi.X)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }
}
