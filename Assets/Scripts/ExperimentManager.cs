using System;
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

    // Manage the index of specific blocks occuring in the experiment timeline
    enum BlockIndex
    {
        HeadsetSetup = 1,
        Welcome = 2,
        Tutorial = 3,
        PrePractice = 4,
        Practice = 5,
        PreMain = 6,
        Calibration = 7,
        Main = 8,
    };

    // Manage the number of trials within a specific block in the experiment timeline
    enum BlockSize
    {
        HeadsetSetup = 1,
        Welcome = 1, // Welcome instructions, includes tutorial instructions
        Tutorial = 20, // Tutorial trials
        PrePractice = 1, // Practice instructions
        Practice = 20, // Practice trials
        PreMain = 1, // Main instructions
        Calibration = 120,
        Main = 200,
    };

    private BlockIndex ActiveBlock = BlockIndex.HeadsetSetup; // Store the currently active Block

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

    // Store references to Manager classes
    StimulusManager stimulusManager;
    UIManager uiManager;
    CameraManager cameraManager;
    CalibrationManager calibrationManager;

    // Input parameters
    bool InputEnabled = false;
    private bool InputReset = true;

    // Confidence parameters
    private readonly int CONFIDENCE_BLOCK_SIZE = 2; // Number of trials to run before asking for confidence
    private CameraManager.VisualField ActiveVisualField; // Variable to store the active visual field

    /// <summary>
    /// Generate the experiment flow
    /// </summary>
    /// <param name="session"></param>
    public void GenerateExperiment(Session session)
    {
        // Validate the "BlockIndex" and "BlockSize" variables are the same length
        if (Enum.GetNames(typeof(BlockIndex)).Length != Enum.GetNames(typeof(BlockSize)).Length)
        {
            Debug.LogWarning("\"BlockIndex\" length does not match \"BlockSize\" length. Timline will not generate correctly.");
        }

        // Validate the "BlockIndex" and "BlockSize" variables contain the same entries
        foreach (string blockName in Enum.GetNames(typeof(BlockIndex)))
        {
            if (!Enum.GetNames(typeof(BlockSize)).Contains(blockName))
            {
                Debug.LogWarning("\"BlockSize\" does not contain block entry: " + blockName);
            }
        }

        // Create trial blocks
        session.CreateBlock((int)BlockSize.Welcome);
        session.CreateBlock((int)BlockSize.HeadsetSetup);
        session.CreateBlock((int)BlockSize.Tutorial);
        session.CreateBlock((int)BlockSize.PrePractice);
        session.CreateBlock((int)BlockSize.Practice);
        session.CreateBlock((int)BlockSize.PreMain);
        session.CreateBlock((int)BlockSize.Calibration);
        session.CreateBlock((int)BlockSize.Main);

        // Store reference to other classes
        stimulusManager = GetComponent<StimulusManager>();
        uiManager = GetComponent<UIManager>();
        cameraManager = GetComponent<CameraManager>();
        calibrationManager = GetComponent<CalibrationManager>();

        // Update the CameraManager value for the aperture offset to be the stimulus radius
        cameraManager.SetStimulusRadius(stimulusManager.GetStimulusRadius());
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
    /// Setup the "Welcome" screen presented to participants after any headset calibration operations.
    /// </summary>
    private void SetupWelcome()
    {
        // Setup the UI manager with instructions
        uiManager.EnablePagination(true);
        List<string> Instructions = new List<string>{
            "You are about to start the task. Before you start, please let the facilitator know if the headset feels uncomfortable or you cannot read this text.\n\nWhen you are ready and comfortable, press the right controller trigger to select <b>Next</b> and continue.",
            "These practice trials are similar to the actual trials, except the moving dots will be displayed a few seconds longer.\n\nPractice watching the dots and observing the appearance of the task.\n\nUse the triggers on the left and right controllers to interact with the task."
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
        // Setup performed at the start of the first "main" type trial
        if (ActiveBlock == BlockIndex.Main && Session.instance.CurrentTrial.numberInBlock == 1)
        {
            // Calculate the coherences to be used in the actual trials, use median of last 20 calibration trials
            List<Trial> CalibrationTrials = Session.instance.GetBlock((int)BlockIndex.Calibration).trials;
            CalibrationTrials.Reverse();

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
            Coherences["both"] = new float[] { kMedBothLow, kMedBothHigh };

            float kMedLeft = LeftCoherenceValues.Take(20).Median();
            float kMedLeftLow = 0.5f * kMedLeft < 0.12 ? 0.12f : 0.5f * kMedLeft;
            float kMedLeftHigh = 2.0f * kMedLeft > 0.5 ? 0.5f : 2.0f * kMedLeft;
            Coherences["left"] = new float[] { kMedLeftLow, kMedLeftHigh };

            float kMedRight = RightCoherenceValues.Take(20).Median();
            float kMedRightLow = 0.5f * kMedRight < 0.12 ? 0.12f : 0.5f * kMedRight;
            float kMedRightHigh = 2.0f * kMedRight > 0.5 ? 0.5f : 2.0f * kMedRight;
            Coherences["right"] = new float[] { kMedRightLow, kMedRightHigh };
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

            // Store the active visual field locally, since it is changed to present the confidence screen
            ActiveVisualField = cameraManager.GetActiveField();
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

        // Clone and store coherence values as a string, separated by "," token
        Session.instance.CurrentTrial.result["combinedCoherences"] = Coherences["both"][0] + "," + Coherences["both"][1];
        Session.instance.CurrentTrial.result["leftCoherences"] = Coherences["left"][0] + "," + Coherences["left"][1];
        Session.instance.CurrentTrial.result["rightCoherences"] = Coherences["right"][0] + "," + Coherences["right"][1];

        // Set the reference direction randomly
        float dotDirection = UnityEngine.Random.value > 0.5f ? 0.0f : (float)Math.PI;
        stimulusManager.SetDirection(dotDirection);
        Session.instance.CurrentTrial.result["referenceDirection"] = stimulusManager.GetDirection() == 0.0f ? "right" : "left";

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

        // Based on the active block, run any required setup operations and present the required stimuli
        ActiveBlock = (BlockIndex)trial.block.number;
        if (ActiveBlock == BlockIndex.Welcome)
        {
            SetupWelcome();
            StartCoroutine(DisplayStimuli("welcome"));
        }
        else if (ActiveBlock == BlockIndex.HeadsetSetup)
        {
            StartCoroutine(DisplayStimuli("eyetracking"));
        }
        else if (ActiveBlock == BlockIndex.Tutorial)
        {
            SetupMotion();
            StartCoroutine(DisplayStimuli("tutorial"));
        }
        else if (ActiveBlock == BlockIndex.PrePractice)
        {
            StartCoroutine(DisplayStimuli("prepractice"));
        }
        else if (ActiveBlock == BlockIndex.Practice)
        {
            SetupMotion();
            StartCoroutine(DisplayStimuli("practice"));
        }
        else if (ActiveBlock == BlockIndex.PreMain)
        {
            StartCoroutine(DisplayStimuli("premain"));
        }
        else if (ActiveBlock == BlockIndex.Calibration)
        {
            SetupMotion();
            StartCoroutine(DisplayStimuli("calibration"));
        }
        else if (ActiveBlock == BlockIndex.Main)
        {
            SetupMotion();
            StartCoroutine(DisplayStimuli("main"));
        }
    }

    private IEnumerator DisplayStimuli(string stimuli)
    {
        // Reset all displayed stimuli and UI
        stimulusManager.SetVisibleAll(false);
        uiManager.SetVisible(false);

        if (stimuli == "welcome")
        {
            // Store the displayed stimuli type
            Session.instance.CurrentTrial.result["name"] = stimuli;

            uiManager.SetVisible(true);
            uiManager.SetHeader("Welcome");
            uiManager.SetLeftButtonState(false, true, "Back");
            uiManager.SetRightButtonState(true, true, "Next");

            // Input delay
            yield return StartCoroutine(WaitSeconds(0.25f, true));
        }
        else if (stimuli == "eyetracking")
        {
            // Store the displayed stimuli type
            Session.instance.CurrentTrial.result["name"] = stimuli;

            uiManager.SetVisible(true);
            uiManager.SetHeader("Headset Calibration");
            uiManager.SetBody("You will be shown a red dot in front of you. Follow the dot movement with your eyes. After a brief series of movements, the calibration will automatically end and you will be shown the task instructions.\n\nPress the right controller trigger to select <b>Start</b>.");
            uiManager.SetLeftButtonState(false, false, "");
            uiManager.SetRightButtonState(true, true, "Start");

            // Input delay
            yield return StartCoroutine(WaitSeconds(0.25f, true));
            SetInputEnabled(true);
        }
        else if (stimuli == "tutorial")
        {
            // Store the displayed stimuli type
            Session.instance.CurrentTrial.result["name"] = stimuli;

            // Fixation (1 second)
            stimulusManager.SetVisible("fixation", true);
            yield return StartCoroutine(WaitSeconds(1.0f, true));
            stimulusManager.SetVisible("fixation", false);

            // Motion ([1.0, 5.0) seconds)
            stimulusManager.SetVisible("motion", true);
            // Override the standard motion duration
            float TutorialMotionDuration = 1.0f + UnityEngine.Random.value * 4.0f;
            Session.instance.CurrentTrial.result["motionDuration"] = TutorialMotionDuration;
            yield return StartCoroutine(WaitSeconds(TutorialMotionDuration, true));
            stimulusManager.SetVisible("motion", false);

            // Decision (wait)
            Session.instance.CurrentTrial.result["referenceStart"] = Time.time;
            stimulusManager.SetVisible("decision", true);
            yield return StartCoroutine(WaitSeconds(0.25f, true));
            SetInputEnabled(true);
        }
        else if (stimuli == "prepractice")
        {
            // Store the displayed stimuli type
            Session.instance.CurrentTrial.result["name"] = stimuli;

            uiManager.SetVisible(true);
            uiManager.SetHeader("Practice Trials");
            uiManager.SetBody("You will now complete another " + BlockSize.Practice + " practice trials. After selecting a direction, the cross in the center of the circlular area will briefly change color if your answer was correct or not. Green is a correct answer, red is an incorrect answer.\n\nWhen you are ready and comfortable, press the right controller trigger to select <b>Next</b> and continue.");
            uiManager.SetLeftButtonState(false, true, "Back");
            uiManager.SetRightButtonState(true, true, "Next");

            // Input delay
            yield return StartCoroutine(WaitSeconds(0.25f, true));
            SetInputEnabled(true);
        }
        else if (stimuli == "practice")
        {
            // Store the displayed stimuli type
            Session.instance.CurrentTrial.result["name"] = stimuli;

            // Fixation (1 second)
            stimulusManager.SetVisible("fixation", true);
            yield return StartCoroutine(WaitSeconds(1.0f, true));
            stimulusManager.SetVisible("fixation", false);

            // Motion (1.5 seconds)
            stimulusManager.SetVisible("motion", true);
            yield return StartCoroutine(WaitSeconds(1.5f, true));
            stimulusManager.SetVisible("motion", false);

            // Decision (wait)
            Session.instance.CurrentTrial.result["referenceStart"] = Time.time;
            stimulusManager.SetVisible("decision", true);
            yield return StartCoroutine(WaitSeconds(0.25f, true));
            SetInputEnabled(true);
        }
        else if (stimuli == "premain")
        {
            // Store the displayed stimuli type
            Session.instance.CurrentTrial.result["name"] = stimuli;

            uiManager.SetVisible(true);
            uiManager.SetHeader("Main Trials");
            uiManager.SetBody("That concludes the practice trials. You will now play " + ((int)BlockSize.Calibration + (int)BlockSize.Main) + " main trials.\n\nYou will not be shown if you answered correctly or not, but sometimes you will be asked whether you were more confident in that trial than in the previous trial.\n\nWhen you are ready and comfortable, press the right controller trigger to select <b>Next</b> and continue.");
            uiManager.SetLeftButtonState(false, true, "Back");
            uiManager.SetRightButtonState(true, true, "Next");

            // Input delay
            yield return StartCoroutine(WaitSeconds(0.25f, true));
            SetInputEnabled(true);
        }
        else if (stimuli == "calibration")
        {
            // Store the displayed stimuli type
            Session.instance.CurrentTrial.result["name"] = stimuli;

            // Fixation (1 second)
            stimulusManager.SetVisible("fixation", true);
            yield return StartCoroutine(WaitSeconds(1.0f, true));
            stimulusManager.SetVisible("fixation", false);

            // Motion (1.5 seconds)
            stimulusManager.SetVisible("motion", true);
            yield return StartCoroutine(WaitSeconds(1.5f, true));
            stimulusManager.SetVisible("motion", false);

            // Decision (wait)
            Session.instance.CurrentTrial.result["referenceStart"] = Time.time;
            stimulusManager.SetVisible("decision", true);
            yield return StartCoroutine(WaitSeconds(0.25f, true));
            SetInputEnabled(true);
        }
        else if (stimuli == "main")
        {
            // Store the displayed stimuli type
            Session.instance.CurrentTrial.result["name"] = stimuli;

            // Fixation (1 second)
            stimulusManager.SetVisible("fixation", true);
            yield return StartCoroutine(WaitSeconds(1.0f, true));
            stimulusManager.SetVisible("fixation", false);

            // Motion (1.5 seconds)
            stimulusManager.SetVisible("motion", true);
            yield return StartCoroutine(WaitSeconds(1.5f, true));
            stimulusManager.SetVisible("motion", false);

            // Decision (wait)
            Session.instance.CurrentTrial.result["referenceStart"] = Time.time;
            stimulusManager.SetVisible("decision", true);
            yield return StartCoroutine(WaitSeconds(0.25f, true));
            SetInputEnabled(true);
        }
        else if (stimuli == "confidence")
        {
            // Override and set the camera to display in both eyes
            cameraManager.SetActiveField(CameraManager.VisualField.Both);

            // Confidence
            uiManager.SetVisible(true);
            uiManager.SetHeader("");
            uiManager.SetBody("Did you feel more confident about your response to: The <b>previous</b> trial or <b>this</b> trial?");
            uiManager.SetLeftButtonState(true, true, "Previous Trial");
            uiManager.SetRightButtonState(true, true, "This Trial");

            // Input delay
            Session.instance.CurrentTrial.result["confidenceStart"] = Time.time;
            yield return StartCoroutine(WaitSeconds(0.25f, true));
            SetInputEnabled(true);
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
            if (selection == "left" && stimulusManager.GetDirection() == (float)Math.PI)
            {
                Session.instance.CurrentTrial.result["selectedCorrectDirection"] = true;
            }
            else if (selection == "right" && stimulusManager.GetDirection() == 0.0f)
            {
                Session.instance.CurrentTrial.result["selectedCorrectDirection"] = true;
            }

            if (ActiveBlock == BlockIndex.Calibration)
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
                }
            }

            // Some trials display additional components, otherwise end the trial
            if (ActiveBlock == BlockIndex.Practice)
            {
                // Display feedback during the practice trials
                if ((bool)Session.instance.CurrentTrial.result["selectedCorrectDirection"] == true)
                {
                    StartCoroutine(DisplayStimuli("feedback_correct"));
                }
                else
                {
                    StartCoroutine(DisplayStimuli("feedback_incorrect"));
                }
            }
            else if ((ActiveBlock == BlockIndex.Tutorial || ActiveBlock == BlockIndex.Calibration || ActiveBlock == BlockIndex.Main) &&
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
            // If `confidenceSelection` is empty, this is a confidence input
            // Store timing data
            Session.instance.CurrentTrial.result["confidenceEnd"] = Time.time;
            Session.instance.CurrentTrial.result["confidenceRT"] = (float)Session.instance.CurrentTrial.result["confidenceEnd"] - (float)Session.instance.CurrentTrial.result["confidenceStart"];

            // Store the confidence selection
            Session.instance.CurrentTrial.result["confidenceSelection"] = selection;

            // Reset the active camera
            cameraManager.SetActiveField(ActiveVisualField);

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

    private void SetInputEnabled(bool state)
    {
        InputEnabled = state;
    }

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

    void Update()
    {
        if (InputEnabled && InputReset)
        {
            // Left-side controls
            if (VRInput.PollLeftTrigger())
            {
                if (ActiveBlock == BlockIndex.Welcome)
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
                else if (
                    ActiveBlock == BlockIndex.Tutorial ||
                    ActiveBlock == BlockIndex.Practice ||
                    ActiveBlock == BlockIndex.Calibration ||
                    ActiveBlock == BlockIndex.Main)
                {
                    // Trigger controller haptics
                    VRInput.SetHaptics(15.0f, 0.4f, 0.1f, true, false);

                    // "Left" direction selected
                    HandleExperimentInput("left");
                }
                InputReset = false;
            }

            // Right-side controls
            if (VRInput.PollRightTrigger())
            {
                if (ActiveBlock == BlockIndex.Welcome ||
                    ActiveBlock == BlockIndex.PrePractice ||
                    ActiveBlock == BlockIndex.PreMain)
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
                }
                else if (ActiveBlock == BlockIndex.HeadsetSetup)
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
                }
                else if (
                    ActiveBlock == BlockIndex.Tutorial ||
                    ActiveBlock == BlockIndex.Practice ||
                    ActiveBlock == BlockIndex.Calibration ||
                    ActiveBlock == BlockIndex.Main)
                {
                    // Trigger controller haptics
                    VRInput.SetHaptics(15.0f, 0.4f, 0.1f, false, true);

                    // "Right" direction selected
                    HandleExperimentInput("right");
                }
                InputReset = false;
            }
        }

        // Reset input state to prevent holding buttons to repeatedly select options
        if (InputEnabled && InputReset == false && !VRInput.PollAnyInput())
        {
            InputReset = true;
        }
    }
}
