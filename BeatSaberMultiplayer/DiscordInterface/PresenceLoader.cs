using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BeatSaberMultiplayerLite.DiscordInterface
{
    public static class PresenceLoader
    {
        public static IDiscordInstance LoadDiscord(string modId, string modName, Sprite modIcon, bool handleInvites, long appid)
        {
            return new DiscordInterop(modId, modName, modIcon, handleInvites, appid);
        }
    }
}
