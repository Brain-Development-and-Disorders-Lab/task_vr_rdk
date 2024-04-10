﻿using UnityEngine;
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
    private GameObject UIAnchor;
    [SerializeField]
    private bool FollowHeadMovement;

    // Store the original anchor position
    private Vector3 InitialAnchorPosition;
    private float StimulusAnchorDistance;
    private float UIAnchorDistance;

    // Visual offset angle to place stimulus in single hemifield
    [SerializeField]
    private float OffsetAngle = 3.0f; // Degrees
    private float StimulusRadius = 0.0f; // Additional world-units to offset the stimulus
    private float TotalOffset = 0.0f;

    // Optional parameter to use a culling mask for full "eyepatch" effect (not recommended)
    [SerializeField]
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
    private LoggerManager logger;

    private void Start()
    {
        InitialAnchorPosition = new Vector3(StimulusAnchor.transform.position.x, StimulusAnchor.transform.position.y, StimulusAnchor.transform.position.z);
        CalculateOffset();
        UIAnchorDistance = Mathf.Abs(LeftCamera.transform.position.z - UIAnchor.transform.position.z);

        // Check if OVRCameraRig has been specified, required for head tracking
        if (CameraRig == null)
        {
            Debug.LogWarning("OVRCameraRig instance not specified, disabling head tracking");
            FollowHeadMovement = false;
        }

        // Logger
        logger = FindAnyObjectByType<LoggerManager>();
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

    // Update active cameras every frame
    void Update()
    {
        // If tracking head movement, update StimulusAnchor position
        if (FollowHeadMovement)
        {
            // Get the head position
            Vector3 headProjection = CameraRig.centerEyeAnchor.transform.TransformDirection(Vector3.forward);
            Vector3 headRotation = CameraRig.centerEyeAnchor.transform.eulerAngles;

            // Projections for stimulus and UI anchors
            Vector3 StimulusProjection = headProjection * StimulusAnchorDistance;
            Vector3 UIProjection = headProjection * UIAnchorDistance;

            // Update position and rotation
            StimulusAnchor.transform.position = new Vector3(StimulusProjection.x, StimulusProjection.y, StimulusProjection.z);
            StimulusAnchor.transform.eulerAngles = headRotation;
            UIAnchor.transform.position = new Vector3(UIProjection.x, UIProjection.y, UIProjection.z);
            UIAnchor.transform.eulerAngles = headRotation;
        }

        // Get the current position
        Vector3 currentPosition = StimulusAnchor.transform.position;

        // Apply masking depending on the active visual field
        if (activeField == VisualField.Left)
        {
            // Left only
            if (UseCullingMask)
            {
                LeftCamera.cullingMask = ~(1 << 6);
                RightCamera.cullingMask = 1 << 6;
            }

            // Set position
            StimulusAnchor.transform.position = new Vector3(currentPosition.x - TotalOffset, currentPosition.y, currentPosition.z);
        }
        else if (activeField == VisualField.Right)
        {
            // Right only
            if (UseCullingMask)
            {
                LeftCamera.cullingMask = 1 << 6;
                RightCamera.cullingMask = ~(1 << 6);
            }

            // Set position
            StimulusAnchor.transform.position = new Vector3(currentPosition.x + TotalOffset, currentPosition.y, currentPosition.z);
        }
        else
        {
            // Both
            if (UseCullingMask)
            {
                LeftCamera.cullingMask = ~(1 << 6);
                RightCamera.cullingMask = ~(1 << 6);
            }
        }
    }
}
