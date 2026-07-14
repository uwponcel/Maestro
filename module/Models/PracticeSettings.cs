using Blish_HUD.Settings;

namespace Maestro.Models
{
    public class PracticeSettings
    {
        public SettingEntry<float> LastUsedSpeed { get; }
        public SettingEntry<float> LookaheadSeconds { get; }
        public SettingEntry<int> CountdownLengthMs { get; }

        public PracticeSettings(SettingCollection settings)
        {
            LastUsedSpeed = settings.DefineSetting(
                "practice.lastUsedSpeed",
                1.0f,
                () => "Last used practice speed",
                () => "Internal: remembers the last practice speed multiplier.");

            LookaheadSeconds = settings.DefineSetting(
                "practice.lookaheadSeconds",
                2.5f,
                () => "Practice: lookahead (seconds)",
                () => "How far ahead in the song the highway shows. Lower = faster scroll, shorter reaction time.");
            LookaheadSeconds.SetRange(1.0f, 5.0f);

            CountdownLengthMs = settings.DefineSetting(
                "practice.countdownLengthMs",
                3000,
                () => "Practice: countdown length (ms)",
                () => "How long the 3-2-1 countdown lasts before a practice session starts.");
            CountdownLengthMs.SetRange(1000, 6000);
        }
    }
}
