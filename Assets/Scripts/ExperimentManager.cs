﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using UXF;
using MathNet.Numerics.Statistics;

// Custom namespaces
using Calibration;
using Stimuli;
using Utilities;

public class ExperimentManager : MonoBehaviour
{
    // Loading screen object, parent object that contains all loading screen components
    public GameObject LoadingScreen;

    // Define the types of blocks that occur during the experiment timeline
    enum BlockType
    {
        Setup = 1,
        Instructions_Introduction = 2,
        Training_Trials_Binocular = 3,
        Main_Trials_Binocular = 4,
        Training_Trials_Monocular = 5,
        Main_Trials_Monocular = 6,
        Training_Trials_Lateralized = 7,
        Main_Trials_Lateralized = 8,
        PostMain = 9,
    };

    // Set the number of trials within a specific block in the experiment timeline
    enum BlockLength
    {
        Setup = 1,
        Instructions_Introduction = 1, // Welcome instructions, includes tutorial instructions
        Training_Trials_Binocular = 30, // Training trials, central presentation to both eyes
        Main_Trials_Binocular = 50,
        Training_Trials_Monocular = 30, // Training trials, central presentation to one eye
        Main_Trials_Monocular = 50,
        Training_Trials_Lateralized = 120, // Training trials, lateralized presentation to one eye
        Main_Trials_Lateralized = 200,
        PostMain = 1,
    };

    // Define the experiment timeline using BlockType values
    readonly List<BlockType> ExperimentTimeline = new() {
        // BlockType.Setup,
        BlockType.Instructions_Introduction,
        BlockType.Training_Trials_Binocular,
        BlockType.Main_Trials_Binocular,
        BlockType.Training_Trials_Monocular,
        BlockType.Main_Trials_Monocular,
        BlockType.Training_Trials_Lateralized,
        BlockType.Main_Trials_Lateralized,
        BlockType.PostMain,
    };

    private BlockType ActiveBlock; // Store the currently active Block
    private CameraManager.VisualField ActiveVisualField; // Variable to store the active visual field

    // Coherence data structure
    private Dictionary<string, float[]> Coherences = new()
    {
        { "both_eyes", new float[]{0.2f, 0.2f} },
        { "left_eye", new float[]{0.2f, 0.2f} },
        { "right_eye", new float[]{0.2f, 0.2f} },
    };
    private float[] ActiveCoherences;
    private readonly int LOW_INDEX = 0; // Index of low coherence value
    private readonly int HIGH_INDEX = 1; // Index of high coherence value
    private readonly int EYE_BLOCK_SIZE = 10; // Number of trials per eye

    // Timing variables
    private readonly float FIXATION_DURATION = 0.5f;
    private readonly float PRE_DISPLAY_DURATION = 0.5f;
    private readonly float DISPLAY_DURATION = 0.180f; // 180 milliseconds

    // Store references to Manager classes
    StimulusManager stimulusManager;
    UIManager uiManager;
    CameraManager cameraManager;
    CalibrationManager calibrationManager;

    // Store references to EyePositionTracker instances
    public EyePositionTracker LeftEyeTracker;
    public EyePositionTracker RightEyeTracker;
    private int FixationMeasurementCounter = 0; // Counter for number of fixation measurements
    private readonly int RequireFixationMeasurements = 48; // Required on-target fixation measurements

    // Input parameters
    private bool InputEnabled = false; // Input is accepted
    private bool InputReset = true; // Flag to prevent input being held down
    public bool RequireFixation = true; // Require participant to be fixation on center before trial begins
    private InputState LastInputState; // Prior frame input state

    // Input button slider GameObjects and variables
    private readonly float TriggerThreshold = 0.8f;
    private readonly float ButtonSliderThreshold = 0.99f;
    private readonly float ButtonHoldFactor = 2.0f;

    /// <summary>
    /// Generate the experiment flow
    /// </summary>
    /// <param name="session"></param>
    public void GenerateExperiment(Session session)
    {
        // Validate the "BlockType" and "BlockLength" variables are the same length
        if (Enum.GetNames(typeof(BlockType)).Length != Enum.GetNames(typeof(BlockLength)).Length)
        {
            Debug.LogWarning("\"BlockType\" length does not match \"BlockLength\" length. Timline will not generate correctly.");
        }

        // Validate the "BlockType" and "BlockLength" variables contain the same entries
        foreach (string blockName in Enum.GetNames(typeof(BlockType)))
        {
            if (!Enum.GetNames(typeof(BlockLength)).Contains(blockName))
            {
                Debug.LogWarning("\"BlockLength\" does not contain block entry: " + blockName);
            }
        }

        // Generate all trials depending on BlockTypes included in experiment timeline
        foreach (BlockType block in ExperimentTimeline)
        {
            if (Enum.IsDefined(typeof(BlockLength), block.ToString()))
            {
                Enum.TryParse(block.ToString(), out BlockLength length);
                session.CreateBlock((int)length);
            }
            else
            {
                Debug.LogWarning("BlockType \"" + "\" does not have a corresponding BlockLength value.");
            }
        }

        // Store reference to other classes
        stimulusManager = GetComponent<StimulusManager>();
        uiManager = GetComponent<UIManager>();
        cameraManager = GetComponent<CameraManager>();
        calibrationManager = GetComponent<CalibrationManager>();

        // Update the CameraManager value for the aperture offset to be the stimulus radius
        cameraManager.SetStimulusRadius(stimulusManager.GetApertureWidth() / 2.0f);
    }

    /// <summary>
    /// Start the experiment by triggering the next trial
    /// </summary>
    /// <param name="session"></param>
    public void BeginExperiment(Session session)
    {
        // If a loading screen was specified, disable / hide it
        if (LoadingScreen)
        {
            LoadingScreen.SetActive(false);
        }

        // Start the first trial of the Session
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
    /// Setup the "Instructions" screen presented to participants after any headset calibration operations.
    /// </summary>
    private void SetupInstructions()
    {
        // Setup the UI manager with instructions
        uiManager.EnablePagination(true);
        List<string> Instructions = new List<string>{
            "Before continuing, ensure you are able to read this text easily.\n\nIf not, go ahead and adjust the headset placement. The rear of the headset should sit higher than the front of the headset, and the front pad above the lenses should be resting on your forehead.\n\n\nPress <b>Right Trigger</b> to select <b>Next</b> and continue.",
            "During the task, a small cross will be visible in the center of the screen.\n\nYou must maintain focus on this cross whenever it is visible.\n\n\nPress <b>Right Trigger</b> to select <b>Next</b> and continue, or press <b>Left Trigger</b> to select <b>Back</b>.",
            "While focusing on the cross, a field of moving dots will appear very briefly around the cross.\n\nSome of the dots will move only up or only down, and the rest of the dots will move randomly as a distraction.\n\n\nPress <b>Right Trigger</b> to select <b>Next</b> and continue, or press <b>Left Trigger</b> to select <b>Back</b>.",
            "After viewing the dots, you will be asked if you thought the dots moving together moved up or down.\n\nYou will have four options to choose from:\n<b>Up - Very Confident (Left Trigger)</b>\n<b>Up - Somewhat Confident (X)</b>\n<b>Down - Somewhat Confident (A)</b>\n<b>Down - Very Confident (Right Trigger)</b>\n\nPress <b>Right Trigger</b> to select <b>Next</b> and continue, or press <b>Left Trigger</b> to select <b>Back</b>.",
            "You <b>must</b> select one of the four options, the one which best represents your decision and how confident you were in your decision. You will need to hold the button for an option approximately 1 second to select it.\n\nYou are about to start the task.\n\n\nWhen you are ready and comfortable, press <b>Right Trigger</b> to select <b>Continue</b> and begin."
        };
        uiManager.SetPages(Instructions);

        cameraManager.SetActiveField(CameraManager.VisualField.Both);
    }

    /// <summary>
    /// Setup operations to perform prior to any dot motion stimuli. Also responsible for generating coherence values
    /// after the calibration stages.
    /// </summary>
    private void SetupMotion()
    {
        // Setup performed at the start of each "Main_" type trial
        if ((ActiveBlock == BlockType.Main_Trials_Binocular ||
            ActiveBlock == BlockType.Main_Trials_Monocular ||
            ActiveBlock == BlockType.Main_Trials_Lateralized) && Session.instance.CurrentTrial.numberInBlock == 1)
        {
            // Calculate the coherences to be used in the actual trials, use median of last 20 calibration trials
            int CalibrationBlockIndex = 1; // Non-zero indexed
            // Get the block of trials containing the staircase results
            if (ActiveBlock == BlockType.Main_Trials_Binocular)
            {
                CalibrationBlockIndex += ExperimentTimeline.IndexOf(BlockType.Training_Trials_Binocular);
            }
            else if (ActiveBlock == BlockType.Main_Trials_Monocular)
            {
                CalibrationBlockIndex += ExperimentTimeline.IndexOf(BlockType.Training_Trials_Monocular);
            }
            else if (ActiveBlock == BlockType.Main_Trials_Lateralized)
            {
                CalibrationBlockIndex += ExperimentTimeline.IndexOf(BlockType.Training_Trials_Lateralized);
            }
            List<Trial> CalibrationTrials = Session.instance.GetBlock(CalibrationBlockIndex).trials;
            CalibrationTrials.Reverse();

            // Create a warning if less than 20 calibration trials are being used
            if (CalibrationTrials.Count < 20)
            {
                Debug.LogWarning("Less than 20 calibration trials are being used to generate coherence.");
            }

            // Calibration trials have the same coherence for low and high
            List<float> BothCoherenceValues = new();
            List<float> LeftCoherenceValues = new();
            List<float> RightCoherenceValues = new();
            foreach (Trial t in CalibrationTrials)
            {
                // Get each coherence value, split by "," token and cast back to float
                BothCoherenceValues.Add(float.Parse(((string)t.result["combinedCoherences"]).Split(",")[LOW_INDEX]));
                LeftCoherenceValues.Add(float.Parse(((string)t.result["leftCoherences"]).Split(",")[LOW_INDEX]));
                RightCoherenceValues.Add(float.Parse(((string)t.result["rightCoherences"]).Split(",")[LOW_INDEX]));
            }

            // Calculate coherence median values
            float kMedBoth = BothCoherenceValues.Take(20).Median();
            float kMedBothLow = 0.5f * kMedBoth < 0.12 ? 0.12f : 0.5f * kMedBoth;
            float kMedBothHigh = 2.0f * kMedBoth > 0.5 ? 0.5f : 2.0f * kMedBoth;
            Coherences["both_eyes"] = new float[] { kMedBothLow, kMedBothHigh };

            float kMedLeft = LeftCoherenceValues.Take(20).Median();
            float kMedLeftLow = 0.5f * kMedLeft < 0.12 ? 0.12f : 0.5f * kMedLeft;
            float kMedLeftHigh = 2.0f * kMedLeft > 0.5 ? 0.5f : 2.0f * kMedLeft;
            Coherences["left_eye"] = new float[] { kMedLeftLow, kMedLeftHigh };

            float kMedRight = RightCoherenceValues.Take(20).Median();
            float kMedRightLow = 0.5f * kMedRight < 0.12 ? 0.12f : 0.5f * kMedRight;
            float kMedRightHigh = 2.0f * kMedRight > 0.5 ? 0.5f : 2.0f * kMedRight;
            Coherences["right_eye"] = new float[] { kMedRightLow, kMedRightHigh };
        }

        // Switch the active eye every fixed number of trials
        if ((Session.instance.CurrentTrial.numberInBlock - 1) % EYE_BLOCK_SIZE == 0)
        {
            bool LateralizedState = ActiveBlock != BlockType.Training_Trials_Monocular;
            if (cameraManager.GetActiveField() == CameraManager.VisualField.Left)
            {
                // Switch from left to right
                cameraManager.SetActiveField(CameraManager.VisualField.Right, LateralizedState);
            }
            else if (cameraManager.GetActiveField() == CameraManager.VisualField.Right)
            {
                // Switch from right to left
                cameraManager.SetActiveField(CameraManager.VisualField.Left, LateralizedState);
            }
            else
            {
                // Randomly select a starting field
                cameraManager.SetActiveField(UnityEngine.Random.value > 0.5f ? CameraManager.VisualField.Left : CameraManager.VisualField.Right, LateralizedState);
            }

            // Store the active visual field locally, since it is changed to present the confidence screen
            ActiveVisualField = cameraManager.GetActiveField();
        }

        // Select the coherence value depending on active camera and difficulty
        ActiveCoherences = Coherences["both_eyes"];
        CameraManager.VisualField activeField = cameraManager.GetActiveField();
        if (activeField == CameraManager.VisualField.Left)
        {
            // Left eye presentation
            ActiveCoherences = Coherences["left_eye"];
            Session.instance.CurrentTrial.result["cameraLayout"] = 0;
        }
        else if (activeField == CameraManager.VisualField.Right)
        {
            // Right eye presentation
            ActiveCoherences = Coherences["right_eye"];
            Session.instance.CurrentTrial.result["cameraLayout"] = 1;
        }
        else
        {
            // Both eye presentation
            Session.instance.CurrentTrial.result["cameraLayout"] = 2;
        }
        int SelectedCoherence = UnityEngine.Random.value > 0.5f ? 0 : 1;
        stimulusManager.SetCoherence(ActiveCoherences[SelectedCoherence]);

        // Clone and store coherence values as a string, separated by "," token
        Session.instance.CurrentTrial.result["combinedCoherences"] = Coherences["both_eyes"][0] + "," + Coherences["both_eyes"][1];
        Session.instance.CurrentTrial.result["leftCoherences"] = Coherences["left_eye"][0] + "," + Coherences["left_eye"][1];
        Session.instance.CurrentTrial.result["rightCoherences"] = Coherences["right_eye"][0] + "," + Coherences["right_eye"][1];

        // Set the reference direction randomly
        float dotDirection = UnityEngine.Random.value > 0.5f ? (float)Math.PI / 2 : (float)Math.PI * 3 / 2;
        stimulusManager.SetDirection(dotDirection);
        Session.instance.CurrentTrial.result["referenceDirection"] = stimulusManager.GetDirection() == (float)Math.PI / 2 ? "up" : "down";

        // Store the standard motion duration value (1.5 seconds)
        Session.instance.CurrentTrial.result["motionDuration"] = 1.5f;

        // Setup the UI
        uiManager.EnablePagination(false);
    }

    /// <summary>
    /// Store timestamps and locale metadata before presenting the stimuli associated with a Trial.
    /// </summary>
    /// <param name="trial">UXF Trial object representing the current trial</param>
    public void RunTrial(Trial trial)
    {
        // Store local date and time data
        Session.instance.CurrentTrial.result["localDate"] = DateTime.Now.ToShortDateString();
        Session.instance.CurrentTrial.result["localTime"] = DateTime.Now.ToShortTimeString();
        Session.instance.CurrentTrial.result["localTimezone"] = TimeZoneInfo.Local.DisplayName;
        Session.instance.CurrentTrial.result["trialStart"] = Time.time;

        // Update the currently active block
        ActiveBlock = ExperimentTimeline[trial.block.number - 1];

        // Based on the active block, run any required setup operations and present the required stimuli
        if (ActiveBlock == BlockType.Instructions_Introduction)
        {
            SetupInstructions();
        }
        else if (ActiveBlock == BlockType.Training_Trials_Binocular || ActiveBlock == BlockType.Training_Trials_Monocular || ActiveBlock == BlockType.Training_Trials_Lateralized)
        {
            SetupMotion();
        }

        // Display the active block
        StartCoroutine(DisplayBlock(ActiveBlock));
    }

    /// <summary>
    /// Switch-like function presenting a specified stimulus
    /// </summary>
    /// <param name="stimuli">A valid stimulus type, specified as a string</param>
    /// <returns></returns>
    private IEnumerator DisplayBlock(BlockType block)
    {
        // Reset all displayed stimuli and UI
        stimulusManager.SetVisibleAll(false);
        uiManager.SetVisible(false);

        // Store the displayed stimuli type if not a "feedback_"-type stimulus
        Session.instance.CurrentTrial.result["name"] = Enum.GetName(typeof(BlockType), block);
        Debug.Log("Current block: " + Session.instance.CurrentTrial.result["name"]);

        if (block == BlockType.Instructions_Introduction)
        {
            uiManager.SetVisible(true);
            uiManager.SetHeader("Instructions");
            uiManager.SetLeftButtonState(false, true, "Back");
            uiManager.SetRightButtonState(true, true, "Next");

            // Input delay
            yield return StartCoroutine(WaitSeconds(0.25f, true));
        }
        else if (block == BlockType.Setup)
        {
            uiManager.SetVisible(true);
            uiManager.SetHeader("Eye-Tracking Setup");
            uiManager.SetBody("You will be shown a red dot in front of you. Follow the dot movement with your eyes. After a brief series of movements, the calibration will automatically end and you will be shown the task instructions.\n\nPress the right controller trigger to select <b>Start</b>.");
            uiManager.SetLeftButtonState(false, false, "");
            uiManager.SetRightButtonState(true, true, "Start");

            // Input delay
            yield return StartCoroutine(WaitSeconds(0.25f, true));
            SetInputEnabled(true);
        }
        else if (block == BlockType.Training_Trials_Binocular || block == BlockType.Main_Trials_Binocular)
        {
            // Override and set the camera to display in both eyes
            cameraManager.SetActiveField(CameraManager.VisualField.Both, false);

            yield return StartCoroutine(DisplayMotion());
        }
        else if (block == BlockType.Training_Trials_Monocular ||
            block == BlockType.Training_Trials_Lateralized ||
            block == BlockType.Main_Trials_Monocular ||
            block == BlockType.Main_Trials_Lateralized)
        {
            yield return StartCoroutine(DisplayMotion());
        }
        // else if (stimuli == "prepractice")
        // {
        //     // Override and set the camera to display in both eyes
        //     cameraManager.SetActiveField(CameraManager.VisualField.Both);

        //     uiManager.SetVisible(true);
        //     uiManager.SetHeader("Practice Trials");
        //     uiManager.SetBody("You will now complete another " + (int)BlockLength.Practice + " practice trials. After selecting a direction, the cross in the center of the circular area will briefly change color if your answer was correct or not. Green is a correct answer, red is an incorrect answer.\n\nWhen you are ready and comfortable, press the right controller trigger to select <b>Next</b> and continue.");
        //     uiManager.SetLeftButtonState(false, true, "Back");
        //     uiManager.SetRightButtonState(true, true, "Next");

        //     // Input delay
        //     yield return StartCoroutine(WaitSeconds(0.15f, true));
        //     SetInputEnabled(true);
        // }
        // else if (stimuli == "premain")
        // {
        //     // Override and set the camera to display in both eyes
        //     cameraManager.SetActiveField(CameraManager.VisualField.Both);

        //     uiManager.SetVisible(true);
        //     uiManager.SetHeader("Main Trials");
        //     uiManager.SetBody("That concludes the practice trials. You will now play " + ((int)BlockLength.Calibration + (int)BlockLength.Main) + " main trials.\n\nYou will not be shown if you answered correctly or not, but sometimes you will be asked whether you were more confident in that trial than in the previous trial.\n\nWhen you are ready and comfortable, press the right controller trigger to select <b>Next</b> and continue.");
        //     uiManager.SetLeftButtonState(false, true, "Back");
        //     uiManager.SetRightButtonState(true, true, "Next");

        //     // Input delay
        //     yield return StartCoroutine(WaitSeconds(0.15f, true));
        //     SetInputEnabled(true);
        // }
        // else if (stimuli == "postmain")
        // {
        //     // Override and set the camera to display in both eyes
        //     cameraManager.SetActiveField(CameraManager.VisualField.Both);

        //     uiManager.SetVisible(true);
        //     uiManager.SetHeader("Complete");
        //     uiManager.SetBody("That concludes all the trials of this task. Please notify the experiment facilitator, and you can remove the headset carefully after releasing the rear adjustment wheel.");
        //     uiManager.SetLeftButtonState(false, false, "Back");
        //     uiManager.SetRightButtonState(true, true, "Finish");

        //     // Input delay
        //     yield return StartCoroutine(WaitSeconds(1.0f, true));
        //     SetInputEnabled(true);
        // }
    }

    /// <summary>
    /// Utility function to display "Feedback_"-type stimuli
    /// </summary>
    /// <param name="correct">`true` to display green cross, `false` to display red cross</param>
    private IEnumerator DisplayFeedback(bool correct)
    {
        stimulusManager.SetFixationCrossColor(correct ? "green" : "red");
        stimulusManager.SetFixationCrossVisibility(true);
        stimulusManager.SetVisible(correct ? StimulusType.Feedback_Correct : StimulusType.Feedback_Incorrect, true);
        yield return StartCoroutine(WaitSeconds(1.0f, true));
        stimulusManager.SetVisible(correct ? StimulusType.Feedback_Correct : StimulusType.Feedback_Incorrect, false);
        stimulusManager.SetFixationCrossVisibility(false);
        stimulusManager.SetFixationCrossColor("white");
        EndTrial();
    }

    /// <summary>
    /// Utility function to display the dot motion stimulus and wait for a response, used in all trial types
    /// </summary>
    /// <returns></returns>
    private IEnumerator DisplayMotion()
    {
        SetInputEnabled(false);
        stimulusManager.SetFixationCrossVisibility(true);
        if (RequireFixation)
        {
            yield return new WaitUntil(() => IsFixated());
        }

        // Fixation
        stimulusManager.SetVisible(StimulusType.Fixation, true);
        yield return StartCoroutine(WaitSeconds(PRE_DISPLAY_DURATION, true));
        // Wait either for fixation or a fixed duration if fixation not required
        if (RequireFixation)
        {
            yield return new WaitUntil(() => IsFixated());
        }
        else
        {
            yield return StartCoroutine(WaitSeconds(FIXATION_DURATION, true));
        }
        stimulusManager.SetVisible(StimulusType.Fixation, false);

        // Motion
        stimulusManager.SetVisible(StimulusType.Motion, true);
        yield return StartCoroutine(WaitSeconds(DISPLAY_DURATION, true));
        stimulusManager.SetVisible(StimulusType.Motion, false);
        stimulusManager.SetFixationCrossVisibility(false);

        // Decision (wait)
        Session.instance.CurrentTrial.result["referenceStart"] = Time.time;
        stimulusManager.SetVisible(StimulusType.Decision, true);
        yield return StartCoroutine(WaitSeconds(0.15f, true));
        SetInputEnabled(true);
    }

    /// <summary>
    /// Wrapper function to handle input
    /// </summary>
    private void HandleExperimentInput(string selection)
    {
        // Handle inputs depending on what data points exist on the current trial
        if (!Session.instance.CurrentTrial.result.ContainsKey("selectedDirection"))
        {
            // If `selectedDirection` is empty, this is reference direction input
            // Store timing data
            Session.instance.CurrentTrial.result["referenceEnd"] = Time.time;
            Session.instance.CurrentTrial.result["referenceRT"] = (float)Session.instance.CurrentTrial.result["referenceEnd"] - (float)Session.instance.CurrentTrial.result["referenceStart"];

            // Store the selection value
            Session.instance.CurrentTrial.result["selectedDirection"] = selection;

            // Determine if a correct response was made
            Session.instance.CurrentTrial.result["selectedCorrectDirection"] = false;
            if ((selection == "vc_u" || selection == "sc_u") && stimulusManager.GetDirection() == (float)Math.PI / 2)
            {
                Session.instance.CurrentTrial.result["selectedCorrectDirection"] = true;
            }
            else if ((selection == "vc_d" || selection == "sc_d") && stimulusManager.GetDirection() == (float)Math.PI * 3 / 2)
            {
                Session.instance.CurrentTrial.result["selectedCorrectDirection"] = true;
            }

            if (ActiveBlock == BlockType.Training_Trials_Binocular ||
                ActiveBlock == BlockType.Training_Trials_Monocular ||
                ActiveBlock == BlockType.Training_Trials_Lateralized)
            {
                // If in the calibration stage, adjust the coherence value
                if ((bool)Session.instance.CurrentTrial.result["selectedCorrectDirection"] == true)
                {
                    // Adjust coherence if two consecutive correct "calibration" trials
                    if (Session.instance.CurrentTrial.numberInBlock > 1)
                    {
                        Trial PreviousTrial = Session.instance.CurrentBlock.GetRelativeTrial(Session.instance.CurrentTrial.numberInBlock - 1);
                        if ((bool)Session.instance.CurrentTrial.result["selectedCorrectDirection"] == true &&
                            (bool)PreviousTrial.result["selectedCorrectDirection"] == true)
                        {
                            // Modify "both_eyes", grouped coherence
                            Coherences["both_eyes"][LOW_INDEX] -= 0.01f;
                            Coherences["both_eyes"][HIGH_INDEX] -= 0.01f;

                            // Modify individual coherences depending on active visual field
                            if (cameraManager.GetActiveField() == CameraManager.VisualField.Left)
                            {
                                Coherences["left_eye"][LOW_INDEX] -= 0.01f;
                                Coherences["left_eye"][HIGH_INDEX] -= 0.01f;
                            }
                            else if (cameraManager.GetActiveField() == CameraManager.VisualField.Right)
                            {
                                Coherences["right_eye"][LOW_INDEX] -= 0.01f;
                                Coherences["right_eye"][HIGH_INDEX] -= 0.01f;
                            }
                        }
                    }
                }
                else
                {
                    // Adjust coherence
                    // Modify "both_eyes", grouped coherence
                    Coherences["both_eyes"][LOW_INDEX] += 0.01f;
                    Coherences["both_eyes"][HIGH_INDEX] += 0.01f;

                    // Modify individual coherences depending on active visual field
                    if (cameraManager.GetActiveField() == CameraManager.VisualField.Left)
                    {
                        Coherences["left_eye"][LOW_INDEX] += 0.01f;
                        Coherences["left_eye"][HIGH_INDEX] += 0.01f;
                    }
                    else if (cameraManager.GetActiveField() == CameraManager.VisualField.Right)
                    {
                        Coherences["right_eye"][LOW_INDEX] += 0.01f;
                        Coherences["right_eye"][HIGH_INDEX] += 0.01f;
                    }
                }

                // Likely that "both_eyes" coherence values could reach 0.0f or 1.0f, keep coherence within [0.0f, 1.0f]
                if (Coherences["both_eyes"][LOW_INDEX] < 0.0f) Coherences["both_eyes"][LOW_INDEX] = 0.0f;
                if (Coherences["both_eyes"][LOW_INDEX] > 1.0f) Coherences["both_eyes"][LOW_INDEX] = 1.0f;
                if (Coherences["both_eyes"][HIGH_INDEX] < 0.0f) Coherences["both_eyes"][HIGH_INDEX] = 0.0f;
                if (Coherences["both_eyes"][HIGH_INDEX] > 1.0f) Coherences["both_eyes"][HIGH_INDEX] = 1.0f;
            }

            // Some trials display additional components, otherwise end the trial
            if (ActiveBlock == BlockType.Training_Trials_Monocular)
            {
                // Display feedback during the practice trials
                if ((bool)Session.instance.CurrentTrial.result["selectedCorrectDirection"] == true)
                {
                    StartCoroutine(DisplayFeedback(true));
                }
                else
                {
                    StartCoroutine(DisplayFeedback(false));
                }
            }
            else
            {
                ResetButtons();
                EndTrial();
            }
        }
        else if (!Session.instance.CurrentTrial.result.ContainsKey("confidenceSelection"))
        {
            // If `confidenceSelection` is empty, this is a confidence input
            // Store timing data
            Session.instance.CurrentTrial.result["confidenceEnd"] = Time.time;
            Session.instance.CurrentTrial.result["confidenceRT"] = (float)Session.instance.CurrentTrial.result["confidenceEnd"] - (float)Session.instance.CurrentTrial.result["confidenceStart"];

            // Store the confidence selection
            Session.instance.CurrentTrial.result["confidenceSelection"] = selection;

            // Reset the active camera
            cameraManager.SetActiveField(ActiveVisualField);

            ResetButtons();
            EndTrial();
        }
    }

    public void EndTrial()
    {
        Session.instance.CurrentTrial.result["trialEnd"] = Time.time;
        Session.instance.EndCurrentTrial();

        try
        {
            // Proceed to the next trial
            Session.instance.BeginNextTrial();
        }
        catch (NoSuchTrialException)
        {
            // End the experiment session
            Session.instance.End();
        }
    }

    /// <summary>
    /// Set the input state, `true` allows input, `false` ignores input
    /// </summary>
    /// <param name="state">Input state</param>
    private void SetInputEnabled(bool state)
    {
        InputEnabled = state;
    }

    /// <summary>
    /// Wait for eye gaze to return to central fixation point prior to returning
    /// </summary>
    /// <returns></returns>
    private bool IsFixated()
    {
        // Get gaze estimates and the current world position
        Vector3 LeftGaze = LeftEyeTracker.GetGazeEstimate();
        Vector3 RightGaze = RightEyeTracker.GetGazeEstimate();
        Vector3 WorldPosition = stimulusManager.GetAnchor().transform.position;

        // Calculate central gaze position and adjust world position if a lateralized trial
        float GazeOffset = cameraManager.GetTotalOffset();
        if (ActiveBlock == BlockType.Training_Trials_Lateralized || ActiveBlock == BlockType.Main_Trials_Lateralized)
        {
            if (cameraManager.GetActiveField() == CameraManager.VisualField.Left)
            {
                WorldPosition.x += GazeOffset;
            }
            else if (cameraManager.GetActiveField() == CameraManager.VisualField.Right)
            {
                WorldPosition.x -= GazeOffset;
            }
        }

        float GazeThreshold = 0.5f; // Error threshold (world units)
        bool Fixated = false; // Fixated state
        if ((Mathf.Abs(LeftGaze.x - WorldPosition.x) <= GazeThreshold && Mathf.Abs(LeftGaze.y - WorldPosition.y) <= GazeThreshold) || (Mathf.Abs(RightGaze.x - WorldPosition.x) <= GazeThreshold && Mathf.Abs(RightGaze.y - WorldPosition.y) <= GazeThreshold))
        {
            // If the gaze is directed in fixation, increment the counter to signify a measurement
            FixationMeasurementCounter += 1;
        }

        if (FixationMeasurementCounter >= RequireFixationMeasurements)
        {
            // Register as fixated if the required number of measurements have been taken
            Fixated = true;
            FixationMeasurementCounter = 0;
        }

        // Return the overall fixation state
        return Fixated;
    }

    /// <summary>
    /// Utility function to block further execution until a duration has elapsed
    /// </summary>
    /// <param name="seconds">Duration to wait, measured in seconds</param>
    /// <param name="disableInput">Flag to disable input when `true`</param>
    /// <param name="callback">Function to execute at the end of the duration</param>
    /// <returns></returns>
    private IEnumerator WaitSeconds(float seconds, bool disableInput = false, Action callback = null)
    {
        if (disableInput)
        {
            SetInputEnabled(false);
        }

        yield return new WaitForSeconds(seconds);

        if (disableInput)
        {
            SetInputEnabled(true);
        }

        // Run callback function
        callback?.Invoke();
    }

    // Functions to bulk-classify BlockType values
    private bool IsIntroductionScreen()
    {
        return ActiveBlock == BlockType.Instructions_Introduction;
    }

    private bool IsTextScreen()
    {
        return IsIntroductionScreen() ||
                ActiveBlock == BlockType.PostMain;
    }

    private bool IsStimulusScreen()
    {
        return ActiveBlock == BlockType.Training_Trials_Binocular ||
                ActiveBlock == BlockType.Training_Trials_Monocular ||
                ActiveBlock == BlockType.Training_Trials_Lateralized ||
                ActiveBlock == BlockType.Main_Trials_Binocular ||
                ActiveBlock == BlockType.Main_Trials_Monocular ||
                ActiveBlock == BlockType.Main_Trials_Lateralized;
    }

    private bool IsSetupScreen()
    {
        return ActiveBlock == BlockType.Setup;
    }

    /// <summary>
    /// Input function to handle `InputState` object and update button presentation or take action depending on
    /// the active `BlockType`
    /// </summary>
    /// <param name="inputs">`InputState` object</param>
    private void ApplyInputs(InputState inputs)
    {
        ButtonSliderInput[] buttonControllers = stimulusManager.GetButtonControllers();

        // Left controller inputs
        if (inputs.L_T_State >= TriggerThreshold || inputs.X_Pressed)
        {
            if (inputs.L_T_State >= TriggerThreshold)
            {
                // Very confident up
                buttonControllers[0].SetSliderValue(buttonControllers[0].GetSliderValue() + ButtonHoldFactor * Time.deltaTime);
                if (LastInputState.L_T_State < TriggerThreshold)
                {
                    Session.instance.CurrentTrial.result["lastKeypressStart"] = Time.time;
                }
            }
            else
            {
                // Somewhat confident up
                buttonControllers[1].SetSliderValue(buttonControllers[1].GetSliderValue() + ButtonHoldFactor * Time.deltaTime);
                if (LastInputState.X_Pressed == false)
                {
                    Session.instance.CurrentTrial.result["lastKeypressStart"] = Time.time;
                }
            }

            // Check if a button has been completely held down, and continue if so
            if (buttonControllers[0].GetSliderValue() >= ButtonSliderThreshold || buttonControllers[1].GetSliderValue() >= ButtonSliderThreshold)
            {
                // Provide haptic feedback
                VRInput.SetHaptics(15.0f, 0.4f, 0.1f, true, false);
                Session.instance.CurrentTrial.result["lastKeypressEnd"] = Time.time;

                // Store appropriate response
                if (buttonControllers[0].GetSliderValue() >= ButtonSliderThreshold)
                {
                    // "Very Confident Up" selected
                    HandleExperimentInput("vc_u");
                    buttonControllers[0].SetSliderValue(0.0f);
                }
                else if (buttonControllers[1].GetSliderValue() >= ButtonSliderThreshold)
                {
                    // "Somewhat Confident Up" selected
                    HandleExperimentInput("sc_u");
                    buttonControllers[1].SetSliderValue(0.0f);
                }
            }
        }

        // Right controller inputs
        else if (inputs.R_T_State >= TriggerThreshold || inputs.A_Pressed)
        {
            if (inputs.R_T_State >= TriggerThreshold)
            {
                // Very confident down
                buttonControllers[2].SetSliderValue(buttonControllers[2].GetSliderValue() + ButtonHoldFactor * Time.deltaTime);
                if (inputs.R_T_State < TriggerThreshold)
                {
                    Session.instance.CurrentTrial.result["lastKeypressStart"] = Time.time;
                }
            }
            else
            {
                // Somewhat confident down
                buttonControllers[3].SetSliderValue(buttonControllers[3].GetSliderValue() + ButtonHoldFactor * Time.deltaTime);
                if (LastInputState.A_Pressed == false)
                {
                    Session.instance.CurrentTrial.result["lastKeypressStart"] = Time.time;
                }
            }

            if (buttonControllers[2].GetSliderValue() >= ButtonSliderThreshold || buttonControllers[3].GetSliderValue() >= ButtonSliderThreshold)
            {
                VRInput.SetHaptics(15.0f, 0.4f, 0.1f, false, true);
                Session.instance.CurrentTrial.result["lastKeypressEnd"] = Time.time;

                // Store appropriate response
                if (buttonControllers[2].GetSliderValue() >= ButtonSliderThreshold)
                {
                    // "Very Confident Down" selected
                    HandleExperimentInput("vc_d");
                    buttonControllers[2].SetSliderValue(0.0f);
                }
                else if (buttonControllers[3].GetSliderValue() >= ButtonSliderThreshold)
                {
                    // "Somewhat Confident Down" selected
                    HandleExperimentInput("sc_d");
                    buttonControllers[3].SetSliderValue(0.0f);
                }
            }
        }

        // Update the prior input state
        LastInputState = inputs;
    }

    /// <summary>
    /// Utility function to reduce the value of all active `ButtonSliderInput` instances back to `0.0f` when inactive.
    /// </summary>
    private void ButtonCooldown()
    {
        foreach (ButtonSliderInput button in stimulusManager.GetButtonControllers())
        {
            button.SetSliderValue(button.GetSliderValue() - ButtonHoldFactor / 3.0f * Time.deltaTime);
        }
    }

    /// <summary>
    /// Utility function to reset the state of all `ButtonSliderFill` buttons
    /// </summary>
    private void ResetButtons()
    {
        foreach (ButtonSliderInput button in stimulusManager.GetButtonControllers())
        {
            button.SetSliderValue(0.0f);
        }
    }

    void Update()
    {
        if (InputEnabled)
        {
            // Get the current input state across both controllers
            InputState inputs = VRInput.PollAllInput();

            if (IsStimulusScreen())
            {
                // Run the cooldown function for buttons
                ButtonCooldown();

                // Take action as specified by the inputs
                ApplyInputs(inputs);
            }
            else
            {
                // Left-side controls
                if (inputs.L_T_State > TriggerThreshold || inputs.X_Pressed)
                {
                    if (IsIntroductionScreen() && InputReset)
                    {
                        if (uiManager.HasPreviousPage())
                        {
                            // If pagination has previous page, go back
                            uiManager.PreviousPage();
                            SetInputEnabled(true);

                            // Trigger controller haptics
                            VRInput.SetHaptics(15.0f, 0.4f, 0.1f, true, false);

                            // Update the "Next" button if the last page
                            if (uiManager.HasNextPage())
                            {
                                uiManager.SetRightButtonState(true, true, "Next");
                            }
                        }
                    }
                    InputReset = false;
                }

                // Right-side controls
                if (inputs.R_T_State > TriggerThreshold || inputs.A_Pressed)
                {
                    if (IsTextScreen() && InputReset)
                    {
                        if (uiManager.HasNextPage())
                        {
                            // If pagination has next page, advance
                            uiManager.NextPage();
                            SetInputEnabled(true);

                            // Trigger controller haptics
                            VRInput.SetHaptics(15.0f, 0.4f, 0.1f, false, true);

                            // Update the "Next" button if the last page
                            if (!uiManager.HasNextPage())
                            {
                                uiManager.SetRightButtonState(true, true, "Continue");
                            }
                        }
                        else
                        {
                            // Trigger controller haptics
                            VRInput.SetHaptics(15.0f, 0.4f, 0.1f, false, true);

                            EndTrial();
                        }
                        InputReset = false;
                    }
                    else if (IsSetupScreen() && InputReset)
                    {
                        // Hide the UI
                        uiManager.SetVisible(false);

                        // Only provide haptic feedback before calibration is run
                        if (!calibrationManager.IsCalibrationActive() && !calibrationManager.CalibrationStatus())
                        {
                            // Trigger controller haptics
                            VRInput.SetHaptics(15.0f, 0.4f, 0.1f, false, true);
                        }

                        // Trigger eye-tracking calibration the end the trial
                        calibrationManager.RunCalibration(() =>
                        {
                            EndTrial();
                        });
                        InputReset = false;
                    }
                }

                // Reset input state to prevent holding buttons to repeatedly select options
                if (InputEnabled && InputReset == false && !VRInput.AnyInput())
                {
                    InputReset = true;
                }
            }
        }
    }
}
