using System.Collections.Generic;
using Blish_HUD.Input;
using Blish_HUD.Settings;
using Microsoft.Xna.Framework.Input;

namespace Maestro.Settings
{
    public class ModuleSettings
    {
        public SettingEntry<KeyBinding> NoteC { get; private set; }
        public SettingEntry<KeyBinding> NoteD { get; private set; }
        public SettingEntry<KeyBinding> NoteE { get; private set; }
        public SettingEntry<KeyBinding> NoteF { get; private set; }
        public SettingEntry<KeyBinding> NoteG { get; private set; }
        public SettingEntry<KeyBinding> NoteA { get; private set; }
        public SettingEntry<KeyBinding> NoteB { get; private set; }
        public SettingEntry<KeyBinding> NoteCHigh { get; private set; }
        public SettingEntry<KeyBinding> OctaveUp { get; private set; }
        public SettingEntry<KeyBinding> OctaveDown { get; private set; }

        public SettingEntry<KeyBinding> SharpC { get; private set; }
        public SettingEntry<KeyBinding> SharpD { get; private set; }
        public SettingEntry<KeyBinding> SharpF { get; private set; }
        public SettingEntry<KeyBinding> SharpG { get; private set; }
        public SettingEntry<KeyBinding> SharpA { get; private set; }

        public ModuleSettings(SettingCollection settings)
        {
            DefineInstrumentKeys(settings);
            DefinePianoSharps(settings);
        }

        private void DefineInstrumentKeys(SettingCollection settings)
        {
            var instrumentKeys = settings.AddSubCollection("InstrumentKeys", true, () => "Instrument Keys");

            NoteC = instrumentKeys.DefineSetting("KeyNoteC",
                new KeyBinding(Keys.NumPad1),
                () => "Note C",
                () => "Match to Weapon Skill 1");

            NoteD = instrumentKeys.DefineSetting("KeyNoteD",
                new KeyBinding(Keys.NumPad2),
                () => "Note D",
                () => "Match to Weapon Skill 2");

            NoteE = instrumentKeys.DefineSetting("KeyNoteE",
                new KeyBinding(Keys.NumPad3),
                () => "Note E",
                () => "Match to Weapon Skill 3");

            NoteF = instrumentKeys.DefineSetting("KeyNoteF",
                new KeyBinding(Keys.NumPad4),
                () => "Note F",
                () => "Match to Weapon Skill 4");

            NoteG = instrumentKeys.DefineSetting("KeyNoteG",
                new KeyBinding(Keys.NumPad5),
                () => "Note G",
                () => "Match to Weapon Skill 5");

            NoteA = instrumentKeys.DefineSetting("KeyNoteA",
                new KeyBinding(Keys.NumPad6),
                () => "Note A",
                () => "Match to Healing Skill");

            NoteB = instrumentKeys.DefineSetting("KeyNoteB",
                new KeyBinding(Keys.NumPad7),
                () => "Note B",
                () => "Match to Utility Skill 1");

            NoteCHigh = instrumentKeys.DefineSetting("KeyNoteCHigh",
                new KeyBinding(Keys.NumPad8),
                () => "Note C High",
                () => "Match to Utility Skill 2");

            OctaveDown = instrumentKeys.DefineSetting("KeyOctaveDown",
                new KeyBinding(Keys.NumPad0),
                () => "Octave Down",
                () => "Match to Utility Skill 3");

            OctaveUp = instrumentKeys.DefineSetting("KeyOctaveUp",
                new KeyBinding(Keys.NumPad9),
                () => "Octave Up",
                () => "Match to Elite Skill");
        }

        private void DefinePianoSharps(SettingCollection settings)
        {
            var pianoSharps = settings.AddSubCollection("PianoSharps", true, () => "Piano Only - Sharp Notes - Keys must not conflict with natural note keybinds");

            SharpC = pianoSharps.DefineSetting("KeySharpC",
                new KeyBinding(ModifierKeys.Alt, Keys.D1),
                () => "Sharp C#",
                () => "Match to Profession Skill 1");

            SharpD = pianoSharps.DefineSetting("KeySharpD",
                new KeyBinding(ModifierKeys.Alt, Keys.D2),
                () => "Sharp D#",
                () => "Match to Profession Skill 2");

            SharpF = pianoSharps.DefineSetting("KeySharpF",
                new KeyBinding(ModifierKeys.Alt, Keys.D3),
                () => "Sharp F#",
                () => "Match to Profession Skill 3");

            SharpG = pianoSharps.DefineSetting("KeySharpG",
                new KeyBinding(ModifierKeys.Alt, Keys.D4),
                () => "Sharp G#",
                () => "Match to Profession Skill 4");

            SharpA = pianoSharps.DefineSetting("KeySharpA",
                new KeyBinding(ModifierKeys.Alt, Keys.D5),
                () => "Sharp A#",
                () => "Match to Profession Skill 5");
        }

        public Dictionary<Keys, SettingEntry<KeyBinding>> GetKeyMappings()
        {
            return new Dictionary<Keys, SettingEntry<KeyBinding>>
            {
                { Keys.NumPad1, NoteC },
                { Keys.NumPad2, NoteD },
                { Keys.NumPad3, NoteE },
                { Keys.NumPad4, NoteF },
                { Keys.NumPad5, NoteG },
                { Keys.NumPad6, NoteA },
                { Keys.NumPad7, NoteB },
                { Keys.NumPad8, NoteCHigh },
                { Keys.NumPad9, OctaveUp },
                { Keys.NumPad0, OctaveDown }
            };
        }

        public Dictionary<Keys, SettingEntry<KeyBinding>> GetSharpMappings()
        {
            return new Dictionary<Keys, SettingEntry<KeyBinding>>
            {
                { Keys.NumPad1, SharpC },  // Alt+1 = C#
                { Keys.NumPad2, SharpD },  // Alt+2 = D#
                { Keys.NumPad3, SharpF },  // Alt+3 = F#
                { Keys.NumPad4, SharpG },  // Alt+4 = G#
                { Keys.NumPad5, SharpA }   // Alt+5 = A#
            };
        }
    }
}
