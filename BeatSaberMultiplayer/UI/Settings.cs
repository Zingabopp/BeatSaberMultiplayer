using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.Notify;
using BeatSaberMultiplayerLite.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace BeatSaberMultiplayerLite.UI
{
    class Settings : MonoBehaviour, INotifiableHost
    {
        public static event Action<string> voiceChatMicrophoneChanged;

        public void Awake()
        {
            //if (ModelSaberAPI.isCalculatingHashes)
            //{
            //    ModelSaberAPI.hashesCalculated -= ListAllAvatars;
            //    ModelSaberAPI.hashesCalculated += ListAllAvatars;
            //}
            //else
            //    ListAllAvatars();

            AudioSettings.OnAudioConfigurationChanged += UpdateMicrophoneList;
            UpdateMicrophoneList(false);
        }

        #region General settings

        //void ListAllAvatars()
        //{
        //    ModelSaberAPI.hashesCalculated -= ListAllAvatars;
        //    publicAvatars.Clear();
        //    //foreach (var avatar in CustomAvatar.Plugin.Instance.AvatarLoader.Avatars)
        //    //{
        //    //    if (avatar.IsLoaded)
        //    //    {
        //    //        publicAvatars.Add(avatar);
        //    //    }
        //    //}

        //    if (publicAvatarSetting)
        //    {
        //        publicAvatarSetting.tableView.ReloadData();
        //        publicAvatarSetting.ReceiveValue();
        //    }
        //}

        //CustomAvatar.CustomAvatar GetSelectedAvatar()
        //{
        //    if (ModelSaberAPI.cachedAvatars.TryGetValue(Config.Instance.SocialSettings.PublicAvatarHash, out CustomAvatar.CustomAvatar avatar))
        //    {
        //        return avatar;
        //    }
        //    else
        //    {
        //        return null;// CustomAvatar.Plugin.Instance.AvatarLoader.Avatars.FirstOrDefault();
        //    }
        //}

        //[UIComponent("public-avatar-setting")]
        //public DropDownListSetting publicAvatarSetting;

        //[UIAction("public-avatar-formatter")]
        //public string PublicAvatarFormatter(object avatar)
        //{
        //    string name = null;// (avatar as CustomAvatar.CustomAvatar)?.Name;
        //    return (avatar == null) ? "LOADING AVATARS..." : (string.IsNullOrEmpty(name) ? "NO NAME" : name);
        //}

        //[UIValue("avatars-in-game")]
        //public bool avatarsInGame
        //{ 
        //    get { return Config.Instance.ShowAvatarsInGame; }
        //    set { Config.Instance.ShowAvatarsInGame = value; }
        //}

        //[UIValue("blocks-in-game")]
        //public bool blocksInGame
        //{
        //    get { return Config.Instance.ShowOtherPlayersBlocks; }
        //    set { Config.Instance.ShowOtherPlayersBlocks = value; }
        //}

        //[UIValue("avatars-in-room")]
        //public bool avatarsInRoom
        //{
        //    get { return Config.Instance.ShowAvatarsInRoom; }
        //    set { Config.Instance.ShowAvatarsInRoom = value; }
        //}

        //[UIValue("download-avatars")]
        //public bool downloadAvatars
        //{
        //    get { return Config.Instance.DownloadAvatars; }
        //    set { Config.Instance.DownloadAvatars = value; }
        //}

        //[UIValue("separate-avatar")]
        //public bool separateAvatar
        //{
        //    get { return Config.Instance.SeparateAvatarForMultiplayer; }
        //    set { InGameOnlineController.Instance.SetSeparatePublicAvatarState(value); }
        //}

        //[UIValue("public-avatar-value")]
        //public object publicAvater
        //{
        //    get { return GetSelectedAvatar(); }
        //    set { InGameOnlineController.Instance.SetSeparatePublicAvatarHash(ModelSaberAPI.cachedAvatars.FirstOrDefault(x => x.Value == (value as CustomAvatar.CustomAvatar)).Key); }
        //}

        //[UIValue("public-avatar-options")]
        //public List<object> publicAvatars = new List<object>() { null };

        [UIValue("spectator-mode")]
        public bool spectatorMode
        {
            get { return Config.Instance.SocialSettings.SpectatorMode; }
            set { Config.Instance.SocialSettings.SpectatorMode = value; }
        }

        [UIValue("submit-scores-options")]
        public List<object> submitScoresOptions = new List<object>() { "Never", "Only ranked", "Always" };
        private SubmitScoreMode[] IndexedSubmitScoreMods = new SubmitScoreMode[] { SubmitScoreMode.Never, SubmitScoreMode.OnlyRanked, SubmitScoreMode.Always };

        [UIValue("submit-scores-value")]
        public object submitScores
        {
            get { return submitScoresOptions[(int)Config.Instance.SocialSettings.SubmitScores]; }
            set { Config.Instance.SocialSettings.SubmitScores = IndexedSubmitScoreMods[submitScoresOptions.IndexOf(value)]; }
        }
        #endregion

        #region Rich Presence Settings

        [UIValue("enable-rich-presence")]
        public bool enableRichPresence
        {
            get { return Config.Instance.SocialSettings.EnableRichPresence; }
            set { Config.Instance.SocialSettings.EnableRichPresence = value; }
        }

        #endregion

        #region Voice settings

        public void UpdateMicrophoneList(bool deviceWasChanged)
        {
            micSelectOptions.Clear();
            micSelectOptions.Add("DEFAULT MIC");
            foreach (var mic in Microphone.devices)
            {
                micSelectOptions.Add(mic);
            }

            if (micSelectSetting)
            {
                micSelectSetting.tableView.ReloadData();
                micSelectSetting.ReceiveValue();
            }

            voiceChatMicrophoneChanged?.Invoke(Config.Instance.VoiceChatSettings.VoiceChatMicrophone);
        }

        [UIComponent("mic-select-setting")]
        public DropDownListSetting micSelectSetting;

        [UIValue("enable-voice-chat")]
        public bool enableVoiceChat
        {
            get { return Config.Instance.VoiceChatSettings.EnableVoiceChat; }
            set { Config.Instance.VoiceChatSettings.EnableVoiceChat = value; }
        }

        [UIValue("voice-chat-volume")]
        public int voiceChatVolume
        {
            get { return Mathf.RoundToInt(Config.Instance.VoiceChatSettings.VoiceChatVolume * 100); }
            set { Config.Instance.VoiceChatSettings.VoiceChatVolume = (value / 100f); }
        }

        [UIValue("mic-enabled")]
        public bool micEnabled
        {
            get { return Config.Instance.VoiceChatSettings.MicEnabled; }
            set { Config.Instance.VoiceChatSettings.MicEnabled = value; }
        }

        [UIValue("push-to-talk")]
        public bool pushToTalk
        {
            get { return Config.Instance.VoiceChatSettings.PushToTalk; }
            set { Config.Instance.VoiceChatSettings.PushToTalk = value; }
        }


        internal PTTOption[] IndexedPTTOptions = new PTTOption[] {PTTOption.LeftTrigger, PTTOption.RightTrigger, PTTOption.LeftAndRightTrigger, PTTOption.AnyTrigger,
            PTTOption.LeftGrip, PTTOption.RightGrip, PTTOption.LeftAndRightGrip, PTTOption.AnyGrip };
        [UIValue("ptt-button-options")]
        internal List<object> pttButtonOptions = new List<object>() { "L Trigger", "R Trigger", "L+R Trigger", "Any Trigger", "L Grip", "R Grip", "L+R Grip", "Any Grip", }; // 

        [UIValue("ptt-button-value")]
        public object pttButton
        {
            get
            {
                int currentIndex = Config.Instance.VoiceChatSettings.PushToTalkButton.OptionIndex() - 1;

                if (currentIndex >= pttButtonOptions.Count || currentIndex < 0)
                {
                    Config.Instance.VoiceChatSettings.PushToTalkButton = PTTOption.LeftTrigger;
                    currentIndex = 0;
                }
                return pttButtonOptions[currentIndex];
            }
            set
            {
                PTTOption newValue = IndexedPTTOptions[pttButtonOptions.IndexOf(value)];
                if (Config.Instance.VoiceChatSettings.PushToTalkButton == newValue)
                    return;
                PttHoverHint = GetPttHoverHintForOption(newValue);
                Config.Instance.VoiceChatSettings.PushToTalkButton = newValue;
            }
        }

        private string GetPttHoverHintForOption(PTTOption option)
        {
            if ((option & PTTOption.AnyGrip) > 0)
                return "When using SteamVR, you may have to set custom bindings for the grips to work.";
            else
                return "Button(s) to active Push-To-Talk.";
        }

        private string pttHoverHint = null;
        [UIValue("ptt-hover")]
        public string PttHoverHint
        {
            get
            {
                if (pttHoverHint == null)
                    pttHoverHint = GetPttHoverHintForOption(Config.Instance.VoiceChatSettings.PushToTalkButton);
                return pttHoverHint;
            }
            set
            {
                if (pttHoverHint == value)
                    return;
                pttHoverHint = value;
                NotifyPropertyChanged();
            }
        }
        [UIValue("mic-select-options")]
        public List<object> micSelectOptions = new List<object>() { "DEFAULT MIC" };

        [UIValue("mic-select-value")]
        public object micSelect
        {
            get
            {
                if (!string.IsNullOrEmpty(Config.Instance.VoiceChatSettings.VoiceChatMicrophone) && micSelectOptions.Contains((object)Config.Instance.VoiceChatSettings.VoiceChatMicrophone))
                {
                    return (object)Config.Instance.VoiceChatSettings.VoiceChatMicrophone;
                }
                else
                    return "DEFAULT MIC";
            }
            set
            {
                if (string.IsNullOrEmpty(value as string) || (value as string) == "DEFAULT MIC")
                {
                    Config.Instance.VoiceChatSettings.VoiceChatMicrophone = null;
                }
                else
                    Config.Instance.VoiceChatSettings.VoiceChatMicrophone = (value as string);

                voiceChatMicrophoneChanged?.Invoke(Config.Instance.VoiceChatSettings.VoiceChatMicrophone);
            }
        }


        #endregion
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var action = PropertyChanged;
            action?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
