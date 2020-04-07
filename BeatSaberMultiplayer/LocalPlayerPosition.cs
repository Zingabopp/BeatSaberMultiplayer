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
                    PosRot retVal = new PosRot(Plugin.fpfc.transform.position, Plugin.fpfc.transform.rotation);
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
    }
}
