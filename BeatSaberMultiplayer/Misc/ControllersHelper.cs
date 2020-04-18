using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

namespace BeatSaberMultiplayerLite.Misc
{
    static class ControllersHelper
    {
        private static bool initialized = false;
        internal static InputDevice LeftController;
        internal static InputDevice RightController;
        internal static InputDevice Head;

        public static InputDevice GetLeftController()
        {
            if (LeftController.isValid && LeftController.characteristics.HasFlag(InputDeviceCharacteristics.Left))
                return LeftController;
            else
                return GetInputDevice(XRNode.LeftHand);
        }
        public static InputDevice GetRightController()
        {
            if (RightController.isValid && RightController.characteristics.HasFlag(InputDeviceCharacteristics.Right))
                return RightController;
            else
                return GetInputDevice(XRNode.RightHand);
        }
        public static InputDevice GetHead()
        {
            if (Head.isValid && Head.characteristics.HasFlag(InputDeviceCharacteristics.HeadMounted))
                return Head;
            else
                return GetInputDevice(XRNode.Head);
        }

        public static InputDevice GetInputDevice(XRNode node)
        {
            InputDevice device;
            switch (node)
            {
                case XRNode.Head:
                    if (!Head.isValid || !Head.characteristics.HasFlag(InputDeviceCharacteristics.HeadMounted))
                    {
                        Head = InputDevices.GetDeviceAtXRNode(node);
#if DEBUG
                        Plugin.log.Debug($"Had to get {node} from InputDevices.");
#endif
                    }
                    return Head;
                case XRNode.LeftHand:
                    if (!LeftController.isValid || !LeftController.characteristics.HasFlag(InputDeviceCharacteristics.Left))
                    {
                        LeftController = InputDevices.GetDeviceAtXRNode(node);
#if DEBUG
                        Plugin.log.Debug($"Had to get {node} from InputDevices.");
#endif
                    }
                    return LeftController;
                case XRNode.RightHand:
                    if (!RightController.isValid || !RightController.characteristics.HasFlag(InputDeviceCharacteristics.Right))
                    {
                        RightController = InputDevices.GetDeviceAtXRNode(node);
#if DEBUG
                        Plugin.log.Debug($"Had to get {node} from InputDevices.");
#endif
                    }
                    return RightController;
                //case XRNode.LeftEye:
                //    break;
                //case XRNode.RightEye:
                //    break;
                //case XRNode.CenterEye:
                //    break;
                //case XRNode.GameController:
                //    break;
                //case XRNode.TrackingReference:
                //    break;
                //case XRNode.HardwareTracker:
                //    break;
                default:
                    device = InputDevices.GetDeviceAtXRNode(node);
                    break;
            }
            
            return device;
        }

        public static void Init()
        {
            LeftController = GetInputDevice(XRNode.LeftHand);
            RightController = GetInputDevice(XRNode.RightHand);

            initialized = true;
        }

        public static bool GetRightGrip()
        {
            if (!initialized)
            {
                Init();
            }
            if (RightController.isValid)
            {
                if (RightController.TryGetFeatureValue(CommonUsages.gripButton, out bool value))
                {
                    return value;
                }
            }
            return false;
        }

        public static bool GetLeftGrip()
        {
            if (!initialized)
            {
                Init();
            }
            if (LeftController.isValid)
            {
                if (LeftController.TryGetFeatureValue(CommonUsages.gripButton, out bool value))
                {
                    return value;
                }
            }
            return false;
        }
    }





}
