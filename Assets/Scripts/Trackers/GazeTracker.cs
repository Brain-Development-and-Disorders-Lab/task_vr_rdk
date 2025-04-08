using UnityEngine;
using System.Collections.Generic;

namespace UXF
{
    /// <summary>
    /// Attach this component to a GameObject and assign it in the trackedObjects field in an ExperimentSession to automatically record position/rotation of the object at each frame.
    /// </summary>
    [RequireComponent(typeof(OVREyeGaze))]
    public class GazeTracker : Tracker
    {
        // Default gaze distance (should be mapped to the "surface" of the furthest 2D stimulus)
        private float _gazeDistance = 10.0f; // World units
        [SerializeField]
        private GameObject _gazeTargetSurface; // Typically mapped to StimulusAnchor `GameObject`
        [SerializeField]
        private GameObject _gazeSource; // Typically mapped to CenterEyeAnchor under the `OVRCameraRig` prefab

        // Fields to enable and manage the gaze indicators
        private bool _showIndicator = false;
        private GameObject _indicator;

        // OVR classes
        private OVREyeGaze _eyeGazeComponent;
        private OVRFaceExpressions _faceComponent;

        // Data variables
        public override string MeasurementDescriptor => "gaze";
        public override IEnumerable<string> CustomHeader => new string[] { "eye", "pos_x", "pos_y", "pos_z", "rot_x", "rot_y", "rot_z", "blink" };
        private string _trackedEye = "left"; // Specify if the left or right eye
        private Vector3 _gazeEstimate;

        public void Start()
        {
            // Get OVR components
            _eyeGazeComponent = GetComponentInParent<OVREyeGaze>();
            _faceComponent = FindObjectOfType<OVRFaceExpressions>();

            // Setup gaze distance
            if (_gazeTargetSurface != null && _gazeSource != null)
            {
                _gazeDistance = _gazeTargetSurface.transform.position.z - _gazeSource.transform.position.z;
            }

            // Eye gaze setup
            if (_eyeGazeComponent)
            {
                _trackedEye = _eyeGazeComponent.name.StartsWith("Left") ? "left" : "right";
            }
            else
            {
                Debug.LogWarning("Missing OVREyeGaze component. Eye tracking indicators will not be shown.");
            }

            // Blink tracking setup
            if (_faceComponent)
            {
                Debug.Log("Eye tracking setup to detect blinks.");
            }
            else
            {
                Debug.LogWarning("Missing OVRFaceExpressions component. Eye tracking will not detect blinks.");
            }

            // Show gaze indicator if enabled
            if (_showIndicator && _eyeGazeComponent)
            {
                // Create a new indicator object
                _indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _indicator.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

                // Assign the colour based on left or right eye
                var indicatorRenderer = _indicator.GetComponent<Renderer>();
                indicatorRenderer.material = new Material(Shader.Find("Sprites/Default"));
                if (_trackedEye == "left")
                {
                    // Left eye is red
                    indicatorRenderer.material.SetColor("_Color", Color.red);
                }
                else
                {
                    // Right eye is blue
                    indicatorRenderer.material.SetColor("_Color", Color.blue);
                }
            }
        }

        /// <summary>
        /// Utility function to access the realtime gaze estimate from other classes
        /// </summary>
        /// <returns>Gaze estimate as Vector3</returns>
        public Vector3 GetGazeEstimate()
        {
#if UNITY_EDITOR
            // Attempt to create an estimate from the mouse position
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                var position = Input.mousePosition;
                position.z = _gazeDistance;
                _gazeEstimate = mainCamera.ScreenToWorldPoint(position);
            }
#endif
            return _gazeEstimate;
        }

        /// <summary>
        /// Set the visibility of the gaze indicator
        /// </summary>
        /// <param name="state">Whether to show the indicator</param>
        public void SetIndicatorVisibility(bool state) => _showIndicator = state;

        /// <summary>
        /// Get the visibility of the gaze indicator
        /// </summary>
        /// <returns>Whether the indicator is visible</returns>
        public bool GetIndicatorVisibility() => _showIndicator;

        /// <summary>
        /// Returns current position and rotation values of the eye
        /// </summary>
        /// <returns></returns>
        protected override UXFDataRow GetCurrentValues()
        {
            // Eye position and rotation
            var p = transform.position;
            var r = transform.eulerAngles;
            _gazeEstimate = (p + transform.forward) * _gazeDistance;

            // If using indicators, update the position
            if (_showIndicator && _indicator != null)
            {
                // Apply the raw position to the primary _indicator
                _indicator.transform.position = GetGazeEstimate();
            }

            float LBlinkWeight = -1.0f;
            float RBlinkWeight = -1.0f;
            if (_faceComponent)
            {
                // Testing collection of blink weights
                _faceComponent.TryGetFaceExpressionWeight(OVRFaceExpressions.FaceExpression.EyesClosedL, out LBlinkWeight);
                _faceComponent.TryGetFaceExpressionWeight(OVRFaceExpressions.FaceExpression.EyesClosedR, out RBlinkWeight);
            }

            // Return eye, position, rotation (x, y, z), and blink estimate as an array
            UXFDataRow values = new()
            {
                ("eye", _trackedEye),
                ("pos_x", _gazeEstimate.x),
                ("pos_y", _gazeEstimate.y),
                ("pos_z", _gazeEstimate.z),
                ("rot_x", r.x),
                ("rot_y", r.y),
                ("rot_z", r.z),
                ("blink", _trackedEye == "left" ? LBlinkWeight : RBlinkWeight)
            };

            return values;
        }
    }
}
