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
    public class LocalPlayerPosition : PlayerPosition
    {
        public static LocalPlayerPosition instance;
        public LocalPlayerPosition()
        {
            instance = this;
            ValidPose = true;
        }
        public override PosRot HeadPosRot
        {
            get 
            {
                if (Plugin.fpfc != null)
                {
                    PosRot retVal = new PosRot(Plugin.fpfc.transform.position, Plugin.fpfc.transform.rotation, true);
                    return retVal;
                }
                return GetXRNodeWorldPosRot(XRNode.Head); 
            }
        }

        public override PosRot LeftPosRot
        {
            get
            {
                return GetXRNodeWorldPosRot(XRNode.LeftHand);
            }
        }

        public override PosRot RightPosRot
        {
            get
            {
                return GetXRNodeWorldPosRot(XRNode.RightHand);
            }
        }
        private static PosRot GetXRNodeWorldPosRot(XRNode node)
        {
            InputDevice device = ControllersHelper.GetInputDevice(node);
            bool valid = true;
            if (!device.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos))
            {
                valid = false;
            }
            if (!device.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot))
            {
                valid = false;
            }
            if (valid)
            {
                var roomCenter = GetRoomCenter();
                var roomRotation = GetRoomRotation();
                pos = roomRotation * pos;
                pos += roomCenter;
                rot = roomRotation * rot;
            }
            return new PosRot(pos, rot, valid);
        }

        private static PosRot GetTrackerWorldPosRot(XRNodeState tracker)
        {
            Vector3 pos = new Vector3();
            Quaternion rot = new Quaternion();
            bool valid = false;
            try
            {
                var nodes = new List<XRNodeState>();
                InputTracking.GetNodeStates(nodes);
                foreach (XRNodeState node in nodes)
                {
                    if (node.uniqueID != tracker.uniqueID)
                        continue;
                    if (node.TryGetPosition(out Vector3 posistion) && node.TryGetRotation(out Quaternion rotation))
                    {
                        var roomCenter = GetRoomCenter();
                        var roomRotation = GetRoomRotation();
                        pos = roomRotation * posistion;
                        pos += roomCenter;
                        rot = roomRotation * rotation;
                        valid = true;
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.log.Error(e);
            }
            return new PosRot(pos, rot, valid);
        }
    }
}
