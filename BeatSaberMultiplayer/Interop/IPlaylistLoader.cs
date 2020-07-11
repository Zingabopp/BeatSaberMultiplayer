using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaberMultiplayerLite.Interop
{
    public interface IPlaylistLoader
    {
        IAnnotatedBeatmapLevelCollection[] GetCustomPlaylists();
        bool IncludesBasePlaylists { get; }
    }
}
