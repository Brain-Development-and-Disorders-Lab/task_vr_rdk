using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

using UXF;

public class ExperimentManager : MonoBehaviour
{
    int CalibrationTrials = 20;
    readonly int CalibrationBlock = 1; // Expected index of the "Calibration"-type block

    int MainTrials = 20;
    readonly int MainBlock = 2; // Expected index of the "Main"-type block

    StimulusManager stimulusManager;

    /// <summary>
    /// Generate the experiment flow
    /// </summary>
    /// <param name="session"></param>
    public void GenerateExperiment(Session session)
    {
        // Create trial blocks
        session.CreateBlock(CalibrationTrials);
        session.CreateBlock(MainTrials);

        // Store reference to `StimulusManager` class
        stimulusManager = GetComponent<StimulusManager>();
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

    /// <summary>
    /// Setup a trial depending on the `Session` type
    /// </summary>
    public void SetupTrial(Trial trial)
    {
        if (trial.block.number == CalibrationBlock)
        {
            Debug.Log("This is a \"Calibration\"-type trial.");
            stimulusManager.SetVisible("fixation", false);
            stimulusManager.SetVisible("decision", false);
            stimulusManager.SetVisible("motion", true);
        }
        else if (trial.block.number == MainBlock)
        {
            Debug.Log("This is a \"Main\"-type trial.");
        }
    }
}
