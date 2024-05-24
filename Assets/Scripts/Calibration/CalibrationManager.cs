using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UXF;

// Custom namespaces
using Utilities;

namespace Calibration
{
    /// <summary>
    /// Manager for headset calibration operations. Currently handles setup, operation, and continual
    /// adjustment for eye-tracking calibration.
    /// </summary>
    public class CalibrationManager : MonoBehaviour
    {
        [Header("Required visual elements")]
        public GameObject CalibrationPrefab; // Prefab containing visual elements aiding in calibration procedure
        public GameObject StimulusAnchor;
        private GameObject calibrationPrefabInstance;

        // Left and right `EyePositionTracker` objects
        [Header("Eye trackers")]
        public EyePositionTracker LeftEyeTracker;
        public EyePositionTracker RightEyeTracker;

        // Flags for state management
        private bool isCalibrationActive = false; // 'true' when running calibration operations
        private bool isCalibrationComplete = false; // 'true' once operations complete

        // Set of points to be displayed for fixation and the "path" of the fixation object used
        // for eye-tracking calibration
        private int unitVectorIndex = 0;
        private Vector2 unitVector; // The active unit vector
        private readonly float unitDistance = 2.0f;
        private readonly Dictionary<string, Vector2> unitVectorsPath = new() {
            {"c", new Vector2(0, 0)},
            {"q_1", new Vector2(1, 1)},
            {"q_2", new Vector2(-1, 1)},
            {"q_3", new Vector2(-1, -1)},
            {"q_4", new Vector2(1, -1)},
        };
        private float updateTimer = 0.0f;
        private readonly float PathInterval = 1.5f; // Duration of each point being displayed in the path
        private GameObject FixationObject; // Object moved around the screen
        private Action CalibrationCallback; // Optional callback function executed after calibration complete

        // Data storage
        private Dictionary<string, List<GazeVector>> GazeData = new() {
            {"c", new List<GazeVector>() },
            {"q_1", new List<GazeVector>() },
            {"q_2", new List<GazeVector>() },
            {"q_3", new List<GazeVector>() },
            {"q_4", new List<GazeVector>() },
        };

        // Calculated offset vectors
        private Dictionary<string, GazeVector> DirectionalOffsets = new();
        private GazeVector globalOffset;

        /// <summary>
        /// Setup function to initialize the fixation object and movement path of object
        /// </summary>
        private void SetupCalibration()
        {
            unitVector = unitVectorsPath[unitVectorsPath.Keys.ToList()[unitVectorIndex]];

            // Create moving fixation object
            FixationObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            FixationObject.name = "calibration_fixation";
            FixationObject.transform.SetParent(StimulusAnchor.transform, false);
            FixationObject.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            FixationObject.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Sprites/Default"));
            FixationObject.GetComponent<MeshRenderer>().material.SetColor("_Color", Color.red);
            FixationObject.SetActive(false);

            // Set initial position of fixation object
            FixationObject.transform.localPosition = new Vector3(unitVector.x * unitDistance, unitVector.y * unitDistance, 0.0f);
        }

        /// <summary>
        /// Wrapper function to initialize class and prepare for calibration operations
        /// </summary>
        void Start()
        {
            SetupCalibration();
        }

        /// <summary>
        /// Public function to externally execute the calibration operations when required
        /// </summary>
        /// <param name="callback">Optional callback function to execute at calibration completion</param>
        public void RunCalibration(Action callback = null)
        {
            calibrationPrefabInstance = Instantiate(CalibrationPrefab, StimulusAnchor.transform);
            isCalibrationActive = true;
            FixationObject.SetActive(isCalibrationActive);

            // Optional callback function
            CalibrationCallback = callback;
        }

        private void EndCalibration()
        {
            isCalibrationActive = false;
            isCalibrationComplete = true;
            FixationObject.SetActive(isCalibrationActive);

            // Run calculation
            CalculateCalibrationValues();

            // Remove the prefab instance
            Destroy(calibrationPrefabInstance);

            // Run callback function if specified
            CalibrationCallback?.Invoke();
        }

        public bool GetCalibrationComplete()
        {
            return isCalibrationComplete;
        }

        public bool GetCalibrationActive()
        {
            return isCalibrationActive;
        }

        private void CalculateCalibrationValues()
        {
            // Function to examine each point and calculate average vector difference from each point
            foreach (string unitVectorDirection in GazeData.Keys)
            {
                Vector2 L_vectorSum = Vector2.zero;
                Vector2 R_vectorSum = Vector2.zero;

                foreach (GazeVector VectorPair in GazeData[unitVectorDirection])
                {
                    // Get the sum of the gaze vector and the actual position of the dot for each eye
                    Vector2 L_result = new Vector2(VectorPair.GetLeft().x, VectorPair.GetLeft().y) + (unitVectorsPath[unitVectorDirection] * unitDistance);
                    L_vectorSum += new Vector2(VectorPair.GetLeft().x, VectorPair.GetLeft().y) + (unitVectorsPath[unitVectorDirection] * unitDistance);

                    Vector2 R_result = new Vector2(VectorPair.GetRight().x, VectorPair.GetRight().y) + (unitVectorsPath[unitVectorDirection] * unitDistance);
                    R_vectorSum += new Vector2(VectorPair.GetRight().x, VectorPair.GetRight().y) + (unitVectorsPath[unitVectorDirection] * unitDistance);
                }

                L_vectorSum /= GazeData[unitVectorDirection].Count;
                R_vectorSum /= GazeData[unitVectorDirection].Count;
                DirectionalOffsets.Add(unitVectorDirection, new GazeVector(L_vectorSum, R_vectorSum));
            }

            // Calculate a global offset correction vector for each eye
            Vector2 L_averageOffsetCorrection = Vector2.zero;
            Vector2 R_averageOffsetCorrection = Vector2.zero;

            foreach (string unitVectorDirection in GazeData.Keys)
            {
                L_averageOffsetCorrection += DirectionalOffsets[unitVectorDirection].GetLeft();
                R_averageOffsetCorrection += DirectionalOffsets[unitVectorDirection].GetRight();
            }

            L_averageOffsetCorrection /= GazeData.Keys.Count;
            R_averageOffsetCorrection /= GazeData.Keys.Count;
            globalOffset = new(L_averageOffsetCorrection, R_averageOffsetCorrection);
        }

        public Dictionary<string, GazeVector> GetDirectionalOffsets()
        {
            return DirectionalOffsets;
        }

        public GazeVector GetCentralOffset()
        {
            return DirectionalOffsets["c"];
        }

        public static Dictionary<string, Tuple<float, float>> GetQuadrants()
        {
            return new() {
                {"q_1", new Tuple<float, float>(0, 89)},
                {"q_2", new Tuple<float, float>(90, 179)},
                {"q_3", new Tuple<float, float>(180, 269)},
                {"q_4", new Tuple<float, float>(270, 360)},
            };
        }

        void Update()
        {
            if (isCalibrationActive)
            {
                updateTimer += Time.deltaTime;
                if (updateTimer >= PathInterval)
                {
                    // Shift to the next position if the timer has been reached
                    unitVectorIndex += 1;
                    if (unitVectorIndex > unitVectorsPath.Count - 1)
                    {
                        unitVectorIndex = 0;
                        EndCalibration();
                    }
                    unitVector = unitVectorsPath[unitVectorsPath.Keys.ToList()[unitVectorIndex]];
                    FixationObject.transform.localPosition = new Vector3(unitVector.x * unitDistance, unitVector.y * unitDistance, 0.0f);

                    // Reset the timer
                    updateTimer = 0.0f;
                }
                else
                {
                    // Capture eye tracking data and store alongside location
                    Vector3 l_p = LeftEyeTracker.GetGazeEstimate();
                    Vector3 r_p = RightEyeTracker.GetGazeEstimate();
                    GazeData[unitVectorsPath.Keys.ToList()[unitVectorIndex]].Add(new(l_p, r_p));
                }
            }
        }
    }
}
