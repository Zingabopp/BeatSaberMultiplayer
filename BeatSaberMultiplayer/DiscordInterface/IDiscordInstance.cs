using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaberMultiplayerLite.DiscordInterface
{
    public interface IDiscordInstance
    {
        void UpdateActivity(GameActivity activity);
        void ClearActivity();
        void Destroy();
        /// <summary>
        /// Raised when a user requests to join your game.
        /// </summary>
        event EventHandler<IActivityJoinRequest> ActivityJoinRequest;
        /// <summary>
        /// Raised when an accepted join is requested by the API.
        /// </summary>
        event EventHandler<string> ActivityJoinReceived;
        /// <summary>
        /// Raised when a user invites you to their game.
        /// </summary>
        event EventHandler<ActivityInviteEventArgs> ActivityInviteReceived;
        event EventHandler Destroyed;
    }

    public class ActivityInviteEventArgs : EventArgs
    {
        public GameActivityActionType GameActivityAction { get; }
        public IUserInfo User { get; }
        public GameActivity Activity { get; }

        public ActivityInviteEventArgs(GameActivityActionType actionType, IUserInfo user, GameActivity activity)
        {
            GameActivityAction = actionType;
            User = user;
            Activity = activity;
        }
    }
}
