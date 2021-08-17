using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace OrchestrionPlugin
{
    struct Song
    {
        public int Id;
        public string Name;
        public string Locations;
    }

    class SongList : IDisposable
    {
        private Dictionary<int, Song> songs = new Dictionary<int, Song>();
        private Configuration configuration;
        private IPlaybackController controller;
        private IResourceLoader loader;
        private int selectedSong;
        private string searchText = string.Empty;
        private ImGuiScene.TextureWrap favoriteIcon = null;
        private ImGuiScene.TextureWrap settingsIcon = null;
        private bool showDebugOptions = false;

        private bool visible = false;
        public bool Visible
        {
            get { return this.visible; }
            set { this.visible = value; }
        }

        private bool settingsVisible = false;
        public bool SettingsVisible
        {
            get { return this.settingsVisible; }
            set { this.settingsVisible = value; }
        }

        public bool AllowDebug { get; set; } = false;

        public SongList(Configuration configuration, IPlaybackController controller, IResourceLoader loader)
        {
            this.configuration = configuration;
            this.controller = controller;
            this.loader = loader;

            ParseSongs();
        }

        public void Dispose()
        {
            this.Stop();
            this.songs = null;
            this.favoriteIcon?.Dispose();
            this.settingsIcon?.Dispose();
        }

        private void ParseSongs()
        {
            var request = (HttpWebRequest)WebRequest.Create(configuration.XivBgmCsv);
            var response = (HttpWebResponse)request.GetResponse();

            using (var stream = new StreamReader(response.GetResponseStream()))
            {
                while (!stream.EndOfStream)
                {
                    var parts = stream.ReadLine().Split(',').ToList();
                    var escapedParts = parts.Select(x => Regex.Replace(x, "\"", string.Empty)).ToArray();
                    if (!int.TryParse(escapedParts[0], out int id))
                    {
                        continue;
                    }

                    var name = escapedParts[1];
                    if (id == 0 || string.IsNullOrEmpty(name) || name == "N/A")
                    {
                        continue;
                    }

                    var song = new Song
                    {
                        Id = id,
                        Name = name.Trim(),
                        Locations = string.Join(", ", escapedParts.Skip(2).Where(s => !string.IsNullOrEmpty(s)).ToArray()).Trim()
                    };

                    this.songs.Add(id, song);
                }
            }
        }

        private void Play(int songId)
        {
            this.controller.PlaySong(songId);
        }

        private void Stop()
        {
            this.controller.StopSong();
        }

        private bool IsFavorite(int songId) => this.configuration.FavoriteSongs.Contains(songId);

        private void AddFavorite(int songId)
        {
            this.configuration.FavoriteSongs.Add(songId);
            this.configuration.Save();
        }

        private void RemoveFavorite(int songId)
        {
            this.configuration.FavoriteSongs.Remove(songId);
            this.configuration.Save();
        }

        public string GetSongTitle(ushort id) => this.songs.ContainsKey(id) ? this.songs[id].Name : null;
        public Dictionary<int, Song> GetSongs() => this.songs;

        public void Draw()
        {
            // temporary bugfix for a race condition where it was possible that
            // we would attempt to load the icon before the ImGuiScene was created in dalamud
            // which would fail and lead to this icon being null
            // Hopefully later the UIBuilder API can add an event to notify when it is ready
            if (this.favoriteIcon == null)
            {
                this.favoriteIcon = loader.LoadUIImage("favoriteIcon.png");
                this.settingsIcon = loader.LoadUIImage("settings.png");
            }

            if (!Visible)
            {
                // manually draw this here only if the main window is hidden
                // This is just so the config ui can work independently
                if (SettingsVisible)
                {
                    DrawSettings();
                }
                return;
            }

            var windowTitle = new StringBuilder("Orchestrion");
            if (this.configuration.ShowSongInTitleBar)
            {
                // TODO: subscribe to the event so this only has to be constructed on change?
                var currentSong = this.controller.CurrentSong;
                if (this.songs.ContainsKey(currentSong))
                {
                    windowTitle.Append($" - [{this.songs[currentSong].Id}] {this.songs[currentSong].Name}");
                }
            }
            windowTitle.Append("###Orchestrion");

            ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(370, 150));
            ImGui.SetNextWindowSize(new Vector2(370, 440), ImGuiCond.FirstUseEver);
            // these flags prevent the entire window from getting a secondary scrollbar sometimes, and also keep it from randomly moving slightly with the scrollwheel
            if (ImGui.Begin(windowTitle.ToString(), ref this.visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Search: ");
                ImGui.SameLine();
                ImGui.InputText("##searchbox", ref searchText, 32);

                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetWindowSize().X - 32);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 1);
                if (ImGui.ImageButton(this.settingsIcon.ImGuiHandle, new Vector2(16, 16)))
                {
                    this.settingsVisible = true;
                }

                ImGui.Separator();

                ImGui.BeginChild("##songlist", new Vector2(0, -35));
                if (ImGui.BeginTabBar("##songlist tabs"))
                {
                    if (ImGui.BeginTabItem("All songs"))
                    {
                        DrawSonglist(false);
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("Favorites"))
                    {
                        DrawSonglist(true);
                        ImGui.EndTabItem();
                    }
                    ImGui.EndTabBar();
                }
                ImGui.EndChild();

                ImGui.Separator();

                ImGui.Columns(2, "footer columns", false);
                ImGui.SetColumnWidth(-1, ImGui.GetWindowSize().X - 150);

                ImGui.TextWrapped(this.selectedSong > 0 ? this.songs[this.selectedSong].Locations : string.Empty);

                ImGui.NextColumn();

                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetWindowSize().X - 150);
                ImGui.SetCursorPosY(ImGui.GetWindowSize().Y - 30);

                if (ImGui.Button("Stop"))
                {
                    Stop();
                }

                ImGui.SameLine();
                if (ImGui.Button("Play"))
                {
                    Play(this.selectedSong);
                }
                ImGui.SameLine();

                if (ImGui.Button("Shuffle"))
                {
                    Shuffle();
                }

                ImGui.Columns(1);
            }
            ImGui.End();

            ImGui.PopStyleVar();

            DrawSettings();
        }

        private void DrawSonglist(bool favoritesOnly)
        {
            // to keep the tab bar always visible and not have it get scrolled out
            ImGui.BeginChild("##songlist_internal");

            ImGui.Columns(2, "songlist columns", false);

            ImGui.SetColumnWidth(-1, 13);
            ImGui.SetColumnOffset(1, 12);

            foreach (var s in this.songs)
            {
                var song = s.Value;
                if (searchText.Length > 0 && !song.Name.ToLower().Contains(searchText.ToLower())
                    && !song.Locations.ToLower().Contains(searchText.ToLower())
                    && !song.Id.ToString().Contains(searchText))
                {
                    continue;
                }

                bool isFavorite = IsFavorite(song.Id);

                if (favoritesOnly && !isFavorite)
                {
                    continue;
                }

                ImGui.SetCursorPosX(-1);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3);

                if (isFavorite)
                {
                    ImGui.Image(favoriteIcon.ImGuiHandle, new Vector2(13, 13));
                    ImGui.SameLine();
                }

                ImGui.NextColumn();

                ImGui.Text(song.Id.ToString());
                ImGui.SameLine();
                if (ImGui.Selectable($"{song.Name}##{song.Id}", this.selectedSong == song.Id, ImGuiSelectableFlags.AllowDoubleClick))
                {
                    this.selectedSong = song.Id;
                    if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    {
                        Play(this.selectedSong);
                    }
                }
                if (ImGui.BeginPopupContextItem())
                {
                    if (!isFavorite)
                    {
                        if (ImGui.Selectable("Add to favorites"))
                        {
                            AddFavorite(song.Id);
                        }
                    }
                    else
                    {
                        if (ImGui.Selectable("Remove from favorites"))
                        {
                            RemoveFavorite(song.Id);
                        }
                    }
                    ImGui.EndPopup();
                }

                ImGui.NextColumn();
            }

            ImGui.EndChild();

            ImGui.Columns(1);
        }

        public void DrawSettings()
        {
            if (!this.settingsVisible)
            {
                return;
            }

            var settingsSize = AllowDebug ? new Vector2(490, 270) : new Vector2(490, 120);

            ImGui.SetNextWindowSize(settingsSize, ImGuiCond.Appearing);
            if (ImGui.Begin("Orchestrion Settings", ref this.settingsVisible, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse))
            {
                if (ImGui.IsWindowAppearing())
                {
                    this.showDebugOptions = false;
                }

                ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
                if (ImGui.TreeNode("Display##orch options"))
                {
                    ImGui.Spacing();

                    var showSongInTitlebar = this.configuration.ShowSongInTitleBar;
                    if (ImGui.Checkbox("Show current song in player title bar", ref showSongInTitlebar))
                    {
                        this.configuration.ShowSongInTitleBar = showSongInTitlebar;
                        this.configuration.Save();
                    }

                    var showSongInChat = this.configuration.ShowSongInChat;
                    if (ImGui.Checkbox("Show \"Now playing\" messages in game chat when the current song changes", ref showSongInChat))
                    {
                        this.configuration.ShowSongInChat = showSongInChat;
                        this.configuration.Save();
                    }

                    if (AllowDebug)
                    {
                        ImGui.Checkbox("Show debug options (Only if you have issues!)", ref this.showDebugOptions);
                    }

                    ImGui.TreePop();
                }

                // I'm sure there are better ways to do this, but I didn't want to change global spacing
                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Spacing();

                if (this.showDebugOptions && AllowDebug)
                {
                    ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
                    if (ImGui.TreeNode("Debug##orch options"))
                    {
                        ImGui.Spacing();

                        bool useFallbackPlayer = this.controller.EnableFallbackPlayer;
                        if (ImGui.Checkbox("Use fallback player", ref useFallbackPlayer))
                        {
                            this.Stop();
                            // this automatically will validate if we can change this, and save the config if so
                            this.controller.EnableFallbackPlayer = useFallbackPlayer;
                        }
                        ImGui.SameLine();
                        HelpMarker("This uses the old version of the player, in case the new version has problems.\n" +
                            "You typically should not use this unless the new version does not work at all.\n" +
                            "(In which case, please report it on discord!)");

                        ImGui.Spacing();

                        int targetPriority = this.configuration.TargetPriority;

                        ImGui.SetNextItemWidth(100.0f);
                        if (ImGui.SliderInt("BGM priority", ref targetPriority, 0, 11))
                        {
                            // stop the current song so it doesn't get 'stuck' on in case we switch to a lower priority
                            this.Stop();

                            this.configuration.TargetPriority = targetPriority;
                            this.configuration.Save();

                            // don't (re)start a song here for now
                        }
                        ImGui.SameLine();
                        HelpMarker("Songs play at various priority levels, from 0 to 11.\n" +
                            "Songs at lower numbers will override anything playing at a higher number, with 0 winning out over everything else.\n" +
                            "You can experiment with changing this value if you want the game to be able to play certain music even when Orchestrion is active.\n" +
                            "(Usually) zone music is 10-11, mount music is 6, GATEs are 4.  There is a lot of variety in this, however.\n" +
                            "The old Orchestrion used to play at level 3 (it now uses 0 by default).");

                        ImGui.Spacing();
                        if (ImGui.Button("Dump priority info"))
                        {
                            this.controller.DumpDebugInformation();
                        }

                        ImGui.TreePop();
                    }
                }
            }
            ImGui.End();
        }

        static void HelpMarker(string desc)
        {
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
                ImGui.TextUnformatted(desc);
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }
        }

        public void Shuffle()
        {
            this.controller.ShuffleSong();
        }
    }
}
