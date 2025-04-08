using System.Collections.Generic;
using UnityEngine;

using UXF;

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

    // Calculated offset vectors
    private readonly Dictionary<string, GazeVector> _directionalOffsets = new();

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
        // Set the indicator visibility according to the show indicators flag
        _leftEyeTracker.SetIndicatorVisibility(_showIndicators);
        _rightEyeTracker.SetIndicatorVisibility(_showIndicators);
    }

    // Get the calibration data
    public Dictionary<string, List<GazeVector>> GetSetupData() => _setupData;
    public Dictionary<string, List<GazeVector>> GetValidationData() => _validationData;

    // Get the directional offsets, fixation object path and radius
    public Dictionary<string, GazeVector> GetDirectionalOffsets() => _directionalOffsets;
    public Dictionary<string, Vector2> GetFixationObjectPath() => _fixationObjectPath;
    public float GetFixationRadius() => _fixationRadius;

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
    /// Get the adjusted gaze estimate for both eyes by applying the calculated corrective vectors
    /// </summary>
    /// <returns>A `GazeVector` object containing the adjusted gaze estimates for both eyes</returns>
    public GazeVector GetAdjustedGazeEstimate()
    {
        // Get raw gaze estimates
        var l_p = _leftEyeTracker.GetGazeEstimate();
        var r_p = _rightEyeTracker.GetGazeEstimate();
        var currentGaze = new Vector2((l_p.x + r_p.x) / 2, (l_p.y + r_p.y) / 2);

        // Calculate weights for each fixation point based on inverse distance
        Dictionary<string, float> weights = new();
        float totalWeight = 0f;

        foreach (var point in _fixationObjectPath.Keys)
        {
            var targetPos = _fixationObjectPath[point] * _fixationRadius;
            var distance = Vector2.Distance(currentGaze, targetPos);

            // Use inverse distance squared for smoother falloff
            var weight = 1f / (distance * distance + 0.0001f); // Add small epsilon to avoid division by zero
            weights[point] = weight;
            totalWeight += weight;
        }

        // Normalize weights and calculate weighted average of corrections
        Vector2 leftCorrection = Vector2.zero;
        Vector2 rightCorrection = Vector2.zero;

        foreach (var point in weights.Keys)
        {
            if (_directionalOffsets.ContainsKey(point))
            {
                var normalizedWeight = weights[point] / totalWeight;
                var corrections = _directionalOffsets[point];

                leftCorrection += corrections.GetLeft() * normalizedWeight;
                rightCorrection += corrections.GetRight() * normalizedWeight;
            }
        }

        // Apply weighted corrections
        var adjustedLeft = new Vector2(l_p.x, l_p.y) + leftCorrection;
        var adjustedRight = new Vector2(r_p.x, r_p.y) + rightCorrection;

        return new GazeVector(adjustedLeft, adjustedRight);
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
        // Get the gaze estimate
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

    /// <summary>
    /// Calculate the corrective vectors for each fixation point based on collected gaze data
    /// </summary>
    public void CalculateOffsetValues()
    {
        // Clear any existing offsets
        _directionalOffsets.Clear();

        // Function to calculate mean and standard deviation for a list of vectors
        static (Vector2 mean, Vector2 stdDev) GetVectorStats(List<Vector2> vectors)
        {
            if (vectors.Count == 0) return (Vector2.zero, Vector2.zero);

            // Calculate mean
            Vector2 sum = Vector2.zero;
            foreach (var v in vectors)
            {
                sum += v;
            }
            Vector2 mean = sum / vectors.Count;

            // Calculate standard deviation
            Vector2 sqSum = Vector2.zero;
            foreach (var v in vectors)
            {
                sqSum.x += (v.x - mean.x) * (v.x - mean.x);
                sqSum.y += (v.y - mean.y) * (v.y - mean.y);
            }
            Vector2 stdDev = new(
                Mathf.Sqrt(sqSum.x / vectors.Count),
                Mathf.Sqrt(sqSum.y / vectors.Count)
            );

            return (mean, stdDev);
        }

        // Process each fixation point
        foreach (string fixationPoint in _setupData.Keys)
        {
            // Get the actual fixation point position
            Vector2 targetPos = _fixationObjectPath[fixationPoint] * _fixationRadius;

            // Collect left and right eye gaze positions
            List<Vector2> leftGazePositions = new();
            List<Vector2> rightGazePositions = new();

            foreach (var gazePair in _setupData[fixationPoint])
            {
                leftGazePositions.Add(gazePair.GetLeft());
                rightGazePositions.Add(gazePair.GetRight());
            }

            // Calculate stats for both eyes
            var (leftMean, leftStdDev) = GetVectorStats(leftGazePositions);
            var (rightMean, rightStdDev) = GetVectorStats(rightGazePositions);

            // Filter out outliers (points more than 2 standard deviations from mean)
            List<Vector2> leftFiltered = new();
            List<Vector2> rightFiltered = new();

            foreach (var pos in leftGazePositions)
            {
                if (Mathf.Abs(pos.x - leftMean.x) <= 2 * leftStdDev.x &&
                    Mathf.Abs(pos.y - leftMean.y) <= 2 * leftStdDev.y)
                {
                    leftFiltered.Add(pos);
                }
            }

            foreach (var pos in rightGazePositions)
            {
                if (Mathf.Abs(pos.x - rightMean.x) <= 2 * rightStdDev.x &&
                    Mathf.Abs(pos.y - rightMean.y) <= 2 * rightStdDev.y)
                {
                    rightFiltered.Add(pos);
                }
            }

            // Recalculate means with filtered data
            var (leftFilteredMean, _) = GetVectorStats(leftFiltered);
            var (rightFilteredMean, _) = GetVectorStats(rightFiltered);

            // Calculate corrective vectors (target position - mean gaze position)
            Vector2 leftCorrection = targetPos - leftFilteredMean;
            Vector2 rightCorrection = targetPos - rightFilteredMean;

            // Store the corrective vectors
            _directionalOffsets[fixationPoint] = new GazeVector(leftCorrection, rightCorrection);

            Debug.Log($"Calculated corrective vectors for {fixationPoint}:");
            Debug.Log($"Left eye: {leftCorrection}, Right eye: {rightCorrection}");
        }
    }
}

