// Core modules
import Dot from "./Dot";

// Three.js modules
import { Mesh, CircleGeometry, MeshBasicMaterial, PlaneGeometry } from "three";

/**
 * Renderer abstraction that interfaces directly with the
 * Three.js graphics library
 */
export class Renderer {
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
  createCircle(x, y, z, r, animate = false, fill = "black") {
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
  createDot(dot, animate = false, fill = "black") {
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
  createRectangle( x, y, z, w, h, animate = false, fill = "black") {
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
   * @param {float} d the width of the fixation cross
   * @param {boolean} update toggles updating the object via a step() method
   * @param {string} fill the colour of the fixation cross
   * @return {Array} array containing two Two.Rectangle objects
   */
  createFixation(x, y, d, update, fill = "black") {
    const coordinates = Renderer.translate(x, y, this.width, this.height);
    const rectangleHorizontal = this.target.makeRectangle(
      coordinates[0],
      coordinates[1],
      d,
      d / 4
    );
    const rectangleVertical = this.target.makeRectangle(
      coordinates[0],
      coordinates[1],
      d / 4,
      d
    );
    rectangleHorizontal.fill = fill;
    rectangleVertical.fill = fill;
    rectangleHorizontal.noStroke();
    rectangleVertical.noStroke();
    if (update) {
      this.addElement(rectangleHorizontal);
      this.addElement(rectangleVertical);
    }
    return [rectangleHorizontal, rectangleVertical];
  }

  /**
   * Create an arc
   * @param {number} startAngle the starting angle of the arc
   * @param {number} endAngle the ending angle of the arc
   * @param {string} fill the colour of the arc
   * @return {ArcSegment} arc object
   */
  createArc(startAngle, endAngle, fill = "red") {
    const coordinates = Renderer.translate(0, 0, this.width, this.height);
    const arc = this.target.makeArcSegment(
      coordinates[0],
      coordinates[1],
      this.viewRadius,
      this.viewRadius,
      startAngle,
      endAngle
    );
    arc.stroke = fill;
    arc.linewidth = 10;
    this.target.add(arc);
    return arc;
  }

  /**
   * Create a line
   * @param {number} x1 starting x-coordinate
   * @param {number} y1 starting y-coordinate
   * @param {number} x2 ending x-coordinate
   * @param {number} y2 ending y-coordinate
   * @param {number} width width of the line
   * @param {string} fill the colour of the line
   * @return {Two.Line} line object
   */
  createLine(
    x1,
    y1,
    x2,
    y2,
    width,
    fill = "black"
  ) {
    const startCoordinates = Renderer.translate(
      x1,
      y1,
      this.width,
      this.height
    );
    const endCoordinates = Renderer.translate(x2, y2, this.width, this.height);
    const line = this.target.makeLine(
      startCoordinates[0],
      startCoordinates[1],
      endCoordinates[0],
      endCoordinates[1]
    );
    line.stroke = fill;
    line.linewidth = width;
    this.target.add(line);
    return line;
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

  /**
   * Takes coordinates of form x [-150, 150], y [-150, 150] and
   * translates them to x [0, 300], y [0, 300]
   * @param {number} x original x-coordinate
   * @param {number} y original y-coordinate
   * @param {number} width width of the view
   * @param {number} height height of the view
   * @return {number[]} translated x and y coordinates
   */
  static translate(
    x,
    y,
    width,
    height
  ) {
    x += width / 2;
    y = height / 2 - y;
    return [x, y];
  }

  /**
   * Determine if a set of coordinates is visible in the view
   * @param {number} x the x-coordinate of the object
   * @param {number} y the y-coordinate of the object
   * @param {number} radius the radius of the view
   * @return {boolean}
   */
  static visible(x, y, radius) {
    const distance = Math.sqrt(x ** 2 + y ** 2);
    return distance < radius;
  }

  /**
   * Determine if a set of coodinates is inside the view
   * @param {number} x the x-coordinate of the object
   * @param {number} y the y-coordinate of the object
   * @param {number} w the width of the view
   * @param {number} h the height of the view
   * @return {boolean}
   */
  static renderable(x, y, w, h) {
    return x >= 0 && x <= w && y >= 0 && y <= h;
  }
}
