using System;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Maestro.Services.Practice;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Maestro.UI.Practice
{
    public class SectionLoopBar : Control
    {
        private const int MinLoopMs = 500;

        private readonly PracticeSession _session;
        public int? LoopStartMs { get; private set; }
        public int? LoopEndMs { get; private set; }

        public event Action LoopChanged;

        public SectionLoopBar(PracticeSession session)
        {
            _session = session;
            Height = 36;
        }

        protected override void OnLeftMouseButtonPressed(MouseEventArgs e)
        {
            base.OnLeftMouseButtonPressed(e);
            int ms = PixelToMs(RelativeMousePosition.X);

            var mods = GameService.Input.Keyboard.ActiveModifiers;
            if (mods.HasFlag(ModifierKeys.Shift))
            {
                LoopEndMs = ms;
                NormalizeLoop();
                LoopChanged?.Invoke();
            }
            else if (mods.HasFlag(ModifierKeys.Ctrl))
            {
                LoopStartMs = ms;
                NormalizeLoop();
                LoopChanged?.Invoke();
            }
            else
            {
                // Session-level seek: re-syncs the instrument octave and re-arms notes,
                // unlike a bare clock seek which would leave the octave state stale.
                _session.SeekTo(ms);
            }
        }

        protected override void OnRightMouseButtonPressed(MouseEventArgs e)
        {
            base.OnRightMouseButtonPressed(e);
            LoopStartMs = null;
            LoopEndMs = null;
            LoopChanged?.Invoke();
        }

        private void NormalizeLoop()
        {
            if (LoopStartMs.HasValue && LoopEndMs.HasValue)
            {
                if (LoopEndMs.Value < LoopStartMs.Value)
                {
                    var tmp = LoopStartMs.Value;
                    LoopStartMs = LoopEndMs.Value;
                    LoopEndMs = tmp;
                }
                if (LoopEndMs.Value - LoopStartMs.Value < MinLoopMs)
                {
                    LoopEndMs = LoopStartMs.Value + MinLoopMs;
                }

                int total = _session.Timeline.TotalDurationMs;
                if (total > 0 && LoopEndMs.Value > total)
                {
                    LoopEndMs = total;
                    if (LoopEndMs.Value - LoopStartMs.Value < MinLoopMs)
                    {
                        LoopStartMs = Math.Max(0, LoopEndMs.Value - MinLoopMs);
                    }
                }
            }
        }

        private int PixelToMs(int x)
        {
            if (Width <= 0 || _session.Timeline.TotalDurationMs <= 0) return 0;
            return (int)((float)x / Width * _session.Timeline.TotalDurationMs);
        }

        private int MsToPixel(int ms)
        {
            if (_session.Timeline.TotalDurationMs <= 0) return 0;
            return (int)((float)ms / _session.Timeline.TotalDurationMs * Width);
        }

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
        {
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, bounds, new Color(20, 20, 26));

            if (LoopStartMs.HasValue && LoopEndMs.HasValue)
            {
                int x1 = bounds.Left + MsToPixel(LoopStartMs.Value);
                int x2 = bounds.Left + MsToPixel(LoopEndMs.Value);
                var rect = new Rectangle(x1, bounds.Top, Math.Max(1, x2 - x1), bounds.Height);
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, rect, new Color(255, 220, 80) * 0.25f);
            }

            int playheadX = bounds.Left + MsToPixel(Math.Max(0, _session.Clock.CurrentMs));
            var playhead = new Rectangle(playheadX - 1, bounds.Top, 2, bounds.Height);
            spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, playhead, new Color(255, 220, 80));
        }

        public void ApplyLoopIfNeeded()
        {
            if (LoopStartMs.HasValue && LoopEndMs.HasValue && _session.Clock.CurrentMs >= LoopEndMs.Value)
            {
                _session.LoopSeek(LoopStartMs.Value, LoopEndMs.Value);
            }
        }
    }
}
