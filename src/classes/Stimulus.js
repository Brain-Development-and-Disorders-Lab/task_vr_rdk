// Custom modules
import Dot from './Dot.js';

// Three.js modules
import {
  Color,
  Mesh,
  CircleGeometry,
  MeshBasicMaterial,
  PlaneGeometry,
  EllipseCurve,
  BufferGeometry,
  SphereGeometry,
  DoubleSide,
} from 'three';
import { MeshLine, MeshLineMaterial } from 'three.meshline';

// Three-Mesh-UI
import FontJSON from '../fonts/Arial-msdf.json';
import FontImage from '../fonts/Arial.png';
import { Block, Text } from 'three-mesh-ui';

// Utility modules
import _ from 'lodash';

// Fixed graphical values
const REFRESH_RATE = 90; // hertz
const APERTURE_RADIUS = 4; // degrees
const FIXATION_RADIUS = 0.1; // degrees
const DOT_RADIUS = 0.03; // degrees
const DOT_OFFSET = 0.25; // units
const DOT_VELOCITY = 2.0 / REFRESH_RATE; // degrees
const DOT_COUNT = 20 ** 2;

/**
 * Stimulus class used to interface with the renderer class and generate the aperture
 */
class Stimulus {
  // Public properties
  target;
  distance;
  presets;

  // Private properties
  _animated;
  _parameters;
  _components;

  /**
   * Stimulus constructor
   * @param {THREE.Group} target instance of a Three.js 'Group' class
   */
  constructor(target, distance, presets = {}) {
    this.target = target;
    this.distance = distance;
    this.presets = presets;

    // Create a 'default' preset with only the background visible
    this.presets.default = {
      background: true,
      outline: false,
      fixation: 'none',
      reference: false,
      dots: {
        visible: false,
        coherence: 0.5,
        direction: Math.PI,
      },
      ui: {
        confidence: false,
        response: false,
        navigation: 'none',
      },
    };

    // Collection of dynamic (animated) components
    this._animated = [];

    // 'reference'-type dots that will remain invisible
    this.referenceError = 0.0;

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
      dots: this._addDots(0.5, Math.PI),
      ui: {
        confidence: this._addConfidenceText(),
        response: this._addResponseButtons(),
        navigation: {
          both: this._addNavigationButtons(),
          left: this._addNavigationButtons('left'),
          right: this._addNavigationButtons('right'),
        },
      },
    };

    // Instantiate the visibility state
    this._parameters = this.presets.default;
    this.setParameters(this._parameters);
  }

  /**
   * Add a new preset to the collection of stored presets
   * @param {string} name the preset name
   * @param {any} parameters the set of preset parameters
   */
  addPreset(name, parameters) {
    if (_.isUndefined(this.presets[name])) {
      this.presets[name] = parameters;
    } else {
      console.warn(`Preset '${name}' already exists`);
    }
  }

  /**
   * Activate a specific preset if it exists
   * @param {string} name the preset name
   */
  usePreset(name) {
    if (_.isUndefined(this.presets[name])) {
      console.error(`Preset '${name}' does not exist`);
      this.setParameters(this.presets.default);
    } else {
      this.setParameters(this.presets[name]);
    }
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
      // Update dot coherence and direction
      this._setDotCoherence(this._parameters.dots.coherence);
      this._setDotDirection(this._parameters.dots.direction);

      // Show the dots
      this._components.dots.forEach((dot) => {
        dot.setActive(true);
      });
    } else {
      // Hide the dots
      this._components.dots.forEach((dot) => {
        dot.setActive(false);
        dot.resetPosition();
      });
    }

    // Text
    this._components.ui.confidence.visible = this._parameters.ui.confidence;
    this._components.ui.response.visible = this._parameters.ui.response;

    // Text - Navigation
    if (this._parameters.ui.navigation === 'left') {
      this._components.ui.navigation.both.visible = false;
      this._components.ui.navigation.left.visible = true;
      this._components.ui.navigation.right.visible = false;
    } else if (this._parameters.ui.navigation === 'right') {
      this._components.ui.navigation.both.visible = false;
      this._components.ui.navigation.left.visible = false;
      this._components.ui.navigation.right.visible = true;
    } else if (this._parameters.ui.navigation === 'both') {
      this._components.ui.navigation.both.visible = true;
      this._components.ui.navigation.left.visible = false;
      this._components.ui.navigation.right.visible = false;
    } else {
      this._components.ui.navigation.both.visible = false;
      this._components.ui.navigation.left.visible = false;
      this._components.ui.navigation.right.visible = false;
    }
  }

  getComponents() {
    return this._components;
  }

  /**
   * Reset all elements managed by the renderer
   */
  reset() {
    this.usePreset('default');
  }

  _addBackground() {
    return this._createSphere(0, 0, 0, 10, false, 'white');
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

  _addConfidenceText(fill = 'black') {
    return this._createConfidenceText(
      0,
      0,
      -this.distance,
      `Between the last two trials, did you feel more confident about your response to:\n\n\tThe previous trial (1 trial ago), or\n\tThe trial before that (2 trials ago)?`,
      fill
    );
  }

  _addResponseButtons() {
    return this._createActionButtons(
      'Left',
      'Right',
      2.0,
      0.8,
      {
        bg: 'orange',
        fontColor: 'black',
        state: 'enabled',
      },
      {
        bg: 'teal',
        fontColor: 'white',
        state: 'enabled',
      }
    );
  }

  _addNavigationButtons(style = 'both') {
    if (style === 'left') {
      return this._createActionButtons(
        '< Back',
        'Next >',
        0.4,
        0.8,
        { state: 'enabled' },
        { state: 'disabled' }
      ).translateY(-1.6);
    } else if (style === 'right') {
      return this._createActionButtons(
        '< Back',
        'Next >',
        0.4,
        0.8,
        { state: 'disabled' },
        { state: 'enabled' }
      ).translateY(-1.6);
    } else {
      return this._createActionButtons('< Back', 'Next >', 0.4, 0.8).translateY(
        -1.6
      );
    }
  }

  /**
   * Create the arrays of dots seen in each stimuli
   */
  _addDots(initialCoherence, initialDirection) {
    const referenceDots = [];
    const randomDots = [];
    const dotRowCount = Math.floor(Math.sqrt(DOT_COUNT));
    const apertureRadius = this.distance * Math.tan(APERTURE_RADIUS);
    for (let i = -dotRowCount / 2; i < dotRowCount / 2; i++) {
      for (let j = -dotRowCount / 2; j < dotRowCount / 2; j++) {
        const x =
          (i * apertureRadius * 2) / dotRowCount +
          (Math.random() * apertureRadius * 2) / dotRowCount;
        const y =
          (j * apertureRadius * 2) / dotRowCount +
          (Math.random() * apertureRadius * 2) / dotRowCount;

        if (Math.random() > initialCoherence) {
          // Dynamic dot that is just moving in a random path
          randomDots.push(
            new Dot(x, y, -this.distance - DOT_OFFSET, {
              type: 'random',
              radius: this.distance * Math.tan(DOT_RADIUS),
              velocity: DOT_VELOCITY,
              direction: 2 * Math.PI * Math.random(),
              apertureRadius: apertureRadius,
            })
          );
        } else {
          referenceDots.push(
            new Dot(x, y, -this.distance - DOT_OFFSET, {
              type: 'reference',
              radius: this.distance * Math.tan(DOT_RADIUS),
              velocity: DOT_VELOCITY,
              direction: initialDirection,
              apertureRadius: apertureRadius,
            })
          );
        }
      }
    }

    // Calculate the number of 'reference'-type dots that will be hidden
    let hiddenReferenceCount = 0;
    referenceDots.forEach((dot) => {
      if (
        Math.sqrt(dot.getX() ** 2 + dot.getY() ** 2) >=
        dot.apertureRadius + dot.radius / 2
      ) {
        hiddenReferenceCount++;
      }
    });
    this.referenceError = hiddenReferenceCount / DOT_COUNT;

    // Create all remaining Dots
    const allDots = _.concat(referenceDots, randomDots);
    allDots.forEach((dot) => {
      this._createDot(dot, true);
    });

    return allDots;
  }

  _setDotCoherence(coherence) {
    // Update the coherence of the dots by modifying the number
    // of 'random' vs. 'reference' dots
    let dotIndex = 0;
    this._components.dots = _.shuffle(this._components.dots);
    this._components.dots.forEach((dot) => {
      if (dotIndex / this._components.dots.length > coherence) {
        dot.type = 'random';
      } else {
        dot.type = 'reference';
      }
      dotIndex++;
    });
  }

  _setDotDirection(direction) {
    // Set the direction of all reference dots
    this._components.dots.forEach((dot) => {
      if (dot.type === 'reference') {
        dot.direction = direction;
      } else {
        dot.direction = 2 * Math.PI * Math.random();
      }
    });
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

  _createSphere(x, y, z, r, animate = false, fill = 'black') {
    const sphere = new Mesh(
      new SphereGeometry(r, 32, 16),
      new MeshBasicMaterial({
        color: fill,
        toneMapped: false,
        side: DoubleSide,
      })
    );
    sphere.position.set(x, y, z);
    sphere.visible = false;

    this.target.add(sphere);
    if (animate) this._addAnimated(sphere);

    return sphere;
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

  _createConfidenceText(x, y, z, text) {
    const confidenceContainer = new Block({
      justifyContent: 'center',
      padding: 0.025,
      margin: 0.1,
      width: 4.0,
      fontFamily: FontJSON,
      fontTexture: FontImage,
      backgroundOpacity: 0,
      backgroundColor: new Color('white'),
    });
    confidenceContainer.position.set(x, y, z);
    this.target.add(confidenceContainer);

    const descriptionContainer = new Block({
      height: 1.4,
      width: 4.0,
      margin: 0.02,
      padding: 0.04,
      borderRadius: 0.04,
      justifyContent: 'center',
      fontSize: 0.1,
      bestFit: 'auto',
      fontColor: new Color('black'),
      backgroundColor: new Color('white'),
    });
    descriptionContainer.add(
      new Text({
        content: text,
      })
    );
    confidenceContainer.add(descriptionContainer);

    const actionButtons = this._createActionButtons(
      `2 trials ago`,
      `1 trial ago`,
      0.8,
      1.0
    );
    confidenceContainer.add(actionButtons);

    return confidenceContainer;
  }

  _createActionButtons(
    leftText,
    rightText,
    spacing = 1.7,
    width = 0.8,
    leftOptions = {
      bg: 0xc9c9c9,
      fontColor: 'black',
      state: 'enabled',
    },
    rightOptions = {
      bg: 0xc9c9c9,
      fontColor: 'black',
      state: 'enabled',
    }
  ) {
    // Container for buttons
    const actionContainer = new Block({
      contentDirection: 'row',
      justifyContent: 'center',
      padding: 0.025,
      width: 8.0,
      fontFamily: FontJSON,
      fontTexture: FontImage,
      backgroundOpacity: 0,
    });

    // Left action button
    // Apply background color
    let leftBackgroundColor = leftOptions.bg ?? 0xc9c9c9;
    let leftFontColor = leftOptions.fontColor ?? 'black';
    if (leftOptions.state === 'disabled') {
      leftBackgroundColor = 0xededed;
      leftFontColor = 0x757575;
    }

    // Create button `Block` and add `Text`
    const leftContainer = new Block({
      height: 0.4,
      width: width,
      margin: spacing,
      borderRadius: 0.04,
      justifyContent: 'center',
      fontSize: 0.08,
      bestFit: 'auto',
      backgroundColor: new Color(leftBackgroundColor),
      fontColor: new Color(leftFontColor),
    });
    leftContainer.add(
      new Text({
        content: leftText,
      })
    );

    // Right action button
    // Apply background color
    let rightBackgroundColor = rightOptions.bg ?? 0xc9c9c9;
    let rightFontColor = rightOptions.fontColor ?? 'black';
    if (rightOptions.state === 'disabled') {
      rightBackgroundColor = 0xededed;
      rightFontColor = 0x757575;
    }

    // Create button `Block` and add `Text`
    const rightContainer = new Block({
      height: 0.4,
      width: width,
      margin: spacing,
      borderRadius: 0.04,
      justifyContent: 'center',
      fontSize: 0.08,
      bestFit: 'auto',
      backgroundColor: new Color(rightBackgroundColor),
      fontColor: new Color(rightFontColor),
    });
    rightContainer.add(
      new Text({
        content: rightText,
      })
    );

    actionContainer.add(leftContainer, rightContainer);
    this.target.add(actionContainer);
    return actionContainer;
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
