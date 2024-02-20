using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace UXF
{
    /// <summary>
    /// Attach this component to a gameobject and assign it in the trackedObjects field in an ExperimentSession to automatically record position/rotation of the object at each frame.
    /// </summary>
    public class EyePositionTracker : Tracker
    {
        // Default gaze distance (should be mapped to the "surface" of the 2D stimulus)
        [SerializeField]
        private float defaultGazeDistance = 10.0f;

        public override string MeasurementDescriptor => "gaze";
        public override IEnumerable<string> CustomHeader => new string[] { "pos_x", "pos_y", "pos_z", "rot_x", "rot_y", "rot_z" };

        /// <summary>
        /// Returns current position and rotation values of the eye
        /// </summary>
        /// <returns></returns>
        protected override UXFDataRow GetCurrentValues()
        {
            // Get the current Transform component
            Transform activeEye = GetComponent<Transform>();

            // Eye position and rotation
            Vector3 p = activeEye.position;
            Vector3 r = activeEye.eulerAngles;

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