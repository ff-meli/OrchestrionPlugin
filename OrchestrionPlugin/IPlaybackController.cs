
namespace OrchestrionPlugin
{
    interface IPlaybackController
    {
        int CurrentSong { get; }
        bool EnableFallbackPlayer { get; set; }
        void PlaySong(int songId);
        void StopSong();

        void ShuffleSong();

        void DumpDebugInformation();
    }
}
