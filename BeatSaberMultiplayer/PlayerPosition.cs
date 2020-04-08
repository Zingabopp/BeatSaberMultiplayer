using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR;

namespace BeatSaberMultiplayerLite
{
    public abstract class PlayerPosition
    {
        public bool ValidPose { get; protected set; }

        public abstract PosRot HeadPosRot { get; }

        public abstract PosRot LeftPosRot { get; }

        public abstract PosRot RightPosRot { get; }


        public bool TryGetHeadPose(out Pose head)
        {
            PosRot posRot = HeadPosRot;
            head = new Pose(posRot.Position, posRot.Rotation);
            return ValidPose;
        }

        public bool TryGetLeftHandPose(out Pose leftHand)
        {
            PosRot posRot = LeftPosRot;
            leftHand = new Pose(posRot.Position, posRot.Rotation);
            return ValidPose;
        }

        public bool TryGetRightHandPose(out Pose rightHand)
        {
            PosRot posRot = RightPosRot;
            rightHand = new Pose(posRot.Position, posRot.Rotation);
            return ValidPose;
        }

        #region Static
        private static Transform _originTransform;
        private static MainSettingsModelSO _mainSettingsModel;

        protected static MainSettingsModelSO MainSettingsModel
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
        #endregion
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
        public override string ToString()
        {
            return $"{Position}|{Rotation}";
        }
    }
}
