using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BeatSaberMultiplayerLite.DiscordInterface
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
