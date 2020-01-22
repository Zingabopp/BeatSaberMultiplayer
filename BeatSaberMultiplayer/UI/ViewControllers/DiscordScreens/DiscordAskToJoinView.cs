using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BeatSaberMultiplayerLite.DiscordInterface;

namespace BeatSaberMultiplayerLite.UI.ViewControllers.DiscordScreens
{
    class DiscordAskToJoinView : BSMLResourceViewController
    {
        public override string ResourceName => string.Join(".", GetType().Namespace, GetType().Name);

        public IUserInfo user => request.User;

        public IActivityJoinRequest request;

        [UIComponent("player-avatar")]
        public RawImage playerAvatar;
        [UIComponent("title-text")]
        public TextMeshProUGUI titleText;

        [UIAction("#post-parse")]
        public void SetupScreen()
        {
            titleText.text = $"<b>{user.FullName}</b> wants to join your game!";
            user.GetAvatarTexture((success, texture) =>
            {
                if(success)
                {
                    playerAvatar.rectTransform.localRotation = Quaternion.Euler(180f, 0f, 0f);
                    playerAvatar.texture = texture;
                }
            });
            
        }

        [UIAction("accept-pressed")]
        public void AcceptPressed()
        {
            request.SendRequestReply(GameActivityRequestReply.Yes);
            Destroy(screen.gameObject);
        }

        [UIAction("decline-pressed")]
        public void DeclinePressed()
        {
            request.SendRequestReply(GameActivityRequestReply.No);
            Destroy(screen.gameObject);
        }

        [UIAction("ignore-pressed")]
        public void IgnorePressed()
        {
            request.SendRequestReply(GameActivityRequestReply.Ignore);
            Destroy(screen.gameObject);
        }


    }
}
