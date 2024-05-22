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

    // Define the types of trials that occur during the experiment timeline
    public enum TrialType
    {
        Setup = 1,
        Pre_Instructions = 2,
        Training_Trials_Binocular = 3,
        Training_Trials_Monocular = 4,
        Training_Trials_Lateralized = 5,
        Mid_Instructions = 6,
        Main_Trials_Binocular = 7,
        Main_Trials_Monocular = 8,
        Main_Trials_Lateralized = 9,
        Post_Instructions = 10,
    };

    // Set the number of trials within a specific block in the experiment timeline
    private enum TrialCount
    {
        Setup = 1,
        Pre_Instructions = 1, // Welcome instructions, includes tutorial instructions
        Training_Trials_Binocular = 30, // Training trials, central presentation to both eyes
        Training_Trials_Monocular = 30, // Training trials, central presentation to one eye
        Training_Trials_Lateralized = 60, // Training trials, lateralized presentation to one eye
        Mid_Instructions = 1,
        Main_Trials_Binocular = 50,
        Main_Trials_Monocular = 50,
        Main_Trials_Lateralized = 100,
        Post_Instructions = 1,
    };

    // Define the order of UXF `Blocks` and their expected block numbers (non-zero indexed)
    private enum BlockSequence
    {
        Setup = 1,
        Pre_Instructions = 2,
        Training = 3,
        Mid_Instructions = 4,
        Main = 5,
        Post_Instructions = 6
    };

    // List to populate with
    private List<TrialType> trainingTimeline = new();
    private List<TrialType> mainTimeline = new();

    // Active fields that are updated during trials
    private BlockSequence activeBlock; // Store the currently active `BlockSequence` type
    private TrialType activeTrialType; // Store the currently active `TrialType`
    private CameraManager.VisualField activeVisualField; // Store the currently active `VisualField`
    private float activeCoherence; // Current coherence value

    // "Training_"-type coherence values start at `0.2f` and are adjusted
    private float trainingBinocularCoherence = 0.2f; // Unified across left and right eye
    private float trainingMonocularCoherenceLeft = 0.2f;
    private float trainingMonocularCoherenceRight = 0.2f;
    private float trainingLateralizedCoherenceLeft = 0.2f;
    private float trainingLateralizedCoherenceRight = 0.2f;

    // "Main_"-type coherence values are calculated from "Training_"-type coherence values
    private Tuple<float, float> mainBinocularCoherence; // Unified across left and right eye
    private Tuple<float, float> mainMonocularCoherenceLeft;
    private Tuple<float, float> mainMonocularCoherenceRight;
    private Tuple<float, float> mainLateralizedCoherenceLeft;
    private Tuple<float, float> mainLateralizedCoherenceRight;

    // Timing variables
    private readonly float PRE_DISPLAY_DURATION = 0.5f;
    private readonly float DISPLAY_DURATION = 0.180f; // 180 milliseconds

    // Store references to Manager classes
    private StimulusManager stimulusManager;
    private UIManager uiManager;
    private CameraManager cameraManager;
    private CalibrationManager calibrationManager;

    // Store references to EyePositionTracker instances
    public EyePositionTracker LeftEyeTracker;
    public EyePositionTracker RightEyeTracker;
    private int fixationMeasurementCounter = 0; // Counter for number of fixation measurements
    private readonly int REQUIRED_FIXATION_MEASUREMENTS = 48; // Required on-target fixation measurements

    // Input parameters
    private bool isInputEnabled = false; // Input is accepted
    private bool isInputReset = true; // Flag to prevent input being held down
    public bool RequireFixation = true; // Require participant to be fixation on center before trial begins
    private InputState lastInputState; // Prior frame input state

    // Input button slider GameObjects and variables
    private readonly float TRIGGER_THRESHOLD = 0.8f;
    private readonly float BUTTON_SLIDER_THRESHOLD = 0.99f;
    private readonly float BUTTON_HOLD_FACTOR = 2.0f;

    /// <summary>
    /// Generate the experiment flow
    /// </summary>
    /// <param name="session"></param>
    public void GenerateExperiment(Session session)
    {
        // Generate the experiment timeline
        // Generate all "Training_"-type trials and shuffle timeline
        for (int i = 0; i < (int)TrialCount.Training_Trials_Binocular; i++)
        {
            trainingTimeline.Add(TrialType.Training_Trials_Binocular);
        }
        for (int j = 0; j < (int)TrialCount.Training_Trials_Monocular; j++)
        {
            trainingTimeline.Add(TrialType.Training_Trials_Monocular);
        }
        for (int k = 0; k < (int)TrialCount.Training_Trials_Lateralized; k++)
        {
            trainingTimeline.Add(TrialType.Training_Trials_Lateralized);
        }
        trainingTimeline.Shuffle();

        // Generate all "Main_"-type trials and shuffle timeline
        for (int i = 0; i < (int)TrialCount.Main_Trials_Binocular; i++)
        {
            mainTimeline.Add(TrialType.Main_Trials_Binocular);
        }
        for (int j = 0; j < (int)TrialCount.Main_Trials_Monocular; j++)
        {
            mainTimeline.Add(TrialType.Main_Trials_Monocular);
        }
        for (int k = 0; k < (int)TrialCount.Main_Trials_Lateralized; k++)
        {
            mainTimeline.Add(TrialType.Main_Trials_Lateralized);
        }
        mainTimeline.Shuffle();

        // Create a UXF `Block` for each part of the experiment, corresponding to `BlockSequence` enum
        // Use UXF `Session` to generate experiment timeline from shuffled "Training_" and "Main_" timelines
        session.CreateBlock((int)TrialCount.Setup); // Pre-experiment setup
        session.CreateBlock((int)TrialCount.Pre_Instructions); // Pre-experiment instructions
        session.CreateBlock(trainingTimeline.Count); // Training trials
        session.CreateBlock((int)TrialCount.Mid_Instructions); // Mid-experiment instructions
        session.CreateBlock(mainTimeline.Count); // Main trials
        session.CreateBlock((int)TrialCount.Post_Instructions); // Post-experiment instructions

        // Collect references to other classes
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
    /// Get trials of a specific `TrialType` and `VisualField` from a `Block`. Used primarily to filter a set of `Trial`s
    /// for calculation of coherence values that are specific to the search parameters.
    /// </summary>
    /// <param name="trialType">`TrialType` value</param>
    /// <param name="visualField">The active `VisualField`</param>
    /// <param name="blockIndex">`BlockSequence` of the `Block` containing the trials</param>
    /// <returns></returns>
    private List<Trial> GetTrialsByType(TrialType trialType, CameraManager.VisualField visualField, BlockSequence blockIndex)
    {
        List<Trial> result = new();

        Block searchBlock = Session.instance.GetBlock((int)blockIndex);
        if (searchBlock.trials.Count > 0)
        {
            foreach (Trial trial in searchBlock.trials)
            {
                // Extract results into enum names
                Enum.TryParse(trial.result["active_visual_field"].ToString(), out CameraManager.VisualField priorVisualField);
                Enum.TryParse(trial.result["trial_type"].ToString(), out TrialType priorTrialType);
                if (priorTrialType == trialType && priorVisualField == visualField)
                {
                    result.Add(trial);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Search within a block to find the index of the previous occurence of that `TrialType` with a matching active
    /// `VisualField`.
    /// </summary>
    /// <returns>`int` > `0` if found, `-1` if no matching `TrialType` found</returns>
    private int GetPreviousTrialIndex(TrialType searchType, CameraManager.VisualField visualField, int currentIndex)
    {
        if (currentIndex <= 1)
        {
            // Invalid starting index specified
            return -1;
        }

        for (int i = currentIndex - 1; i >= 1; i--)
        {
            Trial priorTrial = Session.instance.CurrentBlock.GetRelativeTrial(i);
            string priorTrialType = priorTrial.result["trial_type"].ToString();
            Enum.TryParse(priorTrial.result["active_visual_field"].ToString(), out CameraManager.VisualField priorVisualField);

            // Compared the stored `name` with the name of the `TrialType`and the active visual field being searched for
            if (priorTrialType == Enum.GetName(typeof(TrialType), searchType) && priorVisualField == visualField)
            {
                // Found `Trial` with matching `TrialType`
                return i;
            }
        }

        // No `Trial` found matching the `TrialType`
        return -1;
    }

    /// <summary>
    /// Update the coherence value depending on the accuracy of the given response to the current `Trial` and the
    /// previous `Trial`. This function is only executed during the "Training_"-type trials.
    /// </summary>
    private void UpdateCoherences()
    {
        TrialType targetTrialType = activeTrialType;
        float coherenceDelta = 0.0f;

        // Current `Trial` information
        Trial currentTrial = Session.instance.CurrentTrial;
        bool currentTrialAccuracy = (bool)currentTrial.result["correct_selection"];
        int currentTrialIndex = currentTrial.numberInBlock;
        float currentTrialCoherence = (float)currentTrial.result["active_coherence"];

        if (currentTrialAccuracy == true)
        {
            // Search for previous trial of matching `TrialType` for comparison
            int previousTrialIndex = GetPreviousTrialIndex(activeTrialType, activeVisualField, currentTrialIndex);
            if (previousTrialIndex != -1)
            {
                // Prior trial found matching the current `TrialType`
                Trial previousTrial = Session.instance.CurrentBlock.GetRelativeTrial(previousTrialIndex);
                bool previousTrialAccuracy = (bool)previousTrial.result["correct_selection"];
                float previousTrialCoherence = (float)previousTrial.result["active_coherence"];

                // Check criteria for two consecutive correct responses
                if (previousTrialAccuracy == true && currentTrialCoherence == previousTrialCoherence)
                {
                    // Reduce the target `TrialType` coherence value, increasing difficulty
                    coherenceDelta = -0.01f;
                }
            }
        }
        else
        {
            // Increase the coherence value, reducing difficulty
            coherenceDelta = 0.01f;
        }

        // Apply the modification to the `TrialType` coherence value
        if (targetTrialType == TrialType.Training_Trials_Binocular)
        {
            trainingBinocularCoherence += coherenceDelta;
        }
        else if (targetTrialType == TrialType.Training_Trials_Monocular)
        {
            if (activeVisualField == CameraManager.VisualField.Left)
            {
                trainingMonocularCoherenceLeft += coherenceDelta;
            }
            else
            {
                trainingMonocularCoherenceRight += coherenceDelta;
            }
        }
        else if (targetTrialType == TrialType.Training_Trials_Lateralized)
        {
            if (activeVisualField == CameraManager.VisualField.Left)
            {
                trainingLateralizedCoherenceLeft += coherenceDelta;
            }
            else
            {
                trainingLateralizedCoherenceRight += coherenceDelta;
            }
        }
    }

    /// <summary>
    /// Helper function to calculate the low and high coherence pairs from the median value of a set of 20
    /// coherence values. Requires a set of trials, and the name of the field as stored in the `results` dictionary.
    /// </summary>
    /// <param name="trialSet">Set of `Trial` objects</param>
    /// <param name="coherenceFieldName">Field name storing the coherence value</param>
    /// <returns></returns>
    private Tuple<float, float> CalculateCoherencePairs(List<Trial> trialSet, string coherenceFieldName)
    {
        List<float> coherences = trialSet.Select(trial => (float)trial.result[coherenceFieldName]).ToList();
        coherences.Reverse();
        float kMed = coherences.Take(20).Median();
        float kMedLow = 0.5f * kMed < 0.12f ? 0.12f : 0.5f * kMed;
        float kMedHigh = 2.0f * kMed > 0.5f ? 0.5f : 2.0f * kMed;
        return new Tuple<float, float>(kMedLow, kMedHigh);
    }

    /// <summary>
    /// Utility function to calculate coherence values prior to "Main_"-type trials
    /// </summary>
    private void CalculateCoherences()
    {
        // Calculate coherences for `Training_Trials_Binocular` trials
        List<Trial> binocularTrials = GetTrialsByType(TrialType.Training_Trials_Binocular, CameraManager.VisualField.Both, BlockSequence.Training);
        mainBinocularCoherence = CalculateCoherencePairs(binocularTrials, "training_binocular_coherence");

        // Calculate coherences for left `Training_Trials_Monocular` trials
        List<Trial> monocularTrialsLeft = GetTrialsByType(TrialType.Training_Trials_Monocular, CameraManager.VisualField.Left, BlockSequence.Training);
        mainMonocularCoherenceLeft = CalculateCoherencePairs(monocularTrialsLeft, "training_monocular_coherence_left");

        // Calculate coherences for right `Training_Trials_Monocular` trials
        List<Trial> monocularTrialsRight = GetTrialsByType(TrialType.Training_Trials_Monocular, CameraManager.VisualField.Right, BlockSequence.Training);
        mainMonocularCoherenceRight = CalculateCoherencePairs(monocularTrialsRight, "training_monocular_coherence_right");

        // Calculate coherences for left `Training_Trials_Lateralized` trials
        List<Trial> lateralizedTrialsLeft = GetTrialsByType(TrialType.Training_Trials_Lateralized, CameraManager.VisualField.Left, BlockSequence.Training);
        mainLateralizedCoherenceLeft = CalculateCoherencePairs(lateralizedTrialsLeft, "training_lateralized_coherence_left");

        // Calculate coherences for right `Training_Trials_Lateralized` trials
        List<Trial> lateralizedTrialsRight = GetTrialsByType(TrialType.Training_Trials_Lateralized, CameraManager.VisualField.Right, BlockSequence.Training);
        mainLateralizedCoherenceRight = CalculateCoherencePairs(lateralizedTrialsRight, "training_lateralized_coherence_right");
    }

    /// <summary>
    /// Setup the "Instructions" screen presented to participants after headset setup operations.
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
    }

    /// <summary>
    /// Setup operations to perform prior to any motion stimuli. Also responsible for generating coherence values
    /// after the calibration stages.
    /// </summary>
    private void SetupMotion(TrialType trial)
    {
        // Set the reference direction randomly
        float dotDirection = UnityEngine.Random.value > 0.5f ? (float)Math.PI / 2 : (float)Math.PI * 3 / 2;
        stimulusManager.SetDirection(dotDirection);

        // Setup the camera according to the active `TrialType`
        if (trial == TrialType.Training_Trials_Binocular || trial == TrialType.Main_Trials_Binocular)
        {
            // Activate both cameras in binocular mode
            cameraManager.SetActiveField(CameraManager.VisualField.Both, false);
        }
        else if (trial == TrialType.Training_Trials_Monocular || trial == TrialType.Main_Trials_Monocular)
        {
            // Activate one camera in monocular mode, without lateralization
            CameraManager.VisualField TrialVisualField = UnityEngine.Random.value > 0.5 ? CameraManager.VisualField.Left : CameraManager.VisualField.Right;
            cameraManager.SetActiveField(TrialVisualField, false);
        }
        else
        {
            // Activate one camera in lateralized mode, with lateralization enabled
            CameraManager.VisualField TrialVisualField = UnityEngine.Random.value > 0.5 ? CameraManager.VisualField.Left : CameraManager.VisualField.Right;
            cameraManager.SetActiveField(TrialVisualField, true);
        }
        activeVisualField = cameraManager.GetActiveField();

        // Set the coherence value depending on the `TrialType`
        if (activeTrialType == TrialType.Training_Trials_Binocular)
        {
            activeCoherence = trainingBinocularCoherence;
        }
        else if (activeTrialType == TrialType.Training_Trials_Monocular)
        {
            // Select a coherence value depending on the active `VisualField`
            activeCoherence = activeVisualField == CameraManager.VisualField.Left ? trainingMonocularCoherenceLeft : trainingMonocularCoherenceRight;
        }
        else if (activeTrialType == TrialType.Training_Trials_Lateralized)
        {
            // Select a coherence value depending on the active `VisualField`
            activeCoherence = activeVisualField == CameraManager.VisualField.Left ? trainingLateralizedCoherenceLeft : trainingLateralizedCoherenceRight;
        }
        // All "Main_"-type coherence selections include a difficulty selection
        else if (activeTrialType == TrialType.Main_Trials_Binocular)
        {
            activeCoherence = UnityEngine.Random.value > 0.5 ? mainBinocularCoherence.Item1 : mainBinocularCoherence.Item2;;
        }
        else if (activeTrialType == TrialType.Main_Trials_Monocular)
        {
            // Select the appropriate coherence pair (left or right), then select a coherence value (low or high)
            Tuple<float, float> CoherencePair = activeVisualField == CameraManager.VisualField.Left ? mainMonocularCoherenceLeft : mainMonocularCoherenceRight;
            activeCoherence = UnityEngine.Random.value > 0.5 ? CoherencePair.Item1 : CoherencePair.Item2;
        }
        else if (activeTrialType == TrialType.Main_Trials_Lateralized)
        {
            // Select the appropriate coherence pair (left or right), then select a coherence value (low or high)
            Tuple<float, float> CoherencePair = activeVisualField == CameraManager.VisualField.Left ? mainLateralizedCoherenceLeft : mainLateralizedCoherenceRight;
            activeCoherence = UnityEngine.Random.value > 0.5 ? CoherencePair.Item1 : CoherencePair.Item2;
        }

        // Store motion-related data points
        Session.instance.CurrentTrial.result["dot_direction"] = dotDirection == (float)Math.PI / 2 ? "up" : "down";
        Session.instance.CurrentTrial.result["active_coherence"] = activeCoherence;
        if (activeBlock == BlockSequence.Training)
        {
            Session.instance.CurrentTrial.result["training_binocular_coherence"] = trainingBinocularCoherence;
            Session.instance.CurrentTrial.result["training_monocular_coherence_left"] = trainingMonocularCoherenceLeft;
            Session.instance.CurrentTrial.result["training_monocular_coherence_right"] = trainingMonocularCoherenceRight;
            Session.instance.CurrentTrial.result["training_lateralized_coherence_left"] = trainingLateralizedCoherenceLeft;
            Session.instance.CurrentTrial.result["training_lateralized_coherence_right"] = trainingLateralizedCoherenceRight;
        }
        if (activeBlock == BlockSequence.Main)
        {
            Session.instance.CurrentTrial.result["main_binocular_coherence"] = mainBinocularCoherence.Item1 + "," + mainBinocularCoherence.Item2;
            Session.instance.CurrentTrial.result["main_monocular_coherence_left"] = mainMonocularCoherenceLeft.Item1 + "," + mainMonocularCoherenceLeft.Item2;
            Session.instance.CurrentTrial.result["main_monocular_coherence_right"] = mainMonocularCoherenceRight.Item1 + "," + mainMonocularCoherenceRight.Item2;
            Session.instance.CurrentTrial.result["main_lateralized_coherence_left"] = mainLateralizedCoherenceLeft.Item1 + "," + mainLateralizedCoherenceLeft.Item2;
            Session.instance.CurrentTrial.result["main_lateralized_coherence_right"] = mainLateralizedCoherenceRight.Item1 + "," + mainLateralizedCoherenceRight.Item2;
        }
        Session.instance.CurrentTrial.result["active_visual_field"] = activeVisualField;
        Session.instance.CurrentTrial.result["motion_duration"] = DISPLAY_DURATION;
    }

    /// <summary>
    /// Store timestamps and locale metadata before presenting the stimuli associated with a Trial.
    /// </summary>
    /// <param name="trial">UXF `Trial` object representing the current trial</param>
    public void RunTrial(Trial trial)
    {
        // Store local date and time data
        Session.instance.CurrentTrial.result["local_date"] = DateTime.Now.ToShortDateString();
        Session.instance.CurrentTrial.result["local_time"] = DateTime.Now.ToShortTimeString();
        Session.instance.CurrentTrial.result["local_timezone"] = TimeZoneInfo.Local.DisplayName;
        Session.instance.CurrentTrial.result["trial_start"] = Time.time;

        // Update the currently active block
        activeBlock = (BlockSequence)trial.block.number;

        // Display the active block
        StartCoroutine(DisplayTrial(activeBlock));
    }

    /// <summary>
    /// Switch-like function presenting a specified stimulus
    /// </summary>
    /// <param name="block">The current block type</param>
    /// <returns></returns>
    private IEnumerator DisplayTrial(BlockSequence block)
    {
        // Get the relative position of the current trial in the block
        int RelativeTrialNumber = Session.instance.CurrentTrial.numberInBlock - 1;

        // Define the active `TrialType` depending on the active `Block`
        activeTrialType = TrialType.Pre_Instructions;
        if (block == BlockSequence.Training)
        {
            activeTrialType = trainingTimeline[RelativeTrialNumber];
        }
        else if (block == BlockSequence.Mid_Instructions)
        {
            activeTrialType = TrialType.Mid_Instructions;
        }
        else if (block == BlockSequence.Main)
        {
            activeTrialType = mainTimeline[RelativeTrialNumber];
        }
        else if (block == BlockSequence.Post_Instructions)
        {
            activeTrialType = TrialType.Post_Instructions;
        }

        // Reset all displayed stimuli and UI
        stimulusManager.SetVisibleAll(false);
        uiManager.SetVisible(false);

        // Store the current `TrialType`
        Session.instance.CurrentTrial.result["trial_type"] = Enum.GetName(typeof(TrialType), activeTrialType);

        if (block == BlockSequence.Setup)
        {
            uiManager.SetVisible(true);
            uiManager.SetHeader("Eye-Tracking Setup");
            uiManager.SetBody("You will be shown a red dot in front of you. Follow the dot movement with your eyes. After a brief series of movements, the calibration will automatically end and you will be shown the task instructions.\n\nPress the right controller trigger to select <b>Start</b>.");
            uiManager.SetLeftButtonState(false, false, "");
            uiManager.SetRightButtonState(true, true, "Start");

            // Input delay
            yield return StartCoroutine(WaitSeconds(0.25f, true));
            SetIsInputEnabled(true);
        }
        else if (block == BlockSequence.Pre_Instructions)
        {
            SetupInstructions();
            uiManager.SetVisible(true);
            uiManager.SetHeader("Instructions");
            uiManager.SetLeftButtonState(false, true, "Back");
            uiManager.SetRightButtonState(true, true, "Next");

            // Input delay
            yield return StartCoroutine(WaitSeconds(0.25f, true));
        }
        else if (block == BlockSequence.Training || block == BlockSequence.Main)
        {
            // Run setup for the motion stimuli
            SetupMotion(activeTrialType);
            yield return StartCoroutine(DisplayMotion());
        }
        else if (block == BlockSequence.Mid_Instructions)
        {
            // Run calibration calculations
            CalculateCoherences();

            // Override and set the camera to display in both eyes
            cameraManager.SetActiveField(CameraManager.VisualField.Both);

            uiManager.SetVisible(true);
            uiManager.SetHeader("Main Trials");
            uiManager.SetBody("That concludes the practice trials. You will now play " + mainTimeline.Count + " main trials.\n\nYou will not be shown if you answered correctly or not, but sometimes you will be asked whether you were more confident in that trial than in the previous trial.\n\nWhen you are ready and comfortable, press the right controller trigger to select <b>Next</b> and continue.");
            uiManager.SetLeftButtonState(false, true, "Back");
            uiManager.SetRightButtonState(true, true, "Next");

            // Input delay
            yield return StartCoroutine(WaitSeconds(0.15f, true));
            SetIsInputEnabled(true);
        }
        else if (block == BlockSequence.Post_Instructions)
        {
            // Override and set the camera to display in both eyes
            cameraManager.SetActiveField(CameraManager.VisualField.Both);

            uiManager.SetVisible(true);
            uiManager.SetHeader("Complete");
            uiManager.SetBody("That concludes all the trials of this task. Please notify the experiment facilitator, and you can remove the headset carefully after releasing the rear adjustment wheel.");
            uiManager.SetLeftButtonState(false, false, "Back");
            uiManager.SetRightButtonState(true, true, "Finish");

            // Input delay
            yield return StartCoroutine(WaitSeconds(1.0f, true));
            SetIsInputEnabled(true);
        }
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
    }

    /// <summary>
    /// Utility function to display the dot motion stimulus and wait for a response, used in all trial types
    /// </summary>
    /// <returns></returns>
    private IEnumerator DisplayMotion()
    {
        // Disable input and wait for fixation if enabled
        SetIsInputEnabled(false);
        stimulusManager.SetFixationCrossVisibility(true);
        if (RequireFixation)
        {
            yield return new WaitUntil(() => IsFixated());
        }

        // Present fixation stimulus
        stimulusManager.SetVisible(StimulusType.Fixation, true);
        // Wait either for fixation or a fixed duration if fixation not required
        if (RequireFixation)
        {
            yield return new WaitUntil(() => IsFixated());
        }
        else
        {
            yield return StartCoroutine(WaitSeconds(PRE_DISPLAY_DURATION, true));
        }
        stimulusManager.SetVisible(StimulusType.Fixation, false);

        // Present motion stimulus
        stimulusManager.SetVisible(StimulusType.Motion, true);
        yield return StartCoroutine(WaitSeconds(DISPLAY_DURATION, true));
        stimulusManager.SetVisible(StimulusType.Motion, false);
        stimulusManager.SetFixationCrossVisibility(false);

        // Present decision stimulus and wait for response
        Session.instance.CurrentTrial.result["decision_start"] = Time.time;
        stimulusManager.SetVisible(StimulusType.Decision, true);
        yield return StartCoroutine(WaitSeconds(0.15f, true));
        SetIsInputEnabled(true);
    }

    /// <summary>
    /// Wrapper function to handle selection of a specific direction and confidence value
    /// </summary>
    private void HandleSelection(string selection)
    {
        // Store timing data
        Session.instance.CurrentTrial.result["decision_end"] = Time.time;
        Session.instance.CurrentTrial.result["decision_rt"] = (float)Session.instance.CurrentTrial.result["decision_end"] - (float)Session.instance.CurrentTrial.result["decision_start"];

        // Store the selection value
        Session.instance.CurrentTrial.result["selected_direction"] = selection;

        // Determine if a correct response was made
        Session.instance.CurrentTrial.result["correct_selection"] = false;
        if ((selection == "vc_u" || selection == "sc_u") && stimulusManager.GetDirection() == (float)Math.PI / 2)
        {
            Session.instance.CurrentTrial.result["correct_selection"] = true;
        }
        else if ((selection == "vc_d" || selection == "sc_d") && stimulusManager.GetDirection() == (float)Math.PI * 3 / 2)
        {
            Session.instance.CurrentTrial.result["correct_selection"] = true;
        }

        // If currently in a "Training_"-type block, update the coherences after the selection has been handled
        if (activeBlock == BlockSequence.Training)
        {
            UpdateCoherences();
        }

        // Reset the button states and end the current `Trial`
        ResetButtons();
        EndTrial();
    }

    public void EndTrial()
    {
        Session.instance.CurrentTrial.result["trial_end"] = Time.time;
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
    private void SetIsInputEnabled(bool state)
    {
        isInputEnabled = state;
    }

    /// <summary>
    /// Wait for eye gaze to return to central fixation point prior to returning
    /// </summary>
    /// <returns></returns>
    private bool IsFixated()
    {
        // Get gaze estimates and the current world position
        Vector3 leftGaze = LeftEyeTracker.GetGazeEstimate();
        Vector3 rightGaze = RightEyeTracker.GetGazeEstimate();
        Vector3 worldPosition = stimulusManager.GetAnchor().transform.position;

        // Calculate central gaze position and adjust world position if a lateralized trial
        float gazeOffset = cameraManager.GetTotalOffset();
        if (activeTrialType == TrialType.Training_Trials_Lateralized || activeTrialType == TrialType.Main_Trials_Lateralized)
        {
            if (cameraManager.GetActiveField() == CameraManager.VisualField.Left)
            {
                worldPosition.x += gazeOffset;
            }
            else if (cameraManager.GetActiveField() == CameraManager.VisualField.Right)
            {
                worldPosition.x -= gazeOffset;
            }
        }

        float gazeThreshold = 0.5f; // Error threshold (world units)
        bool isFixated = false; // Fixated state
        if ((Mathf.Abs(leftGaze.x - worldPosition.x) <= gazeThreshold && Mathf.Abs(leftGaze.y - worldPosition.y) <= gazeThreshold) || (Mathf.Abs(rightGaze.x - worldPosition.x) <= gazeThreshold && Mathf.Abs(rightGaze.y - worldPosition.y) <= gazeThreshold))
        {
            // If the gaze is directed in fixation, increment the counter to signify a measurement
            fixationMeasurementCounter += 1;
        }

        if (fixationMeasurementCounter >= REQUIRED_FIXATION_MEASUREMENTS)
        {
            // Register as fixated if the required number of measurements have been taken
            isFixated = true;
            fixationMeasurementCounter = 0;
        }

        // Return the overall fixation state
        return isFixated;
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
            SetIsInputEnabled(false);
        }

        yield return new WaitForSeconds(seconds);

        if (disableInput)
        {
            SetIsInputEnabled(true);
        }

        // Run callback function
        callback?.Invoke();
    }

    // Functions to bulk-classify TrialType values
    private bool IsInstructionsScreen()
    {
        return activeBlock == BlockSequence.Pre_Instructions ||
            activeBlock == BlockSequence.Mid_Instructions ||
            activeBlock == BlockSequence.Post_Instructions;
    }

    private bool IsStimulusScreen()
    {
        return activeBlock == BlockSequence.Training ||
                activeBlock == BlockSequence.Main;
    }

    private bool IsSetupScreen()
    {
        return activeBlock == BlockSequence.Setup;
    }

    /// <summary>
    /// Input function to handle `InputState` object and update button presentation or take action depending on
    /// the active `TrialType`
    /// </summary>
    /// <param name="inputs">`InputState` object</param>
    private void ApplyInputs(InputState inputs)
    {
        ButtonSliderInput[] buttonControllers = stimulusManager.GetButtonControllers();

        // Left controller inputs
        if (inputs.L_T_State >= TRIGGER_THRESHOLD || inputs.X_Pressed)
        {
            if (inputs.L_T_State >= TRIGGER_THRESHOLD)
            {
                // Very confident up
                buttonControllers[0].SetSliderValue(buttonControllers[0].GetSliderValue() + BUTTON_HOLD_FACTOR * Time.deltaTime);
                if (lastInputState.L_T_State < TRIGGER_THRESHOLD)
                {
                    Session.instance.CurrentTrial.result["last_keypress_start"] = Time.time;
                }
            }
            else
            {
                // Somewhat confident up
                buttonControllers[1].SetSliderValue(buttonControllers[1].GetSliderValue() + BUTTON_HOLD_FACTOR * Time.deltaTime);
                if (lastInputState.X_Pressed == false)
                {
                    Session.instance.CurrentTrial.result["last_keypress_start"] = Time.time;
                }
            }

            // Check if a button has been completely held down, and continue if so
            if (buttonControllers[0].GetSliderValue() >= BUTTON_SLIDER_THRESHOLD || buttonControllers[1].GetSliderValue() >= BUTTON_SLIDER_THRESHOLD)
            {
                // Provide haptic feedback
                VRInput.SetHaptics(15.0f, 0.4f, 0.1f, true, false);
                Session.instance.CurrentTrial.result["last_keypress_end"] = Time.time;

                // Store appropriate response
                if (buttonControllers[0].GetSliderValue() >= BUTTON_SLIDER_THRESHOLD)
                {
                    // "Very Confident Up" selected
                    HandleSelection("vc_u");
                    buttonControllers[0].SetSliderValue(0.0f);
                }
                else if (buttonControllers[1].GetSliderValue() >= BUTTON_SLIDER_THRESHOLD)
                {
                    // "Somewhat Confident Up" selected
                    HandleSelection("sc_u");
                    buttonControllers[1].SetSliderValue(0.0f);
                }
            }
        }

        // Right controller inputs
        else if (inputs.R_T_State >= TRIGGER_THRESHOLD || inputs.A_Pressed)
        {
            if (inputs.R_T_State >= TRIGGER_THRESHOLD)
            {
                // Very confident down
                buttonControllers[2].SetSliderValue(buttonControllers[2].GetSliderValue() + BUTTON_HOLD_FACTOR * Time.deltaTime);
                if (lastInputState.R_T_State < TRIGGER_THRESHOLD)
                {
                    Session.instance.CurrentTrial.result["last_keypress_start"] = Time.time;
                }
            }
            else
            {
                // Somewhat confident down
                buttonControllers[3].SetSliderValue(buttonControllers[3].GetSliderValue() + BUTTON_HOLD_FACTOR * Time.deltaTime);
                if (lastInputState.A_Pressed == false)
                {
                    Session.instance.CurrentTrial.result["last_keypress_start"] = Time.time;
                }
            }

            if (buttonControllers[2].GetSliderValue() >= BUTTON_SLIDER_THRESHOLD || buttonControllers[3].GetSliderValue() >= BUTTON_SLIDER_THRESHOLD)
            {
                VRInput.SetHaptics(15.0f, 0.4f, 0.1f, false, true);
                Session.instance.CurrentTrial.result["last_keypress_end"] = Time.time;

                // Store appropriate response
                if (buttonControllers[2].GetSliderValue() >= BUTTON_SLIDER_THRESHOLD)
                {
                    // "Very Confident Down" selected
                    HandleSelection("vc_d");
                    buttonControllers[2].SetSliderValue(0.0f);
                }
                else if (buttonControllers[3].GetSliderValue() >= BUTTON_SLIDER_THRESHOLD)
                {
                    // "Somewhat Confident Down" selected
                    HandleSelection("sc_d");
                    buttonControllers[3].SetSliderValue(0.0f);
                }
            }
        }

        // Update the prior input state
        lastInputState = inputs;
    }

    /// <summary>
    /// Utility function to reduce the value of all active `ButtonSliderInput` instances back to `0.0f` when inactive.
    /// </summary>
    private void ButtonCooldown()
    {
        foreach (ButtonSliderInput button in stimulusManager.GetButtonControllers())
        {
            button.SetSliderValue(button.GetSliderValue() - BUTTON_HOLD_FACTOR / 3.0f * Time.deltaTime);
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
        if (isInputEnabled)
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
                if (inputs.L_T_State > TRIGGER_THRESHOLD || inputs.X_Pressed)
                {
                    if (IsInstructionsScreen() && isInputReset)
                    {
                        if (uiManager.HasPreviousPage())
                        {
                            // If pagination has previous page, go back
                            uiManager.PreviousPage();
                            SetIsInputEnabled(true);

                            // Trigger controller haptics
                            VRInput.SetHaptics(15.0f, 0.4f, 0.1f, true, false);

                            // Update the "Next" button if the last page
                            if (uiManager.HasNextPage())
                            {
                                uiManager.SetRightButtonState(true, true, "Next");
                            }
                        }
                    }
                    isInputReset = false;
                }

                // Right-side controls
                if (inputs.R_T_State > TRIGGER_THRESHOLD || inputs.A_Pressed)
                {
                    if (IsInstructionsScreen() && isInputReset)
                    {
                        if (uiManager.HasNextPage())
                        {
                            // If pagination has next page, advance
                            uiManager.NextPage();
                            SetIsInputEnabled(true);

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
                        isInputReset = false;
                    }
                    else if (IsSetupScreen() && isInputReset)
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
                        isInputReset = false;
                    }
                }

                // Reset input state to prevent holding buttons to repeatedly select options
                if (isInputEnabled && isInputReset == false && !VRInput.AnyInput())
                {
                    isInputReset = true;
                }
            }
        }
    }
}
