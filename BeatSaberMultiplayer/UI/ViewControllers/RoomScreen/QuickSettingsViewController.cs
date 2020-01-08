using BeatSaberMarkupLanguage;
using HMUI;
using System.Reflection;
using UnityEngine;

namespace BeatSaberMultiplayer.UI.ViewControllers.RoomScreen
{
    class QuickSettingsViewController : ViewController
    {
        private Settings _settings;

        public  string Content => ResourcesStorage.RoomScreenResources.GetRoomScreenResource(nameof(QuickSettingsViewController));
        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {

            if (firstActivation)
            {
                _settings = new GameObject("Multiplayer Quick Settings").AddComponent<Settings>();
                BSMLParser.instance.Parse(Content, gameObject, _settings);
            }
        }
    }
}
