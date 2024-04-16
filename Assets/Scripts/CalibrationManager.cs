using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UXF;

public class CalibrationManager : MonoBehaviour
{
  private bool IsCalibrating = false;

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
    FixationObject.SetActive(IsCalibrating);

    // Run calculation
    CalculateCalibrationValues();

    // Run callback function if specified
    CalibrationCallback?.Invoke();
  }

  private void CalculateCalibrationValues()
  {
    // Function to examine each point and calculate average vector difference from each point
    foreach (string UnitVectorDirection in OffsetData.Keys)
    {
      List<Tuple<Vector3, Vector3>> PointPairs = OffsetData[UnitVectorDirection];
      float L_xSum = 0.0f;
      float L_ySum = 0.0f;
      float R_xSum = 0.0f;
      float R_ySum = 0.0f;

      // Extract all x and y coordinates, sum for average calculation
      foreach (Tuple<Vector3, Vector3> Pair in PointPairs)
      {
        L_xSum += Pair.Item1.x;
        L_ySum += Pair.Item1.y;
        R_xSum += Pair.Item2.x;
        R_ySum += Pair.Item2.y;
      }

      // Calculate average values and generate Vector2 representations
      float L_xAvg = L_xSum / PointPairs.Count;
      float L_yAvg = L_ySum / PointPairs.Count;
      float R_xAvg = R_xSum / PointPairs.Count;
      float R_yAvg = R_ySum / PointPairs.Count;

      Vector2 L_Offset = new Vector2(L_xAvg, L_yAvg);
      Vector2 R_Offset = new Vector2(R_xAvg, R_yAvg);
      OffsetVectors[UnitVectorDirection] = new Tuple<Vector2, Vector2> (L_Offset, R_Offset);

      Logger.Log(UnitVectorDirection + ": " + L_Offset.ToString() + " | " + R_Offset.ToString());
    }
  }

  public Dictionary<string, Tuple<Vector2, Vector2>> GetOffsetVectors()
  {
    return OffsetVectors;
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
