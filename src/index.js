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

// Data manipulation imports
import * as d3 from 'd3-array';
import _ from 'lodash';

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
      left: '2',
      right: '7',
    },

    // Experiment behavior
    confidenceGap: 3,

    // Trial structure, n: number of repetitions (n * 2)
    numTutorialTrials: 1,
    numPracticeTrials: 1,
    numCalibrationTrials: 2,
    numMainTrials: 1,

    // Trial durations
    fixationDuration: 1,
    postResponseDuration: 0.25,
    feedbackDuration: 0.25,
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
      'POSTRESPONSE',
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
  // Task "root"
  const taskGroup = new Group();
  taskGroup.position.copy(exp.cfg.homePosn);
  exp.sceneManager.scene.add(taskGroup);

  // Create new Renderers and Graphics
  const TaskRenderer = new Renderer(taskGroup);
  const TaskGraphics = new Graphics(TaskRenderer);

  // Attach the camera to the task if specified
  if (exp.cfg.cameraFixed) {
    exp.sceneManager.camera.attach(taskGroup);
  }

  /*
   * Create trial sequence from array of block objects.
   */
  exp.createTrialSequence([
    new Block({
      variables: {
        coherence: 0.3,
        coherences: [0.3, 0.6],
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
        coherence: 0.3,
        coherences: [0.3, 0.6],
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
        coherence: 0.2,
        coherences: [0.2, 0.2],
        duration: 2,
        showFeedback: false,
      },
      options: {
        name: 'calibration',
        reps: exp.cfg.numCalibrationTrials,
      },
    }),
    new Block({
      variables: {
        coherence: 0.2,
        coherences: [0.2, 0.2],
        duration: 2,
        showFeedback: false,
      },
      options: {
        name: 'main',
        reps: exp.cfg.numMainTrials,
      },
    }),
  ]);

  /**
   * Array to store data generated by the experience
   */
  let data = [];

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
              exp.state.next('POSTRESPONSE');
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

          // Copy and instantiate the 'trial' object
          trial = structuredClone(exp.trials[exp.trialNumber]);
          trial.trialNumber = exp.trialNumber;
          trial.startTime = performance.now();
          trial.referenceDirection = Math.random() > 0.5 ? Math.PI : 0;

          // Assign coherence to the trial
          if (data.length > 0) {
            let lastTrial = data[data.length - 1];
            trial.coherence = lastTrial.coherence;
            trial.coherences = lastTrial.coherences;
          }

          /**
           * 'main'-type operations
           */
          if (trial.block.name === 'main') {
            if (trial.block.trial === 0) {
              console.info("First 'main' trial");
              let trials = d3.filter(
                data,
                (t) => t.block.name === 'calibration'
              );
              trials = _.takeRight(trials, 4);
              let kArray = d3.map(data, (d) => d.coherence);
              let kMedian = d3.median(kArray);
              console.info('Median coherence:', kMedian);
              if (kMedian > 0.5) {
                kMedian = 0.5;
              } else if (kMedian < 0.12) {
                kMedian = 0.12;
              }
              trial.coherences = [kMedian * 0.5, kMedian * 2.0];
            }
            // Select a coherence
            trial.coherence = trial.coherences[Math.random() > 0.5 ? 0 : 1];
            console.info('Main trial k =', trial.coherence);
          }

          /**
           * 'calibration'-type operations
           */
          if (trial.block.name === 'calibration') {
            // Update the coherence of the current trial based on the outcome
            // of up to the last two trials
            let trials = d3.filter(data, (t) => t.block.name === 'calibration');
            if (trials.length > 0) {
              let lastTrial = trials[trials.length - 1];
              if (lastTrial.data.correct === 0) {
                // If last answer was incorrect, increase the coherence
                trial.coherence = trial.coherence + 0.01;
              } else if (trials.length > 1) {
                // If the previous two answers were correct, decrease the coherence
                let a = lastTrial;
                let b = trials[trials.length - 2];
                if (a.data.correct === 1 && b.data.correct === 1) {
                  trial.coherence = trial.coherence - 0.01;
                }
              }
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
          TaskGraphics.addBackground();
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
          TaskGraphics.addBackground();
          TaskGraphics.addOutline();
          TaskGraphics.addFixation();
          TaskGraphics.addLeftArc();
          TaskGraphics.addRightArc();
        });
        break;

      case 'POSTRESPONSE':
        exp.state.once(() => {
          exp.VRUI.visible = false;

          // Construct 'FIXATION'-type stimulus
          TaskGraphics.addBackground();
          TaskGraphics.addOutline();
          TaskGraphics.addFixation();
        });

        // Proceed to the next state upon time expiration
        if (exp.state.expired(exp.cfg.postResponseDuration)) {
          TaskGraphics.clear();
          // Check if we need to show confidence
          if (trial.trialNumber % exp.cfg.confidenceGap === 0) {
            exp.state.next('CONFIDENCE');
          } else {
            exp.state.next('FINISH');
          }
        }
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
          TaskGraphics.addBackground();
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
        data.push(trial); // Add trial data
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
