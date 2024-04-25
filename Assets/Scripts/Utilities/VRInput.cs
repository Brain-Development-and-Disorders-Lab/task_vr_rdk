using UnityEngine;
using System.Collections;
using System.Threading.Tasks;
using System;

namespace Utilities
{
    public static class VRInput
    {
        /// <summary>
        /// Stimulate controller haptics using frequency and amplitude parameters
        /// </summary>
        /// <param name="frequency">Frequency of the vibration</param>
        /// <param name="amplitude">Amplitude of the vibration</param>
        /// <param name="duration">Duration of the haptic</param>
        /// <param name="left">Enable on the left controller</param>
        /// <param name="right">Enable on the right controller</param>
        /// <returns></returns>
        public static async void SetHaptics(float frequency, float amplitude, float duration, bool left, bool right)
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
            await Task.Delay(TimeSpan.FromSeconds(duration));

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

        /// <summary>
        /// Return if the left trigger is active above a defined threshold (sensitivity)
        /// </summary>
        /// <param name="threshold">The sensitivity of the trigger before registering an activation</param>
        /// <returns></returns>
        public static bool PollLeftTrigger(float threshold = 0.8f)
        {
            return OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger) > threshold || Input.GetKeyDown(KeyCode.Alpha2);
        }

        /// <summary>
        /// Return if the right trigger is active above a defined threshold (sensitivity)
        /// </summary>
        /// <param name="threshold">The sensitivity of the trigger before registering an activation</param>
        /// <returns></returns>
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
