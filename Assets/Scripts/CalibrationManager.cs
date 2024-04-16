using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UXF;

public class CalibrationManager : MonoBehaviour
{
  private bool IsCalibrating = false;
  private bool IsCalibrated = false;

  // Set of points to be displayed for fixation
  private int UnitVectorIndex = 0;
  private Vector2 UnitVector;
  private Dictionary<string, Vector2> PointUnitVectors = new() {
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
  private Dictionary<string, List<Tuple<Vector3, Vector3>>> OffsetData = new() {
    {"E", new List<Tuple<Vector3, Vector3>>() },
    {"C", new List<Tuple<Vector3, Vector3>>() },
    {"NE", new List<Tuple<Vector3, Vector3>>() },
    {"N", new List<Tuple<Vector3, Vector3>>() },
    {"NW", new List<Tuple<Vector3, Vector3>>() },
    {"W", new List<Tuple<Vector3, Vector3>>() },
    {"SW", new List<Tuple<Vector3, Vector3>>() },
    {"S", new List<Tuple<Vector3, Vector3>>() },
    {"SE", new List<Tuple<Vector3, Vector3>>() },
  };

  private Dictionary<string, Tuple<Vector2, Vector2>> OffsetVectors = new() {
    {"E", null },
    {"C", null },
    {"NE", null },
    {"N", null },
    {"NW", null },
    {"W", null },
    {"SW", null },
    {"S", null },
    {"SE", null },
  };
  private Tuple<Vector2, Vector2> AverageOffset;

  private readonly float UnitDistance = 2.0f;

  [SerializeField]
  public GameObject StimulusAnchor;
  private GameObject FixationObject;

  private float UpdateTimer = 0.0f;
  private readonly float UpdateInterval = 1.5f;

  private Action CalibrationCallback;

  private void SetupCalibration()
  {
    UnitVector = PointUnitVectors[PointUnitVectors.Keys.ToList()[UnitVectorIndex]];

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
    IsCalibrating = true;
    FixationObject.SetActive(IsCalibrating);

    // Optional callback function
    CalibrationCallback = callback;
  }

  private void EndCalibration()
  {
    IsCalibrating = false;
    IsCalibrated = true;
    FixationObject.SetActive(IsCalibrating);

    // Run calculation
    CalculateCalibrationValues();

    // Run callback function if specified
    CalibrationCallback?.Invoke();
  }

  public bool CalibratedStatus()
  {
    return IsCalibrated;
  }

  private void CalculateCalibrationValues()
  {
    // Function to examine each point and calculate average vector difference from each point
    foreach (string UnitVectorDirection in OffsetData.Keys)
    {
      Vector2 VectorSum = Vector2.zero;
      foreach (Tuple<Vector3, Vector3> Pair in OffsetData[UnitVectorDirection])
      {
        // Get the sum of the left gaze and the actual position of the dot
        Vector2 Result = new Vector2(Pair.Item1.x, Pair.Item1.y) + (PointUnitVectors[UnitVectorDirection] * UnitDistance);
        VectorSum += new Vector2(Pair.Item1.x, Pair.Item1.y) + (PointUnitVectors[UnitVectorDirection] * UnitDistance);
      }
      VectorSum /= OffsetData[UnitVectorDirection].Count;
      OffsetVectors[UnitVectorDirection] = new Tuple<Vector2, Vector2>(VectorSum, VectorSum);
      Logger.Log(UnitVectorDirection + ": " + VectorSum.ToString());
    }

    Vector2 AverageOffsetCorrection = Vector2.zero;
    foreach (string UnitVectorDirection in OffsetData.Keys)
    {
      AverageOffsetCorrection += OffsetVectors[UnitVectorDirection].Item1;
    }
    AverageOffsetCorrection /=  OffsetData.Keys.Count;
    AverageOffset = new (AverageOffsetCorrection, AverageOffsetCorrection);
  }

  public Dictionary<string, Tuple<Vector2, Vector2>> GetOffsetVectors()
  {
    return OffsetVectors;
  }

  public Tuple<Vector2, Vector2> GetOffsetAverage()
  {
    return AverageOffset;
  }

  void Update()
  {
    if (IsCalibrating)
    {
      UpdateTimer += Time.deltaTime;

      if (UpdateTimer >= UpdateInterval)
      {
        // Shift to the next position if the timer has been reached
        FixationObject.transform.localPosition = new Vector3(UnitVector.x * UnitDistance, UnitVector.y * UnitDistance, 0.0f);
        if (UnitVectorIndex + 1 > PointUnitVectors.Count - 1)
        {
          UnitVectorIndex = 0;
          EndCalibration();
        }
        else
        {
          UnitVectorIndex += 1;
        }
        UnitVector = PointUnitVectors[PointUnitVectors.Keys.ToList()[UnitVectorIndex]];

        UpdateTimer = 0.0f;
      }
      else
      {
        // Capture eye tracking data and store alongside location
        Vector3 l_p = leftEyeTracker.GetGazeEstimate();
        Vector3 r_p = rightEyeTracker.GetGazeEstimate();
        OffsetData[PointUnitVectors.Keys.ToList()[UnitVectorIndex]].Add(new (l_p, r_p));
      }
    }
  }
}
