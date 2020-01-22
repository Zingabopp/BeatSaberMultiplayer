using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BeatSaberMultiplayerLite.DiscordInterface;

namespace BeatSaberMultiplayerLite.UI.ViewControllers.DiscordScreens
{
    class DiscordInviteResponseView : BSMLResourceViewController
    {
        public override string ResourceName => string.Join(".", GetType().Namespace, GetType().Name);

        public IUserInfo user { get; set; }
        public GameActivity activity;

        [UIComponent("player-avatar")]
        public RawImage playerAvatar;
        [UIComponent("title-text")]
        public TextMeshProUGUI titleText;

        [UIAction("#post-parse")]
        public void SetupScreen()
        {
            titleText.text = $"<b>{user.FullName}</b> invited you to play! ({activity.Party.Size.CurrentSize}/{activity.Party.Size.MaxSize} players)";

            user.GetAvatarTexture((success, texture) =>
            {
                if (success)
                {
                    playerAvatar.rectTransform.localRotation = Quaternion.Euler(180f, 0f, 0f);
                    playerAvatar.texture = texture;
                }
            });
        }

        [UIAction("accept-pressed")]
        public void AcceptPressed()
        {
            Plugin.instance.OnActivityJoin(this, activity.Secrets.Join);

            Destroy(screen.gameObject);
        }

        [UIAction("decline-pressed")]
        public void DeclinePressed()
        {
            Destroy(screen.gameObject);
        }
    }
}
