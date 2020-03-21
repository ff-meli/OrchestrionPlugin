using Dalamud.Game.Command;
using Dalamud.Plugin;
using System.IO;
using System.Reflection;

namespace OrchestrionPlugin
{
    // these interfaces got a bit out of hand now, probably not worth the isolation
    public class Plugin : IDalamudPlugin, IPlaybackController, IResourceLoader
    {
        public string Name => "Orchestrion plugin";

        private const string songListFile = "xiv_bgm.csv";
        private const string commandName = "/porch";

        private DalamudPluginInterface pi;
        private Configuration configuration;
        private SongList songList;
        private string localDir;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pi = pluginInterface;
            this.configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

            this.localDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            var songlistPath = Path.Combine(this.localDir, songListFile);
            this.songList = new SongList(songlistPath, this, this);

            pluginInterface.CommandManager.AddHandler(commandName, new CommandInfo(OnDisplayCommand)
            {
                HelpMessage = "Displays the orchestrion player, to view, change, or stop in-game BGM."
            });
            pluginInterface.UiBuilder.OnBuildUi += Display;

            // add a config UI for now, even though this isn't config
            pluginInterface.UiBuilder.OnOpenConfigUi += (sender, args) => this.OnDisplayCommand("", "");
        }

        public void Dispose()
        {
            this.songList.Dispose();

            this.pi.UiBuilder.OnBuildUi -= Display;
            this.pi.CommandManager.RemoveHandler(commandName);

            this.pi.Dispose();
        }

        public void PlaySong(int songId)
        {
            this.pi.CommandManager.Commands["/xlbgmset"].Handler("/xlbgmset", songId.ToString());
        }

        public void StopSong()
        {
            // still no real way to do this
            this.pi.CommandManager.Commands["/xlbgmset"].Handler("/xlbgmset", "9999");
        }

        public void AddFavorite(int songId)
        {
            this.configuration.FavoriteSongs.Add(songId);
            this.pi.SavePluginConfig(this.configuration);
        }

        public void RemoveFavorite(int songId)
        {
            this.configuration.FavoriteSongs.Remove(songId);
            this.pi.SavePluginConfig(this.configuration);
        }

        public bool IsFavorite(int songId) => this.configuration.FavoriteSongs.Contains(songId);

        public ImGuiScene.TextureWrap LoadUIImage(string imageFile)
        {
            var path = Path.Combine(this.localDir, imageFile);
            return this.pi.UiBuilder.LoadImage(path);
        }

        private void OnDisplayCommand(string command, string args)
        {
            // might be better to fully add/remove the OnBuildUi handler
            this.songList.Visible = true;
        }

        private void Display()
        {
            this.songList.Draw();
        }
    }
}
