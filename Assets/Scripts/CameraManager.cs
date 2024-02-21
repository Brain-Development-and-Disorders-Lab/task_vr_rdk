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

    /// <summary>
    /// Set the active visual field
    /// </summary>
    /// <param name="field"></param>
    public void SetActiveField(VisualField field)
    {
        if (field != activeField)
        {
            activeField = field;
            Debug.Log("Changed visual field: " + field.ToString());
        }
        else
        {
            Debug.LogWarning("Active visual field unchanged");
        }
    }

    /// <summary>
    /// Get the current active visual field
    /// </summary>
    /// <returns>Active visual field, member of `VisualField` enum</returns>
    public VisualField GetActiveField()
    {
        return activeField;
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
