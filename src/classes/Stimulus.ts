// Core modules
import { Graphics } from "./Graphics";

// Type definitions
import { IRenderer, IStimulus } from "../../types";

/**
 * Stimulus abstraction
 */
export class Stimulus {
  private parameters: IStimulus;
  private name: string;
  private interactive: boolean;
  private selected: any;
  private keybindings: any;
  private timing: any;
  private target: any;
  private trial: any;
  private postTrialHandler: any;
  private rendererParameters: IRenderer;
  private timer: number;
  /**
   * Default constructor for Stimulus
   * @param {IStimulus} parameters configuration information
   */
  constructor(parameters: IStimulus) {
    // Store parameters
    this.parameters = parameters;

    // Unpack parameters
    this.name = parameters.name;
    this.interactive = parameters.interactive;
    this.selected = parameters.selected;
    this.keybindings = parameters.keybindings;
    this.timing = parameters.timing;
    this.target = parameters.target;
    this.trial = parameters.trial;
    this.postTrialHandler = parameters.postTrialHandler;
    this.rendererParameters = parameters.rendererParameters;

    // Timer
    this.timer = null;
  }

  /**
   * Get the parameters of the renderer
   * @return {any}
   */
  getParameters(): any {
    return this.parameters;
  }

  /**
   * Setup the keybindings for the stimulus
   */
  createKeybindings(): void {
    // Check if the stimulus can be interacted with
    if (this.interactive) {
      // Bind keys
      for (const binding in this.keybindings) {
        if (this.keybindings[binding]) {
          document.addEventListener("keyup", this.keybindings[binding].handler);
        }
      }
    }
  }

  /**
   * Remove keybindings for the stimulus
   */
  removeKeybindings(): void {
    // Unbind keys
    for (const binding in this.keybindings) {
      if (this.keybindings[binding]) {
        document.removeEventListener(
          "keyup",
          this.keybindings[binding].handler
        );
      }
    }
  }

  /**
   * Set the timer of the stimuli
   * @param {number} timer the timer
   */
  setTimer(timer: number) {
    this.clearTimer();
    this.timer = timer;
  }

  /**
   * Retrieve the timer of the stimuli
   * @return {number}
   */
  getTimer(): number {
    return this.timer;
  }

  /**
   * Clear the timer of the stimuli
   */
  clearTimer(): void {
    if (this.timer !== null) {
      window.clearTimeout(this.timer);
    }
  }

  /**
   * Retrieve the target element
   * @return {any}
   */
  getTarget(): any {
    return this.target;
  }

  /**
   * Get the handler called after each trial
   * @return {any}
   */
  getPostTrialHandler(): any {
    return this.postTrialHandler;
  }

  /**
   * Run the stimuli animations
   * @param {any} parameters runner properties
   */
  static run(parameters: any): void {
    // Instantiate graphics.
    const two = parameters.two;
    const renderer = parameters.renderer;
    const graphics = parameters.graphics as Graphics;

    for (let c = 0; c < parameters.components.length; c++) {
      const component = parameters.components[c];
      if (component === "outline") {
        graphics.addOutline();
      } else if (component === "fixation") {
        graphics.addFixation();
      } else if (component === "dots") {
        graphics.addDots();
      } else if (component === "ccw_arc") {
        graphics.addCounterclockwiseArc();
      } else if (component === "cw_arc") {
        graphics.addClockwiseArc();
      } else if (component === "left_arc") {
        graphics.addLeftArc();
      } else if (component === "right_arc") {
        graphics.addRightArc();
      } else if (component === "indicator") {
        graphics.addReferenceIndicator();
      } else if (component === "confidence") {
        graphics.addConfidence(parameters);
      } else if (component === "forced_confidence") {
        graphics.addForcedConfidence(parameters);
      } else if (component === "left") {
        graphics.addLeftKey();
      } else if (component === "right") {
        graphics.addRightKey();
      } else {
        console.warn(`Unknown component: '${component}'`);
      }
    }

    two
      .bind("update", (frameCount: number) => {
        for (let d = 0; d < renderer.getElements().length; d++) {
          const element = renderer.getElements()[d];
          element.step(frameCount);
        }
      })
      .play();
  }
}
