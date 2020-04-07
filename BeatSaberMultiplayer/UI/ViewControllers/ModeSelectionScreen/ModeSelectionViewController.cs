using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaberMultiplayerLite.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatSaberMultiplayerLite.UI.ViewControllers.ModeSelectionScreen
{
    class ModeSelectionViewController : BSMLResourceViewController
    {
        public override string ResourceName => string.Join(".", GetType().Namespace, GetType().Name);

        public event Action didSelectRooms;
        public event Action didSelectRadio;
        public event Action didFinishEvent;

        private string[] _dllPaths = new[] { @"Libs\Lidgren.Network.dll", @"Libs\NSpeex.dll" };
        private bool _filesMising;

        [UIValue("MissingFilesString")]
        public string MissingFilesString { get; private set; }


#pragma warning disable CS0649
        [UIComponent("rooms-button")]
        Button _roomsButton;
        [UIComponent("radio-button")]
        Button _radioButton;
        [UIComponent("missing-files-text")]
        TextMeshProUGUI _missingFilesText;
        [UIComponent("version-text")]
        TextMeshProUGUI _versionText;
        [UIComponent("loading-progress-text")]
        TextMeshProUGUI _loadingProgressText;

        [UIComponent("missing-files-rect")]
        RectTransform _missingFilesRect;
        [UIComponent("buttons-rect")]
        RectTransform _buttonsRect;
        [UIComponent("avatars-loading-rect")]
        RectTransform _avatarsLoadingRect;

        [UIComponent("progress-bar-top")]
        RawImage _progressBarTop;
        [UIComponent("progress-bar-bg")]
        RawImage _progressBarBG;
#pragma warning restore CS0649

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            base.DidActivate(firstActivation, activationType);
            if (firstActivation)
            {
                _radioButton.interactable = false;
                List<string> missingFiles = new List<string>();
                foreach (string path in _dllPaths)
                {
                    if (!File.Exists(path))
                    {
                        _filesMising = true;
                        missingFiles.Add(path);
                    }
                }

                _missingFilesText.color = Color.red;

                if (_filesMising)
                {
                    _missingFilesText.text = $"Missing files: {string.Join(", ", missingFiles)}";
                    Plugin.log.Error($"Missing critical files for Multiplayer: {string.Join(", ", missingFiles)}");
                    _missingFilesRect.gameObject.SetActive(true);
                    _buttonsRect.gameObject.SetActive(false);
                }
                else
                {
                    _missingFilesRect.gameObject.SetActive(false);
                    _buttonsRect.gameObject.SetActive(true);
                }

                //if (ModelSaberAPI.isCalculatingHashes)
                //{
                //    _buttonsRect.gameObject.SetActive(false);
                //    _avatarsLoadingRect.gameObject.SetActive(true);
                //    ModelSaberAPI.hashesCalculated += ModelSaberAPI_hashesCalculated;

                //    _progressBarBG.color = new Color(1f, 1f, 1f, 0.2f);
                //    _progressBarTop.color = new Color(1f, 1f, 1f, 1f);

                //}
                //else
                    _avatarsLoadingRect.gameObject.SetActive(false);
                try
                {
                    var pluginVersion = Plugin.PluginMetadata.Version.ToString();
                    var pluginBuild = pluginVersion.Substring(pluginVersion.LastIndexOf('.') + 1);

                    _versionText.text = $"v{pluginVersion}{(!int.TryParse(pluginBuild, out var buildNumber) ? " <color=red>(UNSTABLE)</color>" : "")}";
                }catch(Exception ex)
                {
                    Plugin.log.Error($"Error getting version text: {ex.Message}");
                    _versionText.text = "ERROR";
                }
            }
        }

        private void Update()
        {
            //if (ModelSaberAPI.isCalculatingHashes)
            //{
            //    _loadingProgressText.text = $"{ModelSaberAPI.calculatedHashesCount} out of {ModelSaberAPI.totalAvatarsCount}";
            //    _progressBarTop.rectTransform.sizeDelta = new Vector2(60f * (ModelSaberAPI.calculatedHashesCount / (float)ModelSaberAPI.totalAvatarsCount), 6f);
            //}
        }

        [UIAction("rooms-btn-pressed")]
        private void RoomsBtnPressed()
        {
            didSelectRooms?.Invoke();
        }

        [UIAction("radio-btn-pressed")]
        private void RadioBtnPressed()
        {
            didSelectRadio?.Invoke();
        }

        [UIAction("back-btn-pressed")]
        private void BackBtnPressed()
        {
            didFinishEvent?.Invoke();
        }
    }
}
