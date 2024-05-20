using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.Linq;

namespace Stimuli
{
    /// <summary>
    /// Enum to define known stimuli types within the task
    /// </summary>
    public enum StimulusType
    {
        Fixation = 0,
        Decision = 1,
        Motion = 2,
        Feedback_Correct = 3,
        Feedback_Incorrect = 4
    }

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
        private readonly float ApertureWidth = 2.8f; // Degrees
        private float ApertureWorldWidth; // Calculated from degrees into world units
        private float ApertureWorldHeight; // Calculated from degrees into world units
        private readonly float LineWidth = 0.04f; // Specified in supplementary materials
        private float LineWorldWidth;
        private readonly float FixationDiameter = 0.04f; // Specified in supplementary materials
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
        private Dictionary<StimulusType, List<GameObject>> Stimuli = new();
        private Dictionary<StimulusType, bool> StimuliVisibility = new();

        private GameObject FixationCross;

        // Slider-based button prefab
        public GameObject ButtonPrefab;
        private ButtonSliderInput[] ButtonSliders = new ButtonSliderInput[4];

        // Initialize StimulusManager
        void Start()
        {
            CalculateValues(); // Run pre-component calculations to ensure consistent world sizing
            FixationCross = CreateFixationCross();

            foreach (StimulusType stimuli in Enum.GetValues(typeof(StimulusType)))
            {
                // Create the named set of components and store
                Stimuli.Add(stimuli, CreateStimulus(stimuli));
                StimuliVisibility.Add(stimuli, false);
            }
        }

        /// <summary>
        /// Setup function resposible for calculations to convert all degree-based sizes into world units
        /// </summary>
        private void CalculateValues()
        {
            StimulusDistance = Mathf.Abs(transform.position.z - stimulusAnchor.transform.position.z);
            ApertureWorldWidth = StimulusDistance * Mathf.Tan(ScalingFactor * ApertureWidth * (Mathf.PI / 180.0f)) * 2.0f;
            ApertureWorldHeight = ApertureWorldWidth * 2.0f;
            LineWorldWidth = LineWidth;
            FixationWorldRadius = FixationDiameter * 2.0f;
            DotWorldRadius = StimulusDistance * Mathf.Tan(ScalingFactor * DotDiameter / 2 * (Mathf.PI / 180.0f));
            DotWorldDensity = DotDensity * StimulusDistance * Mathf.Tan(ScalingFactor * 1.0f / 2.0f * (Mathf.PI / 180.0f)) * 2.0f;
        }

        /// <summary>
        /// Switch-like function that takes a `StimulusType` and returns a `GameObject` containing that stimulus
        /// </summary>
        /// <param name="stimulus">A `StimulusType` value</param>
        /// <returns>Stimulus encased in a `GameObject`</returns>
        public List<GameObject> CreateStimulus(StimulusType stimulus)
        {
            List<GameObject> StaticComponents = new();
            // "Fixation" stimulus
            if (stimulus == StimulusType.Fixation)
            {
                // Generate aperture
                StaticComponents.Add(CreateRectangle(ApertureWorldWidth, ApertureWorldHeight, new Vector2(0.0f, 0.0f), Color.white));
            }
            else if (stimulus == StimulusType.Decision)
            {
                // Generate aperture
                StaticComponents.Add(CreateRectangle(ApertureWorldWidth, ApertureWorldHeight, new Vector2(0.0f, 0.0f), Color.white));
                // Add selection buttons
                StaticComponents.Add(CreateDecisionButtons());
            }
            else if (stimulus == StimulusType.Motion)
            {
                // Generate aperture
                StaticComponents.Add(CreateRectangle(ApertureWorldWidth, ApertureWorldHeight, new Vector2(0.0f, 0.0f), Color.white));
                // Add dots
                CreateDots();
            }
            else if (stimulus == StimulusType.Feedback_Correct)
            {
                // Generate aperture
                StaticComponents.Add(CreateRectangle(ApertureWorldWidth, ApertureWorldHeight, new Vector2(0.0f, 0.0f), Color.white));
            }
            else if (stimulus == StimulusType.Feedback_Incorrect)
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

        /// <summary>
        /// Update the visibility status of a stimulus
        /// </summary>
        /// <param name="stimulus">Exact `StimulusType`</param>
        /// <param name="visibility">Visibility state, `true` is visible, `false` is not</param>
        public void SetVisible(StimulusType stimulus, bool visibility)
        {
            // Apply visibility to Dots separately, if this is a stimulus that uses Dots
            if (stimulus == StimulusType.Motion)
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

        /// <summary>
        /// Utility function to either hide or show all stimuli at once
        /// </summary>
        /// <param name="visibility">Visibility state of all stimuli, `true` shows all, `false` hides all</param>
        public void SetVisibleAll(bool visibility)
        {
            foreach (StimulusType Key in Stimuli.Keys)
            {
                SetVisible(Key, visibility);
            }
        }

        /// <summary>
        /// Utility function to create a 2D rectangle with an outline and no fill
        /// </summary>
        /// <param name="width">Width in world units</param>
        /// <param name="height">Height in world units</param>
        /// <param name="position">`Vector2` position of the rectangle center</param>
        /// <param name="color">Color of the rectangle outline</param>
        /// <returns>A `GameObject` containing the rectangle</returns>
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
            rectangleLine.startWidth = LineWorldWidth;
            rectangleLine.endWidth = LineWorldWidth;

            return rectangleObject;
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
            horizontalLine.startWidth = LineWorldWidth;
            horizontalLine.endWidth = LineWorldWidth;

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
            verticalLine.startWidth = LineWorldWidth;
            verticalLine.endWidth = LineWorldWidth;

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
            V_U_Button.transform.localPosition = new Vector3(0.0f, 2.95f, 0.0f);
            ButtonSliderInput V_U_Slider = V_U_Button.GetComponentInChildren<ButtonSliderInput>();
            V_U_Slider.Setup();
            V_U_Slider.SetButtonText("<b>Up</b>\nVery Confident");
            V_U_Slider.SetBackgroundColor(new Color(240f/255f, 185f/255f, 55f/255f));
            V_U_Slider.SetFillColor(new Color(245f/255f, 190f/255f, 62f/255f));

            GameObject S_U_Button = Instantiate(ButtonPrefab, buttonDecisionObject.transform);
            S_U_Button.transform.localPosition = new Vector3(0.0f, 2.25f, 0.0f);
            ButtonSliderInput S_U_Slider = S_U_Button.GetComponentInChildren<ButtonSliderInput>();
            S_U_Slider.Setup();
            S_U_Slider.SetButtonText("<b>Up</b>\nSomewhat Confident");
            S_U_Slider.SetBackgroundColor(new Color(240f/255f, 185f/255f, 55f/255f));
            S_U_Slider.SetFillColor(new Color(245f/255f, 190f/255f, 62f/255f));

            GameObject V_D_Button = Instantiate(ButtonPrefab, buttonDecisionObject.transform);
            V_D_Button.transform.localPosition = new Vector3(0.0f, -2.95f, 0.0f);
            ButtonSliderInput V_D_Slider = V_D_Button.GetComponentInChildren<ButtonSliderInput>();
            V_D_Slider.Setup();
            V_D_Slider.SetButtonText("<b>Down</b>\nVery Confident");
            V_D_Slider.SetBackgroundColor(new Color(61f/255f, 162f/255f, 241f/255f));
            V_D_Slider.SetFillColor(new Color(81f/255f, 178f/255f, 252f/255f));

            GameObject S_D_Button = Instantiate(ButtonPrefab, buttonDecisionObject.transform);
            S_D_Button.transform.localPosition = new Vector3(0.0f, -2.25f, 0.0f);
            ButtonSliderInput S_D_Slider = S_D_Button.GetComponentInChildren<ButtonSliderInput>();
            S_D_Slider.Setup();
            S_D_Slider.SetButtonText("<b>Down</b>\nSomewhat Confident");
            S_D_Slider.SetBackgroundColor(new Color(61f/255f, 162f/255f, 241f/255f));
            S_D_Slider.SetFillColor(new Color(81f/255f, 178f/255f, 252f/255f));

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
                if (StimuliVisibility[StimulusType.Motion] == true)
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

