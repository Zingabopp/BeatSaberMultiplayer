using System.Collections.Generic;
using UnityEngine;

namespace BeatSaberMultiplayerLite.RichPresence
{
    internal class PresenceLoader
    {
        public static IPresenceInstance[] LoadAll(string modId, string modName, Sprite modIcon, bool handleInvites, long appid)
        {
            List<IPresenceInstance> loadedPresences = new List<IPresenceInstance>();
            if (IPA.Loader.PluginManager.GetPluginFromId("DiscordCore") != null)
                loadedPresences.Add(LoadDiscord(modId, modName, modIcon, handleInvites, appid));
            else
                Plugin.log.Debug($"DiscordCore mod not detected, Discord Rich Presence unavailable.");
            if (Plugin.IsSteam)
                loadedPresences.Add(LoadSteam(modId, modName, modIcon, handleInvites, appid));
            else
                Plugin.log.Debug($"Running on Oculus platform, Steam Rich Presence unavailable.");
            return loadedPresences.ToArray();
        }

        public static IPresenceInstance LoadDiscord(string modId, string modName, Sprite modIcon, bool handleInvites, long appid)
        {
            Plugin.log.Debug($"Loading DiscordPresence");
            IPresenceInstance discordPresence = new DiscordPresence.DiscordPresence(modId, modName, modIcon, handleInvites, appid);
            return discordPresence;
        }
        public static IPresenceInstance LoadSteam(string modId, string modName, Sprite modIcon, bool handleInvites, long appid)
        {
            Plugin.log.Debug($"Loading SteamPresence");
            IPresenceInstance steamPresence = new GameObject("SteamPresence").AddComponent<SteamPresence.SteamPresence>();
            return steamPresence;
        }
    }
}
