// Core modules
import Dot from './Dot.js';
import Renderer from './Renderer.js';

/**
 * Graphics class used to interface with the plugin and the renderer class
 */
class Graphics {
  renderer;

  /**
   * Graphics constructor
   * @param {Renderer} renderer renderer class instance
   */
  constructor(renderer) {
    this.renderer = renderer;
  }

  /**
   * Set the visibility of the mouse cursor
   * @param {boolean} visible the status of cursor visibility
   */
  cursorVisibility(visible = true) {
    document.body.style.cursor = visible ? 'auto' : 'none';
  }

  addBackground() {
    this.renderer.createRectangle(0, 0, -2.4, 100, 50, false, 'white', true);
  }

  /**
   * Create the aperture outline
   */
  addOutline() {
    this.renderer.createArc(
      0,
      0,
      -2.2,
      0.97,
      0,
      2 * Math.PI,
      false,
      0.03,
      false,
      'black'
    );
  }

  /**
   * Create a fixation cross
   */
  addFixation(style = 'black') {
    this.renderer.createFixation(0, 0, -1.8, 0.1, false, style);
  }

  /**
   * Create the orange left arc
   */
  addLeftArc() {
    this.renderer.createArc(
      0,
      0,
      -2,
      0.9,
      -Math.PI / 2,
      Math.PI / 2,
      true,
      0.03,
      false,
      '#d78000'
    );
  }

  /**
   * Create the blue right arc
   */
  addRightArc() {
    this.renderer.createArc(
      0,
      0,
      -2,
      0.9,
      -Math.PI / 2,
      Math.PI / 2,
      false,
      0.03,
      false,
      '#3ea3a3'
    );
  }

  /**
   * Create the arrays of dots seen in each stimuli
   */
  addDots(coherence, referenceDirection) {
    const dotCount = 20 ** 2;
    const dotRowCount = Math.floor(Math.sqrt(dotCount));
    for (let i = -dotRowCount / 2; i < dotRowCount / 2; i++) {
      for (let j = -dotRowCount / 2; j < dotRowCount / 2; j++) {
        const delta = Math.random();
        const x = (i * 2) / dotRowCount + (delta * 2) / dotRowCount;
        const y = (j * 2) / dotRowCount + (delta * 2) / dotRowCount;

        if (delta > coherence) {
          // Non-dynamic dot that is just moving in random paths
          const dot = new Dot(x, y, -1.9, {
            type: 'random',
            radius: 0.03,
            velocity: 0.01,
            direction: 2 * Math.PI * Math.random(),
            apertureRadius: 0.87,
          });
          this.renderer.createDot(dot, true);
        } else {
          const dot = new Dot(x, y, -1.9, {
            type: 'reference',
            radius: 0.03,
            velocity: 0.01,
            direction: referenceDirection,
            apertureRadius: 0.87,
          });
          this.renderer.createDot(dot, true);
        }
      }
    }
  }

  /**
   * Clear all elements from the renderer
   */
  clear() {
    this.renderer.clearElements();
    this.renderer.clearAnimated();
  }
}

export default Graphics;
