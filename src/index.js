// Third-party imports
import { Vector3, Group, Quaternion } from 'three';
import { update as tweenUpdate } from '@tweenjs/tween.js'; // https://github.com/tweenjs/tween.js/

// Package imports
import { Experiment, Block } from 'ouvrai';

// Custom classes
import Stimulus from './classes/Stimulus';

// Data manipulation imports
import * as d3 from 'd3-array';
import _ from 'lodash';

// Visual constants
const VIEW_DISTANCE = 2.0;
const VIEW_SCALE = 3.0;
const DEFAULT_CAMERA_LAYOUT = 2;

/*
 * Main function contains all experiment logic
 */

async function main() {
  const exp = new Experiment({
    devOptions: {
      skipConsent: true,
      orbitControls: false,
    },
    demo: false,
    noPoints: true,

    // Platform settings
    requireVR: true,
    handTracking: false,
    controllerModels: false,

    // Three.js settings
    backgroundColor: 'black',
    audio: true,
    taskPosition: new Vector3(0, 1.6, -VIEW_DISTANCE * VIEW_SCALE),
    cameraLayout: 2,

    // Frame and rendering count
    frameCount: 0,

    // Input configuration
    input: {
      left: '2',
      right: '7',
    },

    // Experiment behavior
    confidenceGap: 2,

    // Trial structure, n: number of repetitions (n * 2)
    numTutorialTrials: 2,
    numPracticeTrials: 2,
    numCalibrationTrials: 4,
    numMainTrials: 4,

    // Trial durations
    fixationDuration: 1.0, // seconds
    postResponseDuration: 0.25, // seconds
    feedbackDuration: 0.25, // seconds
  });

  /**
   * Initialize Finite State Machine (FSM) that manages the flow of your experiment.
   * You will define the behavior and transitions of the FSM below in stateFunc().
   */
  exp.state.init(
    [
      'CONSENT',
      'SIGNIN',
      'WELCOME',
      'PRETUTORIAL',
      'PREPRACTICE',
      'PREMAIN',
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

  /*
   * Create task 'Stimulus' instance
   */
  const taskGroup = new Group();
  taskGroup.position.copy(exp.cfg.taskPosition);
  exp.sceneManager.scene.add(taskGroup);
  const stimulus = new Stimulus(taskGroup, VIEW_DISTANCE);

  /*
   * Create trial sequence
   */
  exp.createTrialSequence([
    new Block({
      variables: {
        coherence: 0.3,
        coherences: [0.3, 0.6],
        showFeedback: false,
        cameraLayout: DEFAULT_CAMERA_LAYOUT,
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
        showFeedback: true,
        cameraLayout: DEFAULT_CAMERA_LAYOUT,
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
        showFeedback: false,
        cameraLayout: DEFAULT_CAMERA_LAYOUT,
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
        showFeedback: false,
        cameraLayout: DEFAULT_CAMERA_LAYOUT,
      },
      options: {
        name: 'main',
        reps: exp.cfg.numMainTrials,
      },
    }),
  ]);

  /**
   * Input mapping
   * Implement a function to poll the gamepad button schema, checking for
   * buttons that are pressed or have changed state
   */
  let input = {
    left: {
      x: false,
    },
    right: {
      a: false,
    },
  };

  /**
   * Bind an interval to poll controller input
   */
  window.setInterval(pollControllerInput, 10, input);
  function pollControllerInput(inputState) {
    if (exp.sceneManager.renderer.xr.isPresenting) {
      // 'A' button
      if (exp.rightGrip.input.gamepad.buttons[4].pressed === true) {
        if (inputState.right.a === false) {
          inputState.right.a = true;
          window.dispatchEvent(
            new KeyboardEvent('keyup', { key: exp.cfg.input.right })
          );
        }
      }
      if (exp.rightGrip.input.gamepad.buttons[4].pressed === false) {
        inputState.right.a = false;
      }

      // 'X' button
      if (exp.leftGrip.input.gamepad.buttons[4].pressed === true) {
        if (inputState.left.x === false) {
          inputState.left.x = true;
          window.dispatchEvent(
            new KeyboardEvent('keyup', { key: exp.cfg.input.left })
          );
        }
      }
      if (exp.leftGrip.input.gamepad.buttons[4].pressed === false) {
        inputState.left.x = false;
      }
    }
  }

  /**
   * Bind an event listener to listen for keyboard events
   */
  window.addEventListener('keyup', keyboardInput);
  function keyboardInput(event) {
    if (event.key) {
      switch (exp.state.current) {
        case 'WELCOME':
          if (event.key === exp.cfg.input.right) {
            exp.state.next('PRETUTORIAL');
          }
          break;
        case 'PRETUTORIAL':
        case 'PREPRACTICE':
        case 'PREMAIN':
          if (event.key === exp.cfg.input.right) {
            exp.state.next('START');
          }
          break;
        case 'RESPONSE':
          const responseInput = [exp.cfg.input.left, exp.cfg.input.right];
          if (responseInput.includes(event.key)) {
            // Valid input was received, store responses and outcome
            if (event.key === exp.cfg.input.left) {
              trial.data.response = 'left';
              trial.data.correct = trial.referenceDirection === Math.PI ? 1 : 0;
            } else {
              trial.data.response = 'right';
              trial.data.correct = trial.referenceDirection === 0 ? 1 : 0;
            }
            // Clear graphics and proceed to next state
            stimulus.reset();
            if (trial.showFeedback) {
              exp.state.next('FEEDBACK');
            } else {
              exp.state.next('POSTRESPONSE');
            }
          }
          break;
        case 'CONFIDENCE':
          const confidenceInput = [exp.cfg.input.left, exp.cfg.input.right];
          if (confidenceInput.includes(event.key)) {
            if (event.key === exp.cfg.input.right) {
              trial.data.confidence = 1;
              exp.state.next('FINISH');
            } else {
              trial.data.confidence = 0;
              exp.state.next('FINISH');
            }
          }
          break;
      }
    }
  }

  /**
   * Array to store data generated by the experience
   */
  let data = [];

  /*
   * You must initialize an empty object called trial
   */
  let trial = {};

  exp.start(calcFunc, stateFunc, displayFunc);

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
          exp.state.next('WELCOME');
        }
        break;

      case 'WELCOME':
        exp.state.once(() => {
          exp.VRUI.visible = true;
          exp.sceneManager.setCameraLayout(DEFAULT_CAMERA_LAYOUT);
          exp.VRUI.edit({
            title: 'Instructions',
            instructions: `In each game, you will be briefly shown dots moving inside a circular area.\nAfter watching the dots, a blue section and an orange section will appear on the perimeter of the circle.\n\nYour task: Determine whether there was movement of dots towards the blue or the orange section.`,
            interactive: false,
            buttons: false,
            backButtonState: 'disabled',
            nextButtonState: 'disabled',
          });
          exp.VRUI.updateProgressBar(1, 4);
        });
        break;

      case 'PRETUTORIAL':
        exp.state.once(() => {
          exp.VRUI.visible = true;
          exp.sceneManager.setCameraLayout(DEFAULT_CAMERA_LAYOUT);
          exp.VRUI.edit({
            title: 'Practice Games',
            instructions: `Play a few games now and practice watching the dots while observing the appearance of the game. Use the controller buttons to interact with the game.\n\nWhen you are ready and comfortable, use the controller in your right hand to start the task.`,
            interactive: false,
            buttons: false,
            backButtonState: 'disabled',
            nextButtonState: 'disabled',
          });
          exp.VRUI.updateProgressBar(2, 4);
        });
        break;

      case 'PREPRACTICE':
        exp.state.once(() => {
          exp.VRUI.visible = true;
          exp.sceneManager.setCameraLayout(DEFAULT_CAMERA_LAYOUT);
          exp.VRUI.edit({
            title: 'Practice Games',
            instructions: `You will now play another ${
              exp.cfg.numPracticeTrials * 2
            } practice games. You won't have to rate your confidence after each game, but the cross in the center of the circlular area will briefly change color if your answer was correct or not. Green is a correct answer, red is an incorrect answer.\n\nWhen you are ready and comfortable, use the controller in your right hand to start the task.`,
            interactive: false,
            buttons: false,
            backButtonState: 'disabled',
            nextButtonState: 'disabled',
          });
          exp.VRUI.updateProgressBar(3, 4);
        });
        break;

      case 'PREMAIN':
        exp.state.once(() => {
          exp.VRUI.visible = true;
          exp.sceneManager.setCameraLayout(DEFAULT_CAMERA_LAYOUT);
          exp.VRUI.edit({
            title: 'Instructions',
            instructions: `That concludes all the practice games. You will now play ${
              (exp.cfg.numCalibrationTrials + exp.cfg.numMainTrials) * 2
            } games.\nYou will not be shown if you answered correctly or not, and you will be asked to rate your confidence after some of the games.\n\nWhen you are ready and comfortable, use the controller in your right hand to start the task.`,
            interactive: false,
            buttons: false,
            backButtonState: 'disabled',
            nextButtonState: 'disabled',
          });
          exp.VRUI.updateProgressBar(4, 4);
        });
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
              let trials = d3.filter(
                data,
                (t) => t.block.name === 'calibration'
              );
              if (exp.cfg.numCalibrationTrials > 20) {
                // If we have more than 20 calibration trials, take the median from the last 20
                // This allows us to test with fewer calibration trials
                trials = _.takeRight(trials, 20);
              }
              let kArray = d3.map(data, (d) => d.coherence);
              let kMedian = d3.median(kArray);
              if (kMedian > 0.5) {
                kMedian = 0.5;
              } else if (kMedian < 0.12) {
                kMedian = 0.12;
              }
              trial.coherences = [kMedian * 0.5, kMedian * 2.0];
            }
            // Select a coherence
            trial.coherence = trial.coherences[Math.random() > 0.5 ? 0 : 1];
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

          // Setup the cameras
          exp.sceneManager.setCameraLayout(trial.cameraLayout);
          exp.state.next('FIXATION');
        });
        break;

      case 'FIXATION':
        exp.state.once(() => {
          exp.VRUI.visible = false;
          // Construct 'FIXATION'-type stimulus
          stimulus.setParameters({
            background: true,
            outline: true,
            fixation: 'standard',
            reference: false,
            dots: {
              visible: false,
            },
            text: {
              confidence: false,
              response: false,
            },
          });
        });
        // Proceed to the next state upon time expiration
        if (exp.state.expired(exp.cfg.fixationDuration)) {
          stimulus.reset();
          exp.state.next('MOTION');
        }
        break;

      case 'MOTION':
        exp.state.once(() => {
          exp.VRUI.visible = false;
          // Construct 'MOTION'-type stimulus
          stimulus.setParameters({
            background: true,
            outline: true,
            fixation: 'standard',
            reference: false,
            dots: {
              visible: true,
              coherence: trial.coherence,
              direction: trial.referenceDirection,
            },
            text: {
              confidence: false,
              response: false,
            },
          });
        });

        // Proceed to the next state upon time expiration
        if (exp.state.expired(2.0)) {
          stimulus.reset();
          exp.state.next('RESPONSE');
        }
        break;

      case 'RESPONSE':
        exp.state.once(() => {
          exp.VRUI.visible = false;
          // Construct 'RESPONSE'-type stimulus
          stimulus.setParameters({
            background: true,
            outline: false,
            fixation: 'standard',
            reference: true,
            dots: {
              visible: false,
            },
            text: {
              confidence: false,
              response: true,
            },
          });
        });
        break;

      case 'POSTRESPONSE':
        exp.state.once(() => {
          exp.VRUI.visible = false;
          // Construct 'FIXATION'-type stimulus
          stimulus.setParameters({
            background: true,
            outline: true,
            fixation: 'standard',
            reference: false,
            dots: {
              visible: false,
            },
            text: {
              confidence: false,
              response: false,
            },
          });
        });

        // Proceed to the next state upon time expiration
        if (exp.state.expired(exp.cfg.postResponseDuration)) {
          stimulus.reset();
          // Check if we need to show confidence
          const realTrialNumber =
            trial.block.repetition * 2 + trial.block.trial + 1;
          if (realTrialNumber % exp.cfg.confidenceGap === 0) {
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
          stimulus.setParameters({
            background: true,
            outline: false,
            fixation: 'none',
            reference: false,
            dots: {
              visible: false,
            },
            text: {
              confidence: true,
              response: false,
            },
          });
        });
        break;

      case 'FEEDBACK':
        exp.state.once(() => {
          exp.VRUI.visible = false;
          // Construct 'FEEDBACK'-type stimulus
          stimulus.setParameters({
            background: true,
            outline: true,
            fixation: trial.data.correct === 1 ? 'correct' : 'incorrect',
            reference: false,
            dots: {
              visible: false,
            },
            text: {
              confidence: false,
              response: false,
            },
          });
        });
        if (exp.state.expired(exp.cfg.feedbackDuration)) {
          stimulus.reset();
          if (trial.block.name === 'practice') {
            exp.state.next('FINISH');
          }
        }
        break;

      case 'FINISH':
        exp.state.once(function () {
          stimulus.reset();
        });
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
          // Divert to instructions if required
          let trialType = trial.block.name;
          let elapsed = d3.filter(data, (t) => t.block.name === trialType);
          if (
            trialType === 'tutorial' &&
            elapsed.length === exp.cfg.numTutorialTrials * 2
          ) {
            stimulus.reset();
            exp.state.next('PREPRACTICE');
          } else if (
            trialType === 'practice' &&
            elapsed.length === exp.cfg.numPracticeTrials * 2
          ) {
            stimulus.reset();
            exp.state.next('PREMAIN');
          } else {
            exp.state.next('START');
          }
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
          exp.VRUI.edit({
            title: 'Complete',
            instructions: `The game is now complete. Thank you for your participation!`,
            interactive: false,
            buttons: false,
            backButtonState: 'disabled',
            nextButtonState: 'disabled',
          });
          exp.firebase.localSave();
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

    // Execute the "step()" function on all animated components
    stimulus.getAnimated().forEach((element) => {
      element.step(exp.cfg.frameCount);
    });
    exp.sceneManager.render();

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
