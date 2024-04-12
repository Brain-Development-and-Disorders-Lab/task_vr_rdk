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
  private int PointLocaleIndex = 0;
  private Vector2 PointLocale;
  private Dictionary<string, Vector2> AllPointLocales = new() {
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
  private Dictionary<string, List<Tuple<Vector3, Vector3>>> PointLocaleData = new() {
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

  private float UnitDistance = 2.0f;
  private readonly float GazeDistance = 10.0f;

  [SerializeField]
  public GameObject StimulusAnchor;
  private GameObject FixationObject;
  private GameObject IndicatorObject;

  private float UpdateTimer = 0.0f;
  private readonly float UpdateInterval = 2.0f;

  private void SetupCalibration()
  {
    PointLocale = AllPointLocales[AllPointLocales.Keys.ToList()[PointLocaleIndex]];

    // Create moving sphere object
    FixationObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
    FixationObject.name = "calibration_fixation";
    FixationObject.transform.SetParent(StimulusAnchor.transform, false);
    FixationObject.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
    FixationObject.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Sprites/Default"));
    FixationObject.GetComponent<MeshRenderer>().material.SetColor("_Color", Color.red);

    // Set initial position
    FixationObject.transform.localPosition = new Vector3(PointLocale.x * UnitDistance, PointLocale.y * UnitDistance, 0.0f);
  }

  void Start()
  {
    Logger = FindAnyObjectByType<LoggerManager>();
    SetupCalibration();
    RunCalibration();
  }

  public void RunCalibration()
  {
    IsCalibrating = true;
  }

  private void EndCalibration()
  {
    IsCalibrating = false;
  }

  void Update()
  {
    if (IsCalibrating)
    {
      UpdateTimer += Time.deltaTime;

      if (UpdateTimer >= UpdateInterval)
      {
        // Shift to the next position if the timer has been reached
        FixationObject.transform.localPosition = new Vector3(PointLocale.x * UnitDistance, PointLocale.y * UnitDistance, 0.0f);
        if (PointLocaleIndex + 1 > AllPointLocales.Count - 1)
        {
          PointLocaleIndex = 0;
        }
        else
        {
          PointLocaleIndex += 1;
        }
        PointLocale = AllPointLocales[AllPointLocales.Keys.ToList()[PointLocaleIndex]];

        UpdateTimer = 0.0f;
      }
      else
      {
        // Capture eye tracking data and store alongside location
        Vector3 l_p = leftEyeTracker.GetGazeEstimate();
        Vector3 r_p = rightEyeTracker.GetGazeEstimate();
        PointLocaleData[AllPointLocales.Keys.ToList()[PointLocaleIndex]].Add(new (l_p, r_p));

        // Compute distance from point for each
        Vector2 l_p_2 = new Vector2(l_p.x, l_p.y);
        Vector2 r_p_2 = new Vector2(r_p.x, r_p.y);
        Vector2 actual = new Vector2(FixationObject.transform.position.x, FixationObject.transform.position.y);

        Logger.Log("Distance (L): " + Vector2.Distance(l_p_2, actual));
        Logger.Log("Distance (R): " + Vector2.Distance(r_p_2, actual));
      }
    }
  }
}
