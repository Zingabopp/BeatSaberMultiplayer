using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR;

namespace BeatSaberMultiplayerLite
{
    public class PlayerPosition
    {
        public static PlayerPosition instance;
        public PlayerPosition()
        {
            instance = this;
        }
        public PosRot HeadPosRot
        {
            get { return GetXRNodeWorldPosRot(XRNode.Head); }
        }

        public PosRot LeftPosRot
        {
            get
            {
                return GetXRNodeWorldPosRot(XRNode.LeftHand);
            }
        }

        public PosRot RightPosRot
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

        private static Transform _originTransform;
        private static MainSettingsModelSO _mainSettingsModel;

        private static MainSettingsModelSO MainSettingsModel
        {
            get
            {
                if (_mainSettingsModel == null)
                {
                    _mainSettingsModel = Resources.FindObjectsOfTypeAll<MainSettingsModelSO>().FirstOrDefault();
                }

                return _mainSettingsModel;
            }
        }

        public static Vector3 GetRoomCenter()
        {
            if (_originTransform == null)
            {
                return MainSettingsModel == null ? Vector3.zero : MainSettingsModel.roomCenter;
            }

            return _originTransform.position;
        }

        public static Quaternion GetRoomRotation()
        {
            if (_originTransform == null)
            {
                return MainSettingsModel == null ? Quaternion.identity : Quaternion.Euler(0, MainSettingsModel.roomRotation, 0);
            }

            return _originTransform.rotation;
        }
    }

    public struct PosRot
    {
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }

        public PosRot(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }
    }
}
