
namespace OrchestrionPlugin
{
    interface IPlaybackController
    {
        void PlaySong(int songId);
        void StopSong();

        // ehhhh
        void AddFavorite(int songId);
        void RemoveFavorite(int songId);
        bool IsFavorite(int songId);
    }
}
