using UnityEngine;
using System.Collections;

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
    private bool FollowHeadMovement;

    // Store the original anchor position
    private Vector3 initialAnchorPosition;
    private float anchorDistance;

    // Visual offset angle to place stimulus in single hemifield
    [SerializeField]
    private float OffsetAngle = 3.0f; // degrees
    private float TotalOffset = 0.0f;

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
    private LoggerManager logger;

    private void Start()
    {
        initialAnchorPosition = new Vector3(StimulusAnchor.transform.position.x, StimulusAnchor.transform.position.y, StimulusAnchor.transform.position.z);

        // Calculate required visual offsets for dichoptic presentation
        // Step 1: Calculate IPD
        float ipd = Mathf.Abs(LeftCamera.transform.position.x - RightCamera.transform.position.x);

        anchorDistance = Mathf.Abs(LeftCamera.transform.position.z - StimulusAnchor.transform.position.z);

        // Step 2: Calculate lambda (angle between gaze vector and static eye position vector, using IPD)
        float lambda = Mathf.Atan((ipd / 2) / anchorDistance);

        // Step 3: Calculate omega (angle between static eye position vector and future offset vector)
        float omega = (OffsetAngle * Mathf.PI / 180) - lambda;

        // Step 4: Calculate baseline offset distance from static eye position to offset position
        float offsetDistance = anchorDistance * Mathf.Tan(omega);

        // Step 5: Calculate total offset value
        TotalOffset = offsetDistance + (ipd / 2);

        // To-Do: We need to add in the width of the aperture on top of this.

        // Check if OVRCameraRig has been specified, required for head tracking
        if (CameraRig == null)
        {
            Debug.LogWarning("OVRCameraRig instance not specified, disabling head tracking");
            FollowHeadMovement = false;
        }

        // Logger
        logger = FindAnyObjectByType<LoggerManager>();
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
        // If tracking head movement, update StimulusAnchor position
        if (FollowHeadMovement)
        {
            // Get the head position
            Vector3 headProjection = CameraRig.centerEyeAnchor.transform.TransformDirection(Vector3.forward) * anchorDistance;
            Vector3 headRotation = CameraRig.centerEyeAnchor.transform.eulerAngles;

            // Update position and rotation
            StimulusAnchor.transform.position = new Vector3(headProjection.x, headProjection.y, headProjection.z);
            StimulusAnchor.transform.eulerAngles = headRotation;
        }

        // Get the current position
        Vector3 currentPosition = StimulusAnchor.transform.position;

        // Apply masking depending on the active visual field
        if (activeField == VisualField.Left)
        {
            // Left only
            LeftCamera.cullingMask = ~(1 << 6);
            RightCamera.cullingMask = 1 << 6;

            // Set position
            StimulusAnchor.transform.position.Set(currentPosition.x - TotalOffset, currentPosition.y, currentPosition.z);
        }
        else if (activeField == VisualField.Right)
        {
            // Right only
            LeftCamera.cullingMask = 1 << 6;
            RightCamera.cullingMask = ~(1 << 6);

            // Set position
            StimulusAnchor.transform.position = new Vector3(currentPosition.x + TotalOffset, currentPosition.y, currentPosition.z);
        }
        else
        {
            // Both
            LeftCamera.cullingMask = ~(1 << 6);
            RightCamera.cullingMask = ~(1 << 6);
        }
    }
}
