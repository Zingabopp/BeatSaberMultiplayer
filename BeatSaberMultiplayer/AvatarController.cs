using BeatSaberMultiplayerLite.Data;
using BeatSaberMultiplayerLite.Misc;
using SongCore.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using Image = UnityEngine.UI.Image;

namespace BeatSaberMultiplayerLite
{
    public class AvatarController : MonoBehaviour, IPlayerInfoReceiver//, IAvatarFullBodyInput
    {
        public bool AcceptingUpdates => true;
        PlayerUpdate playerInfo;
        ulong playerId;
        string playerName;

        TextMeshPro playerNameText;
        TextMeshPro playerFaceText;
        Image playerSpeakerIcon;

        OnlinePlayerPosition avatarInput;

        HSBColor nameColor;
        bool rainbowName;

        Camera playerCamera;

        VRCenterAdjust centerAdjust;

        public void Awake()
        {
            InitializeAvatarController();
        }

        void InitializeAvatarController()
        {
            centerAdjust = FindObjectOfType<VRCenterAdjust>();
            avatarInput = new OnlinePlayerPosition();
            playerNameText = CustomExtensions.CreateWorldText(transform, "INVALID");
            playerNameText.rectTransform.anchoredPosition3D = new Vector3(0f, 0.60f, 0f);
            playerNameText.alignment = TextAlignmentOptions.Center;
            playerNameText.fontSize = 2.5f;

            playerFaceText = CustomExtensions.CreateWorldText(transform, "O");
            playerFaceText.rectTransform.anchoredPosition3D = new Vector3(0f, 0f, 0f);
            playerFaceText.alignment = TextAlignmentOptions.Center;
            playerFaceText.fontSize = 2.5f;

            playerSpeakerIcon = new GameObject("Player Speaker Icon", typeof(Canvas), typeof(CanvasRenderer)).AddComponent<Image>();
            playerSpeakerIcon.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            playerSpeakerIcon.rectTransform.SetParent(transform);
            playerSpeakerIcon.rectTransform.localScale = new Vector3(0.004f, 0.004f, 1f);
            playerSpeakerIcon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            playerSpeakerIcon.rectTransform.anchoredPosition3D = new Vector3(0f, 1f, 0f);
            playerSpeakerIcon.sprite = Sprites.speakerIcon;

            transform.SetParent(centerAdjust.transform, false);
        }

        void Update()
        {
            try
            {
                if (playerNameText != null)
                {

                    if (playerCamera == null)
                    {
                        playerCamera = Camera.main;
                    }

                    playerNameText.rectTransform.rotation = Quaternion.LookRotation(playerNameText.rectTransform.position - playerCamera.transform.position);
                    playerSpeakerIcon.rectTransform.rotation = Quaternion.LookRotation(playerSpeakerIcon.rectTransform.position - playerCamera.transform.position);

                    if (rainbowName)
                    {
                        playerNameText.color = HSBColor.ToColor(nameColor);
                        nameColor.h += 0.125f * Time.deltaTime;
                        if (nameColor.h >= 1f)
                        {
                            nameColor.h = 0f;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.log.Warn($"Unable to rotate text to the camera! Exception: {e}");
            }

        }

        public void DestroyReceiver()
        {
            avatarInput.DestroyReceiver();
            avatarInput = null;
            Destroy(this);
        }

        void OnDestroy()
        {
            Plugin.log.Debug("Destroying avatar");

            //if(avatar != null && avatar.GameObject != null)
            //    Destroy(avatar.GameObject);
        }

        public void SetPlayerInfo(PlayerInfo _playerInfo, Vector3 offset, bool isLocal)
        {
            if (_playerInfo == default)
            {
                if (playerNameText != null) playerNameText.gameObject.SetActive(false);
                if (playerSpeakerIcon != null) playerSpeakerIcon.gameObject.SetActive(false);
                return;
            }

            try
            {

                playerInfo = _playerInfo.updateInfo;
                playerId = _playerInfo.playerId;
                playerName = _playerInfo.playerName;

                if (playerNameText != null && playerSpeakerIcon != null)
                {
                    if (isLocal)
                    {
                        playerNameText.gameObject.SetActive(false);
                        playerSpeakerIcon.gameObject.SetActive(false);
                    }
                    else
                    {
                        playerNameText.gameObject.SetActive(true);
                        playerNameText.alignment = TextAlignmentOptions.Center;
                        playerSpeakerIcon.gameObject.SetActive(InGameOnlineController.Instance.VoiceChatIsTalking(playerId));
                    }
                }
                else
                {
                    return;
                }

                avatarInput.SetPlayerInfo(_playerInfo, offset, false);

                transform.position = avatarInput.HeadPosRot.Position;
                //Plugin.log.Debug($"Setting {playerName}'s avatar position to {avatarInput.HeadPosRot}");
                transform.rotation = avatarInput.HeadPosRot.Rotation;

                playerNameText.text = playerName;

                if (playerInfo.playerFlags.rainbowName && !rainbowName)
                {
                    playerNameText.color = playerInfo.playerNameColor;
                    nameColor = HSBColor.FromColor(playerInfo.playerNameColor);
                }
                else if (!playerInfo.playerFlags.rainbowName && playerNameText.color != playerInfo.playerNameColor)
                {
                    playerNameText.color = playerInfo.playerNameColor;
                }

                rainbowName = playerInfo.playerFlags.rainbowName;
            }
            catch (Exception e)
            {
                Plugin.log.Critical(e);
            }

        }


        private void SetRendererInChilds(Transform origin, bool enabled)
        {
            Renderer[] rends = origin.gameObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer rend in rends)
            {
                rend.enabled = enabled;
            }
        }


    }
}
