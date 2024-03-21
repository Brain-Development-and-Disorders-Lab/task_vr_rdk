using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class StimulusManager : MonoBehaviour
{
    [SerializeField]
    private GameObject stimulusAnchor;

    [SerializeField]
    private float ScalingFactor = 4.5f; // Adjust scaling of stimulus to be viewable

    // Calculated dimensions of stimuli
    private float StimulusDistance;
    private readonly float ArcDiameter = 8.0f; // Specified in supplementary materials
    private float ArcWorldRadius;
    private readonly float DotDiameter = 0.12f; // Specified in supplementary materials
    private float DotWorldRadius;

    // Stimuli groups, assembled from individual components
    private readonly List<string> AllStimuli = new() { "fixation", "decision", "motion" };
    private Dictionary<string, List<GameObject>> Stimuli = new();

    // Initialize StimulusManager
    void Start()
    {
        // Run pre-component calculations to ensure consistent world sizing
        CalculateValues();

        foreach (string stimuli in AllStimuli)
        {
            // Create the named set of components and store
            Stimuli.Add(stimuli, CreateStimulus(stimuli));
        }
    }

    private void CalculateValues()
    {
        StimulusDistance = Mathf.Abs(transform.position.z - stimulusAnchor.transform.position.z);
        ArcWorldRadius = StimulusDistance * Mathf.Tan(ScalingFactor * ArcDiameter / 2 * (Mathf.PI / 180.0f));
        DotWorldRadius = StimulusDistance * Mathf.Tan(ScalingFactor * DotDiameter / 2 * (Mathf.PI / 180.0f));
    }

    public List<GameObject> CreateStimulus(string stimulus)
    {
        List<GameObject> StimulusComponents = new();
        // "Fixation" stimulus
        if (stimulus == "fixation")
        {
            // Generate aperture
            StimulusComponents.Add(CreateArc(ArcWorldRadius, 0.0f, 182.0f, 100, Color.white));
            StimulusComponents.Add(CreateArc(ArcWorldRadius, 180.0f, 362.0f, 100, Color.white));
            // Add fixation cross
            StimulusComponents.Add(CreateFixationCross());
        }
        else if (stimulus == "decision")
        {
            // Generate aperture
            StimulusComponents.Add(CreateArc(ArcWorldRadius, 0.0f, 182.0f, 100, Color.cyan));
            StimulusComponents.Add(CreateArc(ArcWorldRadius, 180.0f, 362.0f, 100, Color.red));
            // Add fixation cross
            StimulusComponents.Add(CreateFixationCross());
        }
        else if (stimulus == "motion")
        {
            // Generate aperture
            StimulusComponents.Add(CreateArc(ArcWorldRadius, 0.0f, 182.0f, 100, Color.white));
            StimulusComponents.Add(CreateArc(ArcWorldRadius, 180.0f, 362.0f, 100, Color.white));
            // Add dots
            StimulusComponents.AddRange(CreateDots());
        }
        else
        {
            Debug.LogError("Unknown Stimulus type: " + stimulus);
        }
        return StimulusComponents;
    }

    public void SetVisible(string stimulus, bool visibility)
    {
        List<GameObject> StimuliGroup;
        Stimuli.TryGetValue(stimulus, out StimuliGroup);

        if (StimuliGroup.Count > 0)
        {
            foreach (GameObject component in StimuliGroup)
            {
                component.SetActive(visibility);
            }
            Debug.Log("Visibility of Stimulus \"" + stimulus + "\": " + visibility);
        }
        else
        {
            Debug.LogWarning("Could not apply visibility to Stimulus: " + stimulus);
        }
    }

    public GameObject CreateArc(float radius, float startAngle, float endAngle, int segments, Color color)
    {
        // Create base GameObject
        GameObject arcObject = new GameObject();
        arcObject.name = "rdk_arc_object";
        arcObject.AddComponent<LineRenderer>();
        arcObject.transform.SetParent(stimulusAnchor.transform, false);

        // Generate points to form the arc
        Vector3[] arcPoints = new Vector3[segments];
        float angle = startAngle;
        float arcLength = endAngle - startAngle;

        for (int i = 0; i < segments; i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            float y = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;

            arcPoints[i] = new Vector3(x, y, 0.0f);

            angle += arcLength / segments;
        }

        // Create the LineRenderer
        LineRenderer line = arcObject.GetComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = arcPoints.Length;
        line.SetPositions(arcPoints);
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.material.SetColor("_Color", color);
        line.startWidth = 0.1f;
        line.endWidth = 0.1f;

        return arcObject;
    }

    public GameObject CreateFixationCross()
    {
        // Create base GameObject
        GameObject fixationObjectParent = new GameObject();
        fixationObjectParent.name = "rdk_fixation_object";
        fixationObjectParent.transform.SetParent(stimulusAnchor.transform, false);

        // Create horizontal component
        GameObject fixationObjectHorizontal = new GameObject();
        fixationObjectHorizontal.name = "rdk_fixation_object_h";
        fixationObjectHorizontal.AddComponent<LineRenderer>();
        fixationObjectHorizontal.transform.SetParent(fixationObjectParent.transform, false);

        // Create horizontal LineRenderer
        LineRenderer horizontalLine = fixationObjectHorizontal.GetComponent<LineRenderer>();
        horizontalLine.useWorldSpace = false;
        horizontalLine.positionCount = 2;
        horizontalLine.SetPosition(0, new Vector3(-0.1f, 0.0f, 0.0f));
        horizontalLine.SetPosition(1, new Vector3(0.1f, 0.0f, 0.0f));
        horizontalLine.material = new Material(Shader.Find("Sprites/Default"));
        horizontalLine.material.SetColor("_Color", Color.white);
        horizontalLine.startWidth = 0.05f;
        horizontalLine.endWidth = 0.05f;

        // Create vertical component
        GameObject fixationObjectVertical = new GameObject();
        fixationObjectVertical.name = "rdk_fixation_object_v";
        fixationObjectVertical.AddComponent<LineRenderer>();
        fixationObjectVertical.transform.SetParent(fixationObjectParent.transform, false);

        // Create vertical LineRenderer
        LineRenderer verticalLine = fixationObjectVertical.GetComponent<LineRenderer>();
        verticalLine.useWorldSpace = false;
        verticalLine.positionCount = 2;
        verticalLine.SetPosition(0, new Vector3(0.0f, -0.1f, 0.0f));
        verticalLine.SetPosition(1, new Vector3(0.0f, 0.1f, 0.0f));
        verticalLine.material = new Material(Shader.Find("Sprites/Default"));
        verticalLine.material.SetColor("_Color", Color.white);
        verticalLine.startWidth = 0.05f;
        verticalLine.endWidth = 0.05f;

        return fixationObjectParent;
    }

    public GameObject CreateDot(float radius, float x = 0.0f, float y = 0.0f, bool visible = true)
    {
        // Create base GameObject
        GameObject dotObject = new GameObject();
        dotObject.name = "rdk_dot_object";
        dotObject.transform.SetParent(stimulusAnchor.transform, false);
        dotObject.AddComponent<SpriteRenderer>();
        dotObject.transform.localPosition = new Vector3(x, y, 0.0f);

        // Create SpriteRenderer
        SpriteRenderer dotRenderer = dotObject.GetComponent<SpriteRenderer>();
        dotRenderer.drawMode = SpriteDrawMode.Sliced;
        dotRenderer.sprite = Resources.Load<Sprite>("Sprites/Circle");
        dotRenderer.size = new Vector2(radius * 2.0f, radius * 2.0f);
        dotRenderer.enabled = visible;

        return dotObject;
    }

    public List<GameObject> CreateDots()
    {
        List<GameObject> AllDots = new();
        for (int i = 0; i < 30; i++)
        {
            for (int j = 0; j < 30; j++)
            {
                // Calculate distributed position and initial visibility
                float dotX = (ArcWorldRadius * 2 * j / 30) - ArcWorldRadius;
                float dotY = (ArcWorldRadius * 2 * i / 30) - ArcWorldRadius;
                bool initialVisibility = Mathf.Sqrt(Mathf.Pow(dotX, 2.0f) + Mathf.Pow(dotY, 2.0f)) <= ArcWorldRadius;

                // Create and add dot
                AllDots.Add(CreateDot(DotWorldRadius, dotX, dotY, initialVisibility));
            }
        }
        return AllDots;
    }

    public void Update()
    {
        // Perform movement update on dots if enabled
        List<GameObject> MotionObjects = Stimuli["motion"];
        if (MotionObjects.First().activeSelf == true)
        {
            // Motion is active, update dot positions
            foreach (GameObject motionObject in MotionObjects)
            {
                if (motionObject.name == "rdk_dot_object")
                {
                    // Create and store positions
                    Vector3 originalPosition = motionObject.transform.position;
                    Vector3 newPosition = new Vector3(originalPosition.x + 0.01f, originalPosition.y, originalPosition.z);

                    // Update visibility
                    bool visibility = Mathf.Sqrt(Mathf.Pow(newPosition.x, 2.0f) + Mathf.Pow(newPosition.y, 2.0f)) <= ArcWorldRadius;
                    motionObject.GetComponent<SpriteRenderer>().enabled = visibility;

                    motionObject.transform.position = newPosition;
                }
            }
        }
    }
}
