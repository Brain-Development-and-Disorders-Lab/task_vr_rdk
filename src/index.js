// Third-party imports
import {
  Vector3,
  Group,
  Mesh,
  Quaternion,
  CircleGeometry,
  MeshBasicMaterial,
  PlaneGeometry,
} from 'three';
import { Easing, Tween, update as tweenUpdate } from '@tweenjs/tween.js'; // https://github.com/tweenjs/tween.js/

// Package imports
import { Experiment, InstructionsPanel, Block } from 'ouvrai';

// Static asset imports (https://vitejs.dev/guide/assets.html)
import environmentLightingURL from 'ouvrai/lib/environments/IndoorHDRI003_1K-HDR.exr?url'; // absolute path from ouvrai

import Dot from './classes/Dot';

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
      'START',
      'FINISH',
      'STARTNOFEEDBACK',
      'STARTCLAMP',
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

  const background = new Mesh(
    new PlaneGeometry(100, 50),
    new MeshBasicMaterial({
      color: 'white',
      toneMapped: false,
    })
  );
  background.position.setZ(-2.2);

  // Create a mock RDK stimulus
  // Aperture
  const circleOutline = new Mesh(
    new CircleGeometry(1, 64, 0),
    new MeshBasicMaterial({
      color: 'black',
      toneMapped: false,
    })
  );
  const circleInterior = new Mesh(
    new CircleGeometry(0.97, 64, 0),
    new MeshBasicMaterial({ toneMapped: false })
  );
  circleOutline.position.setZ(-2);
  circleInterior.position.setZ(-2);
  // Dots
  const dots = [];
  const dotCount = 20 ** 2;
  const dotRowCount = Math.floor(Math.sqrt(dotCount));
  for (let i = -dotRowCount / 2; i < dotRowCount / 2; i++) {
    for (let j = -dotRowCount / 2; j < dotRowCount / 2; j++) {
      const delta = Math.random();
      const x = (i * 1.94) / dotRowCount + (delta * 1.94) / dotRowCount;
      const y = (j * 1.94) / dotRowCount + (delta * 1.94) / dotRowCount;

      if (delta > 0.6) {
        // Non-dynamic dot that is just moving in random paths
        const dot = new Dot(x, y, -1.98, {
          type: 'random',
          radius: 0.03,
          velocity: 0.01,
          direction: 2 * Math.PI * Math.random(),
          apertureRadius: 0.97,
        });
        dots.push(dot);
      } else {
        const dot = new Dot(x, y, -1.98, {
          type: 'reference',
          radius: 0.03,
          velocity: 0.01,
          direction: (Math.PI / 180) * 290,
          apertureRadius: 0.97,
        });
        dots.push(dot);
      }
    }
  }
  workspace.add(
    background,
    circleOutline,
    circleInterior,
    ...dots.map((dot) => dot.getObject())
  );

  if (exp.cfg.cameraFixed) {
    // Attach the camera to the entire view if specified
    exp.sceneManager.camera.attach(workspace);
  }

  /*
   * Create trial sequence from array of block objects.
   */
  exp.createTrialSequence([
    new Block({
      variables: {},
      options: {
        name: 'P0',
        reps: exp.cfg.numBaselineCycles,
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
          exp.state.next('WELCOME');
        }
        break;

      case 'WELCOME':
        exp.state.once(() => {
          exp.VRUI.visible = false;
        });
        if (exp.VRUI.clickedNext) exp.state.next('CALIBRATE');
        break;

      case 'START':
        exp.state.once(function () {});
        break;

      case 'FINISH':
        exp.state.once(function () {
          let canRepeatDemo = exp.trialNumber < exp.cfg.maxDemoTrials - 1;
          trial.demoTrial &&
            exp.VRUI.edit({
              title: 'Make sense?',
              instructions: `You will perform ${
                exp.numTrials - exp.trialNumber - 1
              } movements toward the same target. \
              There will be ${
                exp.cfg.restTrials.length
              } rest breaks, but you may rest at any time before returning to the start cube.
              ${
                canRepeatDemo ? 'To repeat the instructions, click Back.\n' : ''
              }\
              If you are ready to start, click Next.`,
              interactive: true,
              backButtonState: canRepeatDemo ? 'idle' : 'disabled',
              nextButtonState: 'idle',
            });
          target.visible = false;
        });
        // Wait for button click on demo trial
        if (trial.demoTrial) {
          if (exp.VRUI.clickedNext) {
            exp.repeatDemoTrial = false;
          } else if (exp.VRUI.clickedBack) {
            exp.repeatDemoTrial = true;
          } else {
            break;
          }
        }
        // Save immediately prior to state transition (ensures one save per trial)
        exp.firebase.saveTrial(trial);
        exp.state.next('ADVANCE');
        break;

      case 'STARTNOFEEDBACK':
        exp.state.once(function () {
          exp.VRUI.edit({
            title: 'Challenge',
            instructions: `Try to hit the target without visual feedback! \
            In the gray area, the tool will disappear. A dark ring shows your distance.\n\
            Try it out now before continuing.`,
            backButtonState: 'disabled',
            nextButtonState: 'idle',
          });
          trial.noFeedback = true; // not a problem bc we've already saved this trial
        });
        if (exp.VRUI.clickedNext) {
          // Hide UI
          exp.VRUI.edit({
            interactive: false,
            buttons: false,
            instructions: false,
          });
          exp.state.next('SETUP');
        }
        break;

      case 'STARTCLAMP':
        exp.state.once(function () {
          exp.VRUI.edit({
            title: 'Almost done',
            instructions: `For the remaining trials, please aim straight at the target, the way you would normally. \
              Do not deliberately aim to either side of the target.`,
            backButtonState: 'disabled',
            nextButtonState: 'idle',
          });
          trial.errorClamp = true; // not a problem bc we've already saved this trial
        });
        if (exp.VRUI.clickedNext) {
          // Hide UI
          exp.VRUI.edit({
            interactive: false,
            buttons: false,
            instructions: false,
          });
          exp.state.next('SETUP');
        }
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

    dots.forEach((dot) => {
      dot.step(exp.cfg.frameCount, exp.sceneManager.renderer.xr.isPresenting);
    });

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
