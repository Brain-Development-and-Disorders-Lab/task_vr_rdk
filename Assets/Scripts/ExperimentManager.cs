﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

using UXF;
using System.Linq;
using MathNet.Numerics.Statistics;

public class ExperimentManager : MonoBehaviour
{
    readonly int InstructionTrials = 1;
    readonly int InstructionBlockIndex = 1;

    readonly int CalibrationTrials = 120;
    readonly int CalibrationBlockIndex = 2; // Expected index of the "Calibration"-type block

    readonly int MainTrials = 200;
    readonly int MainBlockIndex = 3; // Expected index of the "Main"-type block

    private int ActiveBlock = 1;

    // Coherence data structure
    private Dictionary<string, float[]> Coherences = new()
    {
        { "both", new float[]{0.2f, 0.2f} },
        { "left", new float[]{0.2f, 0.2f} },
        { "right", new float[]{0.2f, 0.2f} },
    };
    private float[] ActiveCoherences;
    private readonly int LOW_INDEX = 0;
    private readonly int HIGH_INDEX = 1;
    private readonly int EYE_BLOCK_SIZE = 10; // Number of trials per eye

    StimulusManager stimulusManager;
    UIManager uiManager;
    CameraManager cameraManager;

    // Input parameters
    bool InputEnabled = false;
    private bool InputReset = true;

    // Confidence parameters
    private readonly int CONFIDENCE_BLOCK_SIZE = 2; // Number of trials to run before asking for confidence

    /// <summary>
    /// Generate the experiment flow
    /// </summary>
    /// <param name="session"></param>
    public void GenerateExperiment(Session session)
    {
        // Create trial blocks
        session.CreateBlock(InstructionTrials);
        session.CreateBlock(CalibrationTrials);
        session.CreateBlock(MainTrials);

        // Store reference to other classes
        stimulusManager = GetComponent<StimulusManager>();
        uiManager = GetComponent<UIManager>();
        cameraManager = GetComponent<CameraManager>();

        // Update the CameraManager value for the aperture offset to be the stimulus radius
        cameraManager.SetStimulusRadius(stimulusManager.GetStimulusRadius());
    }

    /// <summary>
    /// Start the experiment by triggering the next trial
    /// </summary>
    /// <param name="session"></param>
    public void BeginExperiment(Session session)
    {
        session.BeginNextTrial();
    }

    /// <summary>
    /// Quit the experiment and close the VR application
    /// </summary>
    public void QuitExperiment()
    {
        Application.Quit();
    }

    private void SetupInstructions()
    {
        // Setup the UI manager with instructions
        uiManager.EnablePagination(true);
        List<string> Instructions = new List<string>{
            "You are about to start the task. Before you start, please let the facilitator know if the headset feels uncomfortable or you cannot read this text.\n\nWhen you are ready and comfortable, press the right controller trigger to select 'Next >' and continue.",
            "These practice trials are similar to the actual trials, except the moving dots will be displayed a few seconds longer.\n\nPractice watching the dots and observing the appearance of the task.\n\nUse the triggers on the left and right controllers to interact with the task."
        };
        uiManager.SetPages(Instructions);

        cameraManager.SetActiveField(CameraManager.VisualField.Both);
    }

    private void SetupMotion()
    {
        // Setup performed at the start of the first "main" type trial
        if (ActiveBlock == MainBlockIndex && Session.instance.CurrentTrial.numberInBlock == 1)
        {
            // Calculate the coherences to be used in the actual trials, use median of last 20 calibration trials
            List<Trial> CalibrationTrials = Session.instance.GetBlock(CalibrationBlockIndex).trials;
            CalibrationTrials.Reverse();

            // Calibration trials have the same coherence for low and high
            List<float> BothCoherenceValues = new List<float>();
            List<float> LeftCoherenceValues = new List<float>();
            List<float> RightCoherenceValues = new List<float>();
            foreach (Trial t in CalibrationTrials)
            {
                BothCoherenceValues.Add(((float[]) t.result["combinedCoherences"])[LOW_INDEX]);
                LeftCoherenceValues.Add(((float[]) t.result["leftCoherences"])[LOW_INDEX]);
                RightCoherenceValues.Add(((float[]) t.result["rightCoherences"])[LOW_INDEX]);
            }

            // Calculate coherence median values
            float kMedBoth = BothCoherenceValues.Take(20).Median();
            float kMedBothLow = 0.5f * kMedBoth < 0.12 ? 0.12f : 0.5f * kMedBoth;
            float kMedBothHigh = 2.0f * kMedBoth > 0.5 ? 0.5f : 2.0f * kMedBoth;
            Coherences["both"] = new float[] { kMedBothLow, kMedBothHigh };

            float kMedLeft = LeftCoherenceValues.Take(20).Median();
            float kMedLeftLow = 0.5f * kMedLeft < 0.12 ? 0.12f : 0.5f * kMedLeft;
            float kMedLeftHigh = 2.0f * kMedLeft > 0.5 ? 0.5f : 2.0f * kMedLeft;
            Coherences["left"] = new float[] { kMedLeftLow, kMedLeftHigh };

            float kMedRight = RightCoherenceValues.Take(20).Median();
            float kMedRightLow = 0.5f * kMedRight < 0.12 ? 0.12f : 0.5f * kMedRight;
            float kMedRightHigh = 2.0f * kMedRight > 0.5 ? 0.5f : 2.0f * kMedRight;
            Coherences["right"] = new float[] { kMedRightLow, kMedRightHigh };

            Debug.Log("Coherences: L " + Coherences["left"][0] + "," + Coherences["left"][1] + " | R " + Coherences["right"][0] + "," + Coherences["right"][1] + " | B " + Coherences["both"][0] + "," + Coherences["both"][1]);
        }

        // Switch the active eye every fixed number of trials
        if ((Session.instance.CurrentTrial.numberInBlock - 1) % EYE_BLOCK_SIZE == 0)
        {
            if (cameraManager.GetActiveField() == CameraManager.VisualField.Left)
            {
                // Switch from left to right
                cameraManager.SetActiveField(CameraManager.VisualField.Right);
            }
            else if (cameraManager.GetActiveField() == CameraManager.VisualField.Right)
            {
                // Switch from right to left
                cameraManager.SetActiveField(CameraManager.VisualField.Left);
            }
            else
            {
                // Randomly select a starting field
                cameraManager.SetActiveField(UnityEngine.Random.value > 0.5f ? CameraManager.VisualField.Left : CameraManager.VisualField.Right);
            }
        }

        // Select the coherence value depending on active camera and difficulty
        ActiveCoherences = Coherences["both"];
        CameraManager.VisualField activeField = cameraManager.GetActiveField();
        if (activeField == CameraManager.VisualField.Left)
        {
            // Left eye presentation
            ActiveCoherences = Coherences["left"];
            Session.instance.CurrentTrial.result["cameraLayout"] = 0;
        }
        else if (activeField == CameraManager.VisualField.Right)
        {
            // Right eye presentation
            ActiveCoherences = Coherences["right"];
            Session.instance.CurrentTrial.result["cameraLayout"] = 1;
        }
        else
        {
            // Both eye presentation
            Session.instance.CurrentTrial.result["cameraLayout"] = 2;
        }
        int SelectedCoherence = UnityEngine.Random.value > 0.5f ? 0 : 1;
        stimulusManager.SetCoherence(ActiveCoherences[SelectedCoherence]);

        // Clone and store coherence values
        Session.instance.CurrentTrial.result["combinedCoherences"] = Coherences["both"].Clone();
        Session.instance.CurrentTrial.result["leftCoherences"] = Coherences["left"].Clone();
        Session.instance.CurrentTrial.result["rightCoherences"] = Coherences["right"].Clone();

        // Set the reference direction randomly
        float dotDirection = UnityEngine.Random.value > 0.5f ? 0.0f : (float) Math.PI;
        stimulusManager.SetDirection(dotDirection);
    }

    public void RunTrial(Trial trial)
    {
        ActiveBlock = trial.block.number;
        if (ActiveBlock == InstructionBlockIndex)
        {
            SetupInstructions();
            StartCoroutine(DisplayStimuli("instructions"));
        }
        else if (ActiveBlock == CalibrationBlockIndex)
        {
            SetupMotion();
            StartCoroutine(DisplayStimuli("calibration"));
        }
        else if (ActiveBlock == MainBlockIndex)
        {
            SetupMotion();
            StartCoroutine(DisplayStimuli("main"));
        }
    }

    private IEnumerator DisplayStimuli(string stimuli)
    {
        // Reset all displayed stimuli
        stimulusManager.SetVisibleAll(false);

        if (stimuli == "instructions")
        {
            uiManager.SetVisible(true);
            uiManager.SetHeader("Instructions");
            uiManager.SetLeftButton(false, true, "Back");
            uiManager.SetRightButton(true, true, "Next");

            yield return StartCoroutine(WaitSeconds(0.5f, true));
        }
        else if (stimuli == "calibration")
        {
            // Fixation (1 second)
            stimulusManager.SetVisible("fixation", true);
            yield return StartCoroutine(WaitSeconds(1.0f, true));
            stimulusManager.SetVisible("fixation", false);

            // Motion (1.5 seconds)
            stimulusManager.SetVisible("motion", true);
            yield return StartCoroutine(WaitSeconds(1.5f, true));
            stimulusManager.SetVisible("motion", false);

            // Decision (wait)
            stimulusManager.SetVisible("decision", true);
            EnableInput(true);
        }
        else if (stimuli == "main")
        {
            // Fixation (1 second)
            stimulusManager.SetVisible("fixation", true);
            yield return StartCoroutine(WaitSeconds(1.0f, true));
            stimulusManager.SetVisible("fixation", false);

            // Motion (1.5 seconds)
            stimulusManager.SetVisible("motion", true);
            yield return StartCoroutine(WaitSeconds(1.5f, true));
            stimulusManager.SetVisible("motion", false);

            // Decision (wait)
            stimulusManager.SetVisible("decision", true);
            EnableInput(true);
        }
        else if (stimuli == "confidence")
        {
            // Confidence (1 second)
            yield return StartCoroutine(WaitSeconds(1.0f, true));

            EnableInput(true);
        }
        else if (stimuli == "feedback_correct")
        {
            stimulusManager.SetVisible("feedback_correct", true);
            yield return StartCoroutine(WaitSeconds(1.0f, true));
            stimulusManager.SetVisible("feedback_correct", false);
            EndTrial();
        }
        else if (stimuli == "feedback_incorrect")
        {
            stimulusManager.SetVisible("feedback_incorrect", true);
            yield return StartCoroutine(WaitSeconds(1.0f, true));
            stimulusManager.SetVisible("feedback_incorrect", false);
            EndTrial();
        }
    }

    /// <summary>
    /// Wrapper function to handle input responses
    /// </summary>
    private void HandleExperimentInput(string selection)
    {
        // If `selectedDirection` is empty, this is reference direction input
        if (!Session.instance.CurrentTrial.result.ContainsKey("selectedDirection"))
        {
            // Store the selection value
            Session.instance.CurrentTrial.result["selectedDirection"] = selection;

            // Determine if a correct response was made
            Session.instance.CurrentTrial.result["selectedCorrectDirection"] = false;
            if (selection == "left" && stimulusManager.GetDirection() == (float) Math.PI)
            {
                Session.instance.CurrentTrial.result["selectedCorrectDirection"] = true;
            }
            else if (selection == "right" && stimulusManager.GetDirection() == 0.0f)
            {
                Session.instance.CurrentTrial.result["selectedCorrectDirection"] = true;
            }

            if (ActiveBlock == CalibrationBlockIndex)
            {
                // If in the calibration stage, adjust the coherence value
                if ((bool) Session.instance.CurrentTrial.result["selectedCorrectDirection"] == true)
                {
                    // Adjust coherence if two consecutive correct "calibration" trials
                    if (Session.instance.CurrentTrial.numberInBlock > 1)
                    {
                        Trial PreviousTrial = Session.instance.CurrentBlock.GetRelativeTrial(Session.instance.CurrentTrial.numberInBlock - 1);
                        if ((bool) Session.instance.CurrentTrial.result["selectedCorrectDirection"] == true &&
                            (bool) PreviousTrial.result["selectedCorrectDirection"] == true)
                        {
                            // Modify "both", grouped coherence
                            Coherences["both"][LOW_INDEX] -= 0.01f;
                            Coherences["both"][HIGH_INDEX] -= 0.01f;

                            // Modify individual coherences depending on active visual field
                            if (cameraManager.GetActiveField() == CameraManager.VisualField.Left)
                            {
                                Coherences["left"][LOW_INDEX] -= 0.01f;
                                Coherences["left"][HIGH_INDEX] -= 0.01f;
                            }
                            else if (cameraManager.GetActiveField() == CameraManager.VisualField.Right)
                            {
                                Coherences["right"][LOW_INDEX] -= 0.01f;
                                Coherences["right"][HIGH_INDEX] -= 0.01f;
                            }
                        }
                    }
                    // StartCoroutine(DisplayStimuli("feedback_correct"));
                }
                else
                {
                    // Adjust coherence
                    // Modify "both", grouped coherence
                    Coherences["both"][LOW_INDEX] += 0.01f;
                    Coherences["both"][HIGH_INDEX] += 0.01f;

                    // Modify individual coherences depending on active visual field
                    if (cameraManager.GetActiveField() == CameraManager.VisualField.Left)
                    {
                        Coherences["left"][LOW_INDEX] += 0.01f;
                        Coherences["left"][HIGH_INDEX] += 0.01f;
                    }
                    else if (cameraManager.GetActiveField() == CameraManager.VisualField.Right)
                    {
                        Coherences["right"][LOW_INDEX] += 0.01f;
                        Coherences["right"][HIGH_INDEX] += 0.01f;
                    }
                    // StartCoroutine(DisplayStimuli("feedback_incorrect"));
                }
            }

            // Display the confidence selection or end the trial
            if ((ActiveBlock == CalibrationBlockIndex || ActiveBlock == MainBlockIndex) &&
                Session.instance.CurrentTrial.numberInBlock % CONFIDENCE_BLOCK_SIZE == 0)
            {
                StartCoroutine(DisplayStimuli("confidence"));
            }
            else
            {
                EndTrial();
            }
        }
        else if (!Session.instance.CurrentTrial.result.ContainsKey("confidenceSelection"))
        {
            Debug.Log("Confidence value selection");
            EndTrial();
        }
    }

    public void EndTrial()
    {
        // Tidy up after specific blocks
        if (ActiveBlock == InstructionBlockIndex)
        {
            uiManager.SetVisible(false);
        }

        Session.instance.EndCurrentTrial();
        Session.instance.BeginNextTrial();
    }

    private void EnableInput(bool state)
    {
        InputEnabled = state;
    }

    private IEnumerator WaitSeconds(float seconds, bool disableInput = false, Action callback = null)
    {
        if (disableInput) EnableInput(false);
        yield return new WaitForSeconds(seconds);
        if (disableInput) EnableInput(true);

        // Run callback function
        callback?.Invoke();
    }

    void Update()
    {
        if (InputEnabled && InputReset)
        {
            // Left-side controls
            if (OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger) > 0.8f || Input.GetKeyDown(KeyCode.Alpha2))
            {
                if (ActiveBlock == InstructionBlockIndex)
                {
                    if (uiManager.HasPreviousPage())
                    {
                        // If pagination has next page, advance
                        uiManager.PreviousPage();
                        EnableInput(true);

                        // Update the "Next" button if the last page
                        if (uiManager.HasNextPage())
                        {
                            uiManager.SetRightButton(true, true, "Next");
                        }
                    }
                }
                else if (ActiveBlock == CalibrationBlockIndex || ActiveBlock == MainBlockIndex)
                {
                    // "Left" direction selected
                    HandleExperimentInput("left");
                }
                InputReset = false;
            }

            // Right-side controls
            if (OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger) > 0.8f || Input.GetKeyDown(KeyCode.Alpha7))
            {
                if (ActiveBlock == InstructionBlockIndex)
                {
                    if (uiManager.HasNextPage())
                    {
                        // If pagination has next page, advance
                        uiManager.NextPage();
                        EnableInput(true);

                        // Update the "Next" button if the last page
                        if (!uiManager.HasNextPage())
                        {
                            uiManager.SetRightButton(true, true, "Continue");
                        }
                    }
                    else
                    {
                        EndTrial();
                    }
                }
                else if (ActiveBlock == CalibrationBlockIndex || ActiveBlock == MainBlockIndex)
                {
                    // "Right" direction selected
                    HandleExperimentInput("right");
                }
                InputReset = false;
            }
        }

        if (InputEnabled && InputReset == false)
        {
            // Reset input state to prevent holding buttons to repeatedly select options
            if (OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger) == 0.0f &&
                OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger) == 0.0f &&
                Input.GetKeyDown(KeyCode.Alpha2) == false &&
                Input.GetKeyDown(KeyCode.Alpha7) == false)
            {
                InputReset = true;
            }
        }
    }
}
