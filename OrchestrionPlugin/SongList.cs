using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

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
        private IPlaybackController controller;
        private int selectedSong;
        private string searchText = string.Empty;
        private ImGuiScene.TextureWrap favoriteIcon;

        private bool visible = false;
        public bool Visible
        {
            get { return this.visible; }
            set { this.visible = value; }
        }

        public SongList(string songListFile, IPlaybackController controller, IResourceLoader loader)
        {
            this.controller = controller;
            this.favoriteIcon = loader.LoadUIImage(@"favoriteIcon.png");

            ParseSongs(songListFile);
        }

        public void Dispose()
        {
            this.Stop();
            this.songs = null;
            this.favoriteIcon.Dispose();
        }

        private void ParseSongs(string path)
        {
            using (var stream = new StreamReader(path))
            {
                while (!stream.EndOfStream)
                {
                    var parts = stream.ReadLine().Split(';');
                    if (parts.Length < 2)
                    {
                        continue;
                    }

                    if (!int.TryParse(parts[0], out int id))
                    {
                        continue;
                    }

                    var name = parts[1];
                    if (id == 0 || string.IsNullOrEmpty(name) || name == "N/A")
                    {
                        continue;
                    }

                    var song = new Song
                    {
                        Id = id,
                        Name = name.Trim(),
                        Locations = string.Join(", ", parts.Skip(2).Where(s => !string.IsNullOrEmpty(s)).ToArray()).Trim()
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

        public void Draw()
        {
            if (!Visible)
                return;

            ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(370, 150));
            ImGui.SetNextWindowSize(new Vector2(370, 440), ImGuiCond.FirstUseEver);
            // these flags prevent the entire window from getting a secondary scrollbar sometimes, and also keep it from randomly moving slightly with the scrollwheel
            if (ImGui.Begin("Orchestrion", ref this.visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Search: ");
                ImGui.SameLine();
                ImGui.InputText("##searchbox", ref searchText, 32);
                ImGui.NextColumn();

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
                ImGui.SetColumnWidth(-1, ImGui.GetWindowSize().X - 100);

                ImGui.TextWrapped(this.selectedSong > 0 ? this.songs[this.selectedSong].Locations : string.Empty);

                ImGui.NextColumn();

                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetWindowSize().X - 100);
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

                ImGui.Columns(1);
            }
            ImGui.End();

            ImGui.PopStyleVar();
        }

        private void DrawSonglist(bool favoritesOnly)
        {
            // to keep the tab bar always visible and not have it get scrolled out
            ImGui.BeginChild("%%songlist_internal");

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

                bool isFavorite = this.controller.IsFavorite(song.Id);

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
                    if (ImGui.IsMouseDoubleClicked(0))
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
                            this.controller.AddFavorite(song.Id);
                        }
                    }
                    else
                    {
                        if (ImGui.Selectable("Remove from favorites"))
                        {
                            this.controller.RemoveFavorite(song.Id);
                        }
                    }
                    ImGui.EndPopup();
                }

                ImGui.NextColumn();
            }

            ImGui.EndChild();

            ImGui.Columns(1);
        }
    }
}
