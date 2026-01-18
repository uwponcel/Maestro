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
using Maestro.Services.Community;
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
        private CommunitySongCache _songCache;
        private UserSongStorage _userSongStorage;
        private CommunityService _communityService;
        private MaestroWindow _maestroWindow;
        private ImportWindow _importWindow;
        private CommunityWindow _communityWindow;
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
            _songCache = new CommunitySongCache(DirectoriesManager);
            _userSongStorage = new UserSongStorage(_songCache);

            const string debugSongsPath = @"C:\git\perso\Maestro\Songs";

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

            _communityService = new CommunityService(_songCache, _songs);
            _communityService.LoadCachedSongsIntoMainList();
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
                _maestroWindow.CommunityRequested += OnCommunityRequested;
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

            if (_importWindow.Visible)
                _importWindow.Hide();
            else
                _importWindow.Show();
        }

        private void OnCommunityRequested(object sender, EventArgs e)
        {
            if (_communityWindow == null)
            {
                _communityWindow = new CommunityWindow(_communityService);
                _communityWindow.SongDownloaded += OnCommunitySongDownloaded;
                _communityWindow.SongDeleteRequested += OnCommunitySongDeleteRequested;
            }

            if (_communityWindow.Visible)
            {
                _communityWindow.Hide();
            }
            else
            {
                _communityWindow.Show();
                _communityWindow.LoadContent();
            }
        }

        private void OnCommunitySongDownloaded(object sender, Song song)
        {
            _maestroWindow?.RefreshAfterCommunityDownload();
        }

        private void OnCommunitySongDeleteRequested(object sender, string communityId)
        {
            var song = _songs.Find(s => s.CommunityId == communityId);
            if (song != null)
            {
                OnSongDeleteRequested(this, song);
            }
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
            try
            {
                if (song.IsUserImported)
                {
                    _userSongStorage.DeleteSong(song);
                    _maestroWindow?.RemoveSong(song);
                    Logger.Info($"Deleted user song: {song.Name} by {song.Artist}");
                }
                else if (song.IsCommunityDownloaded)
                {
                    _communityService.DeleteDownloadedSong(song);
                    _maestroWindow?.RemoveSong(song);
                    _communityWindow?.MarkSongAsDeleted(song.CommunityId);
                    Logger.Info($"Deleted community song: {song.Name} by {song.Artist}");
                }
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
            _communityWindow?.Dispose();
            _importWindow?.Dispose();
            _maestroWindow?.Dispose();
            _communityService?.Dispose();
            _songCache?.Dispose();
            _cornerIcon?.Dispose();

            Instance = null;
        }
    }
}
