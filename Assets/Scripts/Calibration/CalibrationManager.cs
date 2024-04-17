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

        // Set of points to be displayed for fixation
        private int UnitVectorIndex = 0;
        private Vector2 UnitVector; // The active unit vector
        private readonly Dictionary<string, Vector2> UnitVectors = new() {
            {"C", new Vector2(0, 0)},
            {"E", new Vector2(1, 0)},
            {"NE", new Vector2(1, 1)},
            {"N", new Vector2(0, 1)},
            {"NW", new Vector2(-1, 1)},
            {"W", new Vector2(-1, 0)},
            {"SW", new Vector2(-1, -1)},
            {"S", new Vector2(0, -1)},
            {"SE", new Vector2(1, -1)},
        };

        private LoggerManager Logger;

        // Left and right EyeGazeData objects
        [SerializeField]
        private EyePositionTracker leftEyeTracker;
        [SerializeField]
        private EyePositionTracker rightEyeTracker;

        // Data storage
        private Dictionary<string, List<GazeVector>> GazeData = new() {
            {"E", new List<GazeVector>() },
            {"C", new List<GazeVector>() },
            {"NE", new List<GazeVector>() },
            {"N", new List<GazeVector>() },
            {"NW", new List<GazeVector>() },
            {"W", new List<GazeVector>() },
            {"SW", new List<GazeVector>() },
            {"S", new List<GazeVector>() },
            {"SE", new List<GazeVector>() },
        };

        // Calculated offset vectors
        private Dictionary<string, GazeVector> DirectionalOffsets = new();
        private GazeVector GlobalOffset;

        private readonly float UnitDistance = 2.0f;

        [SerializeField]
        public GameObject StimulusAnchor;
        private GameObject FixationObject;

        private float UpdateTimer = 0.0f;
        private readonly float UpdateInterval = 1.5f;

        private Action CalibrationCallback;

        private void SetupCalibration()
        {
            UnitVector = UnitVectors[UnitVectors.Keys.ToList()[UnitVectorIndex]];

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
                Vector2 VectorSum = Vector2.zero;
                foreach (GazeVector VectorPair in GazeData[UnitVectorDirection])
                {
                    // Get the sum of the left gaze and the actual position of the dot
                    Vector2 Result = new Vector2(VectorPair.GetLeft().x, VectorPair.GetLeft().y) + (UnitVectors[UnitVectorDirection] * UnitDistance);
                    VectorSum += new Vector2(VectorPair.GetLeft().x, VectorPair.GetLeft().y) + (UnitVectors[UnitVectorDirection] * UnitDistance);
                }
                VectorSum /= GazeData[UnitVectorDirection].Count;
                DirectionalOffsets.Add(UnitVectorDirection, new GazeVector(VectorSum, VectorSum));
                Logger.Log(UnitVectorDirection + ": " + VectorSum.ToString());
            }

            // Calculate a global offset correction vector
            Vector2 AverageOffsetCorrection = Vector2.zero;
            foreach (string UnitVectorDirection in GazeData.Keys)
            {
                AverageOffsetCorrection += DirectionalOffsets[UnitVectorDirection].GetLeft();
            }
            AverageOffsetCorrection /=  GazeData.Keys.Count;
            GlobalOffset = new (AverageOffsetCorrection, AverageOffsetCorrection);
        }

        public Dictionary<string, GazeVector> GetDirectionalOffsets()
        {
            return DirectionalOffsets;
        }

        public GazeVector GetGlobalOffset()
        {
            return GlobalOffset;
        }

        void Update()
        {
            if (CalibrationActive)
            {
                UpdateTimer += Time.deltaTime;

                if (UpdateTimer >= UpdateInterval)
                {
                    // Shift to the next position if the timer has been reached
                    FixationObject.transform.localPosition = new Vector3(UnitVector.x * UnitDistance, UnitVector.y * UnitDistance, 0.0f);
                    if (UnitVectorIndex + 1 > UnitVectors.Count - 1)
                    {
                        UnitVectorIndex = 0;
                        EndCalibration();
                    }
                    else
                    {
                        UnitVectorIndex += 1;
                    }
                    UnitVector = UnitVectors[UnitVectors.Keys.ToList()[UnitVectorIndex]];

                    UpdateTimer = 0.0f;
                }
                else
                {
                    // Capture eye tracking data and store alongside location
                    Vector3 l_p = leftEyeTracker.GetGazeEstimate();
                    Vector3 r_p = rightEyeTracker.GetGazeEstimate();
                    GazeData[UnitVectors.Keys.ToList()[UnitVectorIndex]].Add(new (l_p, r_p));
                }
            }
        }
    }
}
