using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Steamworks;

namespace BeatSaberMultiplayerLite.RichPresence.SteamPresence
{
    public class SteamPresence : MonoBehaviour, IPresenceInstance
    {
        public static SteamPresence instance;

        public static readonly string NameKey = "Steam";
        public string Name => NameKey;

        /// <summary>
        /// Unused.
        /// </summary>
        public event EventHandler<IActivityJoinRequest> ActivityJoinRequest;
        public event EventHandler<string> ActivityJoinReceived;
        /// <summary>
        /// Unused, handled by the Steam overlay.
        /// </summary>
        public event EventHandler<ActivityInviteEventArgs> ActivityInviteReceived;
        public event EventHandler Destroyed;


        public void UpdateActivity(GameActivity activity)
        {
            if (!SteamManager.Initialized)
                return;
            ClearActivity();
            if (!string.IsNullOrEmpty(activity.Party.Id))
            {
                if (!string.IsNullOrEmpty(activity.Secrets.Join))
                {
#if DEBUG
                    Plugin.log.Debug($"Setting Steam connect string to {activity.Secrets.Join}");
#endif
                    SteamFriends.SetRichPresence("connect", "connect:|" + activity.Secrets.Join + "|");
                }
                else
                    Plugin.log.Warn($"Connect string is null or empty.");
                SteamFriends.SetRichPresence("status", activity.State);
                SteamFriends.SetRichPresence("steam_player_group", activity.Party.Id);
                SteamFriends.SetRichPresence("steam_player_group_size", activity.Party.Size.CurrentSize.ToString());

            }
            //discord.UpdateActivity(activity.ToActivity());
        }

        public void ClearActivity()
        {
            if (SteamManager.Initialized)
            {
                Steamworks.SteamFriends.ClearRichPresence();
            }
        }

        public void Destroy()
        {
            instance = null;
            GameObject.Destroy(this);
        }


#region Callbacks

        protected Callback<GameOverlayActivated_t> m_GameOverlayActivated;
        protected Callback<GameRichPresenceJoinRequested_t> m_GameRichPresenceJoinRequested;

        private void OnGameOverlayActivated(GameOverlayActivated_t pCallback)
        {
            if (pCallback.m_bActive != 0)
            {
                Plugin.log.Debug("Steam Overlay has been activated");
            }
            else
            {
                Plugin.log.Debug("Steam Overlay has been closed");
            }
        }

        private void OnGameRichPresenceJoinRequested(GameRichPresenceJoinRequested_t pCallback)
        {
            string parsedConnectString = pCallback.m_rgchConnect.Replace("connect:", string.Empty).Trim('|');
            Plugin.log.Debug($"Join requested for connection string: {parsedConnectString} ({pCallback.m_rgchConnect})");
            ActivityJoinReceived?.Invoke(this, parsedConnectString);
        }
#endregion


#region MonoBehaviour Messages
        void Awake()
        {
            if (instance != null)
                instance.Destroy();
            instance = this;
            GameObject.DontDestroyOnLoad(this);
        }
        void OnEnable()
        {
            if (!Steamworks.SteamAPI.Init())
            {
                Plugin.log.Error("Unable to initialize SteamAPI");
            }
            if (SteamManager.Initialized)
            {
                Plugin.log.Debug("SteamAPI found, Steam Rich Presence will be available.");
                m_GameOverlayActivated = Callback<GameOverlayActivated_t>.Create(OnGameOverlayActivated);
                m_GameRichPresenceJoinRequested = Callback<GameRichPresenceJoinRequested_t>.Create(OnGameRichPresenceJoinRequested);

            }
            else
                Plugin.log.Error("SteamManager not initialized.");
        }
        void OnDisable()
        {
            m_GameOverlayActivated?.Dispose();
            m_GameOverlayActivated = null;
            m_GameRichPresenceJoinRequested?.Dispose();
            m_GameRichPresenceJoinRequested = null;
        }



        bool notInitialized = false;
        void Update()
        {
            if (SteamManager.Initialized)
            {
                SteamAPI.RunCallbacks();
                notInitialized = false;
            }
            else
            {
                if (!notInitialized)
                {
                    Plugin.log.Warn("SteamAPI is not initialized.");
                    notInitialized = true;
                }
            }
        }
        void OnDestroy()
        {
            //discord.OnActivityJoin -= OnActivityJoin;
            //discord.OnActivityJoinRequest -= ActivityManager_OnActivityJoinRequest;
            //discord.OnActivityInvite -= ActivityManager_OnActivityInvite;
            //discord.DestroyInstance();
            //discord = null;
            m_GameOverlayActivated.Dispose();
            m_GameOverlayActivated = null;
            Destroyed?.Invoke(this, null);
        }
#endregion
    }

    public static class DiscordExtensions
    {
#region To Steam
        public static Discord.Activity ToActivity(this GameActivity activity)
        {
            return new Discord.Activity()
            {
                ApplicationId = activity.ApplicationId,
                Assets = activity.Assets.ToActivityAssets(),
                Details = activity.Details,
                Instance = activity.Instance,
                Name = activity.Name,
                Party = activity.Party.ToActivityParty(),
                Secrets = activity.Secrets.ToActivitySecrets(),
                State = activity.State,
                Timestamps = activity.Timestamps.ToActivityTimestamps(),
                Type = activity.Type.ToActivityType()
            };
        }

        public static Discord.ActivityAssets ToActivityAssets(this GameActivityAssets assets)
        {
            return new Discord.ActivityAssets()
            {
                LargeImage = assets.LargeImage,
                LargeText = assets.LargeText,
                SmallImage = assets.SmallImage,
                SmallText = assets.SmallText
            };
        }
        public static Discord.ActivityTimestamps ToActivityTimestamps(this GameActivityTimestamps timeStamps)
        {
            return new Discord.ActivityTimestamps()
            {
                End = timeStamps.End,
                Start = timeStamps.Start
            };
        }
        public static Discord.ActivityParty ToActivityParty(this GameActivityParty party)
        {
            return new Discord.ActivityParty()
            {
                Id = party.Id,
                Size = party.Size.ToPartySize()
            };
        }
        public static Discord.ActivitySecrets ToActivitySecrets(this GameActivitySecrets secrets)
        {
            return new Discord.ActivitySecrets()
            {
                Join = secrets.Join,
                Match = secrets.Match,
                Spectate = secrets.Spectate
            };
        }

        public static Discord.PartySize ToPartySize(this GamePartySize partySize)
        {
            return new Discord.PartySize()
            {
                CurrentSize = partySize.CurrentSize,
                MaxSize = partySize.MaxSize
            };
        }
        public static Discord.ActivityType ToActivityType(this GameActivityType type)
        {
            switch (type)
            {
                case GameActivityType.Playing:
                    return Discord.ActivityType.Playing;
                case GameActivityType.Streaming:
                    return Discord.ActivityType.Streaming;
                case GameActivityType.Listening:
                    return Discord.ActivityType.Listening;
                case GameActivityType.Watching:
                    return Discord.ActivityType.Watching;
                default:
                    return Discord.ActivityType.Playing;
            }
        }
        public static Discord.ActivityActionType ToActivityActionType(this GameActivityActionType type)
        {
            switch (type)
            {
                case GameActivityActionType.Join:
                    return Discord.ActivityActionType.Join;
                case GameActivityActionType.Spectate:
                    return Discord.ActivityActionType.Spectate;
                default:
                    return Discord.ActivityActionType.Join;
            }
        }
#endregion


#region From Steam
        public static GameActivity ToGameActivity(this Discord.Activity activity)
        {
            return new GameActivity()
            {
                ApplicationId = activity.ApplicationId,
                Assets = activity.Assets.ToGameActivityAssets(),
                Details = activity.Details,
                Instance = activity.Instance,
                Name = activity.Name,
                Party = activity.Party.ToGameActivityParty(),
                Secrets = activity.Secrets.ToGameActivitySecrets(),
                Source = "Discord",
                State = activity.State,
                Timestamps = activity.Timestamps.ToGameActivityTimestamps()//,
                //Type = activity.Type.ToGameActivityType()
            };
        }

        public static GameActivityAssets ToGameActivityAssets(this Discord.ActivityAssets assets)
        {
            return new GameActivityAssets()
            {
                LargeImage = assets.LargeImage,
                LargeText = assets.LargeText,
                SmallImage = assets.SmallImage,
                SmallText = assets.SmallText
            };
        }
        public static GameActivityTimestamps ToGameActivityTimestamps(this Discord.ActivityTimestamps timeStamps)
        {
            return new GameActivityTimestamps()
            {
                End = timeStamps.End,
                Start = timeStamps.Start
            };
        }
        public static GameActivityParty ToGameActivityParty(this Discord.ActivityParty party)
        {
            return new GameActivityParty()
            {
                Id = party.Id,
                Size = party.Size.ToGamePartySize()
            };
        }
        public static GameActivitySecrets ToGameActivitySecrets(this Discord.ActivitySecrets secrets)
        {
            return new GameActivitySecrets()
            {
                Join = secrets.Join,
                Match = secrets.Match,
                Spectate = secrets.Spectate
            };
        }

        public static GamePartySize ToGamePartySize(this Discord.PartySize partySize)
        {
            return new GamePartySize()
            {
                CurrentSize = partySize.CurrentSize,
                MaxSize = partySize.MaxSize
            };
        }
        //public static GameActivityType ToGameActivityType()//this Discord.ActivityType type)
        //{
        //    //switch (type)
        //    //{
        //    //    case ActivityType.Playing:
        //    //        return GameActivityType.Playing;
        //    //    case ActivityType.Streaming:
        //    //        return GameActivityType.Streaming;
        //    //    case ActivityType.Listening:
        //    //        return GameActivityType.Listening;
        //    //    case ActivityType.Watching:
        //    //        return GameActivityType.Watching;
        //    //    default:
        //    //        return GameActivityType.Playing;
        //    //}
        //}
        //public static GameActivityActionType ToGameActivityActionType()//this Discord.ActivityActionType type)
        //{
        //    //switch (type)
        //    //{
        //    //    case ActivityActionType.Join:
        //    //        return GameActivityActionType.Join;
        //    //    case ActivityActionType.Spectate:
        //    //        return GameActivityActionType.Spectate;
        //    //    default:
        //    //        return GameActivityActionType.Join;
        //    //}
        //}
#endregion

    }
}
