// Core modules
import { Dot } from "./Dot";

// External libraries
import { JsPsych } from "jspsych";
import Two from "two.js";
import { Circle } from "two.js/src/shapes/circle";
import { Rectangle } from "two.js/src/shapes/rectangle";
import { ArcSegment } from "two.js/src/shapes/arc-segment";

// Custom types
import { IRenderer } from "../../types";

/**
 * Renderer abstraction that interfaces directly with the
 * Two.js graphics library
 */
export class Renderer {
  public jsPsych: JsPsych;
  private target: Two;
  private displayElement: HTMLElement;
  private distanceFromScreen: number;
  private viewRadius: number;
  private dotRadius: number;
  private width: number;
  private height: number;
  private renderLayer: any;
  private elements: any[];

  /**
   * Default constructor for Renderer
   * @param {Two} two Two.js instance
   * @param {IRenderer} parameters configuration information
   */
  constructor(jsPsych: JsPsych, two: Two, parameters: IRenderer) {
    this.jsPsych = jsPsych;
    this.target = two;
    this.displayElement = parameters.target;
    this.distanceFromScreen = parameters.distanceFromScreen;
    this.viewRadius = parameters.viewRadius;
    this.dotRadius = parameters.dotRadius;
    this.width = parameters.width;
    this.height = parameters.height;
    this.renderLayer = null;
    this.elements = [];
  }

  /**
   * Create a circle
   * @param {number} x x-coordinate of the circle center
   * @param {number} y y-coordinate of the circle center
   * @param {number} r radius of the circle
   * @param {boolean} update toggles updating the object via a step() method
   * @param {string} fill the colour of the fill
   * @param {string} stroke the colour of the stroke
   * @return {Circle} a Two.js circle object
   */
  createCircle(
    x: number,
    y: number,
    r: number,
    update: boolean,
    fill = "black",
    stroke = "black"
  ): Circle {
    const coordinates = Renderer.translate(x, y, this.width, this.height);
    const circle = this.target.makeCircle(coordinates[0], coordinates[1], r);
    circle.fill = fill;
    circle.stroke = stroke;
    if (update) this.addElement(circle);
    return circle;
  }

  /**
   * Create a rectangle
   * @param {number} x x-coordinate of the rectangle
   * @param {number} y y-coorindate of the rectangle
   * @param {number} w width of the rectangle
   * @param {number} h height of the rectangle
   * @param {boolean} update toggles updating the object via a step() method
   * @param {string} fill the colour of the rectangle
   * @return {Rectangle} a Two.js rectangle object
   */
  createRectangle(
    x: number,
    y: number,
    w: number,
    h: number,
    update: boolean,
    fill = "black"
  ): Rectangle {
    const coordinates = Renderer.translate(x, y, w, h);
    const rectangle = this.target.makeRectangle(
      coordinates[0],
      coordinates[1],
      w,
      h
    );
    rectangle.fill = fill;
    if (update) this.addElement(rectangle);
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
  createFixation(x, y, d, update, fill = "black"): any[] {
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
   * Add dot to the render layer
   * @param {Dot} dot instance of Dot
   * @param {boolean} update toggles updating the object via a step() method
   * @param {string} fill the colour of the Dot
   * @return {Circle} Two.Circle instance
   */
  createDot(dot: Dot, update: boolean, fill = "black"): Circle {
    const coordinates = Renderer.translate(
      dot.getX(),
      dot.getY(),
      this.width,
      this.height
    );
    const circle = this.target.makeCircle(
      coordinates[0],
      coordinates[1],
      this.dotRadius
    );
    circle.fill = fill;
    dot.setDot(circle);
    this.renderLayer.add(circle);
    if (update) this.addElement(dot);
    return circle;
  }

  /**
   * Create an arc
   * @param {number} startAngle the starting angle of the arc
   * @param {number} endAngle the ending angle of the arc
   * @param {string} fill the colour of the arc
   * @return {ArcSegment} arc object
   */
  createArc(startAngle: number, endAngle: number, fill = "red"): ArcSegment {
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
   * Add an image onto the target
   * @param {string} imageType the type of image to add
   */
  addImage(imageType: string): void {
    if (imageType === "left") {
      // Access the graphics container
      const graphicsCanvasDiv =
        document.getElementsByClassName("graphics-container")[0];

      // Create the left image container and the left image
      const leftImage = document.createElement("img");
      if (__TARGET__ === "desktop") {
        leftImage.src = this.jsPsych.extensions.Neurocog.getStimulus("F.png");
      } else {
        leftImage.src = this.jsPsych.extensions.Neurocog.getStimulus("2.png");
      }
      leftImage.style.height = "20vh";
      leftImage.style.position = "absolute";
      leftImage.style.paddingRight = "70%";

      // Prepend the left image container to the graphics container
      graphicsCanvasDiv.prepend(leftImage);
    } else if (imageType === "right") {
      // Access the graphics container
      const graphicsCanvasDiv =
        document.getElementsByClassName("graphics-container")[0];

      // Create the right image container and the right image
      const rightImage = document.createElement("img");
      if (__TARGET__ === "desktop") {
        rightImage.src = this.jsPsych.extensions.Neurocog.getStimulus("J.png");
      } else {
        rightImage.src = this.jsPsych.extensions.Neurocog.getStimulus("3.png");
      }
      rightImage.style.height = "20vh";
      rightImage.style.position = "absolute";
      rightImage.style.paddingLeft = "70%";

      // Prepend the right image container to the graphics container
      graphicsCanvasDiv.append(rightImage);
    } else {
      // Warning unknown
      console.warn(`Unknown image type: '${imageType}'`);
    }
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
    x1: number,
    y1: number,
    x2: number,
    y2: number,
    width: number,
    fill = "black"
  ): any {
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
  addElement(element: any): void {
    this.elements.push(element);
  }

  /**
   * Retrieves the list of renderable elements
   * @return {any[]} the array of elements
   */
  getElements(): any[] {
    return this.elements;
  }

  /**
   * Clear the list of renderable elements
   */
  clearElements(): void {
    for (let e = 0; e < this.elements.length; e++) {
      this.target.remove(this.elements[e]);
      this.elements[e] = null;
    }
    this.elements = [];
    this.target.clear();
  }

  /**
   * Get the width of the view
   * @return {number} the width of the view
   */
  getWidth(): number {
    return this.width;
  }

  /**
   * Get the height of the view
   * @return {number} the height of the view
   */
  getHeight(): number {
    return this.height;
  }

  /**
   * Get the distance from the screen
   * @return {number} the distance from the screen
   */
  getDistanceFromScreen(): number {
    return this.distanceFromScreen;
  }

  /**
   * Get the view radius
   * @return {number} the radius of the view
   */
  getViewRadius(): number {
    return this.viewRadius;
  }

  /**
   * Get the dot radius
   * @return {number} the dot radius
   */
  getDotRadius(): number {
    return this.dotRadius;
  }

  /**
   * Get the display element
   * @return {any} HTML element
   */
  getDisplayElement(): any {
    return this.displayElement;
  }

  /**
   * Get the Two.js target instance
   * @return {any} Two.js instance
   */
  getTarget(): any {
    return this.target;
  }

  /**
   * Get the renderable layer
   * @return {any}
   */
  getRenderLayer(): any {
    return this.renderLayer;
  }

  /**
   * Set the render layer
   * @param {any} layer the new layer
   */
  setRenderLayer(layer: any): void {
    this.renderLayer = layer;
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
    x: number,
    y: number,
    width: number,
    height: number
  ): number[] {
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
  static visible(x: number, y: number, radius: number): boolean {
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
  static renderable(x: number, y: number, w: number, h: number): boolean {
    return x >= 0 && x <= w && y >= 0 && y <= h;
  }
}
