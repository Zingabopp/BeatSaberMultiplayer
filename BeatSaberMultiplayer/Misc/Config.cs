using System;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using IPA.Config.Stores;
using UnityEngine;
using BeatSaberMultiplayerLite.Data;
using System.Collections.Generic;
using IPA.Config.Stores.Attributes;
using IPA.Config.Stores.Converters;
using System.ComponentModel;
using BeatSaberMultiplayerLite.Misc;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace BeatSaberMultiplayerLite
{
    internal class MultiplayerConfig
    {
        private string _beatSaverURL = "https://beatsaver.com";

        [SerializedName(nameof(ModVersion))]
        [JsonProperty(nameof(ModVersion), Order = 0)]
        public virtual string ModVersion { get; set; } = Plugin.PluginVersion;

        [SerializedName(nameof(BeatSaverURL))]
        [JsonProperty(nameof(BeatSaverURL), Order = 5)]
        public virtual string BeatSaverURL
        {
            get => _beatSaverURL;
            set
            {
                if (!Uri.IsWellFormedUriString(value, UriKind.Absolute))
                {
                    Plugin.log?.Warn($"BeatSaverURL is not well-formatted, defaulting to beatsaver.com: {value}");
                    value = "https://beatsaver.com";
                }
                _beatSaverURL = value.TrimEnd('/', '\\');
            }
        }

        private List<string> _serverRepositories = new List<string> { "https://raw.githubusercontent.com/Zingabopp/BeatSaberMultiplayerServerRepo/master/CompatibleServers.json" };

        [NonNullable]
        [UseConverter(typeof(ListConverter<string>))]
        [SerializedName(nameof(ServerRepositories))]
        [JsonProperty(nameof(ServerRepositories), Order = 10, IsReference = true)]
        public virtual List<string> ServerRepositories { get => _serverRepositories; set => _serverRepositories = value; }

        [NonNullable]
        [UseConverter(typeof(ListConverter<ServerHub>))]
        [SerializedName(nameof(ServerHubs))]
        [JsonProperty(nameof(ServerHubs), Order = 15, IsReference = true)]
        public virtual List<ServerHub> ServerHubs { get; set; } = new List<ServerHub>() { new ServerHub() { Address = "127.0.0.1", Port = 3700 } };
    }

    internal class ServerHub
    {
        private int _port;

        [JsonProperty(nameof(Address), Order = 0)]
        public virtual string Address { get; set; }
        [JsonProperty(nameof(Port), Order = 10)]
        public virtual int Port
        {
            get => _port;
            set
            {
                if (value < 0 || value > 65535)
                {
                    Plugin.log?.Warn($"Port for ServerHub '{Address}' is out of range: {value}. Defaulting to 3700.");
                    value = 3700;
                }
                _port = value;
            }
        }
    }

    internal class SocialConfig
    {
        private SubmitScoreMode _submitScores = SubmitScoreMode.Always;
        private string _publicAvatarHash = Data.PlayerInfo.avatarHashPlaceholder;
        private bool _enableRichPresence = true;

        [NonNullable]
        [SerializedName(nameof(PublicAvatarHash))]
        [JsonProperty(nameof(PublicAvatarHash), Order = 0)]
        public virtual string PublicAvatarHash
        {
            get => _publicAvatarHash;
            set
            {
                if (value == null)
                    _publicAvatarHash = Data.PlayerInfo.avatarHashPlaceholder;
                else
                    _publicAvatarHash = value;
            }
        }
        [SerializedName(nameof(SpectatorMode))]
        [JsonProperty(nameof(SpectatorMode), Order = 5)]
        public virtual bool SpectatorMode { get; set; } = false;

        [UseConverter(typeof(EnumConverter<SubmitScoreMode>))]
        [SerializedName(nameof(SubmitScores))]
        [JsonProperty(nameof(SubmitScores), Order = 10)]
        public SubmitScoreMode SubmitScores
        {
            get => _submitScores;
            set
            {
                _submitScores = value;
            }
        }

        [SerializedName(nameof(EnableRichPresence))]
        [JsonProperty(nameof(EnableRichPresence), Order = 15)]
        public virtual bool EnableRichPresence
        {
            get => _enableRichPresence;
            set
            {
                if (value == _enableRichPresence) return;
                if (_enableRichPresence)
                {
                    Plugin.PresenceManager?.ClearActivity();
                }
                else
                    Plugin.PresenceManager?.UpdateActivity();
                _enableRichPresence = value;
            }
        }
    }

    public enum SubmitScoreMode
    {
        Never = 0,
        OnlyRanked = 1,
        Always = 2
    }

    internal class VoiceChatConfig
    {
        private float voiceChatVolume = 0.8f;

        [SerializedName(nameof(EnableVoiceChat))]
        [JsonProperty(nameof(EnableVoiceChat), Order = 0)]
        public virtual bool EnableVoiceChat { get; set; }

        [SerializedName(nameof(VoiceChatVolume))]
        [JsonProperty(nameof(VoiceChatVolume), Order = 10)]
        public virtual float VoiceChatVolume
        {
            get => voiceChatVolume;
            set
            {
                if (voiceChatVolume == value) return;
                voiceChatVolume = value;
                var handler = VoiceChatVolumeChanged;
                handler?.Invoke(this, value);
            }
        }
        public event EventHandler<float> VoiceChatVolumeChanged;

        [SerializedName(nameof(SpatialAudio))]
        [JsonProperty(nameof(SpatialAudio), Order = 20)]
        public virtual bool SpatialAudio { get; set; } = false;

        [SerializedName(nameof(MicEnabled))]
        [JsonProperty(nameof(MicEnabled), Order = 30)]
        public virtual bool MicEnabled { get; set; } = true;

        [SerializedName(nameof(PushToTalk))]
        [JsonProperty(nameof(PushToTalk), Order = 40)]
        public virtual bool PushToTalk { get; set; } = true;

        [SerializedName(nameof(PushToTalkButton))]
        [UseConverter(typeof(EnumConverter<PTTOption>))]
        [JsonProperty(nameof(PushToTalkButton), Order = 50)]
        public virtual PTTOption PushToTalkButton { get; set; } = PTTOption.LeftAndRightTrigger;

        [SerializedName(nameof(VoiceChatMicrophone))]
        [JsonProperty(nameof(VoiceChatMicrophone), Order = 60)]
        public string VoiceChatMicrophone { get; set; } = "";

        [NonNullable]
        [JsonProperty(nameof(InputSettings), Order = 70)]
        public virtual InputConfig InputSettings { get; set; } = new InputConfig();
    }

    internal class InputConfig
    {
        private float triggerInputThreshold = 0.85f;
        [SerializedName(nameof(TriggerInputThreshold))]
        [JsonProperty(nameof(TriggerInputThreshold), Order = 0)]
        public virtual float TriggerInputThreshold
        {
            get => triggerInputThreshold;
            set
            {
                if (triggerInputThreshold == value) return;
                if (value < 0)
                    value = 0;
                if (value > 1)
                    value = 1;
                triggerInputThreshold = value;
            }
        }

        private float gripInputThreshold = 0.85f;
        [SerializedName(nameof(GripInputThreshold))]
        [JsonProperty(nameof(GripInputThreshold), Order = 10)]
        public virtual float GripInputThreshold
        {
            get => gripInputThreshold;
            set
            {
                if (gripInputThreshold == value) return;
                if (value < 0)
                    value = 0;
                if (value > 1)
                    value = 1;
                gripInputThreshold = value;
            }
        }

        [SerializedName(nameof(EnableHaptics))]
        [JsonProperty(nameof(EnableHaptics), Order = 15)]
        public virtual bool EnableHaptics { get; set; } = true;

        private float hapticAmplitude = 0.5f;
        [SerializedName(nameof(HapticAmplitude))]
        [JsonProperty(nameof(HapticAmplitude), Order = 20)]
        public virtual float HapticAmplitude
        {
            get => hapticAmplitude;
            set
            {
                if (hapticAmplitude == value) return;
                if (value < 0)
                    value = 0;
                if (value > 1)
                    value = 1;
                hapticAmplitude = value;
            }
        }

        private float hapticDuration = 0.1f;
        [SerializedName(nameof(HapticDuration))]
        [JsonProperty(nameof(HapticDuration), Order = 30)]
        public virtual float HapticDuration
        {
            get => hapticDuration;
            set
            {
                if (hapticDuration == value) return;
                if (value < 0)
                    value = 0;
                if (value > 1)
                    value = 1;
                hapticDuration = value;
            }
        }
    }

    internal class LeaderboardConfig
    {
        [NonNullable]
        [SerializedName(nameof(ScoreScreenPositionOffset))]
        [JsonProperty(PropertyName = nameof(ScoreScreenPositionOffset), Order = 0)]
        public virtual Vector3Dummy PosOffsetAry { get; set; } = new Vector3Dummy();

        [NonNullable]
        [SerializedName(nameof(ScoreScreenRotationOffset))]
        [JsonProperty(PropertyName = nameof(ScoreScreenRotationOffset), Order = 0)]
        public virtual Vector3Dummy RotOffsetAry { get; set; } = new Vector3Dummy();

        [NonNullable]
        [SerializedName(nameof(ScoreScreenScale))]
        [JsonProperty(PropertyName = nameof(ScoreScreenScale), Order = 0)]
        public virtual Vector3Dummy Scale { get; set; } = new Vector3Dummy(1, 1, 1);

        [Ignore]
        public virtual Vector3 ScoreScreenPositionOffset { get => PosOffsetAry.AsVector3(); set => PosOffsetAry = new Vector3Dummy(value); }
        [Ignore]
        public virtual Vector3 ScoreScreenRotationOffset { get => RotOffsetAry.AsVector3(); set => PosOffsetAry = new Vector3Dummy(value); }
        [Ignore]
        public virtual Vector3 ScoreScreenScale { get => Scale.AsVector3(); set => PosOffsetAry = new Vector3Dummy(value); }
    }

    public class Vector3Dummy
    {
        public virtual float X { get; set; } = 0f;
        public virtual float Y { get; set; } = 0f;
        public virtual float Z { get; set; } = 0f;

        public Vector3Dummy() { }
        public Vector3Dummy(float x, float y = 0, float z = 0) { X = x; Y = y; Z = z; }
        public Vector3Dummy(Vector3 vector)
        {
            X = vector.x;
            Y = vector.y;
            Z = vector.z;
        }

        public Vector3 AsVector3() => new Vector3(X, Y, Z);
        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }
    }

    internal class Config
    {
        public static Config Instance { get; set; }

        [NonNullable]
        [JsonProperty(nameof(MultiplayerSettings), Order = 0)]
        public virtual MultiplayerConfig MultiplayerSettings { get; set; } = new MultiplayerConfig();

        [NonNullable]
        [JsonProperty(nameof(SocialSettings), Order = 10)]
        public virtual SocialConfig SocialSettings { get; set; } = new SocialConfig();

        [NonNullable]
        [JsonProperty(nameof(VoiceChatSettings), Order = 20)]
        public virtual VoiceChatConfig VoiceChatSettings { get; set; } = new VoiceChatConfig();

        [NonNullable]
        [JsonProperty(nameof(LeaderboardSettings), Order = 30)]
        public virtual LeaderboardConfig LeaderboardSettings { get; set; } = new LeaderboardConfig();

        /// <summary>
        /// This is called whenever BSIPA reads the config from disk (including when file changes are detected).
        /// </summary>
        public virtual void OnReload()
        {
            // Do stuff after config is read from disk.
            if (VoiceChatSettings.InputSettings != null)
                ControllersHelper.ReloadConfig(VoiceChatSettings.InputSettings);
        }

        /// <summary>
        /// Call this to force BSIPA to update the config file. This is also called by BSIPA if it detects the file was modified.
        /// </summary>
        public virtual void Changed()
        {
            // Do stuff when the config is changed.
            if (VoiceChatSettings.InputSettings != null)
                ControllersHelper.ReloadConfig(VoiceChatSettings.InputSettings);
        }

        /// <summary>
        /// Call this to have BSIPA copy the values from <paramref name="other"/> into this config.
        /// </summary>
        public virtual void CopyFrom(Config other)
        {
            // This instance's members populated from other
        }
    }


    [Flags]
    public enum PTTOption
    {
        None = 0,                                          // 0000 0000
        LeftTrigger = 1 << 0,                              // 0000 0001                              
        RightTrigger = 1 << 1,                             // 0000 0010
        LeftAndRightTrigger = LeftTrigger | RightTrigger,  // 0000 0011
        AnyTrigger = 1 << 3 | LeftAndRightTrigger,         // 0000 0111
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