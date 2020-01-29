using System;

namespace BeatSaberMultiplayerLite.RichPresence
{
    public interface IActivityJoinRequest
    {
        string Source { get; }
        IUserInfo User { get; }
        void SendRequestReply(GameActivityRequestReply reply, Action<bool> callback = null);
    }
}
