using System;
using System.Collections.Generic;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Maestro.Models;
using Maestro.Services.Practice;
using Maestro.Settings;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Maestro.UI.Practice
{
    /// <summary>
    /// Guitar-Hero-style custom control that per-frame ticks a <see cref="PracticeSession"/>,
    /// renders the 8 lanes, scrolling note tiles (bright head + dimmer sustain tail), the hit
    /// window band, judgement floaters, and a 3-2-1 countdown. A note should be pressed when
    /// its head reaches the hit line; the bottom strip shows the player's configured keybinds.
    /// </summary>
    public class NoteHighway : Control
    {
        private static class Layout
        {
            public const int LaneCount = 8;
            public const int HitLineFromBottom = 54;
            public const int KeyHintStripHeight = 24;
            public const int LaneHeaderHeight = 22;
            public const int FloaterLifetimeMs = 600;
            public const int MinNoteHeight = 18;
            public const int HeadHeight = 14;
            public const int MaxTickDeltaMs = 100;

            // Keep long sustain tails visible after their head has crossed the hit line.
            public const int TailGraceMs = 3000;
        }

        // Lane colors approximate the GW2 instrument skill icons (C green, D red,
        // E yellow, F purple, G bronze, A indigo, B pink, high C pale) so the highway
        // reads like the in-game skill bar.
        private static readonly Color[] LaneColors =
        {
            new Color(105, 200, 115),   // C  - green
            new Color(215,  95, 110),   // D  - rose red
            new Color(230, 200,  85),   // E  - yellow
            new Color(190, 110, 210),   // F  - purple
            new Color(185, 145,  85),   // G  - bronze
            new Color(125, 125, 225),   // A  - indigo
            new Color(235, 130, 200),   // B  - pink
            new Color(240, 235, 245),   // C^ - pale white
        };

        // GW2's sharp skills (Alt + note) have their own icon colors; matching them
        // means the tile color points at the exact skill icon to press.
        private struct SharpStyle
        {
            public Color Color;
            public bool DarkText;
        }

        private static SharpStyle GetSharpStyle(int lane)
        {
            switch (lane)
            {
                case 1: return new SharpStyle { Color = new Color(90, 150, 230), DarkText = false };  // C# - blue
                case 2: return new SharpStyle { Color = new Color(225, 180, 70), DarkText = true };   // D# - gold
                case 4: return new SharpStyle { Color = new Color(210, 90, 190), DarkText = false };  // F# - magenta
                case 5: return new SharpStyle { Color = new Color(230, 130, 60), DarkText = false };  // G# - orange
                case 6: return new SharpStyle { Color = new Color(110, 190, 100), DarkText = true };  // A# - green
                default: return new SharpStyle { Color = new Color(200, 200, 200), DarkText = true };
            }
        }

        private static readonly Color HitLineColor = new Color(255, 220, 80);
        private static readonly Color HitWindowColor = new Color(255, 255, 255);
        private static readonly Color KeyStripBackground = new Color(8, 8, 12);
        private static readonly Color SharpTextColor = new Color(255, 255, 255);
        private static readonly Color NaturalTextColor = new Color(20, 20, 25);

        private readonly PracticeSession _session;
        private readonly float _lookaheadSeconds;
        private readonly ModuleSettings _keySettings;
        private readonly List<FloatingJudgement> _floaters = new List<FloatingJudgement>();

        /// <summary>
        /// Invoked each frame immediately after <see cref="PracticeSession.Tick(int)"/>.
        /// Used by <c>PracticeWindow</c> for section-loop detection without coupling.
        /// </summary>
        public Action OnAfterTick { get; set; }

        private double _lastTotalMs;
        private double _hitGlowAlpha;

        private struct FloatingJudgement
        {
            public string Text;
            public Color Color;
            public int Lane;
            public int SpawnTimeMs;
        }

        public NoteHighway(PracticeSession session, float lookaheadSeconds, ModuleSettings keySettings)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _lookaheadSeconds = lookaheadSeconds > 0 ? lookaheadSeconds : 2.0f;
            _keySettings = keySettings ?? throw new ArgumentNullException(nameof(keySettings));
            _session.OnJudgement += HandleJudgement;
        }

        private void HandleJudgement(Judgement j)
        {
            string text;
            switch (j.Verdict)
            {
                case JudgementVerdict.Perfect: text = "PERFECT"; break;
                case JudgementVerdict.Good:    text = "GOOD";    break;
                case JudgementVerdict.Miss:    text = "MISS";    break;
                default:                       text = "X";       break;
            }

            _floaters.Add(new FloatingJudgement
            {
                Text = text,
                Color = VerdictColor(j.Verdict),
                Lane = j.Lane,
                SpawnTimeMs = _session.Clock.CurrentMs,
            });

            if (j.Verdict == JudgementVerdict.Perfect)
                _hitGlowAlpha = 1.0;
        }

        private static Color VerdictColor(JudgementVerdict v)
        {
            switch (v)
            {
                case JudgementVerdict.Perfect: return new Color(120, 255, 160);
                case JudgementVerdict.Good:    return new Color(120, 180, 255);
                case JudgementVerdict.Miss:    return new Color(255, 120, 120);
                default:                       return new Color(200, 200, 200);
            }
        }

        protected override void DisposeControl()
        {
            _session.OnJudgement -= HandleJudgement;
            base.DisposeControl();
        }

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
        {
            // Advance the practice session using wall-clock delta since last paint.
            var now = GameService.Overlay.CurrentGameTime.TotalGameTime.TotalMilliseconds;
            int delta = _lastTotalMs == 0 ? 0 : (int)Math.Max(0, Math.Min(Layout.MaxTickDeltaMs, now - _lastTotalMs));
            _lastTotalMs = now;
            _session.Tick(delta);
            OnAfterTick?.Invoke();

            int clockMs = _session.Clock.CurrentMs;
            int laneWidth = bounds.Width / Layout.LaneCount;
            if (laneWidth <= 0) return;

            int highwayTop = bounds.Top + Layout.LaneHeaderHeight;
            int highwayBottom = bounds.Bottom - Layout.KeyHintStripHeight;
            int highwayHeight = highwayBottom - highwayTop;
            if (highwayHeight <= 0) return;

            int hitLineY = highwayBottom - Layout.HitLineFromBottom;
            int scrollSpan = Math.Max(1, hitLineY - highwayTop);
            int lookaheadMs = Math.Max(1, (int)(_lookaheadSeconds * 1000));

            // Lane backgrounds (tinted) + note letters in the header row.
            var font12 = GameService.Content.DefaultFont12;
            for (int i = 0; i < Layout.LaneCount; i++)
            {
                var rect = new Rectangle(bounds.Left + i * laneWidth, highwayTop, laneWidth, highwayHeight);
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, rect, LaneColors[i] * 0.08f);

                var headerCell = new Rectangle(bounds.Left + i * laneWidth, bounds.Top, laneWidth, Layout.LaneHeaderHeight);
                spriteBatch.DrawStringOnCtrl(
                    this,
                    NoteTimeline.LaneLetter(i + 1).ToString(),
                    font12,
                    headerCell,
                    LaneColors[i],
                    false,
                    false,
                    0,
                    HorizontalAlignment.Center,
                    VerticalAlignment.Middle);
            }

            // Hit window band: press while a note head is inside this band to score.
            int goodPx = Math.Max(4, PracticeSession.GoodWindowMs * scrollSpan / lookaheadMs);
            var bandRect = new Rectangle(bounds.Left, hitLineY - goodPx, bounds.Width, goodPx * 2);
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, bandRect, HitWindowColor * 0.07f);

            // Falling note tiles: the head's bottom edge crosses the hit line at StartMs.
            var visible = _session.Timeline.GetNoteIndicesInWindow(clockMs - Layout.TailGraceMs, clockMs + lookaheadMs);
            foreach (var idx in visible)
            {
                var note = _session.Timeline.Notes[idx];
                if (note.Lane < 1 || note.Lane > Layout.LaneCount) continue;

                int lane = note.Lane - 1;
                float progress = (float)(note.StartMs - clockMs) / lookaheadMs;
                int leadingEdgeY = hitLineY - (int)(progress * scrollSpan);
                int height = Math.Max(Layout.MinNoteHeight, note.DurationMs * scrollSpan / lookaheadMs);

                int tileTop = leadingEdgeY - height;
                if (tileTop > highwayBottom || leadingEdgeY < highwayTop) continue;

                var sharpStyle = note.IsSharp ? GetSharpStyle(note.Lane) : default;
                var tileColor = note.IsSharp ? sharpStyle.Color : LaneColors[lane];
                int left = bounds.Left + lane * laneWidth + 4;
                int width = laneWidth - 8;

                // Sustain tail (dimmer) above the head.
                int headHeight = Math.Min(Layout.HeadHeight, height);
                var tailRect = ClampToHighway(new Rectangle(left, tileTop, width, height - headHeight), highwayTop, highwayBottom);
                if (tailRect.Height > 0)
                    spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, tailRect, tileColor * 0.40f);

                // Head (bright): this is what you hit.
                var headRect = ClampToHighway(new Rectangle(left, leadingEdgeY - headHeight, width, headHeight), highwayTop, highwayBottom);
                if (headRect.Height > 0)
                {
                    spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, headRect, tileColor);

                    string label = NoteTimeline.LaneLetter(note.Lane).ToString() + (note.IsSharp ? "#" : "");
                    var labelRect = new Rectangle(left, headRect.Y - 2, width, Layout.HeadHeight + 4);
                    var textColor = note.IsSharp
                        ? (sharpStyle.DarkText ? NaturalTextColor : SharpTextColor)
                        : NaturalTextColor;
                    spriteBatch.DrawStringOnCtrl(
                        this,
                        label,
                        font12,
                        labelRect,
                        textColor,
                        false,
                        false,
                        0,
                        HorizontalAlignment.Center,
                        VerticalAlignment.Middle);
                }
            }

            // Hit line (bright yellow, pulses on perfect hits).
            var hitLineRect = new Rectangle(bounds.Left, hitLineY, bounds.Width, 3);
            var glow = HitLineColor * (float)Math.Max(0.6, _hitGlowAlpha);
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, hitLineRect, glow);
            _hitGlowAlpha = Math.Max(0, _hitGlowAlpha - 0.05);

            // Bottom key-hint strip: the player's configured key for each lane.
            var keyStripRect = new Rectangle(bounds.Left, highwayBottom, bounds.Width, Layout.KeyHintStripHeight);
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, keyStripRect, KeyStripBackground);
            for (int i = 0; i < Layout.LaneCount; i++)
            {
                var cell = new Rectangle(bounds.Left + i * laneWidth, highwayBottom, laneWidth, Layout.KeyHintStripHeight);
                spriteBatch.DrawStringOnCtrl(
                    this,
                    LaneKeyLabel(i + 1),
                    GameService.Content.DefaultFont14,
                    cell,
                    Color.White,
                    false,
                    false,
                    0,
                    HorizontalAlignment.Center,
                    VerticalAlignment.Middle);
            }

            // Floating judgement text (rises and fades).
            for (int i = _floaters.Count - 1; i >= 0; i--)
            {
                var f = _floaters[i];
                int age = clockMs - f.SpawnTimeMs;
                if (age < 0 || age > Layout.FloaterLifetimeMs)
                {
                    _floaters.RemoveAt(i);
                    continue;
                }

                float alpha = 1f - ((float)age / Layout.FloaterLifetimeMs);
                int rise = (int)(age * 0.05);
                var rect = new Rectangle(
                    bounds.Left + (f.Lane - 1) * laneWidth,
                    hitLineY - 24 - rise,
                    laneWidth,
                    20);

                spriteBatch.DrawStringOnCtrl(
                    this,
                    f.Text,
                    GameService.Content.DefaultFont14,
                    rect,
                    f.Color * alpha,
                    false,
                    false,
                    0,
                    HorizontalAlignment.Center,
                    VerticalAlignment.Middle);
            }

            // 3-2-1 countdown overlay while the clock is still negative.
            if (clockMs < 0)
            {
                string text = clockMs > -1000 ? "1" : clockMs > -2000 ? "2" : "3";
                var cdRect = new Rectangle(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
                var font = GameService.Content.DefaultFont32 ?? GameService.Content.DefaultFont18;
                spriteBatch.DrawStringOnCtrl(
                    this,
                    text,
                    font,
                    cdRect,
                    Color.White,
                    false,
                    false,
                    0,
                    HorizontalAlignment.Center,
                    VerticalAlignment.Middle);
            }
        }

        private static Rectangle ClampToHighway(Rectangle rect, int top, int bottom)
        {
            int y = Math.Max(rect.Y, top);
            int end = Math.Min(rect.Y + rect.Height, bottom);
            return new Rectangle(rect.X, y, rect.Width, Math.Max(0, end - y));
        }

        private string LaneKeyLabel(int lane)
        {
            var entry = LaneEntry(lane);
            var binding = entry?.Value;
            if (binding == null || binding.PrimaryKey == Keys.None) return "-";

            string label = KeyName(binding.PrimaryKey);
            if (binding.ModifierKeys.HasFlag(ModifierKeys.Ctrl)) label = "C+" + label;
            if (binding.ModifierKeys.HasFlag(ModifierKeys.Alt)) label = "A+" + label;
            if (binding.ModifierKeys.HasFlag(ModifierKeys.Shift)) label = "S+" + label;
            return label;
        }

        private Blish_HUD.Settings.SettingEntry<KeyBinding> LaneEntry(int lane)
        {
            switch (lane)
            {
                case 1: return _keySettings.NoteC;
                case 2: return _keySettings.NoteD;
                case 3: return _keySettings.NoteE;
                case 4: return _keySettings.NoteF;
                case 5: return _keySettings.NoteG;
                case 6: return _keySettings.NoteA;
                case 7: return _keySettings.NoteB;
                case 8: return _keySettings.NoteCHigh;
                default: return null;
            }
        }

        private static string KeyName(Keys key)
        {
            if (key >= Keys.D0 && key <= Keys.D9) return ((int)(key - Keys.D0)).ToString();
            if (key >= Keys.NumPad0 && key <= Keys.NumPad9) return "N" + (int)(key - Keys.NumPad0);
            return key.ToString();
        }
    }
}
