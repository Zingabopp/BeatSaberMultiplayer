using IPA.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BeatSaberMultiplayerLite.Interop
{
    public class FallbackPlaylistLoader : IPlaylistLoader
    {
        protected static FieldAccessor<AnnotatedBeatmapLevelCollectionsViewController, IAnnotatedBeatmapLevelCollection[]>.Accessor Collection = FieldAccessor<AnnotatedBeatmapLevelCollectionsViewController, IAnnotatedBeatmapLevelCollection[]>.GetAccessor("_annotatedBeatmapLevelCollections");

        public bool IncludesBasePlaylists => true;

        public IAnnotatedBeatmapLevelCollection[] GetCustomPlaylists()
        {
            try
            {
                AnnotatedBeatmapLevelCollectionsViewController[] playlistViewControllers = Resources.FindObjectsOfTypeAll<AnnotatedBeatmapLevelCollectionsViewController>();
                AnnotatedBeatmapLevelCollectionsViewController playlistController = playlistViewControllers?.FirstOrDefault();
                if (playlistController != null)
                {
                    IAnnotatedBeatmapLevelCollection[] playlists = Collection(ref playlistController);
                    if (playlists == null)
                        Plugin.log.Debug($"Found _playlists is null.");
                    else
                    {
                        Plugin.log.Debug($"Received {playlists.Length} playlists from FallbackPlaylistLoader.");
                        return playlists;
                    }
                }
                else
                    Plugin.log.Warn("Couldn't find the PlaylistsViewController.");
                return Array.Empty<IAnnotatedBeatmapLevelCollection>();
            }
            catch (Exception ex)
            {
                Plugin.log?.Warn($"Error loading playlists from AnnotatedBeatmapLevelCollectionsViewController: {ex.Message}");
                Plugin.log?.Debug(ex);
                return Array.Empty<IAnnotatedBeatmapLevelCollection>();
            }
        }
    }
}
