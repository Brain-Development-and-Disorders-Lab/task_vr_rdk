using UnityEngine;

/// <summary>
/// Class to manage visual field switching, in-game camera distances, and lateralized presentation.
/// </summary>
public class CameraManager : MonoBehaviour
{
    // Left and Right eye cameras
    [Header("Camera components")]
    [SerializeField]
    private OVRCameraRig _cameraRig;
    [SerializeField]
    private Camera _leftCamera;
    [SerializeField]
    private Camera _rightCamera;

    // Anchor for stimulus, this is critical for true dichoptic presentation
    [Header("Anchors")]
    [SerializeField]
    private GameObject _stimulusAnchor;
    [SerializeField]
    private GameObject _uiAnchor;
    [SerializeField]
    private GameObject _fixationAnchor;

    [Header("Visual presentation parameters")]
    [Tooltip("Enable a layer-based mask to provide a full eye-patch effect.")]
    [SerializeField]
    private bool _useCullingMask = false; // Parameter to use a culling mask for full "eye-patch" effect

    [Tooltip("Distance (degrees) to offset the outer edge of the stimulus from the central fixation point")]
    [SerializeField]
    private float _offsetAngle = 3.0f; // Visual offset angle (degrees) to place stimulus in single hemifield

    [Tooltip("Distance (world units) to translate anchors vertically")]
    [SerializeField]
    private float _verticalOffset = -2.0f;

    private float _stimulusWidth = 0.0f; // Additional world-units to offset the stimulus
    private float _totalOffset = 0.0f;
    private float _stimulusAnchorDistance;

    // Camera presentation modes
    public enum EVisualField
    {
        Left,
        Right,
        Both
    };

    // Default visual field (active camera)
    private EVisualField _activeField = EVisualField.Both;

    private void Start()
    {
        // Check if OVRCameraRig has been specified, required for head tracking
        if (_cameraRig)
        {
            // Set the anchor object for stimuli and UI as a child of the _cameraRig
            _stimulusAnchor.transform.SetParent(_cameraRig.centerEyeAnchor.transform, false);
            _uiAnchor.transform.SetParent(_cameraRig.centerEyeAnchor.transform, false);
            _fixationAnchor.transform.SetParent(_cameraRig.centerEyeAnchor.transform, false);

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
        float ipd = Mathf.Abs(_leftCamera.transform.position.x - _rightCamera.transform.position.x);

        // Step 2: Calculate the distance (d) of the view position to the stimulus, world units
        _stimulusAnchorDistance = Mathf.Abs(_leftCamera.transform.position.z - _stimulusAnchor.transform.position.z);

        // Step 3: Calculate theta (angle between static eye position vector and future offset vector), radians
        float theta = _offsetAngle * Mathf.PI / 180;

        // Step 4: Calculate baseline offset distance from static eye position to offset position, world units
        float offsetDistance = _stimulusAnchorDistance * Mathf.Tan(theta);

        // Step 5: Calculate total offset value
        _totalOffset = offsetDistance + (_stimulusWidth / 2.0f) + (ipd / 2.0f);
        Debug.Log("Calculated CameraManager offset distance: " + offsetDistance);
    }

    /// <summary>
    /// Set the active visual field
    /// </summary>
    /// <param name="field"></param>
    public void SetActiveField(EVisualField field, bool lateralized = true)
    {
        // Update the stored field
        _activeField = field;

        // Update the vertical offset of the fixation cross
        _fixationAnchor.transform.localPosition = new Vector3(0.0f, 0.0f + _verticalOffset, _stimulusAnchorDistance);

        // Apply local offset adjustments for lateralized presentation and culling mask for eye-patch effect
        if (field == EVisualField.Left)
        {
            // Left visual presentation
            if (lateralized)
            {
                _stimulusAnchor.transform.localPosition = new Vector3(0.0f - _totalOffset, 0.0f + _verticalOffset, _stimulusAnchorDistance);
            }

            if (_useCullingMask)
            {
                _leftCamera.cullingMask = ~(1 << 6);
                _rightCamera.cullingMask = 1 << 6;
            }
        }
        else if (field == EVisualField.Right)
        {
            // Right visual presentation
            if (lateralized)
            {
                _stimulusAnchor.transform.localPosition = new Vector3(0.0f + _totalOffset, 0.0f + _verticalOffset, _stimulusAnchorDistance);
            }

            if (_useCullingMask)
            {
                _leftCamera.cullingMask = 1 << 6;
                _rightCamera.cullingMask = ~(1 << 6);
            }
        }
        else
        {
            // Central visual presentation
            _stimulusAnchor.transform.localPosition = new Vector3(0.0f, 0.0f + _verticalOffset, _stimulusAnchorDistance);
            if (_useCullingMask)
            {
                _leftCamera.cullingMask = ~(1 << 6);
                _rightCamera.cullingMask = ~(1 << 6);
            }
        }
    }

    /// <summary>
    /// Set the width of the stimulus, measured in world units
    /// </summary>
    /// <param name="width">Stimulus width, measured in world units</param>
    public void SetStimulusWidth(float width)
    {
        _stimulusWidth = width;
        CalculateOffset(); // We need to re-calculate the offset values if this has been updated
    }

    /// <summary>
    /// Get the current active visual field
    /// </summary>
    /// <returns>Active visual field, member of `EVisualField` enum</returns>
    public EVisualField GetActiveField() => _activeField;

    /// <summary>
    /// Get the offset distance, measured in world units
    /// </summary>
    /// <returns>`float` representing the offset distance</returns>
    public float GetTotalOffset() => _totalOffset;
}
