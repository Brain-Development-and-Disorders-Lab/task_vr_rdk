﻿using UnityEngine;
using System;
using System.Collections.Generic;

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
        [Header("Anchors and visual parameters")]
        [SerializeField]
        private GameObject stimulusAnchor;
        [SerializeField]
        private GameObject fixationAnchor;

        // Calculated dimensions of stimuli
        [Tooltip("Global scaling factor used to increase or decrease size of stimuli")]
        public float ScalingFactor = 1.5f;
        private float stimulusDistance;
        private readonly float APERTURE_WIDTH = 8.0f; // Degrees
        private float apertureWorldWidth; // Calculated from degrees into world units
        private float apertureWorldHeight; // Calculated from degrees into world units
        private readonly float LINE_WIDTH = 0.04f; // Specified in supplementary materials
        private float lineWorldWidth;
        private readonly float FIXATION_DIAMETER = 0.5f; // Adapted from supplementary materials
        private float fixationWorldRadius;

        // Dot parameters
        private readonly float DOT_DIAMETER = 0.12f; // Specified in supplementary materials
        private float dotWorldRadius;
        private List<Dot> dots = new();
        private float dotCoherence = 0.5f;
        private float dotDirection = (float)Math.PI; // "reference" dot type direction
        private readonly float DOT_DENSITY = 16.0f;
        private int dotCount = 0;

        // Timer for preserving consistent update rates
        private float updateTimer = 0.0f;
        private readonly int REFRESH_RATE = 90; // hertz

        // Stimuli groups, assembled from individual components
        private Dictionary<StimulusType, List<GameObject>> stimuliCollection = new();
        private Dictionary<StimulusType, bool> stimuliVisibility = new();
        private GameObject fixationCross; // Fixation cross parent GameObject

        // Slider-based button prefab
        [Header("Prefabs")]
        public GameObject ButtonPrefab;
        private ButtonSliderInput[] buttonSliders = new ButtonSliderInput[4];

        // Initialize StimulusManager
        void Start()
        {
            CalculateValues(); // Run pre-component calculations to ensure consistent world sizing
            fixationCross = CreateFixationCross();

            foreach (StimulusType stimuli in Enum.GetValues(typeof(StimulusType)))
            {
                // Create the named set of components and store
                stimuliCollection.Add(stimuli, CreateStimulus(stimuli));
                stimuliVisibility.Add(stimuli, false);
            }
        }

        /// <summary>
        /// Setup function resposible for calculations to convert all degree-based sizes into world units
        /// </summary>
        private void CalculateValues()
        {
            stimulusDistance = Mathf.Abs(transform.position.z - stimulusAnchor.transform.position.z);
            apertureWorldWidth = ScalingFactor * stimulusDistance * Mathf.Tan(APERTURE_WIDTH * (Mathf.PI / 180.0f));
            apertureWorldHeight = apertureWorldWidth * 2.0f;
            lineWorldWidth = ScalingFactor * LINE_WIDTH;
            fixationWorldRadius = ScalingFactor * stimulusDistance * Mathf.Tan(FIXATION_DIAMETER / 2.0f * (Mathf.PI / 180.0f));
            dotWorldRadius = ScalingFactor * stimulusDistance * Mathf.Tan(DOT_DIAMETER / 2.0f * (Mathf.PI / 180.0f));
            dotCount = Mathf.RoundToInt(ScalingFactor * DOT_DENSITY * APERTURE_WIDTH * APERTURE_WIDTH * 2.0f * stimulusDistance * Mathf.Tan(Mathf.PI / 180.0f));
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
                StaticComponents.Add(CreateRectangle(apertureWorldWidth, apertureWorldHeight, new Vector2(0.0f, 0.0f), Color.white));
            }
            else if (stimulus == StimulusType.Decision)
            {
                // Generate aperture
                StaticComponents.Add(CreateRectangle(apertureWorldWidth, apertureWorldHeight, new Vector2(0.0f, 0.0f), Color.white));
                // Add selection buttons
                StaticComponents.Add(CreateDecisionButtons());
            }
            else if (stimulus == StimulusType.Motion)
            {
                // Generate aperture
                StaticComponents.Add(CreateRectangle(apertureWorldWidth, apertureWorldHeight, new Vector2(0.0f, 0.0f), Color.white));
                // Add dots
                CreateDots();
            }
            else if (stimulus == StimulusType.Feedback_Correct)
            {
                // Generate aperture
                StaticComponents.Add(CreateRectangle(apertureWorldWidth, apertureWorldHeight, new Vector2(0.0f, 0.0f), Color.white));
            }
            else if (stimulus == StimulusType.Feedback_Incorrect)
            {
                // Generate aperture
                StaticComponents.Add(CreateRectangle(apertureWorldWidth, apertureWorldHeight, new Vector2(0.0f, 0.0f), Color.white));
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
            // Apply visibility to dots separately, if this is a stimulus that uses dots
            if (stimulus == StimulusType.Motion)
            {
                foreach (Dot dot in dots)
                {
                    // Only set the dot to be visible if it is within the aperture
                    if (visibility == true & Mathf.Sqrt(Mathf.Pow(dot.GetPosition().x, 2.0f) + Mathf.Pow(dot.GetPosition().y, 2.0f)) <= apertureWorldWidth / 2.0f)
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
            stimuliCollection.TryGetValue(stimulus, out StimuliGroup);

            if (StimuliGroup.Count > 0)
            {
                foreach (GameObject component in StimuliGroup)
                {
                    component.SetActive(visibility);
                }
                stimuliVisibility[stimulus] = visibility;
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
            foreach (StimulusType Key in stimuliCollection.Keys)
            {
                SetVisible(Key, visibility);
            }
        }

        /// <summary>
        /// Utility function to get the scaling factor applied to all visual stimuli
        /// </summary>
        /// <returns>`float` greater than or equal to `1.0f`</returns>
        public float GetScalingFactor()
        {
            return ScalingFactor;
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
            rectangleLine.startWidth = lineWorldWidth;
            rectangleLine.endWidth = lineWorldWidth;

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
            horizontalLine.SetPosition(0, new Vector3(-fixationWorldRadius, 0.0f, 0.0f));
            horizontalLine.SetPosition(1, new Vector3(fixationWorldRadius, 0.0f, 0.0f));
            horizontalLine.material = new Material(Resources.Load<Material>("Materials/DefaultWhite"));
            horizontalLine.material.SetColor("_Color", CrossColor);
            horizontalLine.startWidth = lineWorldWidth / 1.8f;
            horizontalLine.endWidth = lineWorldWidth / 1.8f;

            // Create vertical component
            GameObject fixationObjectVertical = new GameObject();
            fixationObjectVertical.name = "rdk_fixation_object_v";
            fixationObjectVertical.AddComponent<LineRenderer>();
            fixationObjectVertical.transform.SetParent(fixationObjectParent.transform, false);

            // Create vertical LineRenderer
            LineRenderer verticalLine = fixationObjectVertical.GetComponent<LineRenderer>();
            verticalLine.useWorldSpace = false;
            verticalLine.positionCount = 2;
            verticalLine.SetPosition(0, new Vector3(0.0f, -fixationWorldRadius, 0.0f));
            verticalLine.SetPosition(1, new Vector3(0.0f, fixationWorldRadius, 0.0f));
            verticalLine.material = new Material(Resources.Load<Material>("Materials/DefaultWhite"));
            verticalLine.material.SetColor("_Color", CrossColor);
            verticalLine.startWidth = lineWorldWidth / 1.8f;
            verticalLine.endWidth = lineWorldWidth / 1.8f;

            return fixationObjectParent;
        }

        public void SetFixationCrossVisibility(bool isVisible)
        {
            fixationCross.SetActive(isVisible);
        }

        public void SetFixationCrossColor(string color)
        {
            LineRenderer[] renderers = fixationCross.GetComponentsInChildren<LineRenderer>();
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
            for (int i = 0; i < dotCount; i++)
            {
                float x = UnityEngine.Random.Range(-apertureWorldWidth / 2.0f, apertureWorldWidth / 2.0f);
                float y = UnityEngine.Random.Range(-apertureWorldHeight / 2.0f, apertureWorldHeight / 2.0f);

                string dotBehavior = UnityEngine.Random.value > dotCoherence ? "random" : "reference";
                dots.Add(new Dot(stimulusAnchor, dotWorldRadius, apertureWorldWidth, apertureWorldHeight, dotBehavior, x, y, false));
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
            buttonSliders = new ButtonSliderInput[] {
                V_U_Slider,
                S_U_Slider,
                V_D_Slider,
                S_D_Slider
            };

            return buttonDecisionObject;
        }

        public ButtonSliderInput[] GetButtonSliders()
        {
            return buttonSliders;
        }

        public float GetCoherence()
        {
            return dotCoherence;
        }

        public void SetCoherence(float coherence)
        {
            // Update the stored coherence value
            if (coherence >= 0.0f && coherence <= 1.0f)
            {
                dotCoherence = coherence;
            }

            // Apply the coherence across all dots
            foreach (Dot dot in dots)
            {
                string dotBehavior = UnityEngine.Random.value > dotCoherence ? "random" : "reference";
                dot.SetBehavior(dotBehavior);
            }
        }

        public float GetDirection()
        {
            return dotDirection;
        }

        public void SetDirection(float direction)
        {
            dotDirection = direction;

            // Apply the direction across all "reference" type dots
            foreach (Dot dot in dots)
            {
                if (dot.GetBehavior() == "reference")
                {
                    dot.SetDirection(dotDirection);
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
            return apertureWorldWidth;
        }

        /// <summary>
        /// Get a reference to the StimulusAnchor `GameObject`
        /// </summary>
        /// <returns>StimulusAnchor `GameObject`</returns>
        public GameObject GetStimulusAnchor()
        {
            return stimulusAnchor;
        }

        /// <summary>
        /// Get a reference to the FixationAnchor `GameObject`
        /// </summary>
        /// <returns>FixationAnchor `GameObject`</returns>
        public GameObject GetFixationAnchor()
        {
            return fixationAnchor;
        }

        void Update()
        {
            updateTimer += Time.deltaTime;

            // Apply updates at the frequency of the desired refresh rate
            if (updateTimer >= 1.0f / REFRESH_RATE)
            {
                if (stimuliVisibility[StimulusType.Motion] == true)
                {
                    foreach (Dot dot in dots)
                    {
                        dot.Update();
                    }
                }

                // Reset the update timer
                updateTimer = 0.0f;
            }
        }
    }
}

