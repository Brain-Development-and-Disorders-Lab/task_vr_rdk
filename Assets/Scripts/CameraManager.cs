using UnityEngine;

using ExperimentUtilities;

public class CameraManager : MonoBehaviour
{
    // Left and Right eye cameras
    [SerializeField]
    private Camera LeftCamera;
    [SerializeField]
    private Camera RightCamera;
    [SerializeField]
    private OVRCameraRig CameraRig;

    // Anchor for stimulus, this is critical for true dichoptic presentation
    [SerializeField]
    private GameObject StimulusAnchor;
    [SerializeField]
    private GameObject UIAnchor;
    [SerializeField]
    private bool FollowHeadMovement;

    private float StimulusAnchorDistance;

    // Visual offset angle to place stimulus in single hemifield
    [SerializeField]
    private float OffsetAngle = 3.0f; // Degrees
    private float StimulusRadius = 0.0f; // Additional world-units to offset the stimulus
    private float TotalOffset = 0.0f;

    // Optional parameter to use a culling mask for full "eyepatch" effect (not recommended)
    [Tooltip("Enable a layer-based mask to provide the eye-path effect. Not recommended but may be required.")]
    public bool UseCullingMask = false;

    // Camera presentation modes
    public enum VisualField
    {
        Left,
        Right,
        Both
    };

    // Default visual field (active camera)
    private VisualField activeField = VisualField.Both;

    // Logger
    private VRLogger logger;

    private void Start()
    {
        // Check if OVRCameraRig has been specified, required for head tracking
        if (CameraRig)
        {
            // Set the anchor object for stimuli and UI as a child of the CameraRig
            StimulusAnchor.transform.SetParent(CameraRig.centerEyeAnchor.transform, false);
            UIAnchor.transform.SetParent(CameraRig.centerEyeAnchor.transform, false);

            CalculateOffset();
        }

        // Logger
        logger = FindAnyObjectByType<VRLogger>();
    }

    private void CalculateOffset()
    {
        // Calculate required visual offsets for dichoptic presentation
        // Step 1: Calculate IPD
        float ipd = Mathf.Abs(LeftCamera.transform.position.x - RightCamera.transform.position.x);

        StimulusAnchorDistance = Mathf.Abs(LeftCamera.transform.position.z - StimulusAnchor.transform.position.z);

        // Step 2: Calculate lambda (angle between gaze vector and static eye position vector, using IPD)
        float lambda = Mathf.Atan((ipd / 2) / StimulusAnchorDistance);

        // Step 3: Calculate omega (angle between static eye position vector and future offset vector)
        float omega = (OffsetAngle * Mathf.PI / 180) - lambda;

        // Step 4: Calculate baseline offset distance from static eye position to offset position
        float offsetDistance = StimulusAnchorDistance * Mathf.Tan(omega);

        // Step 5: Calculate total offset value
        TotalOffset = offsetDistance + StimulusRadius + (ipd / 2);
    }

    /// <summary>
    /// Set the active visual field
    /// </summary>
    /// <param name="field"></param>
    public void SetActiveField(VisualField field)
    {
        if (field != activeField)
        {
            activeField = field;
        }

        // Apply local offset adjustments for lateralized presentation
        if (field == VisualField.Left)
        {
            StimulusAnchor.transform.localPosition = new Vector3(0.0f - TotalOffset, 0.0f, StimulusAnchorDistance);
            if (UseCullingMask)
            {
                LeftCamera.cullingMask = ~(1 << 6);
                RightCamera.cullingMask = 1 << 6;
            }
        }
        else if (field == VisualField.Right)
        {
            StimulusAnchor.transform.localPosition = new Vector3(0.0f + TotalOffset, 0.0f, StimulusAnchorDistance);
            if (UseCullingMask)
            {
                LeftCamera.cullingMask = 1 << 6;
                RightCamera.cullingMask = ~(1 << 6);
            }
        }
        else
        {
            StimulusAnchor.transform.localPosition = new Vector3(0.0f, 0.0f, StimulusAnchorDistance);
            if (UseCullingMask)
            {
                LeftCamera.cullingMask = ~(1 << 6);
                RightCamera.cullingMask = ~(1 << 6);
            }
        }
    }

    /// <summary>
    /// Set the offset value for the aperture. This should be in world units, and typically represents the radius
    /// of the aperture.
    /// </summary>
    /// <param name="offsetValue">Offset value, measured in world units</param>
    public void SetStimulusRadius(float offsetValue)
    {
        StimulusRadius = offsetValue;
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
}
