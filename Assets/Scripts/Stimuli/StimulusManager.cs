using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.Linq;

namespace Stimuli
{
    public class StimulusManager : MonoBehaviour
    {
        [SerializeField]
        private GameObject stimulusAnchor;
        [SerializeField]
        private GameObject fixationAnchor;

        [SerializeField]
        private float ScalingFactor = 3.0f; // Adjust scaling of stimulus to be viewable

        // Calculated dimensions of stimuli
        private float StimulusDistance;
        private readonly float ApertureRadius = 6.0f; // Specified in supplementary materials
        private float ApertureWorldWidth;
        private float ApertureWorldHeight;
        private readonly float ArcWidth = 0.04f; // Specified in supplementary materials
        private float ArcWorldWidth;
        private readonly float FixationDiameter = 0.05f; // Specified in supplementary materials
        private float FixationWorldRadius;

        // Dot parameters
        private readonly float DotDiameter = 0.12f; // Specified in supplementary materials
        private float DotWorldRadius;
        private List<Dot> Dots = new();
        private float DotCoherence = 0.5f;
        private float DotDirection = (float)Math.PI; // "reference" dot type direction
        private readonly float DotDensity = 64.0f;
        private float DotWorldDensity;

        // Timer for preserving consistent update rates
        private float UpdateTimer = 0.0f;
        private readonly int REFRESH_RATE = 90; // hertz

        // Stimuli groups, assembled from individual components
        private readonly List<string> AllStimuli = new() {
            "fixation",
            "decision",
            "motion",
            "feedback_correct",
            "feedback_incorrect"
        };
        private Dictionary<string, List<GameObject>> Stimuli = new();
        private Dictionary<string, bool> StimuliVisibility = new();

        private GameObject FixationCross;

        // Slider-based button prefab
        public GameObject ButtonPrefab;
        private ButtonSliderInput[] ButtonSliders = new ButtonSliderInput[4];

        // Initialize StimulusManager
        void Start()
        {
            CalculateValues(); // Run pre-component calculations to ensure consistent world sizing
            FixationCross = CreateFixationCross();

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
            ApertureWorldWidth = StimulusDistance * Mathf.Tan(ScalingFactor * ApertureRadius / 2 * (Mathf.PI / 180.0f)) * 2.0f;
            ApertureWorldHeight = ApertureWorldWidth * 2.0f;
            ArcWorldWidth = ArcWidth;
            FixationWorldRadius = FixationDiameter * 2.0f;
            DotWorldRadius = StimulusDistance * Mathf.Tan(ScalingFactor * DotDiameter / 2 * (Mathf.PI / 180.0f));
            DotWorldDensity = DotDensity * StimulusDistance * Mathf.Tan(ScalingFactor * 1.0f / 2.0f * (Mathf.PI / 180.0f)) * 2.0f;
        }

        public List<GameObject> CreateStimulus(string stimulus)
        {
            List<GameObject> StaticComponents = new();
            // "Fixation" stimulus
            if (stimulus == "fixation")
            {
                // Generate aperture
                StaticComponents.Add(CreateRectangle(ApertureWorldWidth, ApertureWorldHeight, new Vector2(0.0f, 0.0f), Color.white));
            }
            else if (stimulus == "decision")
            {
                // Generate aperture
                StaticComponents.Add(CreateRectangle(ApertureWorldWidth, ApertureWorldHeight, new Vector2(0.0f, 0.0f), Color.white));
                // Add selection buttons
                StaticComponents.Add(CreateDecisionButtons());
            }
            else if (stimulus == "motion")
            {
                // Generate aperture
                StaticComponents.Add(CreateRectangle(ApertureWorldWidth, ApertureWorldHeight, new Vector2(0.0f, 0.0f), Color.white));
                // Add dots
                CreateDots();
            }
            else if (stimulus == "feedback_correct")
            {
                // Generate aperture
                StaticComponents.Add(CreateRectangle(ApertureWorldWidth, ApertureWorldHeight, new Vector2(0.0f, 0.0f), Color.white));
            }
            else if (stimulus == "feedback_incorrect")
            {
                // Generate aperture
                StaticComponents.Add(CreateRectangle(ApertureWorldWidth, ApertureWorldHeight, new Vector2(0.0f, 0.0f), Color.white));
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
                    if (visibility == true & Mathf.Sqrt(Mathf.Pow(dot.GetPosition().x, 2.0f) + Mathf.Pow(dot.GetPosition().y, 2.0f)) <= ApertureWorldWidth / 2.0f)
                    {
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

        public GameObject CreateRectangle(float width, float height, Vector2 position, Color color)
        {
            // Create base GameObject
            GameObject rectangleObject = new GameObject();
            rectangleObject.name = "rdk_rectangle_object";
            rectangleObject.AddComponent<LineRenderer>();
            rectangleObject.transform.SetParent(stimulusAnchor.transform, false);
            rectangleObject.SetActive(false);

            // Calculate fixed adjustments to center around `position` parameter
            float xOffset = Mathf.Abs(width / 2);
            float yOffset = Mathf.Abs(height / 2);

            LineRenderer rectangleLine = rectangleObject.GetComponent<LineRenderer>();
            rectangleLine.loop = true;
            rectangleLine.useWorldSpace = false;
            rectangleLine.positionCount = 4;
            Vector3[] linePositions = {
                new Vector3(position.x - xOffset, position.y - yOffset, 0.0f),
                new Vector3(position.x - xOffset + width, position.y - yOffset, 0.0f),
                new Vector3(position.x - xOffset + width, position.y - yOffset + height, 0.0f),
                new Vector3(position.x - xOffset, position.y - yOffset + height, 0.0f)
            };
            rectangleLine.SetPositions(linePositions);
            rectangleLine.material = new Material(Resources.Load<Material>("Materials/DefaultWhite"));
            rectangleLine.material.SetColor("_Color", color);
            rectangleLine.startWidth = ArcWorldWidth;
            rectangleLine.endWidth = ArcWorldWidth;

            return rectangleObject;
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
            line.material = new Material(Resources.Load<Material>("Materials/DefaultWhite"));
            line.material.SetColor("_Color", color);
            line.startWidth = ArcWorldWidth;
            line.endWidth = ArcWorldWidth;

            return arcObject;
        }

        public GameObject CreateFixationCross(string color = "white")
        {
            Color CrossColor = Color.white;
            if (color == "red")
            {
                CrossColor = Color.red;
            }
            else if (color == "green")
            {
                CrossColor = Color.green;
            }

            // Create base GameObject
            GameObject fixationObjectParent = new GameObject();
            fixationObjectParent.name = "rdk_fixation_object";
            fixationObjectParent.transform.SetParent(fixationAnchor.transform, false);
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
            horizontalLine.SetPosition(0, new Vector3(-FixationWorldRadius, 0.0f, 0.0f));
            horizontalLine.SetPosition(1, new Vector3(FixationWorldRadius, 0.0f, 0.0f));
            horizontalLine.material = new Material(Resources.Load<Material>("Materials/DefaultWhite"));
            horizontalLine.material.SetColor("_Color", CrossColor);
            horizontalLine.startWidth = ArcWorldWidth;
            horizontalLine.endWidth = ArcWorldWidth;

            // Create vertical component
            GameObject fixationObjectVertical = new GameObject();
            fixationObjectVertical.name = "rdk_fixation_object_v";
            fixationObjectVertical.AddComponent<LineRenderer>();
            fixationObjectVertical.transform.SetParent(fixationObjectParent.transform, false);

            // Create vertical LineRenderer
            LineRenderer verticalLine = fixationObjectVertical.GetComponent<LineRenderer>();
            verticalLine.useWorldSpace = false;
            verticalLine.positionCount = 2;
            verticalLine.SetPosition(0, new Vector3(0.0f, -FixationWorldRadius, 0.0f));
            verticalLine.SetPosition(1, new Vector3(0.0f, FixationWorldRadius, 0.0f));
            verticalLine.material = new Material(Resources.Load<Material>("Materials/DefaultWhite"));
            verticalLine.material.SetColor("_Color", CrossColor);
            verticalLine.startWidth = ArcWorldWidth;
            verticalLine.endWidth = ArcWorldWidth;

            return fixationObjectParent;
        }

        public void SetFixationCrossVisibility(bool isVisible)
        {
            FixationCross.SetActive(isVisible);
        }

        public void SetFixationCrossColor(string color)
        {
            LineRenderer[] renderers = FixationCross.GetComponentsInChildren<LineRenderer>();
            foreach (LineRenderer renderer in renderers)
            {
                if (color == "white") {
                    renderer.material.SetColor("_Color", Color.white);
                }
                else if (color == "red")
                {
                    renderer.material.SetColor("_Color", Color.red);
                }
                else if (color == "green")
                {
                    renderer.material.SetColor("_Color", Color.green);
                }
            }
        }

        public void CreateDots()
        {
            int DotCount = (int)(ApertureWorldHeight * ApertureWorldWidth * DotWorldDensity);
            for (int i = 0; i < DotCount; i++)
            {
                float x = UnityEngine.Random.Range(-ApertureWorldWidth / 2.0f, ApertureWorldWidth / 2.0f);
                float y = UnityEngine.Random.Range(-ApertureWorldHeight / 2.0f, ApertureWorldHeight / 2.0f);

                string dotBehavior = UnityEngine.Random.value > DotCoherence ? "random" : "reference";
                Dots.Add(new Dot(stimulusAnchor, DotWorldRadius, ApertureWorldWidth, ApertureWorldHeight, dotBehavior, x, y, false));
            }
        }

        public GameObject CreateDecisionButtons()
        {
            GameObject buttonDecisionObject = new GameObject();
            buttonDecisionObject.name = "rdk_button_decision_object";
            buttonDecisionObject.transform.SetParent(stimulusAnchor.transform, false);
            buttonDecisionObject.transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
            buttonDecisionObject.SetActive(false);

            GameObject V_U_Button = Instantiate(ButtonPrefab, buttonDecisionObject.transform);
            V_U_Button.transform.localPosition = new Vector3(0.0f, 3.3f, 0.0f);
            ButtonSliderInput V_U_Slider = V_U_Button.GetComponentInChildren<ButtonSliderInput>();
            V_U_Slider.Setup();
            V_U_Slider.SetButtonText("<b>Up</b>\nVery Confident");

            GameObject S_U_Button = Instantiate(ButtonPrefab, buttonDecisionObject.transform);
            S_U_Button.transform.localPosition = new Vector3(0.0f, 2.5f, 0.0f);
            ButtonSliderInput S_U_Slider = S_U_Button.GetComponentInChildren<ButtonSliderInput>();
            S_U_Slider.Setup();
            S_U_Slider.SetButtonText("<b>Up</b>\nSomewhat Confident");

            GameObject V_D_Button = Instantiate(ButtonPrefab, buttonDecisionObject.transform);
            V_D_Button.transform.localPosition = new Vector3(0.0f, -3.3f, 0.0f);
            ButtonSliderInput V_D_Slider = V_D_Button.GetComponentInChildren<ButtonSliderInput>();
            V_D_Slider.Setup();
            V_D_Slider.SetButtonText("<b>Down</b>\nVery Confident");

            GameObject S_D_Button = Instantiate(ButtonPrefab, buttonDecisionObject.transform);
            S_D_Button.transform.localPosition = new Vector3(0.0f, -2.5f, 0.0f);
            ButtonSliderInput S_D_Slider = S_D_Button.GetComponentInChildren<ButtonSliderInput>();
            S_D_Slider.Setup();
            S_D_Slider.SetButtonText("<b>Down</b>\nSomewhat Confident");

            // Store the slider controllers
            ButtonSliders = new ButtonSliderInput[] {
                V_U_Slider,
                S_U_Slider,
                V_D_Slider,
                S_D_Slider
            };

            return buttonDecisionObject;
        }

        public ButtonSliderInput[] GetButtonControllers()
        {
            return ButtonSliders;
        }

        public float GetCoherence()
        {
            return DotCoherence;
        }

        public void SetCoherence(float coherence)
        {
            // Update the stored coherence value
            if (coherence >= 0.0f && coherence <= 1.0f)
            {
                DotCoherence = coherence;
            }

            // Apply the coherence across all dots
            foreach (Dot dot in Dots)
            {
                string dotBehavior = UnityEngine.Random.value > DotCoherence ? "random" : "reference";
                dot.SetBehavior(dotBehavior);
            }
        }

        public float GetDirection()
        {
            return DotDirection;
        }

        public void SetDirection(float direction)
        {
            DotDirection = direction;

            // Apply the direction across all "reference" type dots
            foreach (Dot dot in Dots)
            {
                if (dot.GetBehavior() == "reference")
                {
                    dot.SetDirection(DotDirection);
                }
            }
        }

        /// <summary>
        /// Get the width in world units of the stimuli. Used for correct offset calculations for dioptic / dichoptic
        /// stimulus presentation.
        /// </summary>
        /// <returns>Stimulus width, measured in world units</returns>
        public float GetApertureWidth()
        {
            return ApertureWorldWidth;
        }

        public GameObject GetAnchor()
        {
            return stimulusAnchor;
        }

        void Update()
        {
            UpdateTimer += Time.deltaTime;

            // Apply updates at the frequency of the desired refresh rate
            if (UpdateTimer >= 1.0f / REFRESH_RATE)
            {
                if (StimuliVisibility["motion"] == true)
                {
                    foreach (Dot dot in Dots)
                    {
                        dot.Update();
                    }
                }

                // Reset the update timer
                UpdateTimer = 0.0f;
            }
        }
    }
}

