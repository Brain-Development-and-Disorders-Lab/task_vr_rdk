// Core modules
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

/**
 * Renderer abstraction that interfaces directly with the
 * Three.js graphics library
 */
class Renderer {
  target;
  elements;

  /**
   * Default constructor for Renderer
   * @param {THREE.Group} target Three.js Group instance
   */
  constructor(target) {
    this.target = target;
    this.elements = [];
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
  createCircle(x, y, z, r, animate = false, fill = 'black') {
    const circle = new Mesh(
      new CircleGeometry(r, 64, 0),
      new MeshBasicMaterial({
        color: fill,
        toneMapped: false,
      })
    );
    circle.position.set(x, y, z);

    this.target.add(circle);
    if (animate) this.addElement(circle);

    return circle;
  }

  /**
   * Add dot to the render layer
   * @param {Dot} dot instance of Dot
   * @param {boolean} animate toggles updating the object via a step() method
   * @param {string} fill the colour of the Dot
   * @return {THREE.Mesh} a Three.js Circle object
   */
  createDot(dot, animate = false, fill = 'black') {
    // Create three.js components
    const circle = new Mesh(
      new CircleGeometry(dot.getRadius(), 32, 0),
      new MeshBasicMaterial({
        color: fill,
        toneMapped: false,
      })
    );
    circle.position.set(dot.getX(), dot.getY(), dot.getZ());
    dot.setObject(circle);

    this.target.add(dot.getObject());
    if (animate) this.addElement(dot);

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
  createRectangle(x, y, z, w, h, animate = false, fill = 'black') {
    const rectangle = new Mesh(
      new PlaneGeometry(w, h),
      new MeshBasicMaterial({
        color: fill,
        toneMapped: false,
      })
    );
    rectangle.position.set(x, y, z);

    this.target.add(rectangle);
    if (animate) this.addElement(rectangle);

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
  createFixation(x, y, z, d, animate = false, fill = 'black') {
    // Add a white circle behind the cross to improve contrast
    const background = this.createCircle(x, y, z, d * 0.7, animate, 'white');

    // Bars of the fixation cross
    const horizontal = this.createRectangle(x, y, z, d, d / 4, animate, fill);
    const vertical = this.createRectangle(x, y, z, d / 4, d, animate, fill);

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
  createArc(
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

    this.target.add(mesh);
    if (animate) this.addElement(mesh);

    return mesh;
  }

  /**
   * Adds an element to the list of renderered element
   * @param {any} element add an element
   */
  addElement(element) {
    this.elements.push(element);
  }

  /**
   * Retrieves the list of renderable elements
   * @return {any[]} the array of elements
   */
  getElements() {
    return this.elements;
  }

  /**
   * Clear the list of renderable elements
   */
  clearElements() {
    for (let e = 0; e < this.elements.length; e++) {
      this.target.remove(this.elements[e]);
      this.elements[e] = null;
    }
    this.elements = [];
    this.target.clear();
  }

  /**
   * Get the Three.js Group instance
   * @return {THREE.Group} Three.js Group instance
   */
  getTarget() {
    return this.target;
  }
}

export default Renderer;
