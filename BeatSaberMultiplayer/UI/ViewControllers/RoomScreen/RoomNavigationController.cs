using BeatSaberMarkupLanguage;
using HMUI;
using TMPro;
using UnityEngine;

namespace BeatSaberMultiplayerLite.UI.ViewControllers.RoomScreen
{
    class RoomNavigationController : NavigationController
    {
        public TextMeshProUGUI _errorText;

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            if(firstActivation && addedToHierarchy)
            {
                _errorText = BeatSaberUI.CreateText(rectTransform, "", new Vector2(0f, 0f));
                _errorText.fontSize = 8f;
                _errorText.alignment = TextAlignmentOptions.Center;
                _errorText.rectTransform.sizeDelta = new Vector2(120f, 6f);
            }
            _errorText.text = "";
            _errorText.gameObject.SetActive(false);
        }

        public void DisplayError(string error)
        {
            if (_errorText != null)
            {
                _errorText.gameObject.SetActive(true);
                _errorText.text = error;
            }
        }

    }
}
