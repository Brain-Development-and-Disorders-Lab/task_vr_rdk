using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

using UXF;

public class ExperimentManager : MonoBehaviour
{
    public void GenerateExperiment(Session session)
    {
        int numMainTrials = 1;
        Block mainBlock = session.CreateBlock(numMainTrials);
    }

    public void BeginExperiment(Session session)
    {
        session.BeginNextTrial();
    }

    public void QuitExperiment()
    {
        Application.Quit();
    }
}