using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CalibrationManager : MonoBehaviour
{
  private bool IsCalibrating = false;
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

  private float UnitDistance = 2.0f;

  [SerializeField]
  public GameObject StimulusAnchor;
  private GameObject FixationObject;

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
    FixationObject.GetComponent<MeshRenderer>().material.SetColor("_Color", Color.red);

    // Set initial position
    FixationObject.transform.localPosition = new Vector3(PointLocale.x * UnitDistance, PointLocale.y * UnitDistance, 0.0f);
  }

  void Start()
  {
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
    }
  }
}
