using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.BitmapFonts;

namespace Maestro.UI.Controls
{
    public class MarqueeLabel : LabelBase
    {
        private enum ScrollState
        {
            Idle,
            PauseStart,
            ScrollingLeft,
            PauseEnd
        }

        private const float ScrollSpeed = 40f;
        private const double PauseDurationMs = 2000;

        private ScrollState _state = ScrollState.Idle;
        private float _scrollOffset;
        private double _pauseTimer;
        private double _lastPaintTime;

        public string Text
        {
            get => _text;
            set
            {
                if (SetProperty(ref _text, value, true))
                {
                    ResetScroll();
                    UpdateTooltip();
                }
            }
        }

        public BitmapFont Font
        {
            get => _font;
            set => SetProperty(ref _font, value, true);
        }

        public Color TextColor
        {
            get => _textColor;
            set => SetProperty(ref _textColor, value);
        }

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
        {
            if (_font == null || string.IsNullOrEmpty(_text)) return;

            var textWidth = _font.MeasureString(_text).Width;

            if (textWidth > bounds.Width)
            {
                UpdateScrollState(textWidth - bounds.Width);
            }
            else if (_state != ScrollState.Idle)
            {
                _state = ScrollState.Idle;
                _scrollOffset = 0;
            }

            var drawBounds = new Rectangle(
                bounds.X - (int)_scrollOffset,
                bounds.Y,
                (int)textWidth + 1,
                bounds.Height
            );

            DrawText(spriteBatch, drawBounds, _text);
        }

        private void UpdateScrollState(float maxOffset)
        {
            var now = GameService.Overlay.CurrentGameTime.TotalGameTime.TotalMilliseconds;
            var elapsed = _lastPaintTime > 0 ? now - _lastPaintTime : 0;
            _lastPaintTime = now;

            if (elapsed <= 0 || elapsed > 500) return;

            switch (_state)
            {
                case ScrollState.Idle:
                    _state = ScrollState.PauseStart;
                    _pauseTimer = PauseDurationMs;
                    _scrollOffset = 0;
                    break;

                case ScrollState.PauseStart:
                    _pauseTimer -= elapsed;
                    if (_pauseTimer <= 0)
                        _state = ScrollState.ScrollingLeft;
                    break;

                case ScrollState.ScrollingLeft:
                    _scrollOffset += ScrollSpeed * (float)(elapsed / 1000.0);
                    if (_scrollOffset >= maxOffset)
                    {
                        _scrollOffset = maxOffset;
                        _state = ScrollState.PauseEnd;
                        _pauseTimer = PauseDurationMs;
                    }
                    break;

                case ScrollState.PauseEnd:
                    _pauseTimer -= elapsed;
                    if (_pauseTimer <= 0)
                    {
                        _scrollOffset = 0;
                        _state = ScrollState.PauseStart;
                        _pauseTimer = PauseDurationMs;
                    }
                    break;
            }
        }

        private void ResetScroll()
        {
            _scrollOffset = 0;
            _state = ScrollState.Idle;
            _pauseTimer = 0;
            _lastPaintTime = 0;
        }

        private void UpdateTooltip()
        {
            if (_font == null || string.IsNullOrEmpty(_text))
            {
                BasicTooltipText = null;
                return;
            }

            var textWidth = _font.MeasureString(_text).Width;
            BasicTooltipText = textWidth > Width ? _text : null;
        }
    }
}
