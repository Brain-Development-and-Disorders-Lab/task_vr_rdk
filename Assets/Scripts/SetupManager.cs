using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UXF;

// Custom namespaces
using Utilities;

/// <summary>
/// Manager for headset setup operations. Currently handles setup, operation, and eye-tracking calibration.
/// </summary>
public class SetupManager : MonoBehaviour
{
    [Header("Required visual elements")]
    [SerializeField]
    private GameObject _viewCalibrationPrefab; // Prefab containing visual elements aiding in calibration procedure
    [SerializeField]
    private GameObject _stimulusAnchor;
    private GameObject _viewCalibrationPrefabInstance;

    // Left and right `EyePositionTracker` objects
    [Header("Eye trackers")]
    [SerializeField]
    private EyePositionTracker _leftEyeTracker;
    [SerializeField]
    private EyePositionTracker _rightEyeTracker;

    // Flags for state management
    private bool _isEyeTrackingSetupActive = false; // 'true' when running calibration operations
    private bool _isEyeTrackingSetupComplete = false; // 'true' once operations complete

    // Set of points to be displayed for fixation and the "path" of the fixation object used
    // for eye-tracking setup
    private int _unitVectorIndex = 0;
    private Vector2 _unitVector; // The active unit vector
    private readonly float _unitDistance = 2.4f;
    private readonly Dictionary<string, Vector2> _unitVectorsPath = new() {
        {"c_start", new Vector2(0, 0)},
        {"q_1", new Vector2(1, 1)},
        {"q_2", new Vector2(-1, 1)},
        {"q_3", new Vector2(-1, -1)},
        {"q_4", new Vector2(1, -1)},
        {"c_end", new Vector2(0, 0)}, // Return to center
    };
    private float _updateTimer = 0.0f;
    private readonly float _pathInterval = 1.6f; // Duration of each point being displayed in the path
    private GameObject _fixationObject; // Object moved around the screen
    private Action _setupCallback; // Optional callback function executed after calibration complete

    // Data storage
    private readonly Dictionary<string, List<GazeVector>> _gazeData = new() {
        {"c_start", new List<GazeVector>() },
        {"q_1", new List<GazeVector>() },
        {"q_2", new List<GazeVector>() },
        {"q_3", new List<GazeVector>() },
        {"q_4", new List<GazeVector>() },
        {"c_end", new List<GazeVector>() },
    };

    // Calculated offset vectors
    private readonly Dictionary<string, GazeVector> _directionalOffsets = new();

    /// <summary>
    /// Wrapper function to initialize class and prepare for calibration operations
    /// </summary>
    private void Start()
    {
        _unitVector = _unitVectorsPath[_unitVectorsPath.Keys.ToList()[_unitVectorIndex]];

        // Create moving fixation object
        _fixationObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _fixationObject.name = "calibration_fixation";
        _fixationObject.transform.SetParent(_stimulusAnchor.transform, false);
        _fixationObject.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
        _fixationObject.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Sprites/Default"));
        _fixationObject.GetComponent<MeshRenderer>().material.SetColor("_Color", Color.red);
        _fixationObject.SetActive(false);

        // Set initial position of fixation object
        _fixationObject.transform.localPosition = new Vector3(_unitVector.x * _unitDistance, _unitVector.y * _unitDistance, 0.0f);

        // Setup the calibration prefab instance, initially hidden
        _viewCalibrationPrefabInstance = Instantiate(_viewCalibrationPrefab, _stimulusAnchor.transform);
        _viewCalibrationPrefabInstance.SetActive(false);
    }

    /// <summary>
    /// Public function to externally execute the calibration operations when required
    /// </summary>
    /// <param name="callback">Optional callback function to execute at calibration completion</param>
    public void RunSetup(Action callback = null)
    {
        _isEyeTrackingSetupActive = true;
        _fixationObject.SetActive(_isEyeTrackingSetupActive);

        // Optional callback function
        _setupCallback = callback;
    }

    private void EndSetup()
    {
        _isEyeTrackingSetupActive = false;
        _isEyeTrackingSetupComplete = true;
        _fixationObject.SetActive(_isEyeTrackingSetupActive);

        // Run calculation of gaze offset values
        CalculateOffsetValues();

        // Remove the prefab instance
        Destroy(_viewCalibrationPrefabInstance);

        // Run callback function if specified
        _setupCallback?.Invoke();
    }

    public bool GetCalibrationComplete() => _isEyeTrackingSetupComplete;

    public bool GetCalibrationActive() => _isEyeTrackingSetupActive;

    public void SetViewCalibrationVisibility(bool state) => _viewCalibrationPrefabInstance.SetActive(state);

    private void CalculateOffsetValues()
    {
        // Function to examine each point and calculate average vector difference from each point
        foreach (string _unitVectorDirection in _gazeData.Keys)
        {
            var L_vectorSum = Vector2.zero;
            var R_vectorSum = Vector2.zero;

            foreach (var VectorPair in _gazeData[_unitVectorDirection])
            {
                // Get the sum of the gaze vector and the actual position of the dot for each eye
                var L_result = new Vector2(VectorPair.GetLeft().x, VectorPair.GetLeft().y) + (_unitVectorsPath[_unitVectorDirection] * _unitDistance);
                L_vectorSum += new Vector2(VectorPair.GetLeft().x, VectorPair.GetLeft().y) + (_unitVectorsPath[_unitVectorDirection] * _unitDistance);

                var R_result = new Vector2(VectorPair.GetRight().x, VectorPair.GetRight().y) + (_unitVectorsPath[_unitVectorDirection] * _unitDistance);
                R_vectorSum += new Vector2(VectorPair.GetRight().x, VectorPair.GetRight().y) + (_unitVectorsPath[_unitVectorDirection] * _unitDistance);
            }

            L_vectorSum /= _gazeData[_unitVectorDirection].Count;
            R_vectorSum /= _gazeData[_unitVectorDirection].Count;
            _directionalOffsets.Add(_unitVectorDirection, new GazeVector(L_vectorSum, R_vectorSum));
        }

        // Calculate a global offset correction vector for each eye
        var L_averageOffsetCorrection = Vector2.zero;
        var R_averageOffsetCorrection = Vector2.zero;

        foreach (string _unitVectorDirection in _gazeData.Keys)
        {
            L_averageOffsetCorrection += _directionalOffsets[_unitVectorDirection].GetLeft();
            R_averageOffsetCorrection += _directionalOffsets[_unitVectorDirection].GetRight();
        }

        L_averageOffsetCorrection /= _gazeData.Keys.Count;
        R_averageOffsetCorrection /= _gazeData.Keys.Count;
    }

    public Dictionary<string, GazeVector> Get_directionalOffsets() => _directionalOffsets;

    public GazeVector GetCentralOffset() => _directionalOffsets["c_start"];

    public static Dictionary<string, Tuple<float, float>> GetQuadrants() => new() {
        {"q_1", new Tuple<float, float>(0, 89)},
        {"q_2", new Tuple<float, float>(90, 179)},
        {"q_3", new Tuple<float, float>(180, 269)},
        {"q_4", new Tuple<float, float>(270, 360)},
    };

    private void Update()
    {
        if (_isEyeTrackingSetupActive)
        {
            _updateTimer += Time.deltaTime;
            if (_updateTimer >= _pathInterval)
            {
                // Shift to the next position if the timer has been reached
                _unitVectorIndex += 1;
                if (_unitVectorIndex > _unitVectorsPath.Count - 1)
                {
                    _unitVectorIndex = 0;
                    EndSetup();
                }
                _unitVector = _unitVectorsPath[_unitVectorsPath.Keys.ToList()[_unitVectorIndex]];
                _fixationObject.transform.localPosition = new Vector3(_unitVector.x * _unitDistance, _unitVector.y * _unitDistance, 0.0f);

                // Reset the timer
                _updateTimer = 0.0f;
            }
            else
            {
                // Capture eye tracking data and store alongside location
                var l_p = _leftEyeTracker.GetGazeEstimate();
                var r_p = _rightEyeTracker.GetGazeEstimate();
                _gazeData[_unitVectorsPath.Keys.ToList()[_unitVectorIndex]].Add(new(l_p, r_p));
            }
        }
    }
}
