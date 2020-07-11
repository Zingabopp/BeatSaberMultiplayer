using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaberMultiplayerLite.Interop
{
    public class BeatSaberPlaylistsLibLoader : IPlaylistLoader
    {
        public bool IncludesBasePlaylists => false;

        public IAnnotatedBeatmapLevelCollection[] GetCustomPlaylists()
        {
            try
            {
                var playlists = BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.GetAllPlaylists();
                Plugin.log.Debug($"Received {playlists.Length} playlists from BeatSaberPlaylistsLibLoader.");
                return playlists;
            }
            catch (Exception ex)
            {
                Plugin.log?.Warn($"Error loading playlists from BeatSaberPlaylistsLib: {ex.Message}");
                Plugin.log?.Debug(ex);
                return Array.Empty<IAnnotatedBeatmapLevelCollection>();
            }
        }
    }
}
