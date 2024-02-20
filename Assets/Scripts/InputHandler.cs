using UnityEngine;
using System.Collections;

using UXF;

public class InputHandler : MonoBehaviour
{
    void Update()
    {
        // End the current trial when the trigger button is pressed
        if (OVRInput.Get(OVRInput.Button.One) == true)
        {
            Session.instance.EndCurrentTrial();
        }
    }
}
