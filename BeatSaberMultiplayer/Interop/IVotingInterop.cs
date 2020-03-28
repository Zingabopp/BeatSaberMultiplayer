using BeatSaberMultiplayerLite.UI.ViewControllers.RoomScreen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaberMultiplayerLite.Interop
{
    internal interface IVotingInterop
    {
        void Setup(MultiplayerResultsViewController resultsView, IBeatmapLevel level);
        void Hide();
    }
}
