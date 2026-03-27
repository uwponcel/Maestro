using System;
using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maestro.UI.MaestroCreator
{
    public class NoteSequenceWindow : StandardWindow
    {
        public static class Layout
        {
            public const int DefaultWidth = 700;
            public const int DefaultHeight = 450;
            public const int MinWidth = 500;
            public const int MinHeight = 350;
            public const int MaxWidth = 1200;
            public const int MaxHeight = 800;
            public const int ContentPaddingX = 15;
        }

        public event EventHandler PanelReturned;

        private NoteSequencePanel _panel;

        private static Texture2D _backgroundTexture;

        private static Texture2D GetBackground()
        {
            return _backgroundTexture ?? (_backgroundTexture =
                MaestroTheme.CreateWindowBackground(Layout.MaxWidth, Layout.MaxHeight));
        }

        public NoteSequenceWindow(NoteSequencePanel panel)
            : base(
                GetBackground(),
                new Rectangle(0, 0, Layout.MaxWidth, Layout.MaxHeight),
                new Rectangle(Layout.ContentPaddingX, MaestroTheme.WindowContentTopPadding,
                    Layout.MaxWidth - Layout.ContentPaddingX * 2,
                    Layout.MaxHeight - MaestroTheme.WindowContentTopPadding))
        {
            Title = "Maestro Creator";
            Subtitle = "Notes";
            Emblem = Module.Instance.ContentsManager.GetTexture("creator-emblem.png");
            SavesPosition = true;
            Id = "MaestroNoteSequenceWindow_v1";
            CanResize = true;
            Parent = GameService.Graphics.SpriteScreen;

            Size = new Point(Layout.DefaultWidth, Layout.DefaultHeight);

            _panel = panel;
            _panel.Parent = this;
            _panel.Location = new Point(0, MaestroTheme.PaddingContentTop);

            UpdatePanelSize();
        }

        public override void RecalculateLayout()
        {
            // Enforce minimum size
            if (Width < Layout.MinWidth || Height < Layout.MinHeight)
            {
                Size = new Point(
                    Math.Max(Width, Layout.MinWidth),
                    Math.Max(Height, Layout.MinHeight));
                return;
            }

            base.RecalculateLayout();
            UpdatePanelSize();
        }

        private void UpdatePanelSize()
        {
            if (_panel == null) return;
            const int bottomMargin = 5;
            var contentWidth = ContentRegion.Width;
            var contentHeight = ContentRegion.Height - MaestroTheme.PaddingContentTop - bottomMargin;
            if (contentWidth > 0 && contentHeight > 0)
            {
                _panel.ResizeTo(contentWidth, contentHeight);
            }
        }

        public NoteSequencePanel DetachPanel()
        {
            var panel = _panel;
            if (panel != null)
            {
                panel.Parent = null;
                _panel = null;
            }

            return panel;
        }

        public override void Hide()
        {
            base.Hide();
            PanelReturned?.Invoke(this, EventArgs.Empty);
        }

        protected override void DisposeControl()
        {
            // Don't dispose the panel - it belongs to MaestroCreatorWindow
            _panel = null;
            base.DisposeControl();
        }
    }
}
