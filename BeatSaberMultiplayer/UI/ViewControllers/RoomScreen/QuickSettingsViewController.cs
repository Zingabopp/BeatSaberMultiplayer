using BeatSaberMarkupLanguage;
using HMUI;
using System.Reflection;
using UnityEngine;

namespace BeatSaberMultiplayerLite.UI.ViewControllers.RoomScreen
{
    class QuickSettingsViewController : ViewController
    {
        private Settings _settings;
        string ResourceName => string.Join(".", GetType().Namespace, GetType().Name);
        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {

            if (firstActivation)
            {
                _settings = new GameObject("Multiplayer Quick Settings").AddComponent<Settings>();
                BSMLParser.instance.Parse(Utilities.GetResourceContent(Assembly.GetAssembly(this.GetType()), ResourceName), gameObject, _settings);
            }
        }
    }
}
