using UnityEngine;
using System.Collections;

namespace Utilities
{
    public static class VRInput
    {
        public static IEnumerator SetHaptics(float frequency, float amplitude, float duration, bool left, bool right)
        {
            // Set controller vibration to specified parameters
            if (left)
            {
                OVRInput.SetControllerVibration(frequency, amplitude, OVRInput.Controller.LTouch);
            }
            if (right)
            {
                OVRInput.SetControllerVibration(frequency, amplitude, OVRInput.Controller.RTouch);
            }

            // Wait the specified duration (seconds)
            yield return new WaitForSeconds(duration);

            // Reset controller vibration
            if (left)
            {
                OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.LTouch);
            }
            if (right)
            {
                OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
            }
        }

        public static bool PollLeftTrigger(float threshold = 0.8f)
        {
            return OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger) > threshold || Input.GetKeyDown(KeyCode.Alpha2);
        }

        public static bool PollRightTrigger(float threshold = 0.8f)
        {
            return OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger) > threshold || Input.GetKeyDown(KeyCode.Alpha7);
        }

        public static bool PollAnyInput()
        {
            return OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger) > 0.0f ||
                OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger) > 0.0f ||
                Input.GetKeyDown(KeyCode.Alpha2) == true ||
                Input.GetKeyDown(KeyCode.Alpha7) == true;
        }
    }
}
