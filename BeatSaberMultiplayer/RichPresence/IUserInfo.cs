using UnityEngine;

namespace BeatSaberMultiplayerLite.RichPresence
{
    public interface IUserInfo
    {
        string Source { get; }
        long Id { get; }
        string FullName { get; }
        string Name { get; }
        string Avatar { get; }
        bool Bot { get; }

        void GetAvatarTexture(OnAvatarTextureFinishedCallback callback);
    }

    public delegate void OnAvatarTextureFinishedCallback(bool success, Texture2D texture);
}
