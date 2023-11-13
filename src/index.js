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
const LEFT_CAMERA_LAYOUT = 0;
const RIGHT_CAMERA_LAYOUT = 1;
const DEFAULT_CAMERA_LAYOUT = 2;

// Experiment constants
const DEFAULT_BLOCK_SIZE = 10; // number of trials per 'block'
const TIME_MULTIPLER = 1.0; // factor to adjust the length of timings (use for testing)

/*
 * Main function contains all experiment logic
 */

async function main() {
  const exp = new Experiment({
    devOptions: {
      skipConsent: true,
      orbitControls: false,
      autoplay: false,
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
    cameraFixed: true,

    // Frame and rendering count
    frameCount: 0,

    // Input configuration
    input: {
      left: '2',
      right: '7',
    },

    // Experiment behavior
    confidenceGap: 2,

    // Number of left-right trial sequences per sequence type (n * 20 trials)
    numTutorialSequences: 1, // 20
    numPracticeSequences: 1, // 20
    numCalibrationSequences: 6, // 120
    numMainSequences: 10, // 200

    // Trial durations
    fixationDuration: 1.0 * TIME_MULTIPLER, // seconds
    postResponseDuration: 0.25 * TIME_MULTIPLER, // seconds
    feedbackDuration: 0.25 * TIME_MULTIPLER, // seconds
    motionDuration: 2.0 * TIME_MULTIPLER, // seconds
  });

  /**
   * Initialize Finite State Machine (FSM) that manages the flow of your experiment
   */
  exp.state.init(
    [
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
    ],
    handleStateChange,
    'WELCOME'
  );

  /*
   * Create task 'Stimulus' instance
   */
  const taskGroup = new Group();
  taskGroup.position.copy(exp.cfg.taskPosition);
  exp.sceneManager.scene.add(taskGroup);

  // Fix the camera to the task if specified
  if (exp.cfg.cameraFixed === true) {
    exp.sceneManager.camera.attach(exp.VRUI); // UI
    exp.sceneManager.camera.attach(taskGroup); // Task
  }

  /**
   * Create 'Stimulus' presets
   */
  const presets = {
    fixation: {
      background: true,
      outline: true,
      fixation: 'standard',
      reference: false,
      dots: {
        visible: false,
      },
      ui: {
        confidence: false,
        response: false,
        navigation: false,
      },
    },
    response: {
      background: true,
      outline: false,
      fixation: 'standard',
      reference: true,
      dots: {
        visible: false,
      },
      ui: {
        confidence: false,
        response: true,
        navigation: false,
      },
    },
    confidence: {
      background: true,
      outline: false,
      fixation: 'none',
      reference: false,
      dots: {
        visible: false,
      },
      ui: {
        confidence: true,
        response: false,
        navigation: false,
      },
    },
    navigation: {
      background: true,
      outline: false,
      fixation: 'none',
      reference: false,
      dots: {
        visible: false,
      },
      ui: {
        confidence: false,
        response: false,
        navigation: true,
      },
    },
  };
  const stimulus = new Stimulus(taskGroup, VIEW_DISTANCE, presets);

  /**
   * Calculate the coherence values from a set of trials
   * @param {any[]} trials a collection of trials with coherence values
   * @param {number} coherenceType one of three types: 'left', 'right', or 'combined'
   * coherence, referring to monocular or binocular camera layouts
   */
  function calculateCoherences(trials, coherenceType) {
    // Generate the array of coherence values
    let kArray;
    if (coherenceType === 'left') {
      kArray = d3.map(trials, (d) => d.coherences.left[0]);
    } else if (coherenceType === 'right') {
      kArray = d3.map(trials, (d) => d.coherences.right[0]);
    } else {
      kArray = d3.map(trials, (d) => d.coherences.combined[0]);
    }

    // Calculate and cap the median if required
    let kMedian = d3.median(kArray);
    if (kMedian > 0.5) {
      kMedian = 0.5;
    } else if (kMedian < 0.12) {
      kMedian = 0.12;
    }

    return [kMedian * 0.5, kMedian * 2.0];
  }

  /**
   * Create shuffled left-right block sequences for types of trials
   * @param {string} type the type of trial block to generate
   * @param {number} repetitions the number of repeated left-right blocks, different to
   * the inbuilt 'reps' parameters
   */
  function generateSequence(type, repetitions) {
    const blocks = [];
    for (let i = 0; i < repetitions; i++) {
      if (type === 'tutorial') {
        blocks.push(
          new Block({
            variables: {
              coherence: Array(DEFAULT_BLOCK_SIZE).fill(0.3),
              coherences: Array(DEFAULT_BLOCK_SIZE).fill({
                left: [0.3, 0.6],
                right: [0.3, 0.6],
                combined: [0.3, 0.6],
              }),
              showFeedback: Array(DEFAULT_BLOCK_SIZE).fill(false),
              cameraLayout: Array(DEFAULT_BLOCK_SIZE).fill(LEFT_CAMERA_LAYOUT),
            },
            options: {
              name: 'tutorial',
              reps: 1,
            },
          }),
          new Block({
            variables: {
              coherence: Array(DEFAULT_BLOCK_SIZE).fill(0.3),
              coherences: Array(DEFAULT_BLOCK_SIZE).fill({
                left: [0.3, 0.6],
                right: [0.3, 0.6],
                combined: [0.3, 0.6],
              }),
              showFeedback: Array(DEFAULT_BLOCK_SIZE).fill(false),
              cameraLayout: Array(DEFAULT_BLOCK_SIZE).fill(RIGHT_CAMERA_LAYOUT),
            },
            options: {
              name: 'tutorial',
              reps: 1,
            },
          })
        );
      } else if (type === 'practice') {
        blocks.push(
          new Block({
            variables: {
              coherence: Array(DEFAULT_BLOCK_SIZE).fill(0.3),
              coherences: Array(DEFAULT_BLOCK_SIZE).fill({
                left: [0.3, 0.6],
                right: [0.3, 0.6],
                combined: [0.3, 0.6],
              }),
              showFeedback: Array(DEFAULT_BLOCK_SIZE).fill(true),
              cameraLayout: Array(DEFAULT_BLOCK_SIZE).fill(LEFT_CAMERA_LAYOUT),
            },
            options: {
              name: 'practice',
              reps: 1,
            },
          }),
          new Block({
            variables: {
              coherence: Array(DEFAULT_BLOCK_SIZE).fill(0.3),
              coherences: Array(DEFAULT_BLOCK_SIZE).fill({
                left: [0.3, 0.6],
                right: [0.3, 0.6],
                combined: [0.3, 0.6],
              }),
              showFeedback: Array(DEFAULT_BLOCK_SIZE).fill(true),
              cameraLayout: Array(DEFAULT_BLOCK_SIZE).fill(RIGHT_CAMERA_LAYOUT),
            },
            options: {
              name: 'practice',
              reps: 1,
            },
          })
        );
      } else if (type === 'calibration') {
        // 'calibration'-type trials
        // Add a pair of blocks, one left-eye, one right-eye
        blocks.push(
          new Block({
            variables: {
              coherence: Array(DEFAULT_BLOCK_SIZE).fill(0.2),
              coherences: Array(DEFAULT_BLOCK_SIZE).fill({
                left: [0.2, 0.2],
                right: [0.2, 0.2],
                combined: [0.2, 0.2],
              }),
              showFeedback: Array(DEFAULT_BLOCK_SIZE).fill(false),
              cameraLayout: Array(DEFAULT_BLOCK_SIZE).fill(LEFT_CAMERA_LAYOUT),
            },
            options: {
              name: 'calibration',
              reps: 1,
            },
          }),
          new Block({
            variables: {
              coherence: Array(DEFAULT_BLOCK_SIZE).fill(0.2),
              coherences: Array(DEFAULT_BLOCK_SIZE).fill({
                left: [0.2, 0.2],
                right: [0.2, 0.2],
                combined: [0.2, 0.2],
              }),
              showFeedback: Array(DEFAULT_BLOCK_SIZE).fill(false),
              cameraLayout: Array(DEFAULT_BLOCK_SIZE).fill(RIGHT_CAMERA_LAYOUT),
            },
            options: {
              name: 'calibration',
              reps: 1,
            },
          })
        );
      } else if (type === 'main') {
        // 'main'-type trials
        // Add a pair of blocks, one left-eye, one right-eye
        blocks.push(
          new Block({
            variables: {
              coherence: Array(DEFAULT_BLOCK_SIZE).fill(0.2),
              coherences: Array(DEFAULT_BLOCK_SIZE).fill({
                left: [0.2, 0.2],
                right: [0.2, 0.2],
                combined: [0.2, 0.2],
              }),
              showFeedback: Array(DEFAULT_BLOCK_SIZE).fill(false),
              cameraLayout: Array(DEFAULT_BLOCK_SIZE).fill(LEFT_CAMERA_LAYOUT),
            },
            options: {
              name: 'main',
              reps: 1,
            },
          }),
          new Block({
            variables: {
              coherence: Array(DEFAULT_BLOCK_SIZE).fill(0.2),
              coherences: Array(DEFAULT_BLOCK_SIZE).fill({
                left: [0.2, 0.2],
                right: [0.2, 0.2],
                combined: [0.2, 0.2],
              }),
              showFeedback: Array(DEFAULT_BLOCK_SIZE).fill(false),
              cameraLayout: Array(DEFAULT_BLOCK_SIZE).fill(RIGHT_CAMERA_LAYOUT),
            },
            options: {
              name: 'main',
              reps: 1,
            },
          })
        );
      } else {
        console.warn('Unknown sequence type:', type);
      }
    }

    // Return the shuffled list of blocks
    return _.shuffle(blocks);
  }

  /*
   * Create the experiment trial sequence
   */
  exp.createTrialSequence([
    ...generateSequence('tutorial', exp.cfg.numTutorialSequences),
    ...generateSequence('practice', exp.cfg.numPracticeSequences),
    ...generateSequence('calibration', exp.cfg.numCalibrationSequences),
    ...generateSequence('main', exp.cfg.numMainSequences),
  ]);

  /**
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
   * Bind a function to an interval to poll controller input
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
   * If the autoplayer is enabled, setup keypresses function
   */
  if (exp.cfg.devOptions.autoplay) {
    window.setInterval(() => {
      window.dispatchEvent(
        new KeyboardEvent('keyup', {
          key: Math.random() > 0.5 ? exp.cfg.input.left : exp.cfg.input.right,
        })
      );
    }, 100);
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
          if (event.key === exp.cfg.input.left) {
            exp.state.next('WELCOME');
          } else if (event.key === exp.cfg.input.right) {
            exp.state.next('START');
          }
          break;
        case 'PREPRACTICE':
        case 'PREMAIN':
          if (event.key === exp.cfg.input.right) {
            exp.state.next('START');
          }
          break;
        case 'RESPONSE':
          const responseInput = [exp.cfg.input.left, exp.cfg.input.right];
          if (responseInput.includes(event.key)) {
            // Handle RT calculation
            timings.referenceRT.end = performance.now();
            trial.data.referenceStart = timings.referenceRT.start;
            trial.data.referenceEnd = timings.referenceRT.end;
            trial.data.referenceRT =
              timings.referenceRT.end - timings.referenceRT.start;

            // Valid input was received, store responses and outcome
            if (event.key === exp.cfg.input.left) {
              trial.data.referenceSelection = 'left';
              trial.data.correct = trial.referenceDirection === Math.PI ? 1 : 0;
            } else {
              trial.data.referenceSelection = 'right';
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
            // Handle RT calculation
            timings.confidenceRT.end = performance.now();
            trial.data.confidenceStart = timings.confidenceRT.start;
            trial.data.confidenceEnd = timings.confidenceRT.end;
            trial.data.confidenceRT =
              timings.confidenceRT.end - timings.confidenceRT.start;

            if (event.key === exp.cfg.input.left) {
              trial.data.confidenceSelection = 'left';
              exp.state.next('FINISH');
            } else {
              trial.data.confidenceSelection = 'right';
              exp.state.next('FINISH');
            }
          }
          break;
      }
    }
  }

  /**
   * Array to store data generated by the experiment
   */
  let data = [];

  /*
   * Initialize empty trial object
   */
  let trial = {};

  /**
   * Initialize structure to manage RTs
   */

  const timings = {
    referenceRT: {
      start: 0,
      end: 0,
    },
    confidenceRT: {
      start: 0,
      end: 0,
    },
    trial: {
      start: 0,
      end: 0,
    },
  };

  exp.setupDisplay(); // Manually trigger display functionality
  exp.start(calcFunc, stateFunc, displayFunc);

  /**
   * Use `calcFunc` for calculations used in multiple states
   */
  function calcFunc() {}

  /**
   * Define your procedure as a switch statement implementing a Finite State Machine.
   * Ensure that all states are listed in the array given to `exp.state.init()`
   * @method `exp.state.next(state)` Transitions to new state on next loop.
   * @method `exp.state.once(function)` Runs function one time on entering state.
   */
  function stateFunc() {
    switch (exp.state.current) {
      case 'WELCOME':
        exp.state.once(() => {
          stimulus.usePreset('navigation');
          exp.sceneManager.setCameraLayout(DEFAULT_CAMERA_LAYOUT);
          exp.VRUI.visible = true;
          exp.VRUI.progress.visible = false;
          exp.VRUI.edit({
            title: 'Instructions',
            instructions: `In each game, you will be briefly shown dots moving inside a circular area.\nAfter watching the dots, a blue section and an orange section will appear on the perimeter of the circle.\n\nYour task: Determine whether there was movement of dots towards the blue or the orange section.`,
            interactive: false,
            buttons: false,
            backButtonState: 'disabled',
            nextButtonState: 'disabled',
          });
        });
        break;

      case 'PRETUTORIAL':
        exp.state.once(() => {
          stimulus.usePreset('navigation');
          exp.sceneManager.setCameraLayout(DEFAULT_CAMERA_LAYOUT);
          exp.VRUI.visible = true;
          exp.VRUI.progress.visible = false;
          exp.VRUI.edit({
            title: 'Practice Games',
            instructions: `Play a few games now and practice watching the dots while observing the appearance of the game. Use the controller buttons to interact with the game.\n\nWhen you are ready and comfortable, use the controller in your right hand to start the task.`,
            interactive: false,
            buttons: false,
            backButtonState: 'disabled',
            nextButtonState: 'disabled',
          });
        });
        break;

      case 'PREPRACTICE':
        exp.state.once(() => {
          stimulus.usePreset('navigation');
          exp.sceneManager.setCameraLayout(DEFAULT_CAMERA_LAYOUT);
          exp.VRUI.visible = true;
          exp.VRUI.progress.visible = false;
          exp.VRUI.edit({
            title: 'Practice Games',
            instructions: `You will now play another ${
              exp.cfg.numPracticeSequences * 2 * DEFAULT_BLOCK_SIZE
            } practice games. You won't have to rate your confidence after each game, but the cross in the center of the circlular area will briefly change color if your answer was correct or not. Green is a correct answer, red is an incorrect answer.\n\nWhen you are ready and comfortable, use the controller in your right hand to start the task.`,
            interactive: false,
            buttons: false,
            backButtonState: 'disabled',
            nextButtonState: 'disabled',
          });
        });
        break;

      case 'PREMAIN':
        exp.state.once(() => {
          stimulus.usePreset('navigation');
          exp.sceneManager.setCameraLayout(DEFAULT_CAMERA_LAYOUT);
          exp.VRUI.visible = true;
          exp.VRUI.progress.visible = false;
          exp.VRUI.edit({
            title: 'Instructions',
            instructions: `That concludes all the practice games. You will now play ${
              (exp.cfg.numCalibrationSequences + exp.cfg.numMainSequences) *
              2 *
              DEFAULT_BLOCK_SIZE
            } games.\nYou will not be shown if you answered correctly or not, and you will be asked to rate your confidence after some of the games.\n\nWhen you are ready and comfortable, use the controller in your right hand to start the task.`,
            interactive: false,
            buttons: false,
            backButtonState: 'disabled',
            nextButtonState: 'disabled',
          });
        });
        break;

      case 'START':
        exp.state.once(() => {
          exp.VRUI.visible = false;
          // Store the previous trial type and camera layout
          let lastTrialType = 'none';
          let lastCameraLayout = LEFT_CAMERA_LAYOUT;
          if (trial.block) {
            lastTrialType = trial.block.name;
            lastCameraLayout = trial.cameraLayout;
          }

          // Copy and instantiate the 'trial' object
          trial = structuredClone(exp.trials[exp.trialNumber]);
          trial.trialNumber = exp.trialNumber;
          trial.startTime = performance.now();
          trial.referenceDirection = Math.random() > 0.5 ? Math.PI : 0;

          /**
           * 'tutorial' and 'practice'-type operations
           */
          if (['tutorial', 'practice'].includes(trial.block.name)) {
            // Select coherences randomly
            if (trial.cameraLayout === LEFT_CAMERA_LAYOUT) {
              trial.coherence =
                trial.coherences.left[Math.random() > 0.5 ? 0 : 1];
            } else {
              trial.coherence =
                trial.coherences.right[Math.random() > 0.5 ? 0 : 1];
            }
          }

          /**
           * 'calibration'-type operations
           * The updating of coherence values is performed AFTER all 'calibration'-type trials,
           * regardless of the current trial type. The coherence values need to be updated
           * prior to calculating the median coherences for the first 'main'-type trial.
           */
          if (lastTrialType === 'calibration') {
            // Note: 'calibration'-type trials do not have a 'harder' or 'easier' coherence
            // Store the overall 'coherence' value, not specific to each eye
            let trials = d3.filter(data, (t) => t.block.name === 'calibration');
            if (trials.length > 0) {
              trial.coherences = _.cloneDeep(data[data.length - 1].coherences);
              if (trials[trials.length - 1].data.correct === 0) {
                // If last answer was incorrect, increase the coherence
                trial.coherences.combined = [
                  parseFloat((trial.coherences.combined[0] + 0.01).toFixed(2)),
                  parseFloat((trial.coherences.combined[0] + 0.01).toFixed(2)),
                ];
              } else if (trials.length > 1) {
                // If the previous two answers were correct, decrease the coherence
                let a = trials[trials.length - 1];
                let b = trials[trials.length - 2];
                if (
                  a.data.correct === 1 &&
                  b.data.correct === 1 &&
                  a.coherence === b.coherence
                ) {
                  trial.coherences.combined = [
                    parseFloat(
                      (trial.coherences.combined[0] - 0.01).toFixed(2)
                    ),
                    parseFloat(
                      (trial.coherences.combined[0] - 0.01).toFixed(2)
                    ),
                  ];
                }
              }
            }

            // Get the last 'calibration'-type trials with the same camera
            trials = d3.filter(trials, (t) => {
              return t.cameraLayout === lastCameraLayout;
            });
            if (trials.length > 0) {
              if (trials[trials.length - 1].data.correct === 0) {
                // If last answer was incorrect, increase the coherence
                if (lastCameraLayout === LEFT_CAMERA_LAYOUT) {
                  trial.coherences.left = [
                    parseFloat((trial.coherences.left[0] + 0.01).toFixed(2)),
                    parseFloat((trial.coherences.left[0] + 0.01).toFixed(2)),
                  ];
                } else {
                  trial.coherences.right = [
                    parseFloat((trial.coherences.right[0] + 0.01).toFixed(2)),
                    parseFloat((trial.coherences.right[0] + 0.01).toFixed(2)),
                  ];
                }
              } else if (trials.length > 1) {
                // If the previous two answers were correct, decrease the coherence
                let a = trials[trials.length - 1];
                let b = trials[trials.length - 2];
                if (
                  a.data.correct === 1 &&
                  b.data.correct === 1 &&
                  a.coherence === b.coherence
                ) {
                  if (lastCameraLayout === LEFT_CAMERA_LAYOUT) {
                    trial.coherences.left = [
                      parseFloat((trial.coherences.left[0] - 0.01).toFixed(2)),
                      parseFloat((trial.coherences.left[0] - 0.01).toFixed(2)),
                    ];
                  } else {
                    trial.coherences.right = [
                      parseFloat((trial.coherences.right[0] - 0.01).toFixed(2)),
                      parseFloat((trial.coherences.right[0] - 0.01).toFixed(2)),
                    ];
                  }
                }
              }
            }
          }

          /**
           * 'main'-type operations
           * If this is the first 'main'-type trial, calculate the median coherences from the
           * previous 'calibration'-type trials. Otherwise, copy the coherence values from the
           * previous 'main'-type trial.
           */
          if (trial.block.name === 'main') {
            if (lastTrialType === 'calibration') {
              // Retrieve all 'calibration'-type trials
              let trials = d3.filter(
                data,
                (t) => t.block.name === 'calibration'
              );

              // Group trials by left and right layouts
              let leftTrials = d3.filter(
                trials,
                (t) => t.cameraLayout === LEFT_CAMERA_LAYOUT
              );
              let rightTrials = d3.filter(
                trials,
                (t) => t.cameraLayout === RIGHT_CAMERA_LAYOUT
              );

              if (exp.cfg.numCalibrationSequences > 1) {
                // If we have more than 20 calibration trials, take the median from the last 20
                // This allows us to test with fewer calibration trials
                trials = _.takeRight(trials, 20);
                leftTrials = _.takeRight(leftTrials, 20);
                rightTrials = _.takeRight(rightTrials, 20);
              }

              // Calculate the median coherences from the multiple staircases
              trial.coherences.left = calculateCoherences(leftTrials, 'left');
              trial.coherences.right = calculateCoherences(
                rightTrials,
                'right'
              );
              trial.coherences.combined = calculateCoherences(
                trials,
                'combined'
              );
            } else {
              trial.coherences = _.cloneDeep(data[data.length - 1].coherences);
            }
          }

          // Create the 'trial.data' structure
          trial.data = {
            confidenceSelection: null,
            referenceSelection: null,
            correct: 0, // 0: incorrect, 1: correct
            // Confidence timings
            confidenceStart: 0,
            confidenceEnd: 0,
            confidenceRT: 0,
            // Reference timings
            referenceStart: 0,
            referenceEnd: 0,
            referenceRT: 0,
            // Trial timings
            trialStart: 0,
            trialEnd: 0,
          };

          // Start the trial timer
          timings.trial.start = performance.now();

          // Setup the cameras
          exp.sceneManager.setCameraLayout(trial.cameraLayout);
          exp.state.next('FIXATION');
        });
        break;

      case 'FIXATION':
        exp.state.once(() => {
          exp.VRUI.visible = false;
          // Construct 'FIXATION'-type stimulus
          stimulus.usePreset('fixation');
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

          // Select 'difficulty' of the coherence value used
          const difficulty = Math.random() > 0.5 ? 0 : 1;
          if (trial.cameraLayout === LEFT_CAMERA_LAYOUT) {
            // Left eye coherence
            trial.coherence = trial.coherences.left[difficulty];
          } else {
            // Right eye coherence
            trial.coherence = trial.coherences.right[difficulty];
          }

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
            ui: {
              confidence: false,
              response: false,
              navigation: false,
            },
          });
        });

        // Proceed to the next state upon time expiration
        if (exp.state.expired(exp.cfg.motionDuration)) {
          stimulus.reset();
          exp.state.next('RESPONSE');
        }
        break;

      case 'RESPONSE':
        exp.state.once(() => {
          exp.VRUI.visible = false;
          // Construct 'RESPONSE'-type stimulus
          stimulus.usePreset('response');
          timings.referenceRT.start = performance.now();
        });
        break;

      case 'POSTRESPONSE':
        exp.state.once(() => {
          exp.VRUI.visible = false;
          // Construct 'FIXATION'-type stimulus
          stimulus.usePreset('fixation');
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
          stimulus.usePreset('confidence');
          timings.confidenceRT.start = performance.now();
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
            ui: {
              confidence: false,
              response: false,
              navigation: false,
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
          // Handle trial duration calculation
          timings.trial.end = performance.now();
          trial.data.trialStart = timings.trial.start;
          trial.data.trialEnd = timings.trial.end;
          stimulus.reset();
        });
        exp.state.next('ADVANCE');
        break;

      case 'ADVANCE':
        data.push(trial); // Add trial data
        exp.nextTrial();
        if (exp.trialNumber < exp.numTrials) {
          // Divert to instructions if required
          let trialType = trial.block.name;
          let elapsed = d3.filter(data, (t) => t.block.name === trialType);
          if (
            trialType === 'tutorial' &&
            elapsed.length ===
              exp.cfg.numTutorialSequences * 2 * DEFAULT_BLOCK_SIZE
          ) {
            stimulus.reset();
            exp.state.next('PREPRACTICE');
          } else if (
            trialType === 'practice' &&
            elapsed.length ===
              exp.cfg.numPracticeSequences * 2 * DEFAULT_BLOCK_SIZE
          ) {
            stimulus.reset();
            exp.state.next('PREMAIN');
          } else {
            exp.state.next('START');
          }
        } else {
          exp.complete();
          exp.state.next('DONE');
        }
        break;

      case 'DONE':
        exp.state.once(function () {
          stimulus.usePreset('default');
          exp.VRUI.visible = true;
          exp.VRUI.edit({
            title: 'Complete',
            instructions: `The game is now complete. Thank you for your participation!`,
            interactive: false,
            buttons: false,
            backButtonState: 'disabled',
            nextButtonState: 'disabled',
          });
        });
        if (exp.VRUI.clickedNext) {
          exp.xrSession.end();
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

    // Execute the 'step()' function on all animated components
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
