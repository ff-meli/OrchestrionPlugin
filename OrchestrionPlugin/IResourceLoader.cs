using System;

namespace OrchestrionPlugin
{
    interface IResourceLoader
    {
        ImGuiScene.TextureWrap LoadUIImage(string path);
    }
}
