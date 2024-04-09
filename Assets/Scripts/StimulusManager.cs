using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.UI;

public class StimulusManager : MonoBehaviour
{
    [SerializeField]
    private GameObject stimulusAnchor;

    [SerializeField]
    private float ScalingFactor = 3.0f; // Adjust scaling of stimulus to be viewable

    // Calculated dimensions of stimuli
    private float StimulusDistance;
    private readonly float ArcDiameter = 8.0f; // Specified in supplementary materials
    private float ArcWorldRadius;

    // Dot parameters
    private readonly float DotDiameter = 0.12f; // Specified in supplementary materials
    private float DotWorldRadius;
    private List<Dot> Dots = new();
    private float DotCoherence = 0.5f;

    // Stimuli groups, assembled from individual components
    private readonly List<string> AllStimuli = new() { "fixation", "decision", "motion" };
    private Dictionary<string, List<GameObject>> Stimuli = new();
    private Dictionary<string, bool> StimuliVisibility = new();

    // Initialize StimulusManager
    void Start()
    {
        // Run pre-component calculations to ensure consistent world sizing
        CalculateValues();

        foreach (string stimuli in AllStimuli)
        {
            // Create the named set of components and store
            Stimuli.Add(stimuli, CreateStimulus(stimuli));
            StimuliVisibility.Add(stimuli, false);
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
        List<GameObject> StaticComponents = new();
        // "Fixation" stimulus
        if (stimulus == "fixation")
        {
            // Generate aperture
            StaticComponents.Add(CreateArc(ArcWorldRadius, 0.0f, 182.0f, 100, Color.white));
            StaticComponents.Add(CreateArc(ArcWorldRadius, 180.0f, 362.0f, 100, Color.white));
            // Add fixation cross
            StaticComponents.Add(CreateFixationCross());
        }
        else if (stimulus == "decision")
        {
            // Generate aperture
            StaticComponents.Add(CreateArc(ArcWorldRadius, 180.0f, 362.0f, 100, new Color32(0xd7, 0x80, 0x00, 0xff))); // Left
            StaticComponents.Add(CreateArc(ArcWorldRadius, 0.0f, 182.0f, 100, new Color32(0x3e, 0xa3, 0xa3, 0xff))); // Right
            // Add fixation cross
            StaticComponents.Add(CreateFixationCross());
            // Add selection buttons
            StaticComponents.Add(CreateDecisionButtons());
        }
        else if (stimulus == "motion")
        {
            // Generate aperture
            StaticComponents.Add(CreateArc(ArcWorldRadius, 0.0f, 182.0f, 100, Color.white));
            StaticComponents.Add(CreateArc(ArcWorldRadius, 180.0f, 362.0f, 100, Color.white));
            // Add dots
            CreateDots();
            // Add fixation cross
            StaticComponents.Add(CreateFixationCross());
        }
        else
        {
            Debug.LogError("Unknown Stimulus type: " + stimulus);
        }
        return StaticComponents;
    }

    public void SetVisible(string stimulus, bool visibility)
    {
        // Apply visibility to Dots separately, if this is a stimulus that uses Dots
        if (stimulus == "motion")
        {
            foreach (Dot dot in Dots)
            {
                // Only set the dot to be visible if it is within the aperture
                if (visibility == true & Mathf.Sqrt(Mathf.Pow(dot.GetPosition().x, 2.0f) + Mathf.Pow(dot.GetPosition().y, 2.0f)) <= ArcWorldRadius) {
                    dot.SetVisible(true);
                }
                else
                {
                    dot.SetVisible(false);
                }
            }
        }

        // Apply visibility to general stimuli components
        List<GameObject> StimuliGroup;
        Stimuli.TryGetValue(stimulus, out StimuliGroup);

        if (StimuliGroup.Count > 0)
        {
            foreach (GameObject component in StimuliGroup)
            {
                component.SetActive(visibility);
            }
            StimuliVisibility[stimulus] = visibility;
            Debug.Log("Visibility of Stimulus \"" + stimulus + "\": " + visibility);
        }
        else
        {
            Debug.LogWarning("Could not apply visibility to Stimulus: " + stimulus);
        }
    }

    public void SetVisibleAll(bool visibility)
    {
        foreach (string Key in Stimuli.Keys)
        {
            SetVisible(Key, visibility);
        }
    }

    public GameObject CreateArc(float radius, float startAngle, float endAngle, int segments, Color color)
    {
        // Create base GameObject
        GameObject arcObject = new GameObject();
        arcObject.name = "rdk_arc_object";
        arcObject.AddComponent<LineRenderer>();
        arcObject.transform.SetParent(stimulusAnchor.transform, false);
        arcObject.SetActive(false);

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
        line.startWidth = 0.04f;
        line.endWidth = 0.04f;

        return arcObject;
    }

    public GameObject CreateFixationCross()
    {
        // Create base GameObject
        GameObject fixationObjectParent = new GameObject();
        fixationObjectParent.name = "rdk_fixation_object";
        fixationObjectParent.transform.SetParent(stimulusAnchor.transform, false);
        fixationObjectParent.SetActive(false);

        // Create horizontal component
        GameObject fixationObjectHorizontal = new GameObject();
        fixationObjectHorizontal.name = "rdk_fixation_object_h";
        fixationObjectHorizontal.AddComponent<LineRenderer>();
        fixationObjectHorizontal.transform.SetParent(fixationObjectParent.transform, false);

        // Create horizontal LineRenderer
        LineRenderer horizontalLine = fixationObjectHorizontal.GetComponent<LineRenderer>();
        horizontalLine.useWorldSpace = false;
        horizontalLine.positionCount = 2;
        horizontalLine.SetPosition(0, new Vector3(-0.05f, 0.0f, 0.0f));
        horizontalLine.SetPosition(1, new Vector3(0.05f, 0.0f, 0.0f));
        horizontalLine.material = new Material(Shader.Find("Sprites/Default"));
        horizontalLine.material.SetColor("_Color", Color.white);
        horizontalLine.startWidth = 0.03f;
        horizontalLine.endWidth = 0.03f;

        // Create vertical component
        GameObject fixationObjectVertical = new GameObject();
        fixationObjectVertical.name = "rdk_fixation_object_v";
        fixationObjectVertical.AddComponent<LineRenderer>();
        fixationObjectVertical.transform.SetParent(fixationObjectParent.transform, false);

        // Create vertical LineRenderer
        LineRenderer verticalLine = fixationObjectVertical.GetComponent<LineRenderer>();
        verticalLine.useWorldSpace = false;
        verticalLine.positionCount = 2;
        verticalLine.SetPosition(0, new Vector3(0.0f, -0.05f, 0.0f));
        verticalLine.SetPosition(1, new Vector3(0.0f, 0.05f, 0.0f));
        verticalLine.material = new Material(Shader.Find("Sprites/Default"));
        verticalLine.material.SetColor("_Color", Color.white);
        verticalLine.startWidth = 0.03f;
        verticalLine.endWidth = 0.03f;

        return fixationObjectParent;
    }

    public void CreateDots()
    {
        const int RowCount = 30;
        for (int i = -RowCount / 2; i < RowCount / 2; i++) {
            for (int j = -RowCount / 2; j < RowCount / 2; j++) {
                float x = (i * ArcWorldRadius * 2) / RowCount + (UnityEngine.Random.value * ArcWorldRadius * 2) / RowCount;
                float y = (j * ArcWorldRadius * 2) / RowCount + (UnityEngine.Random.value * ArcWorldRadius * 2) / RowCount;
                string dotBehavior = UnityEngine.Random.value > DotCoherence ? "random" : "reference";
                Dots.Add(new Dot(stimulusAnchor, DotWorldRadius, ArcWorldRadius, dotBehavior, x, y, false));
            }
        }
    }

    public GameObject CreateDecisionButtons()
    {
        GameObject buttonDecisionObject = new GameObject();
        buttonDecisionObject.name = "rdk_button_decision_object";
        buttonDecisionObject.transform.SetParent(stimulusAnchor.transform, false);
        buttonDecisionObject.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
        buttonDecisionObject.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        buttonDecisionObject.AddComponent<Canvas>();
        buttonDecisionObject.AddComponent<GraphicRaycaster>();
        buttonDecisionObject.SetActive(false);

        TMP_DefaultControls.Resources ButtonResources = new TMP_DefaultControls.Resources();

        // Left button, typically "back" action
        GameObject LButton = TMP_DefaultControls.CreateButton(ButtonResources);
        LButton.transform.SetParent(buttonDecisionObject.transform, false);
        LButton.transform.localPosition = new Vector3(-42.5f, 0.0f, 0.0f);
        LButton.GetComponent<RectTransform>().sizeDelta = new Vector2(20.0f, 10.0f);
        LButton.GetComponent<Image>().sprite = Resources.Load<Sprite>("Sprites/Button");
        LButton.GetComponent<Image>().color = new Color32(0xd7, 0x80, 0x00, 0xff);
        TextMeshProUGUI LButtonText = LButton.GetComponentInChildren<TextMeshProUGUI>();
        LButtonText.fontStyle = FontStyles.Bold;
        LButtonText.fontSize = 4.0f;
        LButtonText.text = "Left";

        // Right button, typically "next" action
        GameObject RButton = TMP_DefaultControls.CreateButton(ButtonResources);
        RButton.transform.SetParent(buttonDecisionObject.transform, false);
        RButton.transform.localPosition = new Vector3(42.5f, 0.0f, 0.0f);
        RButton.GetComponent<RectTransform>().sizeDelta = new Vector2(20.0f, 10.0f);
        RButton.GetComponent<Image>().sprite = Resources.Load<Sprite>("Sprites/Button");
        RButton.GetComponent<Image>().color = new Color32(0x3e, 0xa3, 0xa3, 0xff);
        TextMeshProUGUI RButtonText = RButton.GetComponentInChildren<TextMeshProUGUI>();
        RButtonText.fontStyle = FontStyles.Bold;
        RButtonText.fontSize = 4.0f;
        RButtonText.text = "Right";

        return buttonDecisionObject;
    }

    public float GetCoherence()
    {
        return DotCoherence;
    }

    public void SetCoherence(float coherence)
    {
        if (coherence >= 0.0f && coherence <= 1.0f) DotCoherence = coherence;
    }

    /// <summary>
    /// Get the radius of the stimuli. Used for correct offset calculations for dioptic / dichoptic stimulus
    /// presentation.
    /// </summary>
    /// <returns>Stimulus radius, measured in world units</returns>
    public float GetStimulusRadius()
    {
        return ArcWorldRadius;
    }

    void Update()
    {
        if (StimuliVisibility["motion"] == true)
        {
            foreach (Dot dot in Dots)
            {
                dot.Update();
            }
        }
    }
}
