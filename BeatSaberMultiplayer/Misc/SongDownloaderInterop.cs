using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatSaverDownloader;
using BeatSaberMarkupLanguage;
using HMUI;
using BeatSaverDownloader.UI;

namespace BeatSaberMultiplayerLite.Misc
{
    internal class SongDownloaderInterop : ISongDownloader
    {
        private FlowCoordinator _coordinator;

        public FlowCoordinator PresentDownloaderFlowCoordinator(FlowCoordinator parent, Action dismissedCallback)
        {
            try
            {
                if (_coordinator == null)
                {
                    MoreSongsFlowCoordinator moreSongsFlow = BeatSaberUI.CreateFlowCoordinator<MoreSongsFlowCoordinator>();

                    moreSongsFlow.ParentFlowCoordinator = parent;
                    _coordinator = moreSongsFlow;
                }

                parent.PresentFlowCoordinator(_coordinator, dismissedCallback);
                return _coordinator;
            }catch(Exception ex)
            {
                Plugin.log.Error($"Error creating MoreSongsFlowCoordinator: {ex.Message}");
                Plugin.log.Debug(ex);
                return null;
            }
        }
    }
}
