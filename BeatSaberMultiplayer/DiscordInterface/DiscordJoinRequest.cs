using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using DiscordCore;

namespace BeatSaberMultiplayerLite.DiscordInterface
{
    public class DiscordJoinRequest : IActivityJoinRequest
    {
        public DiscordJoinRequest(IUserInfo user)
        {
            User = user;
        }
        public string Source => "Discord";
        public IUserInfo User { get; private set; }

        public void SendRequestReply(GameActivityRequestReply reply, Action<bool> callback = null)
        {
            Discord.ActivityManager activityManager = DiscordClient.GetActivityManager();
            switch (reply)
            {
                case GameActivityRequestReply.No:
                    activityManager.SendRequestReply(User.Id, ActivityJoinRequestReply.No,
                        (result) =>
                        {
                            Plugin.log.Debug("Decline invite result: " + result);
                            callback?.Invoke(result == Result.Ok);
                        }
                    );
                    break;
                case GameActivityRequestReply.Yes:
                    activityManager.SendRequestReply(User.Id, ActivityJoinRequestReply.Yes,
                        (result) =>
                        {
                            Plugin.log.Debug("Accept invite result: " + result);
                            callback?.Invoke(result == Result.Ok);
                        }
                    );
                    break;
                case GameActivityRequestReply.Ignore:
                    activityManager.SendRequestReply(User.Id, ActivityJoinRequestReply.Ignore,
                        (result) =>
                        {
                            Plugin.log.Debug("Ignore invite result: " + result);
                            callback?.Invoke(result == Result.Ok);
                        }
                    );
                    break;
                default:
                    break;
            }
        }
    }
}
