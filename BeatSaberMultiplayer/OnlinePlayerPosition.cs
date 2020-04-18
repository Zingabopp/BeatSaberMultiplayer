using BeatSaberMultiplayerLite.Data;
using BeatSaberMultiplayerLite.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR;

namespace BeatSaberMultiplayerLite
{
    public class OnlinePlayerPosition : PlayerPosition
    {
        private PosRot _headPosRot;
        private PosRot _leftPosRot;
        private PosRot _rightPosRot;
        public override PosRot HeadPosRot => _headPosRot;

        public override PosRot LeftPosRot => _leftPosRot;

        public override PosRot RightPosRot => _rightPosRot;

        public bool AcceptingUpdates => true;

        public void UpdatePlayerPosition(PlayerInfo playerInfo, Vector3 offset, bool isLocal)
        {
            if (playerInfo == null)
            {
                Plugin.log.Debug("Received null PlayerInfo in OnlinePlayerPosition.SetPlayerInfo.");
                return;
            }
            if (isLocal)
            {
                Plugin.log.Debug("OnlinePlayerPosition shouldn't receive local player info.");
                return;
            }
            PlayerUpdate playerUpdate = playerInfo.updateInfo;
            _headPosRot = new PosRot(playerUpdate.headPos + offset, playerUpdate.headRot, true);
            //Plugin.log.Debug($"Received OnlinePlayer update: {_headPosRot.Position}, {_headPosRot.Rotation}");
            _leftPosRot = new PosRot(playerUpdate.leftHandPos + offset, playerUpdate.leftHandRot, true);
            _rightPosRot = new PosRot(playerUpdate.rightHandPos + offset, playerUpdate.rightHandRot, true);

        }

        public void DestroyReceiver()
        {

        }
    }
}
