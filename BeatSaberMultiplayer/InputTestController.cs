using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

namespace BeatSaberMultiplayerLite
{
    public abstract class FeatureValue
    {
    }

    public class FeatureValue<T>
        : FeatureValue where T : struct
    {
        public T Value;

        public FeatureValue(T value)
        {
            Value = value;
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    public class InputTestController : MonoBehaviour
    {
        internal static InputDevice LeftController;
        internal static InputDevice RightController;
        internal static InputDevice Head;
        protected void Awake()
        {
            InputDevices.deviceConnected += OnDeviceConnected;
            InputDevices.deviceDisconnected += OnDeviceDisconnected;
            InputDevices.deviceConfigChanged += OnDeviceConfigChanged;
        }

        private void OnDeviceConfigChanged(InputDevice device)
        {

            if (device.characteristics.HasFlag(InputDeviceCharacteristics.Right))
            {
                Plugin.log.Info("Right controller deviceConfigChanged.");
            }
            else if (device.characteristics.HasFlag(InputDeviceCharacteristics.Left))
            {
                Plugin.log.Info("Left controller deviceConfigChanged.");
            }
        }

        private void OnDeviceDisconnected(InputDevice device)
        {
            if (device.characteristics.HasFlag(InputDeviceCharacteristics.Right))
            {
                Plugin.log.Info("Right controller disconnected.");
            }
            else if (device.characteristics.HasFlag(InputDeviceCharacteristics.Left))
            {
                Plugin.log.Info("Left controller disconnected.");
            }
        }

        private void OnDeviceConnected(InputDevice device)
        {

            if (device.characteristics.HasFlag(InputDeviceCharacteristics.Right))
            {
                RightController = device;
                Plugin.log.Info("Right controller connected.");
            }
            else if (device.characteristics.HasFlag(InputDeviceCharacteristics.Left))
            {
                LeftController = device;
                Plugin.log.Info("Left controller connected.");
            }
        }

        public static InputDevice GetLeftController()
        {
            if (LeftController.isValid && LeftController.characteristics.HasFlag(InputDeviceCharacteristics.Left))
                return LeftController;
            else
            {
                InputDevice device = GetInputDevice(XRNode.LeftHand);
                LeftController = device;
                return device;
            }
        }
        public static InputDevice GetRightController()
        {
            if (RightController.isValid && RightController.characteristics.HasFlag(InputDeviceCharacteristics.Right))
                return RightController;
            else
            {
                InputDevice device = GetInputDevice(XRNode.RightHand);
                RightController = device;
                return device;
            }
        }
        public static InputDevice GetHead()
        {
            if (Head.isValid && Head.characteristics.HasFlag(InputDeviceCharacteristics.HeadMounted))
                return Head;
            else
            {
                InputDevice device = GetInputDevice(XRNode.Head);
                Head = device;
                return device;
            }
        }
        public static InputDevice GetInputDevice(XRNode node)
        {
            InputDevice device;
            switch (node)
            {
                case XRNode.Head:
                    if (!Head.isValid)
                    {
                        Head = InputDevices.GetDeviceAtXRNode(node);
#if DEBUG
                        Plugin.log.Debug($"Had to get {node} from InputDevices.");
#endif
                    }
                    return Head;
                case XRNode.LeftHand:
                    if (!LeftController.isValid)
                    {
                        LeftController = InputDevices.GetDeviceAtXRNode(node);
#if DEBUG
                        Plugin.log.Debug($"Had to get {node} from InputDevices.");
#endif
                    }
                    return LeftController;
                case XRNode.RightHand:
                    if (!RightController.isValid)
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


        public readonly Dictionary<string, FeatureValue> LastFeatureValues = new Dictionary<string, FeatureValue>();

        public enum ButtonOption
        {
            triggerButton,
            thumbrest,
            primary2DAxisClick,
            primary2DAxisTouch,
            menuButton,
            gripButton,
            secondaryButton,
            secondaryTouch,
            primaryButton,
            primaryTouch
        };

        // to obtain input devices
        InputDevice[] inputDevices = new InputDevice[2];


        List<InputFeatureUsage> inputFeatures = new List<InputFeatureUsage>();
        void Update()
        {
            if (!inputDevices[0].isValid)
            {
                inputDevices[0] = GetRightController();
                PrintDeviceInfo(inputDevices[0]);
                Plugin.log.Debug($"Got new RightController");
            }
            if (!inputDevices[1].isValid)
            {
                inputDevices[1] = GetLeftController();
                PrintDeviceInfo(inputDevices[1]);
                Plugin.log.Debug($"Got new LeftController");
            }
            for (int i = 0; i < 2; i++)
            {
                InputDevice device = inputDevices[i];
                inputFeatures.Clear();
                if (device.TryGetFeatureUsages(inputFeatures))
                {
                    foreach (InputFeatureUsage feature in inputFeatures)
                    {
                        string key = $"{feature.name}|{device.name}";
                        if (feature.type == typeof(bool))
                        {
                            bool featureValue;
                            if (device.TryGetFeatureValue(feature.As<bool>(), out featureValue))
                            {
                                if (LastFeatureValues.TryGetValue(key, out FeatureValue value) && value is FeatureValue<bool> v)
                                {
                                    if (v.Value != featureValue)
                                    {
                                        Debug.Log(string.Format($"{key} | Bool value changed to '{featureValue.ToString()}'"));
                                        v.Value = featureValue;
                                    }
                                }
                                else
                                {
                                    LastFeatureValues.Add(key, new FeatureValue<bool>(featureValue));
                                }
                            }
                        }
                        else if (feature.type == typeof(float))
                        {
                            float featureValue;
                            if (device.TryGetFeatureValue(feature.As<float>(), out featureValue) && featureValue > 0.001f)
                            {
                                if (LastFeatureValues.TryGetValue(key, out FeatureValue value) && value is FeatureValue<float> v)
                                {
                                    if (v.Value != featureValue)
                                    {
                                        Debug.Log(string.Format($"{key} | float value changed to '{featureValue.ToString()}'"));
                                        v.Value = featureValue;
                                    }
                                }
                                else
                                {
                                    LastFeatureValues.Add(key, new FeatureValue<float>(featureValue));
                                }
                            }
                        }
                        else if (feature.type == typeof(Vector2))
                        {
                            Vector2 featureValue;
                            if (device.TryGetFeatureValue(feature.As<Vector2>(), out featureValue))
                            {
                                if (LastFeatureValues.TryGetValue(key, out FeatureValue value) && value is FeatureValue<Vector2> v)
                                {
                                    if (v.Value != featureValue)
                                    {
                                        Debug.Log(string.Format($"{key} | Vector2 value changed to '{featureValue.ToString()}'"));
                                        v.Value = featureValue;
                                    }
                                }
                                else
                                {
                                    LastFeatureValues.Add(key, new FeatureValue<Vector2>(featureValue));
                                }
                            }
                        }
                        else if (feature.type == typeof(Vector3) && false)
                        {
                            Vector3 featureValue;
                            if (device.TryGetFeatureValue(feature.As<Vector3>(), out featureValue))
                            {
                                if (LastFeatureValues.TryGetValue(key, out FeatureValue value) && value is FeatureValue<Vector3> v)
                                {
                                    if (v.Value != featureValue)
                                    {
                                        Debug.Log(string.Format($"{key} | Vector3 value changed to '{featureValue.ToString()}'"));
                                        v.Value = featureValue;
                                    }
                                }
                                else
                                {
                                    LastFeatureValues.Add(key, new FeatureValue<Vector3>(featureValue));
                                }
                            }
                        }
                        else if (feature.type == typeof(Quaternion) && false)
                        {
                            Quaternion featureValue;
                            if (device.TryGetFeatureValue(feature.As<Quaternion>(), out featureValue))
                            {
                                if (LastFeatureValues.TryGetValue(key, out FeatureValue value) && value is FeatureValue<Quaternion> v)
                                {
                                    if (v.Value != featureValue)
                                    {
                                        Debug.Log(string.Format($"{key} | Quaternion value changed to '{featureValue.ToString()}'"));
                                        v.Value = featureValue;
                                    }
                                }
                                else
                                {
                                    LastFeatureValues.Add(key, new FeatureValue<Quaternion>(featureValue));
                                }
                            }
                        }
                        else if (feature.type == typeof(uint))
                        {
                            uint featureValue;
                            if (device.TryGetFeatureValue(feature.As<uint>(), out featureValue))
                            {
                                if (LastFeatureValues.TryGetValue(key, out FeatureValue value) && value is FeatureValue<uint> v)
                                {
                                    if (v.Value != featureValue)
                                    {
                                        Debug.Log(string.Format($"{key} | uint value changed to '{featureValue.ToString()}'"));
                                        v.Value = featureValue;
                                    }
                                }
                                else
                                {
                                    LastFeatureValues.Add(key, new FeatureValue<uint>(featureValue));
                                }
                            }
                        }
                    }
                }
            }
        }
        public static void PrintDeviceInfo(InputDevice device)
        {
            Plugin.log.Error($"{device.name}");
            Plugin.log.Info($"Characteristics: {device.characteristics}");
            Plugin.log.Info($"isValid: {device.isValid}");
            Plugin.log.Info($"Role: {device.role}");
            List<InputFeatureUsage> featureList = new List<InputFeatureUsage>();

            if (device.TryGetFeatureUsages(featureList))
            {
                Plugin.log.Info($"Features: {string.Join(", ", featureList.Select(f => $"{f.name}|{f.type}"))}");
            }
            else
                Plugin.log.Error("Unable to get features");
            if (device.TryGetHapticCapabilities(out HapticCapabilities hcap))
            {
                Plugin.log.Info($"HapticCapabilities: {hcap.HapticFeaturesString()}");
                Plugin.log.Info($"BufferValues: {hcap.HapticBufferValues()}");
            }
            else
                Plugin.log.Warn("Unable to get HapticCapabilities");
        }


    }

    public static class InputExtensions
    {
        public static string HapticFeaturesString(this HapticCapabilities hcap)
        {
            return $"{nameof(hcap.numChannels)}: {hcap.numChannels} | {nameof(hcap.supportsBuffer)}: {hcap.supportsBuffer} | {nameof(hcap.supportsImpulse)}: {hcap.supportsImpulse}";
        }

        public static string HapticBufferValues(this HapticCapabilities hcap)
        {
            return $"bufferFrequencyHz: {hcap.bufferFrequencyHz} | {nameof(hcap.bufferMaxSize)}: {hcap.bufferMaxSize} | {nameof(hcap.bufferOptimalSize)}: {hcap.bufferOptimalSize}";
        }
    }
}
