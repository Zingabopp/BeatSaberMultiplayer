using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using DiscordCore;
using UnityEngine;

namespace BeatSaberMultiplayerLite.RichPresence.DiscordPresence
{
    public class DiscordPresence : IPresenceInstance
    {
        public static readonly string NameKey = "Discord";
        public string Name => NameKey;
        private DiscordInstance discord;
        public DiscordPresence(string modId, string modName, Sprite modIcon, bool handleInvites, long appid)
        {
            discord = DiscordManager.Instance.CreateInstance(new DiscordSettings() { modId = modId, modName = modName, modIcon = modIcon, handleInvites = handleInvites, appId = appid });
            discord.OnActivityJoin += OnActivityJoin;
            discord.OnActivityJoinRequest += ActivityManager_OnActivityJoinRequest;
            discord.OnActivityInvite += ActivityManager_OnActivityInvite;
            Plugin.log.Debug("DiscordCore found, Discord Rich Presence will be available.");
        }

        private void ActivityManager_OnActivityInvite(ActivityActionType type, ref User user, ref Activity activity)
        {
            ActivityInviteReceived?.Invoke(this, new ActivityInviteEventArgs(type.ToGameActivityActionType(), new DiscordUser(user), activity.ToGameActivity()));
        }

        private void ActivityManager_OnActivityJoinRequest(ref User user)
        {
            ActivityJoinRequest?.Invoke(this, new DiscordJoinRequest(new DiscordUser(user)));
        }

        public void OnActivityJoin(string secret)
        {
            ActivityJoinReceived?.Invoke(this, secret);
        }

        public void UpdateActivity(GameActivity activity)
        {
            discord.UpdateActivity(activity.ToActivity());
        }

        public void ClearActivity()
        {
            discord.ClearActivity();
        }

        public void Destroy()
        {
            discord.OnActivityJoin -= OnActivityJoin;
            discord.OnActivityJoinRequest -= ActivityManager_OnActivityJoinRequest;
            discord.OnActivityInvite -= ActivityManager_OnActivityInvite;
            discord.DestroyInstance();
            discord = null;
            Destroyed?.Invoke(this, null);
        }

        public event EventHandler<IActivityJoinRequest> ActivityJoinRequest;
        public event EventHandler<string> ActivityJoinReceived;
        public event EventHandler<ActivityInviteEventArgs> ActivityInviteReceived;
        public event EventHandler Destroyed;
    }

    public static class DiscordExtensions
    {
        #region To Discord
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


        #region From Discord
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
                Timestamps = activity.Timestamps.ToGameActivityTimestamps(),
                Type = activity.Type.ToGameActivityType()
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
        public static GameActivityType ToGameActivityType(this Discord.ActivityType type)
        {
            switch (type)
            {
                case ActivityType.Playing:
                    return GameActivityType.Playing;
                case ActivityType.Streaming:
                    return GameActivityType.Streaming;
                case ActivityType.Listening:
                    return GameActivityType.Listening;
                case ActivityType.Watching:
                    return GameActivityType.Watching;
                default:
                    return GameActivityType.Playing;
            }
        }
        public static GameActivityActionType ToGameActivityActionType(this Discord.ActivityActionType type)
        {
            switch (type)
            {
                case ActivityActionType.Join:
                    return GameActivityActionType.Join;
                case ActivityActionType.Spectate:
                    return GameActivityActionType.Spectate;
                default:
                    return GameActivityActionType.Join;
            }
        }
        #endregion

    }
}
