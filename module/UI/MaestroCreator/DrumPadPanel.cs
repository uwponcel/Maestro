using System;
using System.Collections.Generic;
using Blish_HUD;
using Blish_HUD.Controls;
using Maestro.Models;
using Microsoft.Xna.Framework;

namespace Maestro.UI.MaestroCreator
{
    /// <summary>
    /// Drum Set input pad: 12 labeled pads grouped Drums / Toms / Cymbals plus a
    /// Rest button. Percussion sibling to PianoKeyboard - no octave, no sharps.
    /// Sized to PianoKeyboard.Layout.TotalHeight so the Creator layout below is
    /// unchanged. Emits PadPressed(DrumSound) and RestPressed.
    /// </summary>
    public class DrumPadPanel : Panel
    {
        private static class Layout
        {
            // Three pad rows + the REST button must fit inside
            // PianoKeyboard.Layout.TotalHeight (179). Rows end at 132, so REST
            // (132->156) sits clear of the panel bottom.
            public const int PadHeight = 36;
            public const int RowGap = 6;
            public const int PadGap = 4;
            public const int SideMargin = 6;
            public const int TopMargin = 6;
            public const int RestHeight = 24;
            public const int RailWidth = 3;   // group-color accent rail on each pad's left edge
        }

        public event EventHandler<DrumSound> PadPressed;
        public event EventHandler RestPressed;

        private static readonly DrumSound[] Row1 = { DrumSound.Bass, DrumSound.Snare, DrumSound.CrossStick, DrumSound.Ghost };
        private static readonly DrumSound[] Row2 = { DrumSound.HighTom, DrumSound.MidTom, DrumSound.FloorTom };
        private static readonly DrumSound[] Row3 = { DrumSound.Crash, DrumSound.Ride, DrumSound.HatClosed, DrumSound.HatOpen, DrumSound.HatFoot };

        private readonly List<Panel> _pads = new List<Panel>();
        private readonly List<Panel> _padRails = new List<Panel>();
        private readonly List<Label> _padLabels = new List<Label>();
        private readonly Panel _restButton;
        private readonly Label _restLabel;
        private Color _accent = MaestroTheme.AmberGold;

        public DrumPadPanel(int width)
        {
            Size = new Point(width, PianoKeyboard.Layout.TotalHeight);
            BackgroundColor = new Color(0, 0, 0, 65);

            var y = Layout.TopMargin;
            BuildRow(Row1, y, width);
            y += Layout.PadHeight + Layout.RowGap;
            BuildRow(Row2, y, width);
            y += Layout.PadHeight + Layout.RowGap;
            BuildRow(Row3, y, width);
            y += Layout.PadHeight + Layout.RowGap;

            var restWidth = width - Layout.SideMargin * 2;
            _restButton = new Panel
            {
                Parent = this,
                Location = new Point(Layout.SideMargin, y),
                Size = new Point(restWidth, Layout.RestHeight),
                BackgroundColor = MaestroTheme.GhostButtonBackground
            };
            _restLabel = new Label
            {
                Parent = _restButton,
                Text = "REST",
                Location = new Point(0, 0),
                Size = new Point(restWidth, Layout.RestHeight),
                Font = GameService.Content.DefaultFont12,
                TextColor = MaestroTheme.GhostButtonText,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Middle
            };
            _restButton.MouseEntered += (s, e) => _restButton.BackgroundColor = MaestroTheme.GhostButtonHover;
            _restButton.MouseLeft += (s, e) => _restButton.BackgroundColor = MaestroTheme.GhostButtonBackground;
            _restButton.LeftMouseButtonReleased += (s, e) => RestPressed?.Invoke(this, EventArgs.Empty);
        }

        public void Configure(InstrumentType instrument)
        {
            _accent = MaestroTheme.GetInstrumentAccent(instrument);
        }

        private void BuildRow(DrumSound[] sounds, int y, int width)
        {
            var usable = width - Layout.SideMargin * 2 - Layout.PadGap * (sounds.Length - 1);
            var padW = usable / sounds.Length;

            for (int i = 0; i < sounds.Length; i++)
            {
                var info = DrumMapping.Get(sounds[i]);
                var group = info.Group;
                var resting = MaestroTheme.DrumPadResting(group);
                var hover = MaestroTheme.DrumPadHover(group);
                var x = Layout.SideMargin + i * (padW + Layout.PadGap);

                var pad = new Panel
                {
                    Parent = this,
                    Location = new Point(x, y),
                    Size = new Point(padW, Layout.PadHeight),
                    BackgroundColor = resting,
                    BasicTooltipText = info.DisplayName
                };

                // Thin group-color rail on the left edge: encodes Drums/Toms/Cymbals
                // without spending vertical space, matching the note-chip colors.
                var rail = new Panel
                {
                    Parent = pad,
                    Location = new Point(0, 0),
                    Size = new Point(Layout.RailWidth, Layout.PadHeight),
                    BackgroundColor = MaestroTheme.GetDrumGroupColor(group)
                };

                var label = new Label
                {
                    Parent = pad,
                    Text = info.DisplayName,
                    Location = new Point(Layout.RailWidth, 0),
                    Size = new Point(padW - Layout.RailWidth, Layout.PadHeight),
                    Font = GameService.Content.DefaultFont12,
                    TextColor = MaestroTheme.GhostButtonText,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Middle,
                    BasicTooltipText = info.DisplayName
                };

                var captured = sounds[i];
                pad.MouseEntered += (s, e) => pad.BackgroundColor = hover;
                pad.MouseLeft += (s, e) => pad.BackgroundColor = resting;
                pad.LeftMouseButtonPressed += (s, e) => pad.BackgroundColor = _accent;
                pad.LeftMouseButtonReleased += (s, e) =>
                {
                    pad.BackgroundColor = hover;
                    PadPressed?.Invoke(this, captured);
                };

                _pads.Add(pad);
                _padRails.Add(rail);
                _padLabels.Add(label);
            }
        }

        protected override void DisposeControl()
        {
            foreach (var l in _padLabels) l?.Dispose();
            foreach (var r in _padRails) r?.Dispose();
            foreach (var p in _pads) p?.Dispose();
            _restLabel?.Dispose();
            _restButton?.Dispose();
            base.DisposeControl();
        }
    }
}
