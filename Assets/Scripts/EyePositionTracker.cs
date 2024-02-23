using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace UXF
{
    /// <summary>
    /// Attach this component to a GameObject and assign it in the trackedObjects field in an ExperimentSession to automatically record position/rotation of the object at each frame.
    /// </summary>
    public class EyePositionTracker : Tracker
    {
        // Default gaze distance (should be mapped to the "surface" of the furthest 2D stimulus)
        [SerializeField]
        private float gazeDistance = 10.0f;

        // Fields to enable and manage the gaze indicators
        [SerializeField]
        private bool showIndicator = false;
        private GameObject indicator;

        // OVR classes
        private OVREyeGaze eyeGazeComponent;

        public override string MeasurementDescriptor => "gaze";
        public override IEnumerable<string> CustomHeader => new string[] { "pos_x", "pos_y", "pos_z", "rot_x", "rot_y", "rot_z" };

        public void Start()
        {
            if (showIndicator == true)
            {
                // Create a new indicator object
                indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                indicator.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

                // Assign the colour based on left or right eye
                eyeGazeComponent = GetComponentInParent<OVREyeGaze>(); 
                if (eyeGazeComponent != null)
                {
                    var indicatorRenderer = indicator.GetComponent<Renderer>();
                    indicatorRenderer.material = new Material(Shader.Find("Sprites/Default"));
                    if (eyeGazeComponent.name.StartsWith("Left"))
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
                else
                {
                    Debug.LogError("Missing OVREyeGaze component. Eye tracking will not work.");
                }
            }
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

            // If using indicators, update the position
            if (showIndicator == true && indicator != null)
            {
                Vector3 indicatorEstimate = p + transform.forward * gazeDistance;
                indicator.transform.position = indicatorEstimate;
            }

            // Return position, rotation (x, y, z) as an array
            var values = new UXFDataRow()
            {
                ("pos_x", p.x),
                ("pos_y", p.y),
                ("pos_z", p.z),
                ("rot_x", r.x),
                ("rot_y", r.y),
                ("rot_z", r.z)
            };

            return values;
        }
    }
}