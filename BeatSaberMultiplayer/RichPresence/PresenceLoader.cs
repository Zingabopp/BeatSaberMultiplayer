using System;
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
            {
                IPresenceInstance discordPrescence = LoadDiscord(modId, modName, modIcon, handleInvites, appid);
                if (discordPrescence != null)
                    loadedPresences.Add(discordPrescence);
                else
                    Plugin.log.Warn($"Discord Presence failed to load, Discord Rich Presence unavailable.");
            }
            else
                Plugin.log.Debug($"DiscordCore mod not detected, Discord Rich Presence unavailable.");
            if (Plugin.IsSteam)
            {
                IPresenceInstance steamPresence = LoadSteam();
                if (steamPresence != null)
                    loadedPresences.Add(steamPresence);
                else
                    Plugin.log.Warn($"Discord Presence failed to load, Discord Rich Presence unavailable.");
            }
            else
                Plugin.log.Debug($"Running on Oculus platform, Steam Rich Presence unavailable.");
            return loadedPresences.ToArray();
        }

        public static IPresenceInstance LoadDiscord(string modId, string modName, Sprite modIcon, bool handleInvites, long appid)
        {
            Plugin.log.Debug($"Loading DiscordPresence");
            IPresenceInstance discordPresence = null;
            try
            {
                discordPresence = new DiscordPresence.DiscordPresence(modId, modName, modIcon, handleInvites, appid);
            }
            catch (Exception ex)
            {
                Plugin.log.Debug(ex);
            }
            return discordPresence;
        }
        public static IPresenceInstance LoadSteam()
        {
            Plugin.log.Debug($"Loading SteamPresence");
            IPresenceInstance steamPresence = null;
            try
            {
                steamPresence = new GameObject("SteamPresence").AddComponent<SteamPresence.SteamPresence>();
            }
            catch (Exception ex)
            {
                Plugin.log.Debug(ex);
            }
            return steamPresence;
        }
    }
}
