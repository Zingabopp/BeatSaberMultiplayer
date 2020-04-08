using BeatSaberMultiplayerLite.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR;

namespace BeatSaberMultiplayerLite
{
    public class OnlinePlayerPosition : PlayerPosition, IPlayerInfoReceiver
    {
        private PosRot _headPosRot;
        private PosRot _leftPosRot;
        private PosRot _rightPosRot;
        public override PosRot HeadPosRot => _headPosRot;

        public override PosRot LeftPosRot => _leftPosRot;

        public override PosRot RightPosRot => _rightPosRot;

        public bool AcceptingUpdates => true;

        private static PosRot GetXRNodeWorldPosRot(XRNode node)
        {
            var pos = InputTracking.GetLocalPosition(node);
            var rot = InputTracking.GetLocalRotation(node);

            var roomCenter = GetRoomCenter();
            var roomRotation = GetRoomRotation();
            pos = roomRotation * pos;
            pos += roomCenter;
            rot = roomRotation * rot;
            return new PosRot(pos, rot);
        }

        private static PosRot GetTrackerWorldPosRot(XRNodeState tracker)
        {
            Vector3 pos = new Vector3();
            Quaternion rot = new Quaternion();
            try
            {
                var notes = new List<XRNodeState>();
                InputTracking.GetNodeStates(notes);
                foreach (XRNodeState note in notes)
                {
                    if (note.uniqueID != tracker.uniqueID)
                        continue;
                    if (note.TryGetPosition(out pos) && note.TryGetRotation(out rot))
                    {
                        var roomCenter = GetRoomCenter();
                        var roomRotation = GetRoomRotation();
                        pos = roomRotation * pos;
                        pos += roomCenter;
                        rot = roomRotation * rot;
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.log.Error(e);
            }
            return new PosRot(pos, rot);
        }

        public void SetPlayerInfo(PlayerInfo playerInfo, Vector3 offset, bool isLocal)
        {
            if(playerInfo == null)
            {
                Plugin.log.Debug("Received null PlayerInfo in OnlinePlayerPosition.SetPlayerInfo.");
                return;
            }
            if(isLocal)
            {
                Plugin.log.Debug("OnlinePlayerPosition shouldn't receive local player info.");
                return;
            }
            PlayerUpdate playerUpdate = playerInfo.updateInfo;
            _headPosRot = new PosRot(playerUpdate.headPos + offset, playerUpdate.headRot);
            //Plugin.log.Debug($"Received OnlinePlayer update: {_headPosRot.Position}, {_headPosRot.Rotation}");
            _leftPosRot = new PosRot(playerUpdate.leftHandPos + offset, playerUpdate.leftHandRot);
            _rightPosRot = new PosRot(playerUpdate.rightHandPos + offset, playerUpdate.rightHandRot);
            
        }

        public void DestroyReceiver()
        {
            
        }
    }
}
