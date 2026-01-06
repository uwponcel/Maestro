using Microsoft.Xna.Framework.Input;

namespace Maestro.Models
{
    public enum CommandType
    {
        KeyDown,
        KeyUp,
        Wait
    }

    public class SongCommand
    {
        public CommandType Type { get; set; }
        public Keys Key { get; set; }
        public int Duration { get; set; }

        public static SongCommand KeyDownCmd(Keys key) => new SongCommand { Type = CommandType.KeyDown, Key = key };
        public static SongCommand KeyUpCmd(Keys key) => new SongCommand { Type = CommandType.KeyUp, Key = key };
        public static SongCommand WaitCmd(int ms) => new SongCommand { Type = CommandType.Wait, Duration = ms };
    }
}
