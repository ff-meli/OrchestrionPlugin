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

        private bool visible = false;
        public bool Visible
        {
            get { return this.visible; }
            set { this.visible = value; }
        }

        public SongList(string songListFile, IPlaybackController controller)
        {
            this.controller = controller;
            ParseSongs(songListFile);
        }

        public void Dispose()
        {
            this.Stop();
            this.songs = null;
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

            ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(370, 125));
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
                foreach (var s in this.songs)
                {
                    var song = s.Value;
                    if (searchText.Length > 0 && !song.Name.ToLower().Contains(searchText.ToLower())
                        && !song.Locations.ToLower().Contains(searchText.ToLower())
                        && !song.Id.ToString().Contains(searchText))
                    {
                        continue;
                    }

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
                }
                ImGui.EndChild();

                ImGui.Separator();

                ImGui.Columns(2, "orch columns", false);
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
    }
}
