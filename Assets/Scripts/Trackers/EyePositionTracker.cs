using UnityEngine;
using System;
using System.Collections.Generic;

// Custom namespaces
using Calibration;
using Utilities;

namespace UXF
{
    /// <summary>
    /// Attach this component to a GameObject and assign it in the trackedObjects field in an ExperimentSession to automatically record position/rotation of the object at each frame.
    /// </summary>
    [RequireComponent(typeof(OVREyeGaze))]
    public class EyePositionTracker : Tracker
    {
        // Default gaze distance (should be mapped to the "surface" of the furthest 2D stimulus)
        private float gazeDistance = 10.0f; // World units
        public GameObject GazeTargetSurface; // Typically mapped to StimulusAnchor `GameObject`
        public GameObject GazeSource; // Typically mapped to CenterEyeAnchor under the `OVRCameraRig` prefab

        // Fields to enable and manage the gaze indicators
        [SerializeField]
        private bool showIndicator = false;
        private GameObject indicator;
        private GameObject indicatorCalibrated;

        // OVR classes
        private OVREyeGaze eyeGazeComponent;
        private OVRFaceExpressions faceComponent;

        // Calibration class
        private CalibrationManager calibrationManager;

        // Data variables
        public override string MeasurementDescriptor => "gaze";
        public override IEnumerable<string> CustomHeader => new string[] { "eye", "pos_x", "pos_y", "pos_z", "rot_x", "rot_y", "rot_z", "blink" };
        private string TrackedEye = "left"; // Specify if the left or right eye
        private Vector3 GazeEstimate;

        public void Start()
        {
            // Get calibration component
            calibrationManager = FindObjectOfType<CalibrationManager>();

            // Get OVR components
            eyeGazeComponent = GetComponentInParent<OVREyeGaze>();
            faceComponent = FindObjectOfType<OVRFaceExpressions>();

            // Setup gaze distance
            if (GazeTargetSurface != null && GazeSource != null)
            {
                gazeDistance = GazeTargetSurface.transform.position.z - GazeSource.transform.position.z;
            }

            // Eye gaze setup
            if (eyeGazeComponent)
            {
                TrackedEye = eyeGazeComponent.name.StartsWith("Left") ? "left" : "right";
            }
            else
            {
                Debug.LogWarning("Missing OVREyeGaze component. Eye tracking indicators will not be shown.");
            }

            // Blink tracking setup
            if (faceComponent)
            {
                Debug.Log("Eye tracking setup to detect blinks.");
            }
            else
            {
                Debug.LogWarning("Missing OVRFaceExpressions component. Eye tracking will not detect blinks.");
            }

            // Show gaze indicator if enabled
            if (showIndicator == true && eyeGazeComponent)
            {
                // Create a new indicator object
                indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                indicator.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

                // Indicator to show the position of "corrected" gaze data
                indicatorCalibrated = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                indicatorCalibrated.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                indicatorCalibrated.GetComponent<Renderer>().material = new Material(Shader.Find("Sprites/Default"));
                indicatorCalibrated.GetComponent<Renderer>().material.SetColor("_Color", Color.green);
                indicatorCalibrated.SetActive(false);

                // Assign the colour based on left or right eye
                var indicatorRenderer = indicator.GetComponent<Renderer>();
                indicatorRenderer.material = new Material(Shader.Find("Sprites/Default"));
                if (TrackedEye == "left")
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
            return GazeEstimate;
        }

        /// <summary>
        /// Determine which visual quadrant the gaze is placed in, and dynamically apply the adjustment vector to the
        /// raw gaze vector to correct for gaze calibration
        /// </summary>
        /// <param name="currentGaze"></param>
        private void ApplyDynamicGazeCorrection(Vector3 currentGaze)
        {
            // Calculate the vector angle between the gaze and an origin unit vector (in degrees)
            float vectorAngle = Vector3.Angle(new Vector3(1.0f, 0.0f), currentGaze);

            // Determine the quadrant the vector is located in
            string quadrant = "c";
            Dictionary<string, Tuple<float, float>> quadrants = CalibrationManager.GetQuadrants();
            foreach (string q in quadrants.Keys)
            {
                if (vectorAngle >= quadrants[q].Item1 && vectorAngle < quadrants[q].Item2)
                {
                    quadrant = q;
                    break;
                }
            }

            // Get the offset vector for the specific eye
            Vector3 offsetVector;
            if (TrackedEye == "left")
            {
                offsetVector = (Vector3)calibrationManager.GetDirectionalOffsets()[quadrant].GetLeft();
            }
            else
            {
                offsetVector = (Vector3)calibrationManager.GetDirectionalOffsets()[quadrant].GetRight();
            }
            indicatorCalibrated.transform.position = currentGaze - offsetVector;
        }

        /// <summary>
        /// Returns current position and rotation values of the eye
        /// </summary>
        /// <returns></returns>
        protected override UXFDataRow GetCurrentValues()
        {
            // Eye position and rotation
            Vector3 p = transform.position;
            Vector3 r = transform.eulerAngles;
            GazeEstimate = p + transform.forward * gazeDistance;

            // If using indicators, update the position
            if (showIndicator == true && indicator != null)
            {
                // Apply the raw position to the primary indicator
                indicator.transform.position = GetGazeEstimate();

                // If a calibration procedure has taken place, show the adjusted gaze estimate
                if (calibrationManager && calibrationManager.GetCalibrationComplete() == true)
                {
                    // Note: Removed due to inaccuracy
                    // indicatorCalibrated.SetActive(true);
                    // ApplyDynamicGazeCorrection(GetGazeEstimate());
                }
            }

            float LBlinkWeight = -1.0f;
            float RBlinkWeight = -1.0f;
            if (faceComponent)
            {
                // Testing collection of blink weights
                faceComponent.TryGetFaceExpressionWeight(OVRFaceExpressions.FaceExpression.EyesClosedL, out LBlinkWeight);
                faceComponent.TryGetFaceExpressionWeight(OVRFaceExpressions.FaceExpression.EyesClosedR, out RBlinkWeight);
            }

            // Return eye, position, rotation (x, y, z), and blink estimate as an array
            var values = new UXFDataRow()
            {
                ("eye", TrackedEye),
                ("pos_x", p.x),
                ("pos_y", p.y),
                ("pos_z", p.z),
                ("rot_x", r.x),
                ("rot_y", r.y),
                ("rot_z", r.z),
                ("blink", TrackedEye == "left" ? LBlinkWeight : RBlinkWeight)
            };

            return values;
        }
    }
}
