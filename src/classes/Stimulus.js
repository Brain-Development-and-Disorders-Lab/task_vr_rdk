// Custom modules
import Dot from './Dot.js';

// Three.js modules
import {
  Mesh,
  CircleGeometry,
  MeshBasicMaterial,
  PlaneGeometry,
  EllipseCurve,
  BufferGeometry,
} from 'three';
import { MeshLine, MeshLineMaterial } from 'three.meshline';

// Utility modules
import _ from 'lodash';

// Fixed graphical values
const REFRESH_RATE = 90; // hertz
const APERTURE_RADIUS = 4; // degrees
const FIXATION_RADIUS = 0.1; // degrees
const DOT_RADIUS = 0.03; // degrees
const DOT_OFFSET = 0.25; // units
const DOT_VELOCITY = 2 / REFRESH_RATE;
const BACKGROUND_OFFSET = 0.5; // units

/**
 * Stimulus class used to interface with the renderer class and generate the aperture
 */
class Stimulus {
  // Public properties
  target;
  distance;

  // Private properties
  _animated;
  _parameters;
  _components;

  /**
   * Stimulus constructor
   * @param {THREE.Group} target instance of a Three.js 'Group' class
   */
  constructor(target, distance) {
    this.target = target;
    this.distance = distance;

    // Collection of dynamic (animated) components
    this._animated = [];

    // Instantiate the collection of components
    this._components = {
      background: this._addBackground(),
      outline: this._addOutline(),
      fixation: {
        standard: this._addFixation(),
        correct: this._addFixation('green'),
        incorrect: this._addFixation('red'),
      },
      reference: {
        left: this._addLeftArc(),
        right: this._addRightArc(),
      },
      dots: this._addDots(),
    };

    // Instantiate the visibility state
    this._parameters = {
      background: true,
      outline: false,
      fixation: 'none',
      reference: false,
      dots: {
        visible: false,
        coherence: 0.0,
        direction: 0.0,
      },
    };
    this.setParameters(this._parameters);
  }

  /**
   * Adjust the visibility of Stimulus components
   * @param {any} parameters specify the components of the RDK stimulus that are visible
   * @param {boolean} parameters.background show or hide the white background
   * @param {boolean} parameters.outline show or hide the aperture outline
   * @param {string} parameters.fixation show or hide the fixation cross ("none", "standard", "red", "green")
   * @param {boolean} parameters.reference show or hide the reference indicators
   * @param {any} parameters.dots show or hide the dots and adjust their parameters
   * @param {boolean} parameters.dots.visible show or hide the dots
   * @param {number} parameters.dots.coherence adjust the proportion of coherent dots
   * @param {number} parameters.dots.direction adjust the direction of the coherent dots
   */
  setParameters(parameters) {
    this._parameters = parameters;

    // Copy and apply the visibility state (simple toggles)
    this._components.background.visible = this._parameters.background;
    this._components.outline.visible = this._parameters.outline;

    // Fixation cross (composite components)
    if (this._parameters.fixation || _.isUndefined(this._parameters.fixation)) {
      // Reset the fixation cross visibility if anything specified
      this._components.fixation.standard.map((component) => {
        component.visible = false;
      });
      this._components.fixation.correct.map((component) => {
        component.visible = false;
      });
      this._components.fixation.incorrect.map((component) => {
        component.visible = false;
      });
    }
    if (_.isEqual(this._parameters.fixation, 'standard')) {
      // Show 'standard' fixation cross
      this._components.fixation.standard.map((component) => {
        component.visible = true;
      });
    } else if (_.isEqual(this._parameters.fixation, 'correct')) {
      // Show 'correct' fixation cross
      this._components.fixation.correct.map((component) => {
        component.visible = true;
      });
    } else if (_.isEqual(this._parameters.fixation, 'incorrect')) {
      // Show 'incorrect' fixation cross
      this._components.fixation.incorrect.map((component) => {
        component.visible = true;
      });
    }

    // Reference (composite component)
    if (this._parameters.reference === true) {
      this._components.reference.left.visible = true;
      this._components.reference.right.visible = true;
    } else {
      this._components.reference.left.visible = false;
      this._components.reference.right.visible = false;
    }

    // Dots (composite components)
    if (this._parameters.dots.visible === true) {
      // Show the dots
      this._components.dots = this._addDots(
        this._parameters.dots.coherence,
        this._parameters.dots.direction
      );
    } else {
      // Hide the dots
      this._clearAnimated();
    }
  }

  /**
   * Reset all elements managed by the renderer
   */
  reset() {
    this.setParameters({
      background: true,
      outline: false,
      fixation: 'none',
      reference: false,
      dots: false,
    });
  }

  _addBackground() {
    return this._createRectangle(
      0,
      0,
      -this.distance - BACKGROUND_OFFSET,
      100,
      100,
      false,
      'white',
      true
    );
  }

  /**
   * Create the aperture outline
   */
  _addOutline() {
    return this._createArc(
      0,
      0,
      -this.distance,
      this.distance * Math.tan(APERTURE_RADIUS),
      0,
      2 * Math.PI,
      false,
      0.05 * Math.tan(APERTURE_RADIUS),
      false,
      'black'
    );
  }

  /**
   * Create a fixation cross
   */
  _addFixation(style = 'black') {
    return this._createFixation(
      0,
      0,
      -this.distance,
      FIXATION_RADIUS * 2,
      false,
      style
    );
  }

  /**
   * Create the orange left arc
   */
  _addLeftArc() {
    return this._createArc(
      0,
      0,
      -this.distance,
      this.distance * Math.tan(APERTURE_RADIUS),
      -Math.PI / 2,
      Math.PI / 2,
      true,
      0.05 * Math.tan(APERTURE_RADIUS),
      false,
      '#d78000'
    );
  }

  /**
   * Create the blue right arc
   */
  _addRightArc() {
    return this._createArc(
      0,
      0,
      -this.distance,
      this.distance * Math.tan(APERTURE_RADIUS),
      -Math.PI / 2,
      Math.PI / 2,
      false,
      0.05 * Math.tan(APERTURE_RADIUS),
      false,
      '#3ea3a3'
    );
  }

  /**
   * Create the arrays of dots seen in each stimuli
   */
  _addDots(coherence, referenceDirection) {
    const dotCount = 20 ** 2;
    const dotRowCount = Math.floor(Math.sqrt(dotCount));
    const apertureRadius = this.distance * Math.tan(APERTURE_RADIUS);
    for (let i = -dotRowCount / 2; i < dotRowCount / 2; i++) {
      for (let j = -dotRowCount / 2; j < dotRowCount / 2; j++) {
        const delta = Math.random();
        const x =
          (i * apertureRadius * 2) / dotRowCount +
          (delta * apertureRadius * 2) / dotRowCount;
        const y =
          (j * apertureRadius * 2) / dotRowCount +
          (delta * apertureRadius * 2) / dotRowCount;

        if (delta > coherence) {
          // Non-dynamic dot that is just moving in random paths
          const dot = new Dot(x, y, -this.distance - DOT_OFFSET, {
            type: 'random',
            radius: this.distance * Math.tan(DOT_RADIUS),
            velocity: DOT_VELOCITY,
            direction: 2 * Math.PI * Math.random(),
            apertureRadius: apertureRadius,
          });
          this._createDot(dot, true);
        } else {
          const dot = new Dot(x, y, -this.distance - DOT_OFFSET, {
            type: 'reference',
            radius: this.distance * Math.tan(DOT_RADIUS),
            velocity: DOT_VELOCITY,
            direction: referenceDirection,
            apertureRadius: apertureRadius,
          });
          this._createDot(dot, true);
        }
      }
    }
  }

  /**
   * Create a circle
   * @param {number} x x-coordinate of the circle center
   * @param {number} y y-coordinate of the circle center
   * @param {number} z z-coordinate of the circle center
   * @param {number} r radius of the circle
   * @param {boolean} animate toggles updating the object via a step() method
   * @param {string} fill the colour of the fill
   * @return {THREE.Mesh} a Three.js Circle object
   */
  _createCircle(x, y, z, r, animate = false, fill = 'black') {
    const circle = new Mesh(
      new CircleGeometry(r, 64, 0),
      new MeshBasicMaterial({
        color: fill,
        toneMapped: false,
      })
    );
    circle.position.set(x, y, z);
    circle.visible = false;

    this.target.add(circle);
    if (animate) this._addAnimated(circle);

    return circle;
  }

  /**
   * Add dot to the render layer
   * @param {Dot} dot instance of Dot
   * @param {boolean} animate toggles updating the object via a step() method
   * @param {string} fill the colour of the Dot
   * @return {THREE.Mesh} a Three.js Circle object
   */
  _createDot(dot, animate = false, fill = 'black') {
    // Create three.js components
    const circle = new Mesh(
      new CircleGeometry(dot.getRadius(), 32, 0),
      new MeshBasicMaterial({
        color: fill,
        toneMapped: false,
      })
    );
    circle.position.set(dot.getX(), dot.getY(), dot.getZ());
    circle.visible = false;
    dot.setObject(circle);

    this.target.add(dot.getObject());
    if (animate) this._addAnimated(dot);

    return circle;
  }

  /**
   * Create a rectangle
   * @param {number} x x-coordinate of the rectangle
   * @param {number} y y-coorindate of the rectangle
   * @param {number} z z-coorindate of the rectangle
   * @param {number} w width of the rectangle
   * @param {number} h height of the rectangle
   * @param {boolean} animate toggles updating the object via a step() method
   * @param {string} fill the colour of the rectangle
   * @return {THREE.Mesh} a Three.js PlaneGeometry object
   */
  _createRectangle(x, y, z, w, h, animate = false, fill = 'black') {
    const rectangle = new Mesh(
      new PlaneGeometry(w, h),
      new MeshBasicMaterial({
        color: fill,
        toneMapped: false,
      })
    );
    rectangle.position.set(x, y, z);
    rectangle.visible = false;

    this.target.add(rectangle);
    if (animate) this._addAnimated(rectangle);

    return rectangle;
  }

  /**
   * Creates the fixation cross in Two.js
   * @param {number} x x-coordinate of the fixation cross
   * @param {number} y y-coordinate of the fixation cros
   * @param {number} z z-coordinate of the fixation cros
   * @param {float} d the width of the fixation cross
   * @param {boolean} animate toggles updating the object via a step() method
   * @param {string} fill the colour of the fixation cross
   * @return {Array} array containing two Two.Rectangle objects
   */
  _createFixation(x, y, z, d, animate = false, fill = 'black') {
    // Add a white circle behind the cross to improve contrast
    const background = this._createCircle(
      x,
      y,
      z - 0.05,
      d * 0.8,
      animate,
      'white'
    );

    // Bars of the fixation cross
    const horizontal = this._createRectangle(
      x,
      y,
      z + 0.02,
      d,
      d / 4,
      animate,
      fill
    );
    const vertical = this._createRectangle(x, y, z, d / 4, d, animate, fill);

    return [background, horizontal, vertical];
  }

  /**
   * Create an arc
   * @param {number} x x-coordinate of the arc
   * @param {number} y y-coordinate of the arc
   * @param {number} z z-coordinate of the arc
   * @param {number} thetaStart the starting angle of the arc
   * @param {number} thetaEnd the ending angle of the arc
   * @param {boolean} clockwise angle the arc clockwise or not
   * @param {number} width the line width of the arc
   * @param {string} fill the colour of the arc
   * @return {ArcSegment} arc object
   */
  _createArc(
    x,
    y,
    z,
    r,
    thetaStart,
    thetaEnd,
    clockwise = false,
    width = 1,
    animate = false,
    fill = 'red'
  ) {
    const curve = new EllipseCurve(
      x,
      y,
      r,
      r,
      thetaStart,
      thetaEnd,
      clockwise,
      0
    );
    const geometry = new BufferGeometry().setFromPoints(curve.getPoints(50));
    const line = new MeshLine();
    line.setGeometry(geometry);
    const mesh = new Mesh(
      line.geometry,
      new MeshLineMaterial({ color: fill, lineWidth: width })
    );
    mesh.position.set(x, y, z);
    mesh.visible = false;

    this.target.add(mesh);
    if (animate) this._addAnimated(mesh);

    return mesh;
  }

  /**
   * Adds an element to the list of animated element
   * @param {any} element add an element
   */
  _addAnimated(element) {
    this._animated.push(element);
  }

  /**
   * Retrieves the list of animated elements
   * @return {any[]} the array of elements
   */
  getAnimated() {
    return this._animated;
  }

  /**
   * Get the Three.js Group instance
   * @return {THREE.Group} Three.js Group instance
   */
  getTarget() {
    return this.target;
  }

  /**
   * Clear the list of animated elements
   */
  _clearAnimated() {
    for (let e = 0; e < this._animated.length; e++) {
      this.target.remove(this._animated[e].getObject());
      this._animated[e] = null;
    }
    this._animated = [];
  }

  /**
   * Clear all elements from the renderer
   */
  clear() {
    this._clearAnimated();
  }
}

export default Stimulus;
