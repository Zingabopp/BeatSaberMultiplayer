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
        private static InputDevice LeftController;
        private static InputDevice RightController;

        private static InputDevice GetInputDevice(XRNode node)
        {
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (device != null)
            {
                Plugin.log.Debug($"Found {node}: {device.manufacturer}|{device.name}|{device.isValid}");
            }
            else
                Plugin.log.Error($"{node} not found.");
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
