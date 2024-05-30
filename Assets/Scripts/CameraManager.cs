using UnityEngine;

// Custom namespaces
using Utilities;

public class CameraManager : MonoBehaviour
{
    // Left and Right eye cameras
    public Camera LeftCamera;
    public Camera RightCamera;
    public OVRCameraRig CameraRig;

    // Anchor for stimulus, this is critical for true dichoptic presentation
    public GameObject PrimaryStimulusAnchor;
    public GameObject SecondaryStimulusAnchor;
    public GameObject UIAnchor;
    public GameObject FixationAnchor;

    // Visual offset angle to place stimulus in single hemifield
    public float OffsetAngle = 3.0f; // Degrees
    public float VerticalOffset = -2.0f;
    private float stimulusWidth = 0.0f; // Additional world-units to offset the stimulus
    private float totalOffset = 0.0f;
    private float stimulusAnchorDistance;

    // Parameter to use a culling mask for full "eye-patch" effect
    [Tooltip("Enable a layer-based mask to provide a full eye-patch effect.")]
    public bool UseCullingMask = false;

    // Camera presentation modes
    public enum ActiveCamera
    {
        Left,
        Right,
    };

    // Camera presentation modes
    public enum VisualField
    {
        Left,
        Right,
        Both
    };

    // Default visual field (active camera)
    private VisualField activeField = VisualField.Both;

    private void Start()
    {
        // Check if OVRCameraRig has been specified, required for head tracking
        if (CameraRig)
        {
            // Set the anchor object for stimuli and UI as a child of the CameraRig
            PrimaryStimulusAnchor.transform.SetParent(CameraRig.centerEyeAnchor.transform, false);
            SecondaryStimulusAnchor.transform.SetParent(CameraRig.centerEyeAnchor.transform, false);
            UIAnchor.transform.SetParent(CameraRig.centerEyeAnchor.transform, false);
            FixationAnchor.transform.SetParent(CameraRig.centerEyeAnchor.transform, false);

            CalculateOffset();
        }
    }

    /// <summary>
    /// Calculate the offset distance to accurately present the stimulus 2.5-3 degrees offset from the central
    /// fixation point.
    /// </summary>
    private void CalculateOffset()
    {
        // Calculate required visual offsets for dichoptic presentation
        // Step 1: Calculate IPD
        float ipd = Mathf.Abs(LeftCamera.transform.position.x - RightCamera.transform.position.x);

        // Step 2: Calculate the distance (d) of the view position to the stimulus, world units
        stimulusAnchorDistance = Mathf.Abs(LeftCamera.transform.position.z - PrimaryStimulusAnchor.transform.position.z);

        // Step 3: Calculate theta (angle between static eye position vector and future offset vector), radians
        float theta = OffsetAngle * Mathf.PI / 180;

        // Step 4: Calculate baseline offset distance from static eye position to offset position, world units
        float offsetDistance = stimulusAnchorDistance * Mathf.Tan(theta);

        // Step 5: Calculate total offset value
        totalOffset = offsetDistance + (stimulusWidth / 2.0f) + (ipd / 2.0f);
        Debug.Log("Calculated CameraManager offset distance: " + offsetDistance);
    }

    /// <summary>
    /// Set the active visual field
    /// </summary>
    /// <param name="field"></param>
    public void SetActiveField()
    {
        // Update the vertical offset of the fixation cross
        FixationAnchor.transform.localPosition = new Vector3(0.0f, 0.0f + VerticalOffset, stimulusAnchorDistance);

        // Central visual presentation
        PrimaryStimulusAnchor.transform.localPosition = new Vector3(0.0f, 0.0f + VerticalOffset, stimulusAnchorDistance);
        SecondaryStimulusAnchor.transform.localPosition = new Vector3(0.0f, 0.0f + VerticalOffset, stimulusAnchorDistance);

        if (UseCullingMask)
        {
            LeftCamera.cullingMask = ~(1 << 6);
            RightCamera.cullingMask = ~(1 << 6);
        }
    }

    public void SetActiveField(ActiveCamera primaryStimulusCamera, VisualField primaryStimulusVisualField, ActiveCamera secondaryStimulusCamera, VisualField secondaryStimulusVisualField)
    {
        if (primaryStimulusCamera == secondaryStimulusCamera)
        {
            Debug.LogError("Stimuli are being presented to the same eye");
            return;
        }

        // Update the vertical offset of the fixation cross
        FixationAnchor.transform.localPosition = new Vector3(0.0f, 0.0f + VerticalOffset, stimulusAnchorDistance);

        if (primaryStimulusCamera == ActiveCamera.Left)
        {
            LeftCamera.cullingMask = ~(1 << 6);
            RightCamera.cullingMask = 1 << 6;
        }

        else
        {
            LeftCamera.cullingMask = 1 << 6;
            RightCamera.cullingMask = ~(1 << 6);
        }

        // Apply local offset adjustments for lateralized presentation and culling mask for eye-patch effect
        switch (primaryStimulusVisualField)
        {
            case VisualField.Left:
                PrimaryStimulusAnchor.transform.localPosition = new Vector3(0.0f - totalOffset, 0.0f + VerticalOffset, stimulusAnchorDistance); break;
            case VisualField.Right:
                PrimaryStimulusAnchor.transform.localPosition = new Vector3(0.0f + totalOffset, 0.0f + VerticalOffset, stimulusAnchorDistance); break;
            default:
                PrimaryStimulusAnchor.transform.localPosition = new Vector3(0.0f, 0.0f + VerticalOffset, stimulusAnchorDistance); break;
        }

        switch (secondaryStimulusVisualField)
        {
            case VisualField.Left:
                SecondaryStimulusAnchor.transform.localPosition = new Vector3(0.0f - totalOffset, 0.0f + VerticalOffset, stimulusAnchorDistance); break;
            case VisualField.Right:
                SecondaryStimulusAnchor.transform.localPosition = new Vector3(0.0f + totalOffset, 0.0f + VerticalOffset, stimulusAnchorDistance); break;
            default:
                SecondaryStimulusAnchor.transform.localPosition = new Vector3(0.0f, 0.0f + VerticalOffset, stimulusAnchorDistance); break;
        }
    }

    /// <summary>
    /// Set the width of the stimulus, measured in world units
    /// </summary>
    /// <param name="width">Stimulus width, measured in world units</param>
    public void SetStimulusWidth(float width)
    {
        stimulusWidth = width;
        CalculateOffset(); // We need to re-calculate the offset values if this has been updated
    }

    /// <summary>
    /// Get the current active visual field
    /// </summary>
    /// <returns>Active visual field, member of `VisualField` enum</returns>
    public VisualField GetActiveField()
    {
        return activeField;
    }

    /// <summary>
    /// Get the offset distance, measured in world units
    /// </summary>
    /// <returns>`float` representing the offset distance</returns>
    public float GetTotalOffset()
    {
        return totalOffset;
    }
}
