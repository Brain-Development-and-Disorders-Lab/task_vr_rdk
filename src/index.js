// Third-party imports
import {
  Vector3,
  Group,
  Quaternion,
} from 'three';
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

    // Trial structure
    numTutorialTrials: 10,

    // Trial durations
    fixationDuration: 0.25,
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
      'TUTORIAL',
      'RESPONSE',
      'FEEDBACK',
      'FINISH',
      'ADVANCE',
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
  // Workspace "root" (helpful for individual height calibration)
  const workspace = new Group();
  workspace.position.copy(exp.cfg.homePosn);
  exp.sceneManager.scene.add(workspace);

  // Create a new Renderer
  const TaskRenderer = new Renderer(workspace);
  const TaskGraphics = new Graphics(TaskRenderer);

  // Attach the camera to the entire view if specified
  if (exp.cfg.cameraFixed) {
    exp.sceneManager.camera.attach(workspace);
  }

  /*
   * Create trial sequence from array of block objects.
   */
  exp.createTrialSequence([
    new Block({
      variables: {
        coherence: 0.2,
        duration: 2,
        direction: Math.PI,
        showFeedback: false,
        requireConfidence: false,
      },
      options: {
        name: 'tutorial',
        reps: exp.cfg.numTutorialTrials,
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

          trial = structuredClone(exp.trials[exp.trialNumber]);
          trial.trialNumber = exp.trialNumber;
          trial.startTime = performance.now();

          exp.state.next('FIXATION');
        });
        break;

      case 'FIXATION':
        exp.state.once(() => {
          exp.VRUI.visible = false;

          // Construct 'FIXATION'-type stimulus
          TaskGraphics.addOutline();
          TaskGraphics.addFixation();
        });

        // Proceed to the next state upon time expiration
        if (exp.state.expired(exp.cfg.fixationDuration)) {
          if (trial.block.name === "tutorial") {
            exp.state.next("TUTORIAL");
          }
        }
        break;

      case 'TUTORIAL':
        exp.state.once(() => {
          exp.VRUI.visible = false;

          // Construct 'TUTORIAL'-type stimulus
          TaskGraphics.addOutline();
          TaskGraphics.addFixation();
          TaskGraphics.addDots();
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

          // exp.state.next('FEEDBACK');
        });

        break;

      case 'FEEDBACK':
        exp.state.once(() => {
          exp.VRUI.visible = false;

          // Construct stimulus

          exp.state.next('FINISH');
        });
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
        exp.state.once(function () {});
        break;

      case 'CONTROLLER':
        exp.state.once(function () {
          // Ok to put down controller during rest
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
      element.step(exp.cfg.frameCount, exp.sceneManager.renderer.xr.isPresenting);
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

  function handleDeviceInput(event) {
    console.info('Event:', event);
  }
}

window.addEventListener('DOMContentLoaded', main);
window.addEventListener('keypress', handleDeviceInput);
