using Dalamud.Configuration;
using System;
using System.Collections.Generic;

namespace OrchestrionPlugin
{
    [Serializable]
    class Configuration : IPluginConfiguration
    {
        // does this have a point?
        int IPluginConfiguration.Version { get; set; }

        public HashSet<int> FavoriteSongs { get; internal set; } = new HashSet<int>();
    }
}
