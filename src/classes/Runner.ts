// Core modules
import { Stimulus } from "./Stimulus";

/**
 * Driver class that operates trials, showing stimuli
 */
export class Runner {
  /**
   * Start the runner
   * @param {Stimulus} stimulus stimulus to display
   */
  static start(stimulus: Stimulus): void {
    Runner.pre(stimulus);
  }

  /**
   * First stage of the runner
   * @param {Stimulus} stimulus stimulus to display
   */
  static pre(stimulus: Stimulus): void {
    stimulus.setTimer(
      window.setTimeout(() => {
        Runner.run(stimulus);
      }, stimulus.getParameters().timing.pre)
    );
  }

  /**
   * Run stage of the runner
   * @param {Stimulus} stimulus stimulus to display
   */
  static run(stimulus: Stimulus): void {
    stimulus.createKeybindings();
    Stimulus.run(stimulus.getParameters());
    window.clearTimeout(stimulus.getTimer());
    if (stimulus.getParameters().timing.run >= 0) {
      stimulus.setTimer(
        window.setTimeout(() => {
          Runner.post(stimulus);
        }, stimulus.getParameters().timing.run)
      );
    }
  }

  /**
   * Post stage of the runner
   * @param {Stimulus} stimulus stimulus to display
   */
  static post(stimulus: Stimulus): void {
    stimulus.removeKeybindings();
    window.clearTimeout(stimulus.getTimer());
    stimulus.setTimer(
      window.setTimeout(() => {
        Runner.finish(stimulus);
      }, stimulus.getParameters().timing.post)
    );
  }

  /**
   * Final stage of the runner
   * @param {Stimulus} stimulus stimulus to display
   */
  static finish(stimulus: Stimulus): void {
    stimulus.getParameters().graphics.renderer.target.clear();
    window.clearTimeout(stimulus.getTimer());
    stimulus.getPostTrialHandler()();
  }
}
