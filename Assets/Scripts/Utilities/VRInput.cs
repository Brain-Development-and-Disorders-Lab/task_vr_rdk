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
            return OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger) > threshold || Input.GetKey(KeyCode.Alpha2);
        }

        /// <summary>
        /// Return if the right trigger is active above a defined threshold (sensitivity)
        /// </summary>
        /// <param name="threshold">The sensitivity of the trigger before registering an activation</param>
        /// <returns></returns>
        public static bool PollRightTrigger(float threshold = 0.8f)
        {
            return OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger) > threshold || Input.GetKey(KeyCode.Alpha7);
        }

        /// <summary>
        /// Return a structure representing the current input state
        /// </summary>
        /// <returns></returns>
        public static InputState PollAllInput()
        {
            return new InputState(
                OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger),
                OVRInput.Get(OVRInput.RawButton.Y) || Input.GetKey(KeyCode.Alpha2),
                OVRInput.Get(OVRInput.RawButton.X) || Input.GetKey(KeyCode.Alpha3),
                OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger),
                OVRInput.Get(OVRInput.RawButton.B) || Input.GetKey(KeyCode.Alpha7),
                OVRInput.Get(OVRInput.RawButton.A) || Input.GetKey(KeyCode.Alpha6)
            );
        }

        /// <summary>
        /// Flag if any inputs are active at one time
        /// </summary>
        /// <returns></returns>
        public static bool AnyInput()
        {
            InputState inputs = PollAllInput();
            return inputs.L_T_State == 1.0f || inputs.Y_Pressed || inputs.X_Pressed ||
                inputs.R_T_State == 1.0f || inputs.B_Pressed || inputs.A_Pressed;
        }
    }
}
