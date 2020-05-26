﻿using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.FloatingScreen;
using BeatSaberMarkupLanguage.Settings;
using BeatSaberMultiplayerLite.Data;
using BeatSaberMultiplayerLite.RichPresence;
using BeatSaberMultiplayerLite.UI.FlowCoordinators;
using BeatSaberMultiplayerLite.UI.ViewControllers.DiscordScreens;
using BS_Utils.Gameplay;
using BS_Utils.Utilities;
using Polyglot;
using HMUI;
using BeatSaberMultiplayerLite.Misc.SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace BeatSaberMultiplayerLite.UI
{
    class PluginUI : MonoBehaviour
    {
        public static PluginUI instance;

        internal MainMenuViewController _mainMenuViewController;
        private RectTransform _mainMenuRectTransform;
        private SimpleDialogPromptViewController _noUserInfoWarning;

        public ServerHubFlowCoordinator serverHubFlowCoordinator;
        public RoomCreationFlowCoordinator roomCreationFlowCoordinator;
        public RoomFlowCoordinator roomFlowCoordinator;
        public ModeSelectionFlowCoordinator modeSelectionFlowCoordinator;
        //public ChannelSelectionFlowCoordinator channelSelectionFlowCoordinator;
        //public RadioFlowCoordinator radioFlowCoordinator;

        private TextMeshProUGUI _newVersionText;
        private Button _multiplayerButton;

        private Settings _settings;

        public static void OnLoad()
        {
            if (instance == null)
            {
                new GameObject("Multiplayer Plugin").AddComponent<PluginUI>().Setup();
            }
        }

        public void Setup()
        {
            instance = this;
            GetUserInfo.UpdateUserInfo();

            CreateUI();

            if (SongCore.Loader.AreSongsLoading)
                SongCore.Loader.SongsLoadedEvent += SongsLoaded;
            else
                SongsLoaded(null, null);
        }

        public void SongsLoaded(SongCore.Loader sender, Dictionary<string, CustomPreviewBeatmapLevel> levels)
        {
            if (_multiplayerButton != null)
            {
                _multiplayerButton.interactable = true;
            }

            SongInfo.GetOriginalLevelHashes();
        }

        public void CreateUI()
        {
            try
            {
                _mainMenuViewController = Resources.FindObjectsOfTypeAll<MainMenuViewController>().First();
                _mainMenuRectTransform = _mainMenuViewController.transform as RectTransform;

                if (serverHubFlowCoordinator == null)
                {
                    serverHubFlowCoordinator = BeatSaberUI.CreateFlowCoordinator<ServerHubFlowCoordinator>();
                }
                if (roomCreationFlowCoordinator == null)
                {
                    roomCreationFlowCoordinator = BeatSaberUI.CreateFlowCoordinator<RoomCreationFlowCoordinator>();
                }
                if (roomFlowCoordinator == null)
                {
                    roomFlowCoordinator = BeatSaberUI.CreateFlowCoordinator<RoomFlowCoordinator>();
                }
                if (modeSelectionFlowCoordinator == null)
                {
                    modeSelectionFlowCoordinator = BeatSaberUI.CreateFlowCoordinator<ModeSelectionFlowCoordinator>();
                    modeSelectionFlowCoordinator.didFinishEvent += () =>
                    {
                        Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First().InvokeMethod("DismissFlowCoordinator", modeSelectionFlowCoordinator, null, false);
                        Plugin.PresenceManager?.UpdateActivity(default);
                        Plugin.PresenceManager?.ClearActivity();
                    };

                }
                /*
                if (channelSelectionFlowCoordinator == null)
                {
                    channelSelectionFlowCoordinator = BeatSaberUI.CreateFlowCoordinator<ChannelSelectionFlowCoordinator>();
                }
                if (radioFlowCoordinator == null)
                {
                    radioFlowCoordinator = BeatSaberUI.CreateFlowCoordinator<RadioFlowCoordinator>();
                }*/

                CreateOnlineButton();

                StartCoroutine(CheckVersion());

                _settings = new GameObject("Multiplayer Settings").AddComponent<Settings>();
                BSMLSettings.instance.AddSettingsMenu("Multiplayer", "BeatSaberMultiplayerLite.UI.Settings", _settings);
            }
            catch (Exception e)
            {
                Plugin.log.Critical($"Unable to create UI! Exception: {e}");
            }
        }

        private void CreateOnlineButton()
        {
            _newVersionText = BeatSaberUI.CreateText(_mainMenuRectTransform, "A new version of the mod\nis available!", new Vector2(55.5f, 33f));
            _newVersionText.fontSize = 5f;
            _newVersionText.lineSpacing = -52;
            _newVersionText.gameObject.SetActive(false);

            Button[] mainButtons = Resources.FindObjectsOfTypeAll<RectTransform>().First(x => x.name == "MainButtons" && x.parent.name == "MainMenuViewController").GetComponentsInChildren<Button>();

            foreach (var item in mainButtons)
            {
                (item.transform as RectTransform).sizeDelta = new Vector2(35f, 30f);
            }

            _multiplayerButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().Last(x => (x.name == "SoloFreePlayButton")), _mainMenuRectTransform, false);
            _multiplayerButton.name = "BSMultiplayerButton";
            Destroy(_multiplayerButton.GetComponentInChildren<LocalizedTextMeshProUGUI>());
            Destroy(_multiplayerButton.GetComponentInChildren<HoverHint>());
            _multiplayerButton.transform.SetParent(mainButtons.First(x => x.name == "SoloFreePlayButton").transform.parent);
            _multiplayerButton.transform.SetAsLastSibling();

            _multiplayerButton.SetButtonText("Online");
            _multiplayerButton.SetButtonIcon(Sprites.onlineIcon);

            _multiplayerButton.interactable = !SongCore.Loader.AreSongsLoading;

            _multiplayerButton.onClick = new Button.ButtonClickedEvent();
            _multiplayerButton.onClick.AddListener(delegate ()
            {
                try
                {
                    Plugin.ReadUserInfo();
                    SetLobbyPresenceActivity();

                    MainFlowCoordinator mainFlow = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First();

                    if (_noUserInfoWarning == null)
                    {
                        var dialogOrig = BS_Utils.Utilities.ReflectionUtil.GetPrivateField<SimpleDialogPromptViewController>(mainFlow, "_simpleDialogPromptViewController");
                        _noUserInfoWarning = Instantiate(dialogOrig.gameObject).GetComponent<SimpleDialogPromptViewController>();
                    }

                    if (GetUserInfo.GetUserID() == 0)
                    {
                        _noUserInfoWarning.Init("Invalid username and ID", $"Your username and ID are invalid\nMake sure you are logged in", "Go back", "Continue anyway",
                              (selectedButton) =>
                              {
                                  mainFlow.InvokeMethod("DismissViewController", _noUserInfoWarning, null, selectedButton == 1);
                                  if (selectedButton == 1)
                                  {
                                      mainFlow.InvokeMethod("PresentFlowCoordinator", modeSelectionFlowCoordinator, null, true, false);
                                  }
                              });
                        mainFlow.InvokeMethod("PresentViewController", _noUserInfoWarning, null, false);
                    }
                    else
                    {
                        mainFlow.InvokeMethod("PresentFlowCoordinator", modeSelectionFlowCoordinator, null, false, false);
                    }
                }
                catch (Exception e)
                {
                    Plugin.log.Critical($"Unable to present flow coordinator! Exception: {e}");
                }
            });
        }

        public void ShowJoinRequest(IActivityJoinRequest request)
        {
            FloatingScreen screen = FloatingScreen.CreateFloatingScreen(new Vector2(100, 50), true, new Vector3(0f, 0.9f, 2.4f), Quaternion.Euler(30f, 0f, 0f));

            DiscordAskToJoinView discordView = BeatSaberUI.CreateViewController<DiscordAskToJoinView>();
            discordView.request = request;

            screen.SetRootViewController(discordView, false);
        }

        public void ShowInvite(IUserInfo user, GameActivity activity)
        {
            FloatingScreen screen = FloatingScreen.CreateFloatingScreen(new Vector2(100, 50), true, new Vector3(0f, 0.9f, 2.4f), Quaternion.Euler(30f, 0f, 0f));

            DiscordInviteResponseView discordView = BeatSaberUI.CreateViewController<DiscordInviteResponseView>();
            discordView.user = user;
            discordView.activity = activity;

            screen.SetRootViewController(discordView, false);
        }

        public void SetLobbyPresenceActivity()
        {

            Plugin.PresenceManager.UpdateActivity(new GameActivity
            {
                State = "Playing multiplayer",
                Details = "In lobby",
                Timestamps =
                        {
                            Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        },
                Instance = false,
            });

        }

        public IEnumerator JoinGameWithSecret(string secret)
        {
            if (Plugin.UserId == 0 || string.IsNullOrEmpty(Plugin.Username))
                Plugin.ReadUserInfo();
            yield return null;
            yield return null;
            MainFlowCoordinator mainFlow = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First();
            try
            {
                mainFlow.InvokeMethod("PresentFlowCoordinator", modeSelectionFlowCoordinator, null, true, false);
                modeSelectionFlowCoordinator.JoinGameWithSecret(secret);
            }
            catch (Exception ex)
            {
                Plugin.log.Error($"Error joining game: {ex.Message}");
                Plugin.log.Debug(ex);
            }
        }
        IEnumerator CheckVersion()
        {

            Plugin.log.Info("Checking for updates...");
            Uri uri = new Uri("https://api.github.com/repos/Zingabopp/BeatSaberMultiplayer/releases");
            UnityWebRequest www = Misc.SongDownloader.GetRequestForUri(uri);
            www.timeout = 10;

            yield return www.SendWebRequest();
            try
            {
                if (!www.isNetworkError && !www.isHttpError)
                {
                    JSONNode releases = JSON.Parse(www.downloadHandler.text);

                    JSONNode latestRelease = releases[0];

                    SemVer.Version currentVer = Plugin.PluginMetadata.Version;
                    string githubVerStr = latestRelease["tag_name"]?.Value.Replace("-L", "");
                    SemVer.Version githubVer = new SemVer.Version(githubVerStr);

                    bool newTag = new SemVer.Range($">{currentVer}").IsSatisfied(githubVer);

                    if (newTag)
                    {
                        Plugin.log.Info($"An update for the mod is available!\nNew mod version: {githubVerStr}\nCurrent mod version: {currentVer}");
                        _newVersionText.gameObject.SetActive(true);
                        _newVersionText.text = $"Version {githubVerStr}\n of the mod is available!\nCurrent mod version: {currentVer}";
                        _newVersionText.alignment = TextAlignmentOptions.Center;
                    }
                }
                else
                    Plugin.log.Warn($"Unable to check latest release: {www.error}");
            }
            catch (Exception ex)
            {
                Plugin.log.Error($"Error checking version: {ex.Message}");
                Plugin.log.Debug(ex);
            }
        }
    }
}
