using ImGuiNET;
using ImGuiScene;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using static SDL2.SDL;

namespace OrchestrionTest
{
    class Program
    {
        static TextureWrap starImg;
        static TextureWrap settingsImg;

        static void Main(string[] args)
        {
            using (var scene = new SimpleImGuiScene(RendererFactory.RendererBackend.DirectX11, new WindowCreateInfo
            {
                Title = "UI Test",
                Fullscreen = true,
                TransparentColor = new float[] { 0, 0, 0 },
            }))
            {
                scene.Renderer.ClearColor = new Vector4(0, 0, 0, 0);

                scene.Window.OnSDLEvent += (ref SDL_Event sdlEvent) =>
                {
                    if (sdlEvent.type == SDL_EventType.SDL_KEYDOWN && sdlEvent.key.keysym.scancode == SDL_Scancode.SDL_SCANCODE_ESCAPE)
                    {
                        scene.ShouldQuit = true;
                    }
                };

                var fontPathJp = @"NotoSansCJKjp-Medium.otf";
                ImGui.GetIO().Fonts.AddFontFromFileTTF(fontPathJp, 17.0f, null, ImGui.GetIO().Fonts.GetGlyphRangesJapanese());

                //ImGui.GetIO().Fonts.Build();

                ImGui.GetStyle().GrabRounding = 3f;
                ImGui.GetStyle().FrameRounding = 4f;
                ImGui.GetStyle().WindowRounding = 4f;
                ImGui.GetStyle().WindowBorderSize = 0f;
                ImGui.GetStyle().WindowMenuButtonPosition = ImGuiDir.Right;
                ImGui.GetStyle().ScrollbarSize = 16f;

                ImGui.GetStyle().Colors[(int)ImGuiCol.WindowBg] = new Vector4(0.06f, 0.06f, 0.06f, 0.87f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBg] = new Vector4(0.29f, 0.29f, 0.29f, 0.54f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.54f, 0.54f, 0.54f, 0.40f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.64f, 0.64f, 0.64f, 0.67f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.29f, 0.29f, 0.29f, 1.00f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.CheckMark] = new Vector4(0.86f, 0.86f, 0.86f, 1.00f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.54f, 0.54f, 0.54f, 1.00f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.67f, 0.67f, 0.67f, 1.00f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.Button] = new Vector4(0.71f, 0.71f, 0.71f, 0.40f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.47f, 0.47f, 0.47f, 1.00f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.74f, 0.74f, 0.74f, 1.00f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.Header] = new Vector4(0.59f, 0.59f, 0.59f, 0.31f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.50f, 0.50f, 0.50f, 0.80f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.60f, 0.60f, 0.60f, 1.00f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.79f, 0.79f, 0.79f, 0.25f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.78f, 0.78f, 0.78f, 0.67f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.88f, 0.88f, 0.88f, 0.95f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.Tab] = new Vector4(0.23f, 0.23f, 0.23f, 0.86f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.TabHovered] = new Vector4(0.71f, 0.71f, 0.71f, 0.80f);
                ImGui.GetStyle().Colors[(int)ImGuiCol.TabActive] = new Vector4(0.36f, 0.36f, 0.36f, 1.00f);

                Init(scene);

                scene.Run();
            }
        }

        static void Init(SimpleImGuiScene scene)
        {
            ParseSongs("xiv_bgm.csv");
            starImg = scene.LoadImage("favoriteIcon.png");
            settingsImg = scene.LoadImage("settings.png");

            scene.OnBuildUI += Display;
        }

        struct Song
        {
            public int Id;
            public string Name;
            public string Locations;
        }

        private static Dictionary<int, Song> _songs = new Dictionary<int, Song>();
        private static HashSet<int> _favorites = new HashSet<int>();

        private static void ParseSongs(string path)
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

                    _songs.Add(id, song);
                }
            }
        }


        private static int _selected;
        private static string _searchText = string.Empty;
        private static bool _showSongInTitleBar = true;
        private static bool _visible = true;
        private static bool _drawSettings = false;
        private static bool _showAdvancedOptions = false;
        private static bool _showSongChanges = false;
        private static bool _useFallbackPlayer = false;
        private static int _targetPriority = 0;
        private static bool _allowDebug = true;

        private static void Display()
        {
            // don't actually allow closing in the test ui
            _visible = true;

            var windowTitle = new StringBuilder("Orchestrion");
            if (_showSongInTitleBar)
            {
                var currentSong = 442;
                if (_songs.ContainsKey(currentSong))
                {
                    windowTitle.Append($" - [{_songs[currentSong].Id}] {_songs[currentSong].Name}");
                }
            }
            windowTitle.Append("###Orchestrion");

            ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(370, 150));
            ImGui.SetNextWindowSize(new Vector2(370, 440), ImGuiCond.FirstUseEver);
            // these flags prevent the entire window from getting a secondary scrollbar sometimes, and also keep it from randomly moving slightly with the scrollwheel
            if (ImGui.Begin(windowTitle.ToString(), ref _visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                ImGui.AlignTextToFramePadding();
                ImGui.Text("Search: ");
                ImGui.SameLine();
                ImGui.InputText("##searchbox", ref _searchText, 32);

                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetWindowSize().X - 32);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 1);
                if (ImGui.ImageButton(settingsImg.ImGuiHandle, new Vector2(16, 16)))
                {
                    _drawSettings = true;
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

                ImGui.TextWrapped(_selected > 0 ? _songs[_selected].Locations : string.Empty);

                ImGui.NextColumn();

                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetWindowSize().X - 150);
                ImGui.SetCursorPosY(ImGui.GetWindowSize().Y - 30);

                ImGui.Button("Stop");
                ImGui.SameLine();
                ImGui.Button("Play");
                ImGui.SameLine();
                ImGui.Button("Shuffle");


                ImGui.Columns(1);
            }

            ImGui.End();

            ImGui.PopStyleVar();

            DrawSettings();
        }

        private static void DrawSonglist(bool favoritesOnly)
        {
            // to keep the tab bar always visible and not have it get scrolled out
            ImGui.BeginChild("##songlist_internal");

            ImGui.Columns(2, "songlist columns", false);

            ImGui.SetColumnWidth(-1, 13);
            ImGui.SetColumnOffset(1, 12);

            foreach (var s in _songs)
            {
                var song = s.Value;
                if (_searchText.Length > 0 && !song.Name.ToLower().Contains(_searchText.ToLower())
                    && !song.Locations.ToLower().Contains(_searchText.ToLower())
                    && !song.Id.ToString().Contains(_searchText))
                {
                    continue;
                }

                bool isFavorite = _favorites.Contains(song.Id);

                if (favoritesOnly && !isFavorite)
                {
                    continue;
                }

                ImGui.SetCursorPosX(-1);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 3);

                if (isFavorite)
                {
                    ImGui.Image(starImg.ImGuiHandle, new Vector2(13, 13));
                    ImGui.SameLine();
                }

                ImGui.NextColumn();

                ImGui.Text(song.Id.ToString());
                ImGui.SameLine();
                if (ImGui.Selectable($"{song.Name}##{song.Id}", _selected == song.Id, ImGuiSelectableFlags.AllowDoubleClick))
                {
                    _selected = song.Id;
                    if (ImGui.IsMouseDoubleClicked(0))
                    {
                    }
                }
                if (ImGui.BeginPopupContextItem())
                {
                    if (!isFavorite)
                    {
                        if (ImGui.Selectable("Add to favorites"))
                        {
                            _favorites.Add(song.Id);
                        }
                    }
                    else
                    {
                        if (ImGui.Selectable("Remove from favorites"))
                        {
                            _favorites.Remove(song.Id);
                        }
                    }
                    ImGui.EndPopup();
                }

                ImGui.NextColumn();
            }

            ImGui.EndChild();

            ImGui.Columns(1);
        }

        private static void DrawSettings()
        {
            if (!_drawSettings)
            {
                return;
            }

            var settingsSize = _allowDebug ? new Vector2(490, 270) : new Vector2(490, 120);

            ImGui.SetNextWindowSize(settingsSize, ImGuiCond.Appearing);
            if (ImGui.Begin("Orchestrion Settings", ref _drawSettings, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse))
            {
                if (ImGui.IsWindowAppearing())
                {
                    _showAdvancedOptions = false;
                }

                ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
                if (ImGui.TreeNode("Display##orch options"))
                {
                    ImGui.Spacing();
                    ImGui.Checkbox("Show current song in player title bar", ref _showSongInTitleBar);
                    ImGui.Checkbox("Show \"Now playing\" messages in game chat when the current song changes", ref _showSongChanges);

                    if (_allowDebug)
                    {
                        ImGui.Checkbox("Show debug options (You probably do NOT want this!)", ref _showAdvancedOptions);
                    }

                    ImGui.TreePop();
                }

                // I'm sure there are better ways to do this, but I didn't want to change global spacing
                ImGui.Spacing();
                ImGui.Spacing();
                ImGui.Spacing();

                if (_allowDebug && _showAdvancedOptions)
                {
                    ImGui.SetNextItemOpen(false, ImGuiCond.Appearing);
                    if (ImGui.TreeNode("Debug##orch options"))
                    {
                        ImGui.Spacing();

                        ImGui.Checkbox("Use fallback player", ref _useFallbackPlayer);
                        ImGui.SameLine();
                        HelpMarker("This uses the old version of the player, in case the new version has problems.\n" +
                            "You typically should not use this unless the new version does not work at all.\n" +
                            "(In which case, please report it on discord!)");

                        ImGui.Spacing();

                        ImGui.SetNextItemWidth(100.0f);
                        ImGui.SliderInt("BGM priority", ref _targetPriority, 0, 11);
                        ImGui.SameLine();
                        HelpMarker("Songs play at various priority levels, from 0 to 11.\n" +
                            "Songs at lower numbers will override anything playing at a higher number, with 0 winning out over everything else.\n" +
                            "You can experiment with changing this value if you want the game to be able to play certain music even when Orchestrion is active.\n" +
                            "(Usually) zone music is 10-11, mount music is 6, GATEs are 4.  There is a lot of variety in this, however.\n" +
                            "The old Orchestrion used to play at level 3 (it now uses 0 by default).");

                        ImGui.Spacing();
                        ImGui.Button("Dump priority info");

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
    }
}
