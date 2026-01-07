using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Blish_HUD.Modules;
using Blish_HUD.Modules.Managers;
using Blish_HUD.Settings;
using Maestro.Models;
using Maestro.Services;
using Maestro.Services.Data;
using Maestro.Services.Playback;
using Maestro.Settings;
using Maestro.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maestro
{
    [Export(typeof(Blish_HUD.Modules.Module))]
    public class Module : Blish_HUD.Modules.Module
    {
        private static readonly Logger Logger = Logger.GetLogger<Module>();

        internal static Module Instance { get; private set; }

        #region Service Managers

        internal SettingsManager SettingsManager => ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => ModuleParameters.Gw2ApiManager;

        #endregion

        private ModuleSettings _moduleSettings;
        private KeyboardService _keyboardService;
        private SongPlayer _songPlayer;
        private MaestroWindow _maestroWindow;
        private CornerIcon _cornerIcon;
        private List<Song> _songs;
        private Texture2D _windowBackground;

        [ImportingConstructor]
        public Module([Import("ModuleParameters")] ModuleParameters moduleParameters)
            : base(moduleParameters)
        {
            Instance = this;
        }

        protected override void DefineSettings(SettingCollection settings)
        {
            _moduleSettings = new ModuleSettings(settings);
        }

        protected override void Initialize()
        {
            _keyboardService = new KeyboardService(
                _moduleSettings.GetKeyMappings(),
                _moduleSettings.GetSharpMappings());
            _songPlayer = new SongPlayer(_keyboardService);
            _songs = new List<Song>();
        }

        protected override async Task LoadAsync()
        {
            var songsDirectory = GetSongsDirectory();
            _songs = await SongLoader.LoadAllAsync(songsDirectory);
        }

        private string GetSongsDirectory()
        {
            const string debugSongsPath = @"C:\git\Maestro\Songs";

            try
            {
                var assemblyLocation = GetType().Assembly.Location;
                if (!string.IsNullOrEmpty(assemblyLocation))
                {
                    var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
                    var embeddedSongsPath = Path.Combine(assemblyDirectory, "Songs");

                    if (Directory.Exists(embeddedSongsPath))
                    {
                        Logger.Info($"Using embedded songs path: {embeddedSongsPath}");
                        return embeddedSongsPath;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Debug($"Could not get assembly location: {ex.Message}");
            }

            if (Directory.Exists(debugSongsPath))
            {
                Logger.Info("Using debug songs path");
                return debugSongsPath;
            }

            Logger.Warn("No songs directory found");
            return debugSongsPath;
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            CreateWindowBackground();

            try
            {
                var iconTexture = ContentsManager.GetTexture("icon.png");
                _cornerIcon = new CornerIcon
                {
                    Icon = iconTexture ?? ContentService.Textures.Error,
                    BasicTooltipText = "Maestro - Music Player"
                };
                _cornerIcon.Click += OnCornerIconClick;
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to load icon, using default");
                _cornerIcon = new CornerIcon
                {
                    Icon = ContentService.Textures.Error,
                    BasicTooltipText = "Maestro - Music Player"
                };
                _cornerIcon.Click += OnCornerIconClick;
            }

            base.OnModuleLoaded(e);
        }

        private void CreateWindowBackground()
        {
            const int width = 419;
            const int height = 447;

            var graphicsDeviceContext = GameService.Graphics.LendGraphicsDeviceContext();
            try
            {
                _windowBackground = new Texture2D(graphicsDeviceContext.GraphicsDevice, width, height);
                var data = new Color[width * height];

                for (var i = 0; i < data.Length; i++)
                {
                    data[i] = new Color(30, 30, 30, 255);
                }

                _windowBackground.SetData(data);
            }
            finally
            {
                graphicsDeviceContext.Dispose();
            }
        }

        private void OnCornerIconClick(object sender, MouseEventArgs e)
        {
            if (_maestroWindow == null)
            {
                _maestroWindow = new MaestroWindow(_windowBackground, _songPlayer, _songs);
            }

            _maestroWindow.ToggleWindow();
        }

        protected override void Update(GameTime gameTime)
        {
        }

        protected override void Unload()
        {
            _songPlayer?.Stop();
            _maestroWindow?.Dispose();
            _cornerIcon?.Dispose();
            _windowBackground?.Dispose();

            Instance = null;
        }
    }
}
