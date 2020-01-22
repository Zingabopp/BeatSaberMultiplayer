using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaberMultiplayerLite.DiscordInterface
{
    public interface IActivityJoinRequest
    {
        string Source { get; }
        IUserInfo User { get; }
        void SendRequestReply(GameActivityRequestReply reply, Action<bool> callback = null);
    }
}
