using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

using UXF;

public class ExperimentManager : MonoBehaviour
{
    readonly int CalibrationTrials = 20;
    readonly int CalibrationBlock = 1; // Expected index of the "Calibration"-type block

    readonly int MainTrials = 20;
    readonly int MainBlock = 2; // Expected index of the "Main"-type block

    StimulusManager stimulusManager;
    CameraManager cameraManager;

    // Input parameters
    bool WaitingForInput = false;

    /// <summary>
    /// Generate the experiment flow
    /// </summary>
    /// <param name="session"></param>
    public void GenerateExperiment(Session session)
    {
        // Create trial blocks
        session.CreateBlock(CalibrationTrials);
        session.CreateBlock(MainTrials);

        // Store reference to other classes
        stimulusManager = GetComponent<StimulusManager>();
        cameraManager = GetComponent<CameraManager>();
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
        stimulusManager.SetVisibleAll(false);
        if (trial.block.number == CalibrationBlock)
        {
            Debug.Log("This is a \"Calibration\"-type trial.");
        }
        else if (trial.block.number == MainBlock)
        {
            Debug.Log("This is a \"Main\"-type trial.");
        }

        StartCoroutine(DisplayStimuli());
    }

    private IEnumerator DisplayStimuli()
    {
        stimulusManager.SetVisible("fixation", true);
        yield return StartCoroutine(WaitSeconds(0.25f));
        stimulusManager.SetVisible("fixation", false);

        stimulusManager.SetVisible("motion", true);
        yield return StartCoroutine(WaitSeconds(2.0f));
        stimulusManager.SetVisible("motion", false);

        WaitInput();
    }

    public void EndTrial()
    {
        // Reset visual field
        cameraManager.SetActiveField(CameraManager.VisualField.Both);

        Session.instance.EndCurrentTrial();
        Session.instance.BeginNextTrial();
    }

    private void WaitInput()
    {
        Debug.Log("Waiting for controller input...");
        WaitingForInput = true;
    }

    private IEnumerator WaitSeconds(float seconds, Action callback = null)
    {
        Debug.Log("Waiting " + seconds + " seconds...");
        yield return new WaitForSeconds(seconds);

        // Run callback function
        callback?.Invoke();
    }

    void Update()
    {
        // Listen for input
        if (WaitingForInput)
        {
            // End the current trial when the trigger button is pressed
            if (OVRInput.Get(OVRInput.Button.One) || Input.GetKeyDown(KeyCode.Alpha7))
            {
                WaitingForInput = false;
                EndTrial();
            }
        }
    }
}
