﻿using UnityEngine;
using System.Collections;

public class CameraManager : MonoBehaviour
{
    // Left and Right eye cameras
    [SerializeField]
    private Camera leftCamera;
    [SerializeField]
    private Camera rightCamera;

    // Anchor for stimulus, this is critical for true dichoptic presentation
    [SerializeField]
    private GameObject stimulusAnchor;

    // Store the original anchor position
    private Vector3 initialAnchorPosition;

    // Visual offset angle to place stimulus in single hemifield
    [SerializeField]
    private float offsetAngle = 3.0f; // degrees
    private float totalOffset = 0.0f;

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
        initialAnchorPosition = new Vector3(stimulusAnchor.transform.position.x, stimulusAnchor.transform.position.y, stimulusAnchor.transform.position.z);

        // Calculate required visual offsets for dichoptic presentation
        // Step 1: Calculate IPD
        float ipd = Mathf.Abs(leftCamera.transform.position.x - rightCamera.transform.position.x);

        float anchorDistance = Mathf.Abs(leftCamera.transform.position.z - stimulusAnchor.transform.position.z);

        // Step 2: Calculate lambda (angle between gaze vector and static eye position vector, using IPD)
        float lambda = Mathf.Atan((ipd / 2) / anchorDistance);

        // Step 3: Calculate omega (angle between static eye position vector and future offset vector)
        float omega = (offsetAngle * Mathf.PI / 180) - lambda;

        // Step 4: Calculate baseline offset distance from static eye position to offset position
        float offsetDistance = anchorDistance * Mathf.Tan(omega);

        // Step 5: Calculate total offset value
        totalOffset = offsetDistance + (ipd / 2);
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
        // Apply masking depending on the active visual field
        if (activeField == VisualField.Left)
        {
            // Left only
            leftCamera.cullingMask = ~(1 << 6);
            rightCamera.cullingMask = 1 << 6;

            // Set position
            stimulusAnchor.transform.position = new Vector3(
                initialAnchorPosition.x - totalOffset,
                initialAnchorPosition.y,
                initialAnchorPosition.z
            );
        }
        else if (activeField == VisualField.Right)
        {
            // Right only
            leftCamera.cullingMask = 1 << 6;
            rightCamera.cullingMask = ~(1 << 6);

            // Set position
            stimulusAnchor.transform.position = new Vector3(
                initialAnchorPosition.x + totalOffset,
                initialAnchorPosition.y,
                initialAnchorPosition.z
            );
        }
        else
        {
            // Both
            leftCamera.cullingMask = ~(1 << 6);
            rightCamera.cullingMask = ~(1 << 6);

            // Reset position
            stimulusAnchor.transform.position = new Vector3(
                initialAnchorPosition.x,
                initialAnchorPosition.y,
                initialAnchorPosition.z
            );
        }
    }
}
