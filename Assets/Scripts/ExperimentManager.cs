using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

// add the UXF namespace
using UXF;

public class ExperimentManager : MonoBehaviour
{
    /// <summary>
    /// Generate the experiment timeline
    /// </summary>
    /// <param name="session"></param>
    public void GenerateExperiment(Session session)
    {
        int numMainTrials = 1;
        Block mainBlock = session.CreateBlock(numMainTrials);
    }

    /// <summary>
    /// Commence the experiment
    /// </summary>
    /// <param name="session"></param>
    public void BeginExperiment(Session session)
    {
        session.BeginNextTrial();
    }

    /// <summary>
    /// Close the application, perform any cleanup if required
    /// </summary>
    public void QuitApplication()
    {
        Application.Quit();
    }
}