using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

using UXF;
using MathNet.Numerics.Statistics;

// Custom namespaces
using Utilities;

public class GazeManager : MonoBehaviour
{
    [SerializeField]
    private GazeTracker _leftEyeTracker;
    [SerializeField]
    private GazeTracker _rightEyeTracker;

    [SerializeField]
    private bool _requireFixation = false;

    public GazeTracker GetLeftEyeTracker() => _leftEyeTracker;
    public GazeTracker GetRightEyeTracker() => _rightEyeTracker;

    /// <summary>
    /// Get the raw gaze estimate for both eyes
    /// </summary>
    /// <returns>A `GazeVector` object containing the raw gaze estimates for both eyes</returns>
    public GazeVector GetRawGazeEstimate()
    {
        var l_p = _leftEyeTracker.GetGazeEstimate();
        var r_p = _rightEyeTracker.GetGazeEstimate();

        return new GazeVector(l_p, r_p);
    }

    /// <summary>
    /// Get the adjusted gaze estimate for both eyes
    /// </summary>
    /// <returns>A `GazeVector` object containing the adjusted gaze estimates for both eyes</returns>
    public GazeVector GetAdjustedGazeEstimate()
    {
        var l_p = _leftEyeTracker.GetGazeEstimate();
        var r_p = _rightEyeTracker.GetGazeEstimate();

        return new GazeVector(l_p, r_p);
    }

    /// <summary>
    /// Get the require fixation flag
    /// </summary>
    /// <returns>True if the gaze manager requires fixation, false otherwise</returns>
    public bool GetRequireFixation() => _requireFixation;

    /// <summary>
    /// Set the require fixation flag
    /// </summary>
    public void SetRequireFixation(bool state)
    {
        _requireFixation = state;

        // Logging output
        if (state)
        {
            Debug.Log("Fixation required: Enabled");
        }
        else
        {
            Debug.Log("Fixation required: Disabled");
        }
    }

    /// <summary>
    /// Check if the gaze is fixated on a static point from a single frame
    /// </summary>
    /// <param name="fixationPoint">The point to check if the gaze is fixated on</param>
    /// <param name="threshold">The threshold for the gaze to be considered fixated</param>
    /// <param name="useAdjustedGaze">Whether to use the adjusted gaze estimate</param>
    /// <returns>True if the gaze is fixated on the point, false otherwise</returns>
    public bool IsFixatedStatic(Vector2 fixationPoint, float threshold = 0.0f, bool useAdjustedGaze = false)
    {
        var _gazeEstimate = useAdjustedGaze ? GetAdjustedGazeEstimate() : GetRawGazeEstimate();

        // Get gaze estimates and the current world position
        var _leftGaze = _gazeEstimate.GetLeft();
        var _rightGaze = _gazeEstimate.GetRight();
        var _worldPosition = fixationPoint;

        // If the gaze is directed in fixation, increment the counter to signify a measurement
        return (Mathf.Abs(_leftGaze.x - _worldPosition.x) <= threshold && Mathf.Abs(_leftGaze.y - _worldPosition.y) <= threshold) || (Mathf.Abs(_rightGaze.x - _worldPosition.x) <= threshold && Mathf.Abs(_rightGaze.y - _worldPosition.y) <= threshold);
    }

    /// <summary>
    /// Check if the gaze is fixated on a static point for a duration
    /// </summary>
    /// <param name="fixationPoint">The point to check if the gaze is fixated on</param>
    /// <param name="duration">The duration for the gaze to be considered fixated</param>
    /// <param name="threshold">The threshold for the gaze to be considered fixated</param>
    /// <param name="useAdjustedGaze">Whether to use the adjusted gaze estimate</param>
    /// <returns>True if the gaze is fixated on the point for the duration, false otherwise</returns>
    public bool IsFixatedDuration(Vector2 fixationPoint, float duration, float threshold = 0.0f, bool useAdjustedGaze = false)
    {
        // Initialize variables
        float _elapsedTime = 0.0f;
        bool _isFixated = false;
        float _fixationStartTime = 0.0f;

        while (_elapsedTime < duration)
        {
            // Check if the gaze is fixated on the point
            if (IsFixatedStatic(fixationPoint, threshold, useAdjustedGaze))
            {
                // If the gaze is fixated, set the start time and the fixated flag
                if (!_isFixated)
                {
                    // Set the start time and the fixated flag
                    _fixationStartTime = Time.time;
                    _isFixated = true;
                }
                // Update the elapsed time
                _elapsedTime = Time.time - _fixationStartTime;
            }
            else
            {
                // If the gaze is not fixated, reset the fixated flag and the elapsed time
                _isFixated = false;
                _elapsedTime = 0.0f;
            }
        }
        return true;
    }
}
