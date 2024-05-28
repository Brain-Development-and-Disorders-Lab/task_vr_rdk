using UnityEngine;
using System.Collections.Generic;

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

        // OVR classes
        private OVREyeGaze eyeGazeComponent;
        private OVRFaceExpressions faceComponent;

        // Data variables
        public override string MeasurementDescriptor => "gaze";
        public override IEnumerable<string> CustomHeader => new string[] { "eye", "pos_x", "pos_y", "pos_z", "rot_x", "rot_y", "rot_z", "blink" };
        private string TrackedEye = "left"; // Specify if the left or right eye
        private Vector3 GazeEstimate;

        public void Start()
        {
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
