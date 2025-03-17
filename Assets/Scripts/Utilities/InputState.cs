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

        public InputState(float l_trigger, Vector2 l_joystick, bool y, bool x, float r_trigger, Vector2 r_joystick, bool b, bool a)
        {
            // Store left controller state
            L_T_State = l_trigger;
            L_J_State = l_joystick;
            Y_Pressed = y;
            X_Pressed = x;

            // Store right controller state
            R_T_State = r_trigger;
            R_J_State = r_joystick;
            B_Pressed = b;
            A_Pressed = a;
        }
    }
}
