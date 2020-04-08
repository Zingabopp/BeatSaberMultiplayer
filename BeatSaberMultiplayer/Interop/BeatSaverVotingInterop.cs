using BeatSaberMarkupLanguage;
using BeatSaberMultiplayerLite.UI.ViewControllers.RoomScreen;
using BeatSaverVoting.UI;
using IPA.Utilities;
using IPA.Loader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BeatSaberMultiplayerLite.Interop
{
    internal class BeatSaverVotingInterop : IVotingInterop
    {
        //private FieldAccessor<VotingUI, Transform>.Accessor UpButton;
        //private FieldAccessor<VotingUI, Transform>.Accessor DownButton;
        //private FieldAccessor<VotingUI, IBeatmapLevel>.Accessor LastSong;

        private object votingInstance;
        private RectTransform votingUIHost;

        public BeatSaverVotingInterop()
        {
            //UpButton = FieldAccessor<VotingUI, Transform>.GetAccessor("upButton");
            //DownButton = FieldAccessor<VotingUI, Transform>.GetAccessor("downButton");
            //LastSong = FieldAccessor<VotingUI, IBeatmapLevel>.GetAccessor("_lastSong");
        }

        public void Setup(MultiplayerResultsViewController resultsView, IBeatmapLevel level)
        {
            if (!resultsView) return;
            VotingUI voting = votingInstance as VotingUI;
            if (voting == null)
            {
                Plugin.log.Debug("Setting up BeatSaverVoting interop...");

                var modInfo = IPA.Loader.PluginManager.GetPluginFromId("BeatSaverVoting");

                if (modInfo == null) return;

                Plugin.log.Debug("Found BeatSaverVoting plugin!");

                Assembly votingAssembly = modInfo.Assembly;
                voting = VotingUI.instance;
                votingInstance = voting;

                votingUIHost = new GameObject("VotingUIHost").AddComponent<RectTransform>();
                votingUIHost.SetParent(resultsView.transform, false);
                votingUIHost.anchorMin = Vector2.zero;
                votingUIHost.anchorMax = Vector2.one;
                votingUIHost.sizeDelta = Vector2.zero;
                votingUIHost.anchoredPosition = new Vector2(2.25f, -6f);
                votingUIHost.SetParent(resultsView.resultsTab, true);

                BSMLParser.instance.Parse(Utilities.GetResourceContent(votingAssembly, "BeatSaverVoting.UI.votingUI.bsml"), votingUIHost.gameObject, votingInstance);

                Plugin.log.Debug("Created UI");


                UnityEngine.UI.Image upArrow = voting.GetField<Transform, VotingUI>("upButton").transform.Find("Arrow")?.GetComponent<UnityEngine.UI.Image>();
                UnityEngine.UI.Image downArrow = voting.GetField<Transform, VotingUI>("downButton").transform.Find("Arrow")?.GetComponent<UnityEngine.UI.Image>();
                if (upArrow != null && downArrow != null)
                {
                    upArrow.color = new Color(0.341f, 0.839f, 0.341f);
                    downArrow.color = new Color(0.984f, 0.282f, 0.305f);
                }
            }
            else
            {
                votingUIHost.gameObject.SetActive(true);
            }
            if (voting != null)
            {
                voting.SetField("_lastSong", level);

                Plugin.log.Debug("Calling GetVotesForMap...");
                voting.InvokeMethod<object, VotingUI>("GetVotesForMap", new object[0]);

                Plugin.log.Debug("Called GetVotesForMap!");
            }
        }

        public void Hide()
        {
            if(votingUIHost != null)
                votingUIHost.gameObject.SetActive(false);
        }

    }
}
