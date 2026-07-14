using Blish_HUD;
using Blish_HUD.Input;
using Maestro.Services.Playback;
using Maestro.Settings;
using Maestro.UI.Main;
using Microsoft.Xna.Framework.Input;

namespace Maestro.Services.Practice
{
    /// <summary>
    /// Bridges Blish HUD's live keyboard input into a <see cref="PracticeSession"/>.
    /// Presses are resolved against the player's configured Maestro keybinds
    /// (<see cref="ModuleSettings"/>), so practice grades the same physical keys the
    /// player uses in game. Sharps are strict: the press must match the sharp binding
    /// (modifiers included). Echoes of Maestro's own injected keys and events while
    /// text input is focused are filtered out.
    /// </summary>
    public class PracticeInputListener
    {
        private static readonly Logger Logger = Logger.GetLogger<PracticeInputListener>();

        private struct LaneBinding
        {
            public Blish_HUD.Settings.SettingEntry<KeyBinding> Entry;
            public int Lane;
            public bool IsSharp;
        }

        private readonly KeyboardService _keyboard;
        private readonly LaneBinding[] _bindings;
        private PracticeSession _session;
        private bool _subscribed;

        public PracticeInputListener(KeyboardService keyboard, ModuleSettings settings)
        {
            _keyboard = keyboard;
            _bindings = new[]
            {
                // Sharps first: they carry modifiers, so on binding collisions the
                // stricter match must win.
                Bind(settings.SharpC, 1, true),
                Bind(settings.SharpD, 2, true),
                Bind(settings.SharpF, 4, true),
                Bind(settings.SharpG, 5, true),
                Bind(settings.SharpA, 6, true),
                Bind(settings.NoteC, 1, false),
                Bind(settings.NoteD, 2, false),
                Bind(settings.NoteE, 3, false),
                Bind(settings.NoteF, 4, false),
                Bind(settings.NoteG, 5, false),
                Bind(settings.NoteA, 6, false),
                Bind(settings.NoteB, 7, false),
                Bind(settings.NoteCHigh, 8, false),
            };
        }

        private static LaneBinding Bind(Blish_HUD.Settings.SettingEntry<KeyBinding> entry, int lane, bool isSharp) =>
            new LaneBinding { Entry = entry, Lane = lane, IsSharp = isSharp };

        // Number of explicit sharp bindings at the start of _bindings.
        private const int SharpCount = 5;

        // GW2 sharp skill slot (Alt+1..Alt+5) -> highway lane: C#, D#, F#, G#, A#.
        private static readonly int[] SharpLaneBySlot = { 1, 2, 4, 5, 6 };

        /// <summary>
        /// Subscribes to Blish HUD's global key-pressed event and routes presses to
        /// <paramref name="session"/>. Safe to call multiple times; re-subscription is a no-op.
        /// </summary>
        public void Attach(PracticeSession session)
        {
            _session = session;
            if (_subscribed) return;
            GameService.Input.Keyboard.KeyPressed += OnKeyPressed;
            _subscribed = true;
        }

        /// <summary>
        /// Unsubscribes from Blish HUD's key-pressed event and clears the session reference.
        /// Safe to call when not attached.
        /// </summary>
        public void Detach()
        {
            if (!_subscribed) return;
            GameService.Input.Keyboard.KeyPressed -= OnKeyPressed;
            _subscribed = false;
            _session = null;
        }

        private void OnKeyPressed(object sender, KeyboardEventArgs e)
        {
            if (_session == null) return;

            if (_keyboard.WasJustSent(e.Key)) return;

            if (SongFilterBar.IsTextInputFocused) return;
            if (GameService.Gw2Mumble.UI.IsTextInputFocused) return;

            // Snapshot the held modifiers from KeysDown rather than trusting
            // ActiveModifiers alone; both come from Blish's hook but KeysDown is the
            // raw state at this instant.
            var modifiers = KeysUtil.ModifiersFromKeys(GameService.Input.Keyboard.KeysDown);

            // Implicit sharp: GW2 packs the five sharps onto Alt + profession-skill
            // slots 1-5 in order (Alt+1 C#, Alt+2 D#, Alt+3 F#, Alt+4 G#, Alt+5 A# -
            // E has no sharp so the row compresses). Slot k's physical key is the
            // k-th natural binding (C..G), so "Alt + that key" resolves to the k-th
            // sharp even if the player's per-sharp Maestro bindings point elsewhere.
            // Checked before naturals so Alt+key never falls through to the natural.
            if (modifiers.HasFlag(ModifierKeys.Alt))
            {
                for (int slot = 0; slot < SharpLaneBySlot.Length; slot++)
                {
                    var natural = _bindings[SharpCount + slot];   // naturals follow the sharps, lanes 1..5 = slots 1..5
                    var binding = natural.Entry?.Value;
                    if (binding == null || binding.PrimaryKey == Keys.None) continue;
                    if (binding.PrimaryKey != e.Key) continue;
                    if ((binding.ModifierKeys | ModifierKeys.Alt) != modifiers) continue;

                    int sharpLane = SharpLaneBySlot[slot];
                    Logger.Debug($"Practice press {e.Key} (+{modifiers}) -> lane {sharpLane}# (sharp slot {slot + 1})");
                    _session.OnPlayerNotePressed(sharpLane, true, _session.Clock.CurrentMs);
                    return;
                }
            }

            // Pass 1: exact modifier match (sharps listed first, so a configured
            // sharp binding resolves before the bare natural).
            foreach (var b in _bindings)
            {
                var binding = b.Entry?.Value;
                if (binding == null || binding.PrimaryKey == Keys.None) continue;
                if (binding.PrimaryKey != e.Key) continue;
                if (binding.ModifierKeys != modifiers) continue;

                Logger.Debug($"Practice press {e.Key} (+{modifiers}) -> lane {b.Lane}{(b.IsSharp ? "#" : "")}");
                _session.OnPlayerNotePressed(b.Lane, b.IsSharp, _session.Clock.CurrentMs);
                return;
            }

            // Pass 2: tolerant match - all of the binding's modifiers are held, extra
            // held modifiers are ignored. Still prefers sharps over naturals.
            foreach (var b in _bindings)
            {
                var binding = b.Entry?.Value;
                if (binding == null || binding.PrimaryKey == Keys.None) continue;
                if (binding.PrimaryKey != e.Key) continue;
                if ((binding.ModifierKeys & modifiers) != binding.ModifierKeys) continue;

                Logger.Debug($"Practice press {e.Key} (+{modifiers}) ~> lane {b.Lane}{(b.IsSharp ? "#" : "")} (tolerant)");
                _session.OnPlayerNotePressed(b.Lane, b.IsSharp, _session.Clock.CurrentMs);
                return;
            }

            // Diagnostic: the key belongs to some lane binding but no modifier
            // combination matched - surfaces sharp-detection problems in the log.
            foreach (var b in _bindings)
            {
                var binding = b.Entry?.Value;
                if (binding != null && binding.PrimaryKey == e.Key)
                {
                    Logger.Info($"Practice press {e.Key} (+{modifiers}) matched no binding (candidate lane {b.Lane}{(b.IsSharp ? "#" : "")} wants +{binding.ModifierKeys})");
                    return;
                }
            }
        }
    }
}
