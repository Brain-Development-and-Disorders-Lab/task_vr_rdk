/**
 * Stimulus abstraction
 */
class Stimulus {
  parameters;
  name;
  interactive;
  keybindings;
  timing;
  postTrialHandler;
  timer;

  /**
   * Default constructor for Stimulus
   * @param {any} parameters configuration information
   */
  constructor(parameters) {
    // Store parameters
    this.parameters = parameters;

    // Unpack parameters
    this.name = parameters.name;
    this.interactive = parameters.interactive;
    this.keybindings = parameters.keybindings;
    this.timing = parameters.timing;
    this.postTrialHandler = parameters.postTrialHandler;

    // Timer
    this.timer = null;
  }

  /**
   * Get the parameters of the renderer
   * @return {any}
   */
  getParameters() {
    return this.parameters;
  }

  /**
   * Setup the keybindings for the stimulus
   */
  createKeybindings() {
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
  removeKeybindings() {
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
  setTimer(timer) {
    this.clearTimer();
    this.timer = timer;
  }

  /**
   * Retrieve the timer of the stimuli
   * @return {number}
   */
  getTimer() {
    return this.timer;
  }

  /**
   * Clear the timer of the stimuli
   */
  clearTimer() {
    if (this.timer !== null) {
      window.clearTimeout(this.timer);
    }
  }

  /**
   * Get the handler called after each trial
   * @return {any}
   */
  getPostTrialHandler() {
    return this.postTrialHandler;
  }

  /**
   * Run the stimuli animations
   * @param {any} parameters runner properties
   */
  static run(parameters) {
    // Instantiate graphics.
    const graphics = parameters.graphics;

    for (let c = 0; c < parameters.components.length; c++) {
      const component = parameters.components[c];
      if (component === "outline") {
        graphics.addOutline();
      } else if (component === "fixation") {
        graphics.addFixation();
      } else if (component === "dots") {
        graphics.addDots();
      } else if (component === "left_arc") {
        graphics.addLeftArc();
      } else if (component === "right_arc") {
        graphics.addRightArc();
      } else {
        console.warn(`Unknown component: '${component}'`);
      }
    }
  }
}

export default Stimulus;
