using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

// add the UXF namespace
using UXF;

public class SessionGenerator : MonoBehaviour
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
}