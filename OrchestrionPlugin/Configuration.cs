using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace OrchestrionPlugin
{
    [Serializable]
    class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        public bool ShowSongInTitleBar { get; set; } = true;
        public bool ShowSongInChat { get; set; } = true;

        public bool UseOldPlayback { get; set; } = false;
        public int TargetPriority { get; set; } = 0;
        public string XivBgmCsv { get; } = "https://docs.google.com/spreadsheets/d/14yjTMHYmuB1m5-aJO8CkMferRT9sNzgasYq02oJENWs/gviz/tq?tqx=out:csv&sheet=FFXIVBG:ID";

        public HashSet<int> FavoriteSongs { get; internal set; } = new HashSet<int>();

        [NonSerialized]
        private DalamudPluginInterface pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface.SavePluginConfig(this);
        }
    }
}
