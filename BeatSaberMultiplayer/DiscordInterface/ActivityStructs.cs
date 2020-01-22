using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaberMultiplayerLite.DiscordInterface
{

    public struct GameActivity
    {
        public string Source;
        public GameActivityType Type;
        public long ApplicationId;
        public string Name;
        public string State;
        public string Details;
        public GameActivityTimestamps Timestamps;
        public GameActivityAssets Assets;
        public GameActivityParty Party;
        public GameActivitySecrets Secrets;
        public bool Instance;
    }

    public enum GameActivityType
    {
        Playing = 0,
        Streaming = 1,
        Listening = 2,
        Watching = 3
    }

    public enum GameActivityRequestReply
    {
        No = 0,
        Yes = 1,
        Ignore = 2
    }
    public enum GameActivityActionType
    {
        Join = 1,
        Spectate = 2
    }

    public struct GameActivityTimestamps
    {
        public long Start;
        public long End;
    }

    public struct GameActivityAssets
    {
        public string LargeImage;
        public string LargeText;
        public string SmallImage;
        public string SmallText;
    }

    public struct GameActivityParty
    {
        public string Id;
        public GamePartySize Size;
    }

    public struct GamePartySize
    {
        public int CurrentSize;
        public int MaxSize;
    }

    public struct GameActivitySecrets
    {
        public string Match;
        public string Join;
        public string Spectate;
    }
}
