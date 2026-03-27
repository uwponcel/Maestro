using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;

namespace Maestro.UI.MaestroCreator
{
    public class SectionMarkerChip : BaseChip
    {
        public static class Layout
        {
            public const int Height = 26;
            public const int CloseButtonSize = 16;
            public const int Padding = 6;
            public const int CloseButtonMargin = 2;
        }

        private static readonly Color SectionColor = new Color(58, 120, 140);
        private static readonly Color SectionColorHover = new Color(72, 140, 162);

        public string SectionName { get; }

        private readonly Label _sectionLabel;
        private readonly Label _closeButton;

        public SectionMarkerChip(string sectionName, int index, int containerWidth)
        {
            SectionName = sectionName;
            Index = index;

            // Full width of the container minus scrollbar
            var chipWidth = containerWidth - 26;
            Size = new Point(chipWidth, Layout.Height);
            BackgroundColor = SectionColor;

            _sectionLabel = new Label
            {
                Parent = this,
                Text = sectionName,
                Location = new Point(Layout.Padding, 1),
                Size = new Point(chipWidth - Layout.CloseButtonSize - Layout.Padding * 2, Layout.Height - 2),
                Font = GameService.Content.DefaultFont16,
                TextColor = MaestroTheme.CreamWhite,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Middle
            };

            _closeButton = new Label
            {
                Parent = this,
                Text = "\u00d7",
                Location = new Point(chipWidth - Layout.CloseButtonSize - Layout.CloseButtonMargin, Layout.CloseButtonMargin),
                Size = new Point(Layout.CloseButtonSize, Layout.Height - Layout.Padding),
                Font = GameService.Content.DefaultFont14,
                TextColor = MaestroTheme.MutedCream,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Middle
            };

            _closeButton.MouseEntered += (s, e) => _closeButton.TextColor = MaestroTheme.Error;
            _closeButton.MouseLeft += (s, e) => _closeButton.TextColor = MaestroTheme.MutedCream;
            _closeButton.LeftMouseButtonReleased += (s, e) => FireRemoveClicked();

            MouseEntered += (s, e) => BackgroundColor = SectionColorHover;
            MouseLeft += (s, e) => BackgroundColor = SectionColor;

            Resized += (s, e) => UpdateLayout();
        }

        private void UpdateLayout()
        {
            _sectionLabel.Size = new Point(Width - Layout.CloseButtonSize - Layout.Padding * 2, Layout.Height - 2);
            _closeButton.Location = new Point(Width - Layout.CloseButtonSize - Layout.CloseButtonMargin, Layout.CloseButtonMargin);
        }

        protected override void OnClick(MouseEventArgs e)
        {
            var closeButtonX = Width - Layout.CloseButtonSize - Layout.CloseButtonMargin;
            if (RelativeMousePosition.X < closeButtonX)
            {
                FireChipClicked(e);
            }

            base.OnClick(e);
        }

        protected override void DisposeControl()
        {
            _sectionLabel?.Dispose();
            _closeButton?.Dispose();
            base.DisposeControl();
        }
    }
}
