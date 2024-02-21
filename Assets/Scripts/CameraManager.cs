using UnityEngine;
using System.Collections;

public class CameraManager : MonoBehaviour
{
    // Left and Right eye cameras
    [SerializeField]
    private Camera leftCamera;
    [SerializeField]
    private Camera rightCamera;

    // Camera presentation modes
    public enum VisualField
    {
        Left,
        Right,
        Both
    };

    // Default visual field (active camera)
    private VisualField activeField = VisualField.Both;

    public void SetActiveField(VisualField field)
    {
        activeField = field;
    }

    // Update active cameras every frame
    void Update()
    {
        // Apply masking depending on the active visual field
        if (activeField == VisualField.Left)
        {
            // Left only
            leftCamera.cullingMask = ~(1 << 6);
            rightCamera.cullingMask = 1 << 6;
        }
        else if (activeField == VisualField.Right)
        {
            // Right only
            leftCamera.cullingMask = 1 << 6;
            rightCamera.cullingMask = ~(1 << 6);
        }
        else
        {
            // Both
            leftCamera.cullingMask = ~(1 << 6);
            rightCamera.cullingMask = ~(1 << 6);
        }
    }
}
