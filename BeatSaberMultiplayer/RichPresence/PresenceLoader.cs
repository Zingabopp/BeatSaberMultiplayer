using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using BeatSaberMultiplayerLite.RichPresence.DiscordPresence;

namespace BeatSaberMultiplayerLite.RichPresence
{
    internal class PresenceLoader
    {
        public static IPresenceInstance[] LoadAll(string modId, string modName, Sprite modIcon, bool handleInvites, long appid)
        {
            List<IPresenceInstance> loadedPresences = new List<IPresenceInstance>();
            if (IPA.Loader.PluginManager.GetPluginFromId("DiscordCore") != null)
                loadedPresences.Add(LoadDiscord(modId, modName, modIcon, handleInvites, appid));
            loadedPresences.Add(LoadSteam(modId, modName, modIcon, handleInvites, appid));
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
            IPresenceInstance steamPresence = new SteamPresence.SteamPresence(modId, modName, modIcon, handleInvites, appid);
            return steamPresence;
        }


        
    }
}
