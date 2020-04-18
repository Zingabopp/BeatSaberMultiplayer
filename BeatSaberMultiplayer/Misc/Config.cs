using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BeatSaberMultiplayerLite
{
    [Serializable]
    public class Config {

        [SerializeField] private string _modVersion;
        [SerializeField] private string[] _serverRepositories;
        [SerializeField] private string[] _serverHubIPs;
        [SerializeField] private int[] _serverHubPorts;
        [SerializeField] private string _publicAvatarHash;
        [SerializeField] private bool _spectatorMode;
        [SerializeField] private int _submitScores;
        [SerializeField] private string _beatSaverURL;

        [SerializeField] private bool _enableRichPresence;

        [SerializeField] private bool _enableVoiceChat;
        [SerializeField] private float _voiceChatVolume;
        [SerializeField] private bool _micEnabled;
        [SerializeField] private bool _spatialAudio;
        [SerializeField] private bool _pushToTalk;
        [SerializeField] private int _pushToTalkButton;
        [SerializeField] private string _voiceChatMicrophone;

        [SerializeField] private Vector3 _scoreScreenPosOffset;
        [SerializeField] private Vector3 _scoreScreenRotOffset;
        [SerializeField] private Vector3 _scoreScreenScale;


        private static Config _instance;

        private static FileInfo FileLocation { get; } = new FileInfo($"./UserData/{Assembly.GetExecutingAssembly().GetName().Name}.json");

        public static bool Load()
        {
            if (_instance != null) return false;
            try
            {
                FileLocation.Directory.Create();
                Plugin.log.Debug($"Attempting to load JSON @ {FileLocation.FullName}");
                _instance = JsonUtility.FromJson<Config>(File.ReadAllText(FileLocation.FullName));

                UpdateModVersion(_instance);

                _instance.MarkDirty();
                _instance.Save();
            }
            catch (Exception ex)
            {
                Plugin.log.Error($"Unable to load config @ {FileLocation.FullName}: {ex.Message}");
                Plugin.log.Debug(ex);
                return false;
            }
            return true;
        }

        public static bool Create()
        {
            if (_instance != null) return false;
            try
            {
                FileLocation.Directory.Create();
                Plugin.log.Info($"Creating new config @ {FileLocation.FullName}");
                Instance.Save();
            }
            catch (Exception)
            {
                Plugin.log.Error($"Unable to create new config @ {FileLocation.FullName}");
                return false;
            }
            return true;
        }

        public static void UpdateModVersion(Config _instance)
        {
            
            _instance.ModVersion = IPA.Loader.PluginManager.GetPluginFromId(Plugin.PluginID).Version.ToString();
        }

        public static Config Instance {
            get {
                if (_instance == null)
                {
                    _instance = new Config();
                    UpdateModVersion(_instance);
                }
                return _instance;
            }
        }

        private bool IsDirty { get; set; }

        /// <summary>
        /// Remember to Save after changing the value
        /// </summary>
        public string ModVersion
        {
            get { return _modVersion; }
            set
            {
                _modVersion = value;
                MarkDirty();
            }
        }

        public string[] ServerRepositories
        {
            get { return _serverRepositories; }
            set
            {
                _serverRepositories = value;
                MarkDirty();
            }
        }

        /// <summary>
        /// Remember to Save after changing the value
        /// </summary>
        public string[] ServerHubIPs
        {
            get { return _serverHubIPs; }
            set
            {
                _serverHubIPs = value;
                MarkDirty();
            }
        }

        public int[] ServerHubPorts
        {
            get { return _serverHubPorts; }
            set
            {
                _serverHubPorts = value;
                MarkDirty();
            }
        }

        public string PublicAvatarHash
        {
            get { return _publicAvatarHash; }
            set
            {
                if (value == null)
                {
                    _publicAvatarHash = Data.PlayerInfo.avatarHashPlaceholder;
                }
                else
                {
                    _publicAvatarHash = value;
                }
                MarkDirty();
            }
        }

        public bool SpectatorMode
        {
            get { return _spectatorMode; }
            set
            {
                _spectatorMode = value;
                MarkDirty();
            }
        }

        public int SubmitScores
        {
            get { return _submitScores; }
            set
            {
                _submitScores = value;
                MarkDirty();
            }
        }

        public string BeatSaverURL
        {
            get { return _beatSaverURL; }
            set
            {
                _beatSaverURL = value;
                MarkDirty();
            }
        }

        public bool EnableRichPresence
        {
            get { return _enableRichPresence; }
            set
            {
                if (_enableRichPresence == value)
                    return;
                _enableRichPresence = value;
                if (_enableRichPresence == false)
                    Plugin.PresenceManager?.ClearActivity();
                else
                    Plugin.PresenceManager?.UpdateActivity();
                MarkDirty();
            }
        }

        public bool EnableVoiceChat
        {
            get { return _enableVoiceChat; }
            set
            {
                _enableVoiceChat = value;
                MarkDirty();
            }
        }

        public float VoiceChatVolume
        {
            get { return _voiceChatVolume; }
            set
            {
                if (_voiceChatVolume == value)
                    return;
                _voiceChatVolume = value;
                VoiceChatVolumeChanged?.Invoke(this, value);
                MarkDirty();
            }
        }
        public event EventHandler<float> VoiceChatVolumeChanged;
        public bool MicEnabled
        {
            get { return _micEnabled; }
            set
            {
                _micEnabled = value;
                MarkDirty();
            }
        }

        public bool SpatialAudio
        {
            get { return _spatialAudio; }
            set
            {
                _spatialAudio = value;
                MarkDirty();
            }
        }

        public bool PushToTalk
        {
            get { return _pushToTalk; }
            set
            {
                _pushToTalk = value;
                MarkDirty();
            }
        }

        public PTTOption PushToTalkButton
        {
            get { return (PTTOption)_pushToTalkButton; }
            set
            {
                _pushToTalkButton = (int)value;
                MarkDirty();
            }
        }

        public string VoiceChatMicrophone
        {
            get { return _voiceChatMicrophone; }
            set
            {
                _voiceChatMicrophone = value;
                MarkDirty();
            }
        }

        public Vector3 ScoreScreenPosOffset
        {
            get { return _scoreScreenPosOffset; }
            set
            {
                _scoreScreenPosOffset = value;
                MarkDirty();
            }
        }

        public Vector3 ScoreScreenRotOffset
        {
            get { return _scoreScreenRotOffset; }
            set
            {
                _scoreScreenRotOffset = value;
                MarkDirty();
            }
        }

        public Vector3 ScoreScreenScale
        {
            get { return _scoreScreenScale; }
            set
            {
                _scoreScreenScale = value;
                MarkDirty();
            }
        }

        Config()
        {
            _modVersion = string.Empty;
            _serverRepositories = new string[1] { "https://raw.githubusercontent.com/Zingabopp/BeatSaberMultiplayerServerRepo/master/CompatibleServers.json" };
            _serverHubIPs = new string[0];
            _serverHubPorts = new int[0];
            _spectatorMode = false;
            _submitScores = 2;
            _beatSaverURL = "https://beatsaver.com";

            _enableRichPresence = true;

            _enableVoiceChat = false;
            _voiceChatVolume = 0.8f;
            _micEnabled = true;
            _spatialAudio = false;
            _pushToTalk = true;
            _pushToTalkButton = 0;
            _voiceChatMicrophone = null;

            _scoreScreenPosOffset = Vector3.zero;
            _scoreScreenRotOffset = Vector3.zero;
            _scoreScreenScale = Vector3.one;

            IsDirty = true;
        }

        public bool Save() {
            if (!IsDirty) return false;
            try {
                using (var f = new StreamWriter(FileLocation.FullName)) {
                    Plugin.log.Debug($"Writing to File @ {FileLocation.FullName}");
                    var json = JsonUtility.ToJson(this, true);
                    f.Write(json);
                }
                MarkClean();
                return true;
            }
            catch (Exception ex) {
                Plugin.log.Critical(ex);
                return false;
            }
        }

        void MarkDirty() {
            IsDirty = true;
            Save();
        }

        void MarkClean() {
            IsDirty = false;
        }

        
    }

    [Flags]
    public enum PTTOption
    {
        None = 0,                                          // 0000
        LeftTrigger = 1 << 0,                              // 0001                              
        RightTrigger = 1 << 1,                             // 0010
        LeftAndRightTrigger = LeftTrigger | RightTrigger,  // 0011
        AnyTrigger = 1 << 3 | LeftAndRightTrigger,         // 0111
        LeftGrip = 1 << 5,                                 // 0001 0000
        RightGrip = 1 << 6,                                // 0010 0000
        LeftAndRightGrip = LeftGrip | RightGrip,           // 0011 0000
        AnyGrip = 1 << 7 | LeftAndRightGrip                // 0111 0000
    }

    public static class PPTOptionExtensions
    {
        public static bool Satisfies(this PTTOption actualState, PTTOption checkState)
        {
            if (checkState == PTTOption.AnyTrigger)
                return (actualState & PTTOption.AnyTrigger) != 0;
            if (checkState == PTTOption.AnyGrip)
                return (actualState & PTTOption.AnyGrip) != 0;
            return actualState.HasFlag(checkState);
        }

        public static int OptionIndex(this PTTOption option)
        {
            switch (option)
            {
                case PTTOption.None:
                    return 0;
                case PTTOption.LeftTrigger:
                    return 1;
                case PTTOption.RightTrigger:
                    return 2;
                case PTTOption.LeftAndRightTrigger:
                    return 3;
                case PTTOption.AnyTrigger:
                    return 4;
                case PTTOption.LeftGrip:
                    return 5;
                case PTTOption.RightGrip:
                    return 6;
                case PTTOption.LeftAndRightGrip:
                    return 7;
                case PTTOption.AnyGrip:
                    return 8;
                default:
                    return 0;
            }
        }
    }

}
