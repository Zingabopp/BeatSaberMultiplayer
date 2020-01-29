using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BeatSaberMultiplayerLite.RichPresence
{
    public class PresenceManager
    {
        private readonly Dictionary<string, IPresenceInstance> presenceInstances = new Dictionary<string, IPresenceInstance>();
        public void Initialize(string modId, string modName, Sprite modIcon, bool handleInvites, long appid)
        {
            foreach (var presence in presenceInstances)
            {
                presence.Value.Destroy();
            }
            presenceInstances.Clear();
            IPresenceInstance[] loadedPresences = PresenceLoader.LoadAll(modId, modName, modIcon, handleInvites, appid);
            foreach (var presence in loadedPresences)
            {
                presence.ActivityJoinReceived -= OnActivityJoin;
                presence.ActivityJoinRequest -= ActivityManager_OnActivityJoinRequest;
                presence.ActivityInviteReceived -= ActivityManager_OnActivityInvite;
                presence.Destroyed -= Presence_Destroyed;

                presence.ActivityJoinReceived += OnActivityJoin;
                presence.ActivityJoinRequest += ActivityManager_OnActivityJoinRequest;
                presence.ActivityInviteReceived += ActivityManager_OnActivityInvite;
                presence.Destroyed += Presence_Destroyed;
                presenceInstances.Add(presence.Name, presence);
            }
        }

        private void Presence_Destroyed(object sender, EventArgs e)
        {
            if (sender is IPresenceInstance presence)
                if (presenceInstances.Remove(presence.Name))
                    Plugin.log.Debug($"{presence.Name} presence destroyed.");
        }

        public bool TryGetPresence(string name, out IPresenceInstance instance) => presenceInstances.TryGetValue(name, out instance);
        public IPresenceInstance[] GetLoadedPresences() => presenceInstances.Values.ToArray();

        private GameActivity _currentActivity;
        public GameActivity CurrentActivity
        {
            get => _currentActivity;
            protected set
            {
                _currentActivity = value;
                try
                {
                    GameActivityChanged?.Invoke(this, _currentActivity);
                }
                catch (Exception ex)
                {
                    Plugin.log.Error($"Error invoking GameActivityChanged event: {ex.Message}");
                    Plugin.log.Debug(ex);
                }
            }
        }

        public void ClearActivity()
        {
            foreach (var presence in presenceInstances.Values.ToArray())
            {
                presence.ClearActivity();
            }
        }

        internal void UpdateActivity()
        {
            UpdateActivity(CurrentActivity);
        }

        public void UpdateActivity(GameActivity gameActivity)
        {
            CurrentActivity = gameActivity;
            if (!Config.Instance.EnableRichPresence)
                return;
            foreach (var presence in presenceInstances.Values.ToArray())
            {
                presence.UpdateActivity(gameActivity);
            }
        }

        private void ActivityManager_OnActivityInvite(object sender, ActivityInviteEventArgs args)
        {
            if (sender is IPresenceInstance presence)
            {
                if (!Config.Instance.EnableRichPresence)
                {
                    Plugin.log.Debug($"Ignoring join request from {presence?.Name}, Rich Presence is disabled.");
                    return;
                }
                ActivityInviteReceived?.Invoke(presence, args);
            }
            else
                Plugin.log.Debug($"ActivityManager_OnActivityInvite: sender (type {sender?.GetType().Name ?? "<NULL>"} is not a {nameof(IPresenceInstance)}");
        }

        private void ActivityManager_OnActivityJoinRequest(object sender, IActivityJoinRequest joinRequest)
        {
            if (sender is IPresenceInstance presence)
            {
                if (!Config.Instance.EnableRichPresence)
                {
                    Plugin.log.Debug($"Ignoring join request from {presence?.Name}, Rich Presence is disabled.");
                    return;
                }
                ActivityJoinRequest?.Invoke(presence, joinRequest);
            }
            else
                Plugin.log.Debug($"ActivityManager_OnActivityJoinRequest: sender (type {sender?.GetType().Name ?? "<NULL>"} is not a {nameof(IPresenceInstance)}");
        }

        private void OnActivityJoin(object sender, string secret)
        {
            if (sender is IPresenceInstance presence)
                ActivityJoinReceived?.Invoke(presence, secret);
            else
                Plugin.log.Debug($"OnActivityJoin: sender (type {sender?.GetType().Name ?? "<NULL>"} is not a {nameof(IPresenceInstance)}");
        }

        /// <summary>
        /// Raised when a user requests to join your game. Sender is the <see cref="IPresenceInstance"/> that raised the event.
        /// </summary>
        public event EventHandler<IActivityJoinRequest> ActivityJoinRequest;
        /// <summary>
        /// Raised when an accepted join is requested by the API. Sender is the <see cref="IPresenceInstance"/> that raised the event.
        /// </summary>
        public event EventHandler<string> ActivityJoinReceived;
        /// <summary>
        /// Raised when a user invites you to their game. Sender is the <see cref="IPresenceInstance"/> that raised the event.
        /// </summary>
        public event EventHandler<ActivityInviteEventArgs> ActivityInviteReceived;

        /// <summary>
        /// Raised when something changes the CurrentActivity.
        /// </summary>
        public event EventHandler<GameActivity> GameActivityChanged;
    }


}
