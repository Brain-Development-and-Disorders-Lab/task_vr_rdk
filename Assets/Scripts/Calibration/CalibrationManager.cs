using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UXF;

namespace Calibration
{
    public class CalibrationManager : MonoBehaviour
    {
        private bool CalibrationActive = false;
        private bool CalibrationComplete = false;

        // Set of points to be displayed for fixation and the "path" of the fixation object
        private int UnitVectorIndex = 0;
        private Vector2 UnitVector; // The active unit vector
        private readonly float UnitDistance = 2.0f;
        private readonly Dictionary<string, Vector2> UnitVectorsPath = new() {
            {"c", new Vector2(0, 0)},
            {"q_1", new Vector2(1, 1)},
            {"q_2", new Vector2(-1, 1)},
            {"q_3", new Vector2(-1, -1)},
            {"q_4", new Vector2(1, -1)},
        };
        private float UpdateTimer = 0.0f;
        private readonly float PathInterval = 1.5f; // Duration of each point being displayed in the path
        private GameObject FixationObject;
        private Action CalibrationCallback;

        private LoggerManager Logger;

        // Left and right EyeGazeData objects
        [SerializeField]
        private EyePositionTracker leftEyeTracker;
        [SerializeField]
        private EyePositionTracker rightEyeTracker;

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
        private GazeVector GlobalOffset;

        [SerializeField]
        public GameObject StimulusAnchor;

        private void SetupCalibration()
        {
            UnitVector = UnitVectorsPath[UnitVectorsPath.Keys.ToList()[UnitVectorIndex]];

            // Create moving sphere object
            FixationObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            FixationObject.name = "calibration_fixation";
            FixationObject.transform.SetParent(StimulusAnchor.transform, false);
            FixationObject.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            FixationObject.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Sprites/Default"));
            FixationObject.GetComponent<MeshRenderer>().material.SetColor("_Color", Color.red);
            FixationObject.SetActive(false);

            // Set initial position
            FixationObject.transform.localPosition = new Vector3(UnitVector.x * UnitDistance, UnitVector.y * UnitDistance, 0.0f);
        }

        void Start()
        {
            Logger = FindAnyObjectByType<LoggerManager>();
            SetupCalibration();
        }

        public void RunCalibration(Action callback = null)
        {
            CalibrationActive = true;
            FixationObject.SetActive(CalibrationActive);

            // Optional callback function
            CalibrationCallback = callback;
        }

        private void EndCalibration()
        {
            CalibrationActive = false;
            CalibrationComplete = true;
            FixationObject.SetActive(CalibrationActive);

            // Run calculation
            CalculateCalibrationValues();

            // Run callback function if specified
            CalibrationCallback?.Invoke();
        }

        public bool CalibrationStatus()
        {
            return CalibrationComplete;
        }

        private void CalculateCalibrationValues()
        {
            // Function to examine each point and calculate average vector difference from each point
            foreach (string UnitVectorDirection in GazeData.Keys)
            {
                Vector2 L_vectorSum = Vector2.zero;
                Vector2 R_vectorSum = Vector2.zero;

                foreach (GazeVector VectorPair in GazeData[UnitVectorDirection])
                {
                    // Get the sum of the gaze vector and the actual position of the dot for each eye
                    Vector2 L_result = new Vector2(VectorPair.GetLeft().x, VectorPair.GetLeft().y) + (UnitVectorsPath[UnitVectorDirection] * UnitDistance);
                    L_vectorSum += new Vector2(VectorPair.GetLeft().x, VectorPair.GetLeft().y) + (UnitVectorsPath[UnitVectorDirection] * UnitDistance);

                    Vector2 R_result = new Vector2(VectorPair.GetRight().x, VectorPair.GetRight().y) + (UnitVectorsPath[UnitVectorDirection] * UnitDistance);
                    R_vectorSum += new Vector2(VectorPair.GetRight().x, VectorPair.GetRight().y) + (UnitVectorsPath[UnitVectorDirection] * UnitDistance);
                }

                L_vectorSum /= GazeData[UnitVectorDirection].Count;
                R_vectorSum /= GazeData[UnitVectorDirection].Count;
                DirectionalOffsets.Add(UnitVectorDirection, new GazeVector(L_vectorSum, R_vectorSum));
            }

            // Calculate a global offset correction vector for each eye
            Vector2 L_averageOffsetCorrection = Vector2.zero;
            Vector2 R_averageOffsetCorrection = Vector2.zero;

            foreach (string UnitVectorDirection in GazeData.Keys)
            {
                L_averageOffsetCorrection += DirectionalOffsets[UnitVectorDirection].GetLeft();
                R_averageOffsetCorrection += DirectionalOffsets[UnitVectorDirection].GetRight();
            }

            L_averageOffsetCorrection /=  GazeData.Keys.Count;
            R_averageOffsetCorrection /=  GazeData.Keys.Count;
            GlobalOffset = new (L_averageOffsetCorrection, R_averageOffsetCorrection);
        }

        public Dictionary<string, GazeVector> GetDirectionalOffsets()
        {
            return DirectionalOffsets;
        }

        public GazeVector GetGlobalOffset()
        {
            return GlobalOffset;
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
            if (CalibrationActive)
            {
                UpdateTimer += Time.deltaTime;
                if (UpdateTimer >= PathInterval)
                {
                    // Shift to the next position if the timer has been reached
                    UnitVectorIndex += 1;
                    if (UnitVectorIndex > UnitVectorsPath.Count - 1)
                    {
                        UnitVectorIndex = 0;
                        EndCalibration();
                    }
                    UnitVector = UnitVectorsPath[UnitVectorsPath.Keys.ToList()[UnitVectorIndex]];
                    FixationObject.transform.localPosition = new Vector3(UnitVector.x * UnitDistance, UnitVector.y * UnitDistance, 0.0f);

                    // Reset the timer
                    UpdateTimer = 0.0f;
                }
                else
                {
                    // Capture eye tracking data and store alongside location
                    Vector3 l_p = leftEyeTracker.GetGazeEstimate();
                    Vector3 r_p = rightEyeTracker.GetGazeEstimate();
                    GazeData[UnitVectorsPath.Keys.ToList()[UnitVectorIndex]].Add(new (l_p, r_p));
                }
            }
        }
    }
}
