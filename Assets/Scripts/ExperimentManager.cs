using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using UXF;
using MathNet.Numerics.Statistics;

// Custom namespaces
using Stimuli;
using Utilities;
using UnityEngine.Networking;

public class ExperimentManager : MonoBehaviour
{
    // Loading screen object, parent object that contains all loading screen components
    public GameObject LoadingScreen;

    // Define the types of trials that occur during the experiment timeline
    public enum TrialType
    {
        Fit = 1,
        Setup = 2,
        Instructions_Motion = 3,
        Instructions_Demo_Motion = 4,
        Instructions_Selection = 5,
        Instructions_Demo_Selection = 6,
        Instructions_Training = 7,
        Training_Trials_Binocular = 8,
        Training_Trials_Monocular = 9,
        Training_Trials_Lateralized = 10,
        Mid_Instructions = 11,
        Main_Trials_Binocular = 12,
        Main_Trials_Monocular = 13,
        Main_Trials_Lateralized = 14,
        Post_Instructions = 15,
    };

    // Set the number of trials within a specific block in the experiment timeline
    private enum TrialCount
    {
        Fit = 1,
        Setup = 1,
        Instructions_Motion = 1, // Welcome instructions, includes tutorial instructions
        Instructions_Demo_Motion = 1,
        Instructions_Selection = 1,
        Instructions_Demo_Selection = 1,
        Instructions_Training = 1,
        Training_Trials_Binocular = 20, // Training trials, central presentation to both eyes
        Training_Trials_Monocular = 20, // Training trials, central presentation to one eye
        Training_Trials_Lateralized = 20, // Training trials, lateralized presentation to one eye
        Mid_Instructions = 1,
        Main_Trials_Binocular = 10, // Main trials, central presentation to both eyes
        Main_Trials_Monocular = 10, // Main trials, central presentation to one eye
        Main_Trials_Lateralized = 20, // Main trials, lateralized presentation to one eye
        Post_Instructions = 1,
    };

    // Define the order of UXF `Blocks` and their expected block numbers (non-zero indexed)
    private enum BlockSequence
    {
        Fit = 1,
        Setup = 2,
        Instructions_Motion = 3,
        Instructions_Demo_Motion = 4,
        Instructions_Selection = 5,
        Instructions_Demo_Selection = 6,
        Instructions_Training = 7,
        Training = 8,
        Mid_Instructions = 9,
        Main = 10,
        Post_Instructions = 11
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
    private readonly float POST_FIXATION_DURATION = 0.1f; // 500 milliseconds
    private readonly float PRE_DISPLAY_DURATION = 0.5f; // 500 milliseconds
    private readonly float DISPLAY_DURATION = 0.180f; // 180 milliseconds

    // Store references to Manager classes
    private StimulusManager stimulusManager;
    private UIManager uiManager;
    private CameraManager cameraManager;
    private SetupManager setupManager;

    // Store references to EyePositionTracker instances
    [Header("Gaze Fixation Parameters")]
    [Tooltip("Require a central fixation prior to presenting the trial stimuli")]
    public bool RequireFixation = true; // Require participant to be fixation on center before trial begins
    [Tooltip("Radius (in world units) around the central fixation point which registers as fixated")]
    public float FixationRadius = 0.5f; // Specify the fixation radius
    [Header("EyePositionTrackers")]
    public EyePositionTracker LeftEyeTracker;
    public EyePositionTracker RightEyeTracker;
    private int fixationMeasurementCounter = 0; // Counter for number of fixation measurements
    private readonly int REQUIRED_FIXATION_MEASUREMENTS = 48; // Required on-target fixation measurements

    // Input parameters
    private bool isInputEnabled = false; // Input is accepted
    private bool isInputReset = true; // Flag to prevent input being held down
    private InputState lastInputState; // Prior frame input state

    // Input button slider GameObjects and variables
    private readonly float TRIGGER_THRESHOLD = 0.8f;
    private readonly float JOYSTICK_THRESHOLD = 0.6f;
    private readonly float BUTTON_SLIDER_THRESHOLD = 0.99f;
    private readonly float BUTTON_HOLD_FACTOR = 2.0f;

    // Selected button state
    private int selectedButtonIndex = 1; // Starting index of 1, actual range [0, 3]
    private bool hasMovedSelection = false; // Initially `false` until first movement made

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
        session.CreateBlock((int)TrialCount.Fit); // Pre-experiment headset fit
        session.CreateBlock((int)TrialCount.Setup); // Pre-experiment setup
        session.CreateBlock((int)TrialCount.Instructions_Motion); // Pre-experiment instructions
        session.CreateBlock((int)TrialCount.Instructions_Demo_Motion); // Pre-experiment motion demo
        session.CreateBlock((int)TrialCount.Instructions_Selection); // Pre-experiment selection instructions
        session.CreateBlock((int)TrialCount.Instructions_Demo_Selection); // Pre-experiment selection demo
        session.CreateBlock((int)TrialCount.Instructions_Training); // Pre-experiment training instructions
        session.CreateBlock(trainingTimeline.Count); // Training trials
        session.CreateBlock((int)TrialCount.Mid_Instructions); // Mid-experiment instructions
        session.CreateBlock(mainTimeline.Count); // Main trials
        session.CreateBlock((int)TrialCount.Post_Instructions); // Post-experiment instructions

        // Collect references to other classes
        stimulusManager = GetComponent<StimulusManager>();
        uiManager = GetComponent<UIManager>();
        cameraManager = GetComponent<CameraManager>();
        setupManager = GetComponent<SetupManager>();

        // Update the CameraManager value for the aperture offset to be the stimulus radius
        cameraManager.SetStimulusWidth(stimulusManager.GetApertureWidth());
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
    /// <returns>`int` >= `1` if found, `-1` if no matching `TrialType` found</returns>
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

        // Restrict `kMed` value [0.12f, 0.5f]
        float kMed = coherences.Take(20).Median();
        kMed = kMed < 0.12f ? 0.12f : kMed;
        kMed = kMed > 0.5f ? 0.5f : kMed;

        return new Tuple<float, float>(0.5f * kMed, 2.0f * kMed);
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
    /// Setup the "Instructions" screens presented to participants
    /// </summary>
    private void SetupInstructions()
    {
        // Setup the UI manager with instructions
        List<string> Instructions = new();

        // Configure the list of instructions depending on the instruction block
        if (activeBlock == BlockSequence.Instructions_Motion)
        {
            // Instructions shown to the participant before the start of the experiment
            Instructions.AddRange(new List<string>{
                "During this task, you will be presented with a field of moving dots and a fixation cross. The dot movement will be visible for only a short period of time and will appear either around the cross or next to the cross.\n\n\nPress the <b>Trigger</b> to select <b>Next</b> and continue.",
                "During the trials, you <b>must maintain focus on the central fixation cross</b> whenever it is visible and during dot motion. The dots will be visible in either one eye or both eyes at once.\n\nSome of the dots will move only up or only down, and the rest of the dots will move randomly as a distraction.\n\n\nPress the <b>Trigger</b> to select <b>Next</b> and preview the dot movement.",
            });
        }
        else if (activeBlock == BlockSequence.Instructions_Selection)
        {
            // Instructions shown to the participant before the start of the experiment
            Instructions.AddRange(new List<string>{
                "After viewing the dot movement, you will be asked if you thought the dots moving together moved up or down.\n\nYou will have four options to choose from:\n<b>Up - Very Confident\t\t\t</b>\n<b>Up - Somewhat Confident\t\t</b>\n<b>Down - Somewhat Confident\t</b>\n<b>Down - Very Confident\t\t</b>\n\nPress the <b>Trigger</b> to select <b>Next</b> and continue.",
                "You <b>must</b> select one of the four options. Please choose the one that best represents your conclusion about how the dots were moving and your confidence in that conclusion.\n\nUse the <b>Joystick</b> to move the cursor between options, and hold the <b>Trigger</b> for approximately 1 second to select an option.\n\n\nPress the <b>Trigger</b> to select <b>Next</b> and practice making a selection.",
            });
        }
        else if (activeBlock == BlockSequence.Instructions_Training)
        {
            // Instructions shown to the participant before the start of the experiment
            Instructions.AddRange(new List<string>{
                "You will first play <b>" + trainingTimeline.Count + " training trials</b>. After the training trials, you will be shown an instruction screen before continuing to the main trials.\n\nYou are about to start the training trials.\n\n\nWhen you are ready and comfortable, press the <b>Trigger</b> to select <b>Continue</b> and begin.",
            });
        }
        else if (activeBlock == BlockSequence.Mid_Instructions)
        {
            // Instructions shown to the participant between the training trials and the main trials
            Instructions.AddRange(new List<string>{
                "That concludes all the training trials. You will now play <b>" + mainTimeline.Count + " main trials</b>.\n\nWhen you are ready and comfortable, press the <b>Trigger</b> to select <b>Next</b> and continue.",
            });
        }
        else if (activeBlock == BlockSequence.Post_Instructions)
        {
            // Instructions shown to the participant after they have completed all the trials
            Instructions.AddRange(new List<string>{
                "That concludes all the trials for this task. Please notify the experiment facilitator, and you can remove the headset carefully after releasing the rear adjustment wheel.",
            });
        }

        // Enable pagination if > 1 page of instructions, then set the active instructions
        uiManager.EnablePagination(Instructions.Count > 1);
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
            cameraManager.SetActiveField(UnityEngine.Random.value > 0.5 ? CameraManager.VisualField.Left : CameraManager.VisualField.Right, false);
        }
        else
        {
            // Activate one camera in lateralized mode, with lateralization enabled
            cameraManager.SetActiveField(UnityEngine.Random.value > 0.5 ? CameraManager.VisualField.Left : CameraManager.VisualField.Right, true);
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
        Session.instance.CurrentTrial.result["active_visual_field"] = activeVisualField.ToString();
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
        activeTrialType = TrialType.Instructions_Motion;
        switch (block) {
            case BlockSequence.Training:
                // Set the `activeTrialType` depending on the position within the `trainingTimeline`
                activeTrialType = trainingTimeline[RelativeTrialNumber];
                break;
            case BlockSequence.Mid_Instructions:
                activeTrialType = TrialType.Mid_Instructions;
                break;
            case BlockSequence.Main:
                // Set the `activeTrialType` depending on the position within the `mainTimeline`
                activeTrialType = mainTimeline[RelativeTrialNumber];
                break;
            case BlockSequence.Post_Instructions:
                activeTrialType = TrialType.Post_Instructions;
                break;
        }

        // Reset all displayed stimuli and UI
        stimulusManager.SetVisibleAll(false);
        uiManager.SetVisible(false);

        // Store the current `TrialType`
        Session.instance.CurrentTrial.result["trial_type"] = Enum.GetName(typeof(TrialType), activeTrialType);

        switch (block) {
            case BlockSequence.Fit:
                setupManager.SetViewCalibrationVisibility(true);

                // Input delay
                yield return StartCoroutine(WaitSeconds(2.0f, true));
                break;
            case BlockSequence.Setup:
                uiManager.SetVisible(true);
                uiManager.SetHeaderText("Eye-Tracking Setup");
                uiManager.SetBodyText("You will be shown a red dot in front of you. Follow the dot movement with your gaze.\n\nAfter a brief series of dot movements, the headset setup will be complete and you will be shown further instructions.\n\n\nWhen you are ready and comfortable, press the <b>Trigger</b> to select <b>Continue</b> and begin.");
                uiManager.SetLeftButtonState(false, false, "");
                uiManager.SetRightButtonState(true, true, "Continue");

                // Input delay
                yield return StartCoroutine(WaitSeconds(0.25f, true));
                break;
            case BlockSequence.Instructions_Motion:
                SetupInstructions();
                uiManager.SetVisible(true);
                uiManager.SetHeaderText("Instructions");
                uiManager.SetLeftButtonState(false, false, "Back");
                uiManager.SetRightButtonState(true, true, "Next");

                // Input delay
                yield return StartCoroutine(WaitSeconds(0.25f, true));
                break;
            case BlockSequence.Instructions_Demo_Motion:
                // Run setup for the motion stimuli
                SetupMotion(TrialType.Training_Trials_Binocular);
                yield return StartCoroutine(DisplayMotion(3.0f));
                EndTrial();
                break;
            case BlockSequence.Instructions_Selection:
                SetupInstructions();
                uiManager.SetVisible(true);
                uiManager.SetHeaderText("Instructions");
                uiManager.SetLeftButtonState(false, false, "Back");
                uiManager.SetRightButtonState(true, true, "Next");

                // Input delay
                yield return StartCoroutine(WaitSeconds(0.25f, true));
                break;
            case BlockSequence.Instructions_Demo_Selection:
                // Run setup for the decision stimuli
                SetupMotion(TrialType.Training_Trials_Binocular);
                yield return StartCoroutine(DisplaySelection());
                break;
            case BlockSequence.Instructions_Training:
                SetupInstructions();
                uiManager.SetVisible(true);
                uiManager.SetHeaderText("Instructions");
                uiManager.SetLeftButtonState(false, false, "Back");
                uiManager.SetRightButtonState(true, true, "Next");

                // Input delay
                yield return StartCoroutine(WaitSeconds(0.25f, true));
                break;
            case BlockSequence.Training:
            case BlockSequence.Main:
                // Run setup for the motion stimuli
                SetupMotion(activeTrialType);
                yield return StartCoroutine(DisplayMotion(DISPLAY_DURATION));
                yield return StartCoroutine(DisplaySelection());
                break;
            case BlockSequence.Mid_Instructions:
                SetupInstructions();

                // Run calibration calculations
                CalculateCoherences();

                // Override and set the camera to display in both eyes
                cameraManager.SetActiveField(CameraManager.VisualField.Both);

                uiManager.SetVisible(true);
                uiManager.SetHeaderText("Main Trials");
                uiManager.SetLeftButtonState(false, false, "Back");
                uiManager.SetRightButtonState(true, true, "Continue");

                // Input delay
                yield return StartCoroutine(WaitSeconds(0.15f, true));
                break;
            case BlockSequence.Post_Instructions:
                SetupInstructions();
                // Override and set the camera to display in both eyes
                cameraManager.SetActiveField(CameraManager.VisualField.Both);

                uiManager.SetVisible(true);
                uiManager.SetHeaderText("Complete");
                uiManager.SetLeftButtonState(false, false, "Back");
                uiManager.SetRightButtonState(true, true, "Finish");

                // Input delay
                yield return StartCoroutine(WaitSeconds(1.0f, true));
                break;
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
    private IEnumerator DisplayMotion(float duration)
    {
        // Disable input and wait for fixation if enabled
        SetIsInputEnabled(false);
        stimulusManager.SetFixationCrossVisibility(true);
        if (RequireFixation)
        {
            yield return new WaitUntil(() => IsFixated());
        }
        yield return StartCoroutine(WaitSeconds(POST_FIXATION_DURATION, true));

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
        yield return StartCoroutine(WaitSeconds(duration, true));
        stimulusManager.SetVisible(StimulusType.Motion, false);
        stimulusManager.SetFixationCrossVisibility(false);
    }

    private IEnumerator DisplaySelection()
    {
        // Present decision stimulus and wait for response
        Session.instance.CurrentTrial.result["decision_start"] = Time.time;
        stimulusManager.ResetCursor();
        stimulusManager.SetCursorSide(activeVisualField == CameraManager.VisualField.Left ? StimulusManager.CursorSide.Right : StimulusManager.CursorSide.Left);
        stimulusManager.SetCursorVisiblity(true);
        stimulusManager.SetVisible(StimulusType.Decision, true);
        yield return StartCoroutine(WaitSeconds(0.1f, true));
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

        // Reset the button and UI states and end the current `Trial`
        isInputReset = false;
        hasMovedSelection = false;
        stimulusManager.SetCursorVisiblity(false);
        ResetButtons();
        EndTrial();
    }

    public void EndTrial()
    {
        // Store a timestamp and end the trial
        Session.instance.CurrentTrial.result["trial_end"] = Time.time;
        Session.instance.EndCurrentTrial();

        // Reset the active visual field
        cameraManager.SetActiveField(CameraManager.VisualField.Both, false);

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
        Vector3 worldPosition = stimulusManager.GetFixationAnchor().transform.position;

        bool isFixated = false; // Fixated state
        // If the gaze is directed in fixation, increment the counter to signify a measurement
        if ((Mathf.Abs(leftGaze.x - worldPosition.x) <= FixationRadius && Mathf.Abs(leftGaze.y - worldPosition.y) <= FixationRadius) || (Mathf.Abs(rightGaze.x - worldPosition.x) <= FixationRadius && Mathf.Abs(rightGaze.y - worldPosition.y) <= FixationRadius))
        {
            fixationMeasurementCounter += 1;
        }

        // Register as fixated if the required number of measurements have been taken
        if (fixationMeasurementCounter >= REQUIRED_FIXATION_MEASUREMENTS)
        {
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
        return activeBlock == BlockSequence.Instructions_Motion ||
            activeBlock == BlockSequence.Instructions_Selection ||
            activeBlock == BlockSequence.Instructions_Training ||
            activeBlock == BlockSequence.Mid_Instructions ||
            activeBlock == BlockSequence.Post_Instructions;
    }

    private bool IsStimulusScreen()
    {
        return activeBlock == BlockSequence.Instructions_Demo_Motion ||
            activeBlock == BlockSequence.Instructions_Demo_Selection ||
            activeBlock == BlockSequence.Training ||
            activeBlock == BlockSequence.Main;
    }

    private bool IsSetupScreen()
    {
        return activeBlock == BlockSequence.Setup;
    }

    private bool IsFitScreen()
    {
        return activeBlock == BlockSequence.Fit;
    }

    /// <summary>
    /// Input function to handle `InputState` object and update button presentation or take action depending on
    /// the active `TrialType`
    /// </summary>
    /// <param name="inputs">`InputState` object</param>
    private void ApplyInputs(InputState inputs)
    {
        ButtonSliderInput[] buttonControllers = stimulusManager.GetButtonSliders();

        // Increment button selection
        if ((inputs.L_J_State.y > JOYSTICK_THRESHOLD || inputs.R_J_State.y > JOYSTICK_THRESHOLD) && isInputReset)
        {
            DecrementButtonSelection();
            isInputReset = false;

            // Update the cursor position
            stimulusManager.SetCursorIndex(selectedButtonIndex);
        }

        // Decrement button selection
        if ((inputs.L_J_State.y < -JOYSTICK_THRESHOLD || inputs.R_J_State.y < -JOYSTICK_THRESHOLD) && isInputReset)
        {
            IncrementButtonSelection();
            isInputReset = false;

            // Update the cursor position
            stimulusManager.SetCursorIndex(selectedButtonIndex);
        }

        // Apply trigger input if trigger inputs are active and a selection has been made
        if ((VRInput.LeftTrigger() || VRInput.RightTrigger()) && hasMovedSelection)
        {
            buttonControllers[selectedButtonIndex].SetSliderValue(buttonControllers[selectedButtonIndex].GetSliderValue() + BUTTON_HOLD_FACTOR * Time.deltaTime);

            // Signify that these inputs are new, store `last_keypress_start` timestamp
            if ((lastInputState.L_T_State < TRIGGER_THRESHOLD && VRInput.LeftTrigger()) ||
                (lastInputState.R_T_State < TRIGGER_THRESHOLD && VRInput.RightTrigger()))
            {
                Session.instance.CurrentTrial.result["last_keypress_start"] = Time.time;
            }
        }

        // Audit button state and handle a button selection if the button has been selected for the required duration
        if (buttonControllers[selectedButtonIndex].GetSliderValue() >= BUTTON_SLIDER_THRESHOLD)
        {
            // Provide haptic feedback
            VRInput.SetHaptics(15.0f, 0.4f, 0.1f, true, true);
            Session.instance.CurrentTrial.result["last_keypress_end"] = Time.time;

            // Store selected button response
            switch (selectedButtonIndex)
            {
                case 0:
                    HandleSelection("vc_u");
                    break;
                case 1:
                    HandleSelection("sc_u");
                    break;
                case 2:
                    HandleSelection("sc_d");
                    break;
                case 3:
                    HandleSelection("vc_d");
                    break;
            }

            // Reset the slider value of the selected button
            buttonControllers[selectedButtonIndex].SetSliderValue(0.0f);
        }

        // Update the prior input state
        lastInputState = inputs;
    }

    /// <summary>
    /// Utility function to reset the state of all `ButtonSliderFill` buttons
    /// </summary>
    private void ResetButtons()
    {
        foreach (ButtonSliderInput button in stimulusManager.GetButtonSliders())
        {
            button.SetSliderValue(0.0f);
        }
    }

    /// <summary>
    /// Increment the selected button, based on input from the controller joystick
    /// </summary>
    private void IncrementButtonSelection()
    {
        if (hasMovedSelection == false)
        {
            // Initial button press
            selectedButtonIndex = 2;
            hasMovedSelection = true;
        }
        else if (selectedButtonIndex < 3)
        {
            selectedButtonIndex += 1;
        }
    }

    /// <summary>
    /// Decrement the selected button, based on input from the controller joystick
    /// </summary>
    private void DecrementButtonSelection()
    {
        if (hasMovedSelection == false)
        {
            // Initial button press
            selectedButtonIndex = 1;
            hasMovedSelection = true;
        }
        else if (selectedButtonIndex > 0)
        {
            selectedButtonIndex -= 1;
        }
    }

    void Update()
    {
        // Inputs:
        // - Trigger (any controller): Advance instructions page, (hold) select button
        // - Joystick (any controller): Directional selection of buttons
        if (isInputEnabled)
        {
            // Get the current input state across both controllers
            InputState inputs = VRInput.PollAllInput();

            // Handle input on a stimulus screen
            if (IsStimulusScreen() && isInputReset)
            {
                // Take action as specified by the inputs
                ApplyInputs(inputs);

                if (!VRInput.AnyInput())
                {
                    ResetButtons();
                }
            }
            else
            {
                // Trigger controls
                if (VRInput.LeftTrigger() || VRInput.RightTrigger())
                {
                    if (IsInstructionsScreen() && isInputReset)
                    {
                        if (uiManager.HasNextPage())
                        {
                            // If pagination has next page, advance
                            uiManager.NextPage();
                            SetIsInputEnabled(true);

                            // Trigger controller haptics
                            VRInput.SetHaptics(15.0f, 0.4f, 0.1f, true, true);

                            // Update the "Next" button if the last page
                            if (!uiManager.HasNextPage())
                            {
                                uiManager.SetRightButtonState(true, true, "Continue");
                            }
                        }
                        else
                        {
                            // Trigger controller haptics
                            VRInput.SetHaptics(15.0f, 0.4f, 0.1f, true, true);

                            EndTrial();
                        }
                        isInputReset = false;
                    }
                    else if (IsSetupScreen() && isInputReset)
                    {
                        // Hide the UI
                        uiManager.SetVisible(false);

                        // Only provide haptic feedback before calibration is run
                        if (!setupManager.GetCalibrationActive() && !setupManager.GetCalibrationComplete())
                        {
                            // Trigger controller haptics
                            VRInput.SetHaptics(15.0f, 0.4f, 0.1f, true, true);
                        }

                        // Trigger eye-tracking calibration the end the trial
                        setupManager.RunSetup(() =>
                        {
                            EndTrial();
                        });
                        isInputReset = false;
                    }
                    else if (IsFitScreen() && isInputReset)
                    {
                        // Hide the fit calibration screen
                        setupManager.SetViewCalibrationVisibility(false);

                        // Trigger controller haptics
                        VRInput.SetHaptics(15.0f, 0.4f, 0.1f, true, true);

                        EndTrial();
                        isInputReset = false;
                    }
                }
            }

            // Reset input state to prevent holding buttons to repeatedly select options
            if (isInputReset == false && !VRInput.AnyInput())
            {
                isInputReset = true;
            }
        }
    }
}
