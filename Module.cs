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
        private UserSongStorage _userSongStorage;
        private MaestroWindow _maestroWindow;
        private ImportWindow _importWindow;
        private CornerIcon _cornerIcon;
        private List<Song> _songs;

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
            _userSongStorage = new UserSongStorage(DirectoriesManager);

            const string debugSongsPath = @"C:\git\Maestro\Songs";

            if (Directory.Exists(debugSongsPath))
            {
                Logger.Info("Debug mode: Loading songs from directory");
                _songs = await SongLoader.LoadFromDirectoryAsync(debugSongsPath);
            }
            else
            {
                Logger.Info("Production mode: Loading songs from ContentsManager");
                _songs = await SongLoader.LoadFromContentsManagerAsync(ContentsManager);
            }

            var userSongs = await _userSongStorage.LoadUserSongsAsync();
            _songs.AddRange(userSongs);
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
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

        private void OnCornerIconClick(object sender, MouseEventArgs e)
        {
            if (_maestroWindow == null)
            {
                _maestroWindow = new MaestroWindow(_songPlayer, _songs);
                _maestroWindow.ImportRequested += OnImportRequested;
                _maestroWindow.SongDeleteRequested += OnSongDeleteRequested;
            }

            _maestroWindow.ToggleWindow();
        }

        private void OnImportRequested(object sender, EventArgs e)
        {
            if (_importWindow == null)
            {
                _importWindow = new ImportWindow();
                _importWindow.SongImported += OnSongImported;
            }

            _importWindow.Show();
        }

        private async void OnSongImported(object sender, Song song)
        {
            try
            {
                await _userSongStorage.SaveSongAsync(song);
                _maestroWindow?.AddImportedSong(song);
                Logger.Info($"Imported and saved song: {song.Name} by {song.Artist}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to save imported song: {song.Name}");
                ScreenNotification.ShowNotification("Failed to save song", ScreenNotification.NotificationType.Error);
            }
        }

        private void OnSongDeleteRequested(object sender, Song song)
        {
            if (!song.IsUserImported)
                return;

            try
            {
                _userSongStorage.DeleteSong(song);
                _maestroWindow?.RemoveSong(song);
                Logger.Info($"Deleted song: {song.Name} by {song.Artist}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to delete song: {song.Name}");
                ScreenNotification.ShowNotification("Failed to delete song", ScreenNotification.NotificationType.Error);
            }
        }

        protected override void Update(GameTime gameTime)
        {
        }

        protected override void Unload()
        {
            _songPlayer?.Stop();
            _importWindow?.Dispose();
            _maestroWindow?.Dispose();
            _cornerIcon?.Dispose();

            Instance = null;
        }
    }
}
