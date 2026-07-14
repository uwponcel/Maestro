using Microsoft.Xna.Framework.Graphics;

namespace Maestro.UI.Controls
{
    /// <summary>
    /// Cached accessors for the module's embedded glyph icons (ref/*.png).
    /// Textures are loaded once and shared; do not dispose them.
    /// </summary>
    public static class MaestroIcons
    {
        private static Texture2D _play;
        private static Texture2D _pause;
        private static Texture2D _stop;
        private static Texture2D _queue;
        private static Texture2D _community;
        private static Texture2D _create;
        private static Texture2D _import;
        private static Texture2D _starFilled;
        private static Texture2D _starOutline;
        private static Texture2D _next;
        private static Texture2D _trash;
        private static Texture2D _clipboard;
        private static Texture2D _cancel;
        private static Texture2D _download;
        private static Texture2D _upload;
        private static Texture2D _check;
        private static Texture2D _refresh;
        private static Texture2D _undo;
        private static Texture2D _save;
        private static Texture2D _section;
        private static Texture2D _playAll;
        private static Texture2D _repeat;
        private static Texture2D _shuffle;
        private static Texture2D _practice;

        public static Texture2D Play => _play ?? (_play = Load("play-icon.png"));
        public static Texture2D Pause => _pause ?? (_pause = Load("pause-icon.png"));
        public static Texture2D Stop => _stop ?? (_stop = Load("stop-icon.png"));
        public static Texture2D Queue => _queue ?? (_queue = Load("queue-icon.png"));
        public static Texture2D Community => _community ?? (_community = Load("community-icon.png"));
        public static Texture2D Create => _create ?? (_create = Load("create-icon.png"));
        public static Texture2D Import => _import ?? (_import = Load("import-icon.png"));
        public static Texture2D StarFilled => _starFilled ?? (_starFilled = Load("star-filled.png"));
        public static Texture2D StarOutline => _starOutline ?? (_starOutline = Load("star-outline.png"));
        public static Texture2D Next => _next ?? (_next = Load("next-icon.png"));
        public static Texture2D Trash => _trash ?? (_trash = Load("trash-icon.png"));
        public static Texture2D Clipboard => _clipboard ?? (_clipboard = Load("clipboard-icon.png"));
        public static Texture2D Cancel => _cancel ?? (_cancel = Load("cancel-icon.png"));
        public static Texture2D Download => _download ?? (_download = Load("download-icon.png"));
        public static Texture2D Upload => _upload ?? (_upload = Load("upload-icon.png"));
        public static Texture2D Check => _check ?? (_check = Load("check-icon.png"));
        public static Texture2D Refresh => _refresh ?? (_refresh = Load("refresh-icon.png"));
        public static Texture2D Undo => _undo ?? (_undo = Load("undo-icon.png"));
        public static Texture2D Save => _save ?? (_save = Load("save-icon.png"));
        public static Texture2D Section => _section ?? (_section = Load("section-icon.png"));
        public static Texture2D PlayAll => _playAll ?? (_playAll = Load("playall-icon.png"));
        public static Texture2D Repeat => _repeat ?? (_repeat = Load("repeat-icon.png"));
        public static Texture2D Shuffle => _shuffle ?? (_shuffle = Load("shuffle-icon.png"));
        public static Texture2D Practice => _practice ?? (_practice = Load("practice-icon.png"));

        private static Texture2D Load(string fileName)
        {
            return Module.Instance.ContentsManager.GetTexture(fileName);
        }
    }
}
