using UnityEngine;

namespace Utilities
{
    /// <summary>
    /// Struct to manage input states captured during experiment
    /// </summary>
    public struct InputState
    {
        // Left controller state
        public float L_T_State { get; set; }
        public Vector2 L_J_State { get; set; }
        public bool Y_Pressed { get; set; }
        public bool X_Pressed { get; set; }

        // Right controller state
        public float R_T_State { get; set; }
        public Vector2 R_J_State { get; set; }
        public bool B_Pressed { get; set; }
        public bool A_Pressed { get; set; }

        public InputState(float L_trigger, Vector2 L_joystick, bool Y, bool X, float R_trigger, Vector2 R_joystick, bool B, bool A)
        {
            // Store left controller state
            L_T_State = L_trigger;
            L_J_State = L_joystick;
            Y_Pressed = Y;
            X_Pressed = X;

            // Store right controller state
            R_T_State = R_trigger;
            R_J_State = R_joystick;
            B_Pressed = B;
            A_Pressed = A;
        }
    }
}
