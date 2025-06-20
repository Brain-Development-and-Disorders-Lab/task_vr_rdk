using System.Collections.Generic;
using UnityEngine;
using System.Collections;

using UXF;

// Custom namespaces
using Utilities;

public class GazeManager : MonoBehaviour
{
    [SerializeField]
    private GameObject _gazeTargetSurface; // Typically mapped to StimulusAnchor `GameObject`
    private float _gazeDistance = 10.0f; // World units

    [SerializeField]
    private GameObject _gazeSource; // Typically mapped to CenterEyeAnchor under the `OVRCameraRig` prefab

    [SerializeField]
    private GazeTracker _leftEyeTracker;
    [SerializeField]
    private GazeTracker _rightEyeTracker;

    [Header("Fixation parameters")]
    [SerializeField]
    private bool _requireFixation = false;
    [SerializeField]
    private bool _showIndicators = false;

    // Eye tracker objects
    public GazeTracker GetLeftEyeTracker() => _leftEyeTracker;
    public GazeTracker GetRightEyeTracker() => _rightEyeTracker;

    // Fixation object radius and path
    private readonly float _fixationRadius = 2.4f;
    private readonly Dictionary<string, Vector2> _fixationObjectPath = new() {
        {"c_start", new Vector2(0, 0)},
        {"q_1", new Vector2(1, 1)},
        {"q_2", new Vector2(-1, 1)},
        {"q_3", new Vector2(-1, -1)},
        {"q_4", new Vector2(1, -1)},
        {"c_end", new Vector2(0, 0)}, // Return to center
    };

    // Fixation threshold
    [SerializeField]
    private float _defaultThreshold = 0.70f;
    private float _activeThreshold = 0.70f;

    // Calibration data storage
    private readonly Dictionary<string, List<GazeVector>> _setupData = new() {
        {"c_start", new List<GazeVector>() },
        {"q_1", new List<GazeVector>() },
        {"q_2", new List<GazeVector>() },
        {"q_3", new List<GazeVector>() },
        {"q_4", new List<GazeVector>() },
        {"c_end", new List<GazeVector>() },
    };
    private readonly Dictionary<string, List<GazeVector>> _validationData = new() {
        {"c_start", new List<GazeVector>() },
        {"q_1", new List<GazeVector>() },
        {"q_2", new List<GazeVector>() },
        {"q_3", new List<GazeVector>() },
        {"q_4", new List<GazeVector>() },
        {"c_end", new List<GazeVector>() },
    };

    private void Start()
    {
        // Set the default active threshold
        SetActiveThreshold(_defaultThreshold);

        if (_gazeTargetSurface != null && _gazeSource != null)
        {
            _gazeDistance = Vector3.Distance(_gazeSource.transform.position, _gazeTargetSurface.transform.position);
        }

        // Set the gaze distance for the left and right eye trackers
        _leftEyeTracker.SetGazeDistance(_gazeDistance);
        _rightEyeTracker.SetGazeDistance(_gazeDistance);

        // Set the indicator visibility according to the show indicators flag
        _leftEyeTracker.SetIndicatorVisibility(_showIndicators);
        _rightEyeTracker.SetIndicatorVisibility(_showIndicators);
    }

    public GameObject GetGazeSource() => _gazeSource;
    public float GetGazeDistance() => _gazeDistance;

    /// <summary>
    /// Get the active threshold for fixation
    /// </summary>
    /// <returns></returns>
    public float GetActiveThreshold() => _activeThreshold;

    /// <summary>
    /// Set the active threshold for fixation
    /// </summary>
    /// <param name="threshold">A value expressing the world units, the radius for fixation</param>
    public void SetActiveThreshold(float threshold) => _activeThreshold = threshold;

    // Get the calibration data
    public Dictionary<string, List<GazeVector>> GetSetupData() => _setupData;
    public Dictionary<string, List<GazeVector>> GetValidationData() => _validationData;

    // Get the directional offsets, fixation object path and radius
    public Dictionary<string, Vector2> GetFixationObjectPath() => _fixationObjectPath;
    public float GetFixationRadius() => _fixationRadius;

    /// <summary>
    /// Get the adjusted gaze estimate for both eyes by applying the calculated corrective vectors
    /// </summary>
    /// <returns>A `GazeVector` object containing the adjusted gaze estimates for both eyes</returns>
    public GazeVector GetGazeEstimate()
    {
        // Get raw gaze estimates
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
    /// <returns>True if the gaze is fixated on the point, false otherwise</returns>
    public bool IsFixatedStatic(GameObject fixationTarget)
    {
        // Get the gaze estimate
        var _gazeEstimate = GetGazeEstimate();

        // Get gaze estimates and current world position
        var _leftGaze = _gazeEstimate.GetLeft();
        var _rightGaze = _gazeEstimate.GetRight();
        var _worldPosition = fixationTarget.transform.position;

        // If the gaze is directed in fixation, return true
        return (Mathf.Abs(_leftGaze.x - _worldPosition.x) <= _activeThreshold && Mathf.Abs(_leftGaze.y - _worldPosition.y) <= _activeThreshold) || (Mathf.Abs(_rightGaze.x - _worldPosition.x) <= _activeThreshold && Mathf.Abs(_rightGaze.y - _worldPosition.y) <= _activeThreshold);
    }

    /// <summary>
    /// Check if the gaze is fixated on a static point for a duration
    /// </summary>
    /// <param name="fixationPoint">The point to check if the gaze is fixated on</param>
    /// <param name="duration">The duration for the gaze to be considered fixated</param>
    /// <returns>True if the gaze is fixated on the point for the duration, false otherwise</returns>
    public IEnumerator IsFixatedDuration(GameObject fixationTarget, float duration)
    {
        // Initialize variables
        float _elapsedTime = 0.0f;
        bool _isFixated = false;
        float _fixationStartTime = 0.0f;

        while (_elapsedTime < duration)
        {
            // Check if the gaze is fixated on the point
            if (IsFixatedStatic(fixationTarget))
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

            // Yield to allow Unity to update the frame
            yield return null;
        }
    }

    private void Update()
    {
        // Update gaze distance values
        _gazeDistance = Vector3.Distance(_gazeSource.transform.position, _gazeTargetSurface.transform.position);
        _leftEyeTracker.SetGazeDistance(_gazeDistance);
        _rightEyeTracker.SetGazeDistance(_gazeDistance);

        if (_showIndicators)
        {
            // Get the gaze estimate and update the indicator positions
            var l_p = GetGazeEstimate().GetLeft();
            var r_p = GetGazeEstimate().GetRight();

            // Use the gaze estimates directly as they are already in world coordinates
            _leftEyeTracker.SetIndicatorPosition(l_p);
            _rightEyeTracker.SetIndicatorPosition(r_p);
        }
    }
}

