using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using DiscordCore;
using UnityEngine;

namespace BeatSaberMultiplayerLite.RichPresence.DiscordPresence
{
    public class DiscordUser : IUserInfo
    {
        private User _user;
        public DiscordUser(User user)
        {
            _user = user;
        }
        public string Source => "Discord";
        public long Id => _user.Id;

        public string FullName => $"{_user.Username}#{_user.Discriminator}";

        public string Name => _user.Username;

        public string Discriminator => _user.Discriminator;

        public string Avatar => _user.Avatar;

        public bool Bot => _user.Bot;

        public void GetAvatarTexture(OnAvatarTextureFinishedCallback callback)
        {
            if (callback == null)
                return;
            var imageManager = DiscordClient.GetImageManager();

            var handle = new ImageHandle()
            {
                Id = Id,
                Size = 256
            };

            imageManager.Fetch(handle, false, (result, img) =>
            {
                bool success = false;
                Texture2D texture = null;
                if (result == Result.Ok)
                {
                    texture = imageManager.GetTexture(img);
                    success = true;   
                }
                callback.Invoke(success, texture);
            });
        }
    }
}
