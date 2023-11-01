// Third-party imports
import { Vector3, Group, Quaternion } from 'three';
import { update as tweenUpdate } from '@tweenjs/tween.js'; // https://github.com/tweenjs/tween.js/

// Package imports
import { Experiment, InstructionsPanel, Block } from 'ouvrai';

// Static asset imports (https://vitejs.dev/guide/assets.html)
import environmentLightingURL from 'ouvrai/lib/environments/IndoorHDRI003_1K-HDR.exr?url'; // absolute path from ouvrai

// Custom classes
import Renderer from './classes/Renderer';
import Graphics from './classes/Graphics';

/*
 * Main function contains all experiment logic. At a minimum you should:
 * 1. Create a `new Experiment({...config})`
 * 2. Initialize the state machine with `exp.state.init(states, changeFunc)`
 * 3. Create stimuli and add them with `exp.sceneManager.scene.add(...objects)`
 * 4. Create trial sequence with `exp.createTrialSequence([...blocks])`
 * 5. Start the main loop with `exp.start(calcFunc, stateFunc, displayFunc)`
 * 6. Design your experiment by editing `calcFunc`, `stateFunc`, and `displayFunc`
 */

async function main() {
  // Configure your experiment
  const exp = new Experiment({
    // Options to make development easier
    devOptions: {
      skipConsent: true,
      orbitControls: false,
    },
    demo: false,

    // Platform settings
    requireVR: true,
    handTracking: false,
    controllerModels: false,

    // Three.js settings
    environmentLighting: environmentLightingURL,
    gridRoom: false,
    backgroundColor: 'black',
    audio: true,

    // Assume meters and seconds for three.js, but note tween.js uses milliseconds
    homePosn: new Vector3(0, 1.75, -0.3),
    cameraLayout: 0,
    cameraFixed: true,

    // Frame and rendering count
    frameCount: 0,

    // Input configuration
    input: {
      left: 'f',
      right: 'j',
    },

    // Experiment behavior
    confidenceGap: 3,

    // Trial structure
    numTutorialTrials: 10,
    numPracticeTrials: 10,
    numCalibrationTrials: 120,

    // Trial durations
    fixationDuration: 0.25,
    feedbackDuration: 0.4,
  });

  /**
   * Initialize Finite State Machine (FSM) that manages the flow of your experiment.
   * You will define the behavior and transitions of the FSM below in stateFunc().
   */
  exp.state.init(
    [
      'CONSENT',
      'SIGNIN',
      'INSTRUCTIONS',
      'START',
      'FIXATION',
      'MOTION',
      'RESPONSE',
      'FEEDBACK',
      'CONFIDENCE',
      'FINISH',
      'ADVANCE',
      'DONE',
      'CONTROLLER',
      'DATABASE',
      'BLOCKED',
    ],
    handleStateChange
  );

  // Short instruction panel telling them to click ENTER VR
  exp.instructions = new InstructionsPanel({
    content: `Click the ENTER VR button to start.\nYou will see more instructions in VR.`,
    collapsible: false,
  });

  /*
   * Create visual stimuli with three.js
   */
  // Background elements
  const backgroundGroup = new Group();
  backgroundGroup.position.copy(exp.cfg.homePosn);
  exp.sceneManager.scene.add(backgroundGroup);

  // Task "root"
  const taskGroup = new Group();
  taskGroup.position.copy(exp.cfg.homePosn);
  exp.sceneManager.scene.add(taskGroup);

  // Create new Renderers and Graphics
  const BackgroundRenderer = new Renderer(backgroundGroup);
  const BackgroundGraphics = new Graphics(BackgroundRenderer);
  const TaskRenderer = new Renderer(taskGroup);
  const TaskGraphics = new Graphics(TaskRenderer);

  // Attach the camera to the task if specified
  if (exp.cfg.cameraFixed) {
    exp.sceneManager.camera.attach(taskGroup);
  }

  // Setup the background prior to starting the task
  BackgroundGraphics.addBackground();

  /*
   * Create trial sequence from array of block objects.
   */
  exp.createTrialSequence([
    new Block({
      variables: {
        coherence: 0.8,
        duration: 2,
        showFeedback: true,
      },
      options: {
        name: 'tutorial',
        reps: exp.cfg.numTutorialTrials,
      },
    }),
    new Block({
      variables: {
        coherence: 0.8,
        duration: 2,
        showFeedback: false,
      },
      options: {
        name: 'practice',
        reps: exp.cfg.numPracticeTrials,
      },
    }),
    new Block({
      variables: {
        coherence: 0.8,
        duration: 2,
        showFeedback: false,
      },
      options: {
        name: 'calibration',
        reps: exp.cfg.numCalibrationTrials,
      },
    }),
  ]);

  /*
   * You must initialize an empty object called trial
   */
  let trial = {};

  // Start the main loop! These three functions will take it from here.
  exp.start(calcFunc, stateFunc, displayFunc);

  /**
   * Bind an event listener to listen for keyboard events
   */
  window.addEventListener('keyup', inputFunc);
  function inputFunc(event) {
    if (event.key) {
      switch (exp.state.current) {
        case 'RESPONSE':
          const validInput = [exp.cfg.input.left, exp.cfg.input.right];
          if (validInput.includes(event.key)) {
            // Valid input was received, store responses and outcome
            if (event.key === exp.cfg.input.left) {
              trial.data.response = 'left';
              trial.data.correct = trial.referenceDirection === Math.PI ? 1 : 0;
            } else {
              trial.data.response = 'right';
              trial.data.correct = trial.referenceDirection === 0 ? 1 : 0;
            }
            // Clear graphics and proceed to next state
            TaskGraphics.clear();
            if (trial.showFeedback) {
              exp.state.next('FEEDBACK');
            } else {
              // Check if we need to show confidence
              if (trial.trialNumber % exp.cfg.confidenceGap === 0) {
                exp.state.next('CONFIDENCE');
              } else {
                exp.state.next('FINISH');
              }
            }
          }
          break;
        case 'CONFIDENCE':
          if (event.key === exp.cfg.input.right) {
            exp.state.next('FINISH');
          }
          break;
      }
    }
  }

  /**
   * Use `calcFunc` for calculations used in _multiple states_
   */
  function calcFunc() {}

  /**
   * Define your procedure as a switch statement implementing a Finite State Machine.
   * Ensure that all states are listed in the array given to `exp.state.init()`
   * @method `exp.state.next(state)` Transitions to new state on next loop.
   * @method `exp.state.once(function)` Runs function one time on entering state.
   */
  function stateFunc() {
    /**
     * If one of these checks fails, divert into an interrupt state.
     * Interrupt states wait for the condition to be satisfied, then return to the previous state.
     * Interrupt states are included at the end of the stateFunc() switch statement.
     */
    if (exp.databaseInterrupt()) {
      exp.blocker.show('database');
      exp.state.push('DATABASE');
    } else if (exp.controllerInterrupt(false, true)) {
      exp.state.push('CONTROLLER');
    }

    switch (exp.state.current) {
      // CONSENT state can be left alone
      case 'CONSENT':
        exp.state.once(function () {
          if (exp.checkDeviceCompatibility()) {
            exp.state.next('BLOCKED');
          } else {
            exp.consent.show();
          }
        });
        if (exp.waitForConsent()) {
          exp.state.next('SIGNIN');
        }
        break;

      // SIGNIN state can be left alone
      case 'SIGNIN':
        if (exp.waitForAuthentication()) {
          exp.state.next('START');
        }
        break;

      case 'INSTRUCTIONS':
        exp.state.once(() => {
          exp.VRUI.visible = false;
        });
        if (exp.VRUI.clickedNext) exp.state.next('CALIBRATE');
        break;

      case 'START':
        exp.state.once(() => {
          exp.VRUI.visible = false;

          // Modifications to coherence if required
          let updateCoherence = false;
          let coherenceDelta = 0;

          // Update the coherence as required
          if (trial.trialNumber >= 0 && trial.block.name === 'calibration') {
            if (trial.data.correct === 0) {
              // Update the coherence if there has been one incorrect trial
              updateCoherence = true;
              coherenceDelta = 0.01;
            } else if (
              trial.data.correct === 1 &&
              trial.lastTrial.data &&
              trial.lastTrial.data.correct === 1
            ) {
              // Examine condition across two trials
              if (trial.coherence === trial.lastTrial.coherence) {
                // Update the coherence if there have been two correct trials
                updateCoherence = true;
                coherenceDelta = -0.01;
              }
            }
          }

          // Store previous trial information, excluding 'lastTrial'
          const { lastTrial: _, ...lastTrial } = trial;

          // Copy and instantiate the 'trial' object
          trial = structuredClone(exp.trials[exp.trialNumber]);
          trial.lastTrial = lastTrial;
          trial.trialNumber = exp.trialNumber;
          trial.startTime = performance.now();
          trial.referenceDirection = Math.random() > 0.5 ? Math.PI : 0;

          // Adjust the coherence if required
          if (trial.block.name === 'calibration') {
            // Copy the coherences from the previous trial
            if (trial.trialNumber > 0) {
              trial.coherence = trial.lastTrial.coherence;
            }
            // Apply an update to the coherence if calculated
            if (updateCoherence) {
              trial.coherence = trial.coherence + coherenceDelta;
            }
          }

          // Create the 'trial.data' structure
          trial.data = {
            response: null,
            correct: 0,
          };

          exp.state.next('FIXATION');
        });
        break;

      case 'FIXATION':
        exp.state.once(() => {
          exp.VRUI.visible = false;

          // Construct 'FIXATION'-type stimulus
          TaskGraphics.addBackground();
          TaskGraphics.addOutline();
          TaskGraphics.addFixation();
        });

        // Proceed to the next state upon time expiration
        if (exp.state.expired(exp.cfg.fixationDuration)) {
          TaskGraphics.clear();
          exp.state.next('MOTION');
        }
        break;

      case 'MOTION':
        exp.state.once(() => {
          exp.VRUI.visible = false;

          // Construct 'MOTION'-type stimulus
          TaskGraphics.addOutline();
          TaskGraphics.addFixation();
          TaskGraphics.addDots(trial.coherence, trial.referenceDirection);
        });

        // Proceed to the next state upon time expiration
        if (exp.state.expired(trial.duration)) {
          TaskGraphics.clear();
          exp.state.next('RESPONSE');
        }
        break;

      case 'RESPONSE':
        exp.state.once(() => {
          exp.VRUI.visible = false;
          // Construct 'RESPONSE'-type stimulus
          TaskGraphics.addOutline();
          TaskGraphics.addFixation();
          TaskGraphics.addLeftArc();
          TaskGraphics.addRightArc();
        });
        break;

      case 'CONFIDENCE':
        exp.state.once(() => {
          exp.VRUI.visible = false;
          // Construct 'CONFIDENCE'-type stimulus
        });
        break;

      case 'FEEDBACK':
        exp.state.once(() => {
          exp.VRUI.visible = false;
          // Construct 'FEEDBACK'-type stimulus
          TaskGraphics.addOutline();
          if (trial.data.correct === 1) {
            TaskGraphics.addFixation('green');
          } else {
            TaskGraphics.addFixation('red');
          }
        });
        if (exp.state.expired(exp.cfg.feedbackDuration)) {
          if (trial.block.name === 'tutorial') {
            TaskGraphics.clear();
            exp.state.next('FINISH');
          }
        }
        break;

      case 'FINISH':
        exp.state.once(function () {
          TaskRenderer.clearElements();
        });
        // Save immediately prior to state transition (ensures one save per trial)
        exp.firebase.saveTrial(trial);
        exp.state.next('ADVANCE');
        break;

      case 'ADVANCE':
        if (!exp.firebase.saveSuccessful) {
          break; // wait until firebase save returns successful
        } else if (exp.firebase.saveFailed) {
          exp.blocker.fatal(err);
          exp.state.push('BLOCKED');
        }
        exp.nextTrial();
        if (exp.trialNumber < exp.numTrials) {
          exp.state.next('START');
        } else {
          exp.complete();

          // Clean up
          taskGroup.visible = false;
          exp.state.next('DONE');
        }
        break;

      case 'DONE':
        if (!exp.firebase.saveSuccessful) {
          break;
        }
        exp.state.once(function () {
          exp.goodbye.show(); // show the goodbye screen
          exp.VRUI.edit({
            title: 'Complete',
            instructions:
              'Thank you. Exit VR to find the submission link on the study web page.',
            interactive: true,
            backButtonState: 'disabled',
            nextButtonState: 'idle',
            nextButtonText: 'Exit',
          });
        });
        if (exp.VRUI.clickedNext) {
          exp.xrSession.end();
        }
        break;

      case 'CONTROLLER':
        exp.state.once(function () {
          if (exp.state.last !== 'REST') {
            exp.VRUI.edit({
              title: 'Controller?',
              instructions: 'Please connect right hand controller.',
              buttons: false,
              interactive: false,
            });
          }
        });
        if (!exp.controllerInterrupt(false, true)) {
          exp.state.pop();
        }
        break;

      case 'DATABASE':
        exp.state.once(function () {
          exp.blocker.show('database');
          exp.VRUI.edit({
            title: 'Not connected',
            instructions:
              'Your device is not connected to the internet. Reconnect to resume.',
            buttons: false,
            interactive: false,
          });
        });
        if (!exp.databaseInterrupt()) {
          exp.blocker.hide();
          exp.state.pop();
        }
        break;

      case 'BLOCKED':
        break;
    }
  }

  /**
   * Compute and update stimulus and UI presentation.
   */
  function displayFunc() {
    tweenUpdate();
    exp.VRUI.updateUI();
    exp.sceneManager.render();

    // Execute the "step()" function on all animated components
    TaskRenderer.getElements().forEach((element) => {
      element.step(
        exp.cfg.frameCount,
        exp.sceneManager.renderer.xr.isPresenting
      );
    });

    // Increment the frame count
    exp.cfg.frameCount += 1;
  }

  /**
   * Event handlers
   */
  // Record state transition data
  function handleStateChange() {
    trial?.stateChange?.push(exp.state.current);
    trial?.stateChangeTime?.push(performance.now());
    // Head data at state changes only (see handleFrameData)
    trial?.stateChangeHeadPos?.push(
      exp.sceneManager.camera.getWorldPosition(new Vector3())
    );
    trial?.stateChangeHeadOri?.push(
      exp.sceneManager.camera.getWorldQuaternion(new Quaternion())
    );
  }
}

window.addEventListener('DOMContentLoaded', main);
