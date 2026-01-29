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
using Maestro.Services.Community;
using Maestro.Services.Data;
using Maestro.Services.Playback;
using Maestro.Settings;
using Maestro.UI.Community;
using Maestro.UI.Import;
using Maestro.UI.Main;
using Maestro.UI.MaestroCreator;
using Microsoft.Xna.Framework;

namespace Maestro
{
    [Export(typeof(Blish_HUD.Modules.Module))]
    public class Module : Blish_HUD.Modules.Module
    {
        private const int CORNER_ICON_PRIORITY = 1316531834;

        private static readonly Logger Logger = Logger.GetLogger<Module>();

        internal static Module Instance { get; private set; }

        internal SettingsManager SettingsManager => ModuleParameters.SettingsManager;
        internal ContentsManager ContentsManager => ModuleParameters.ContentsManager;
        internal DirectoriesManager DirectoriesManager => ModuleParameters.DirectoriesManager;
        internal Gw2ApiManager Gw2ApiManager => ModuleParameters.Gw2ApiManager;

        private ModuleSettings _moduleSettings;
        private KeyboardService _keyboardService;
        private SongPlayer _songPlayer;
        private SongStorage _songStorage;
        private CommunityService _communityService;
        private CommunityUploadService _uploadService;
        private UploadRateLimiter _uploadRateLimiter;
        private MaestroWindow _maestroWindow;
        private ImportWindow _importWindow;
        private CommunityWindow _communityWindow;
        private UploadWindow _uploadWindow;
        private MaestroCreatorWindow _maestroCreatorWindow;
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
            _songStorage = new SongStorage(DirectoriesManager);

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

            var userSongs = _songStorage.GetAllSongs();
            Logger.Info($"Loaded {userSongs.Count} user songs from storage");
            _songs.AddRange(userSongs);

            _communityService = new CommunityService(_songStorage, _songs);

            _uploadRateLimiter = new UploadRateLimiter(_songStorage.Database);
            _uploadRateLimiter.CleanupOldRecords();

            _uploadService = new CommunityUploadService(
                new CommunityApiClient(),
                _communityService,
                _uploadRateLimiter);
        }

        protected override void OnModuleLoaded(EventArgs e)
        {
            try
            {
                var iconTexture = ContentsManager.GetTexture("icon.png");
                _cornerIcon = new CornerIcon
                {
                    Icon = iconTexture ?? ContentService.Textures.Error,
                    BasicTooltipText = "Maestro - Music Player",
                    Priority = CORNER_ICON_PRIORITY
                };
                _cornerIcon.Click += OnCornerIconClick;
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to load icon, using default");
                _cornerIcon = new CornerIcon
                {
                    Icon = ContentService.Textures.Error,
                    BasicTooltipText = "Maestro - Music Player",
                    Priority = CORNER_ICON_PRIORITY
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
                _maestroWindow.CreateRequested += OnCreateRequested;
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
                _communityWindow.UploadRequested += OnUploadRequested;
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

        private void OnCreateRequested(object sender, InstrumentType instrument)
        {
            if (_maestroCreatorWindow == null)
            {
                _maestroCreatorWindow = new MaestroCreatorWindow();
                _maestroCreatorWindow.SongCreated += OnPianoSongCreated;
                _maestroCreatorWindow.WindowClosed += OnCreatorWindowClosed;
            }

            _maestroCreatorWindow.SetInstrument(instrument);

            if (_maestroCreatorWindow.Visible)
            {
                _maestroCreatorWindow.Hide();
            }
            else
            {
                _maestroWindow?.SetCreateButtonEnabled(false);
                _maestroCreatorWindow.Show();
            }
        }

        private void OnCreatorWindowClosed(object sender, EventArgs e)
        {
            _maestroWindow?.SetCreateButtonEnabled(true);
        }

        private void OnPianoSongCreated(object sender, Song song)
        {
            try
            {
                _songStorage.SaveSong(song);
                _maestroWindow?.AddImportedSong(song);
                Logger.Info($"Created and saved song: {song.Name} by {song.Artist}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to save created song: {song.Name}");
                ScreenNotification.ShowNotification("Failed to save song", ScreenNotification.NotificationType.Error);
            }
        }

        /// <summary>
        /// Plays a musical note on the in-game instrument.
        /// </summary>
        /// <param name="note">The note name (C, D, E, F, G, A, B).</param>
        /// <param name="isSharp">Whether to play the sharp variant.</param>
        /// <param name="isHighC">Whether this is high C.</param>
        public void PlayNote(string note, bool isSharp = false, bool isHighC = false)
        {
            _keyboardService?.PlayNoteByName(note, isSharp, isHighC);
        }

        /// <summary>
        /// Changes the in-game instrument octave.
        /// </summary>
        /// <param name="up">True to go up one octave, false to go down.</param>
        public void PlayOctaveChange(bool up)
        {
            _keyboardService?.PlayOctaveChange(up);
        }

        /// <summary>
        /// Pauses the currently playing song if one is active.
        /// </summary>
        public void PauseIfPlaying()
        {
            if (_songPlayer.IsPlaying && !_songPlayer.IsPaused)
                _songPlayer.Pause();
        }

        /// <summary>
        /// Resets the in-game instrument to middle octave.
        /// </summary>
        public void ResetToMiddleOctave()
        {
            _keyboardService?.ResetToMiddleOctave();
        }

        /// <summary>
        /// Starts playing a song using the SongPlayer.
        /// </summary>
        /// <param name="song">The song to play.</param>
        public void PreviewSong(Song song)
        {
            _songPlayer?.Play(song);
        }

        private void OnUploadRequested(object sender, EventArgs e)
        {
            if (_uploadWindow == null)
            {
                _uploadWindow = new UploadWindow(_uploadService, _songs);
                _uploadWindow.UploadCompleted += OnUploadCompleted;
            }

            if (_uploadWindow.Visible)
            {
                _uploadWindow.Hide();
            }
            else
            {
                _uploadWindow.Show();
            }
        }

        private void OnUploadCompleted(object sender, UploadResponse response)
        {
            if (response.Success)
            {
                Logger.Info($"Song upload complete. PR URL: {response.PrUrl}");
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

        private void OnSongImported(object sender, Song song)
        {
            try
            {
                _songStorage.SaveSong(song);
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
                _communityService.DeleteDownloadedSong(song);
                _maestroWindow?.RemoveSong(song);

                if (!string.IsNullOrEmpty(song.CommunityId))
                {
                    _communityWindow?.MarkSongAsDeleted(song.CommunityId);
                }

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
            _maestroCreatorWindow?.Dispose();
            _uploadWindow?.Dispose();
            _communityWindow?.Dispose();
            _importWindow?.Dispose();
            _maestroWindow?.Dispose();
            _communityService?.Dispose();
            _songStorage?.Dispose();
            _cornerIcon?.Dispose();

            Instance = null;
        }
    }
}
