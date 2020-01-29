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
            Destroyed?.Invoke(this, null);
        }
#endregion
    }
}
