using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

using UXF;

public class ExperimentManager : MonoBehaviour
{
    readonly int InstructionTrials = 1;
    readonly int InstructionBlock = 1;

    readonly int CalibrationTrials = 20;
    readonly int CalibrationBlock = 2; // Expected index of the "Calibration"-type block

    readonly int MainTrials = 20;
    readonly int MainBlock = 3; // Expected index of the "Main"-type block

    private int ActiveBlock = 1;

    StimulusManager stimulusManager;
    UIManager uiManager;
    CameraManager cameraManager;

    // Input parameters
    bool InputEnabled = false;
    private bool InputReset = true;

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

        // Setup the UI manager with instructions
        uiManager.EnablePagination(true);
        List<string> Instructions = new List<string>{
            "You are about to start the task. Before you start, please let the facilitator know if the headset feels uncomfortable or you cannot read this text.\n\nWhen you are ready and comfortable, press the right controller trigger to select 'Next >' and continue.",
            "These practice trials are similar to the actual trials, except the moving dots will be displayed a few seconds longer.\n\nPractice watching the dots and observing the appearance of the task.\n\nUse the triggers on the left and right controllers to interact with the task."
        };
        uiManager.SetPages(Instructions);

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

    public void RunTrial(Trial trial)
    {
        ActiveBlock = trial.block.number;
        stimulusManager.SetVisibleAll(false);
        if (ActiveBlock == InstructionBlock)
        {
            Debug.Log("This is an \"Instruction\"-type trial.");
        }
        else if (ActiveBlock == CalibrationBlock)
        {
            Debug.Log("This is a \"Calibration\"-type trial.");
        }
        else if (ActiveBlock == MainBlock)
        {
            Debug.Log("This is a \"Main\"-type trial.");
        }
        StartCoroutine(DisplayStimuli());
    }

    private IEnumerator DisplayStimuli()
    {
        if (ActiveBlock == InstructionBlock)
        {
            uiManager.SetVisible(true);
            uiManager.SetHeader("Instructions");
            uiManager.SetLeftButton(false, true, "Back");
            uiManager.SetRightButton(true, true, "Next");

            yield return StartCoroutine(WaitSeconds(0.5f, true));
        }
        else if (ActiveBlock == CalibrationBlock)
        {
            // Fixation (1 second)
            stimulusManager.SetVisible("fixation", true);
            yield return StartCoroutine(WaitSeconds(1.0f, true));
            stimulusManager.SetVisible("fixation", false);

            // Motion (1.5 seconds)
            stimulusManager.SetVisible("motion", true);
            yield return StartCoroutine(WaitSeconds(2.0f, true));
            stimulusManager.SetVisible("motion", false);

            // Decision (wait)
            stimulusManager.SetVisible("decision", true);
            EnableInput(true);
        }
    }

    /// <summary>
    /// Wrapper function to handle input responses
    /// </summary>
    private void HandleExperimentInput(string selection)
    {
        // Store the selection value
        Session.instance.CurrentTrial.result["referenceSelection"] = selection;
        Debug.Log("Selected: " + selection);
    }

    public void EndTrial()
    {
        // Tidy up after specific blocks
        if (ActiveBlock == InstructionBlock)
        {
            uiManager.SetVisible(false);
        }

        // Reset visual field
        cameraManager.SetActiveField(CameraManager.VisualField.Both);

        Session.instance.EndCurrentTrial();
        Session.instance.BeginNextTrial();
    }

    private void EnableInput(bool state)
    {
        InputEnabled = state;
    }

    private IEnumerator WaitSeconds(float seconds, bool disableInput = false, Action callback = null)
    {
        Debug.Log("Waiting " + seconds + " seconds...");
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
                if (ActiveBlock == InstructionBlock)
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
                else if (ActiveBlock == CalibrationBlock)
                {
                    // "Left" direction selected
                    HandleExperimentInput("left");
                    EndTrial();
                }
                InputReset = false;
            }

            // Right-side controls
            if (OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger) > 0.8f || Input.GetKeyDown(KeyCode.Alpha7))
            {
                if (ActiveBlock == InstructionBlock)
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
                else if (ActiveBlock == CalibrationBlock)
                {
                    // "Right" direction selected
                    HandleExperimentInput("right");
                    EndTrial();
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
