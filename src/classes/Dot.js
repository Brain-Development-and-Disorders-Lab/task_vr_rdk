import { Vector3 } from 'three';

/**
 * Dot class used to abstract the positioning of moving dots.
 */
class Dot {
  // Positional coordinates
  x;
  y;
  z;
  initialPosition; // Vector3

  // Dot parameters
  type; // 'random' or 'reference'
  velocity; // float
  radius; // float, radians
  direction; // float, radians

  // Aperture parameters
  apertureRadius;

  // Dot Three.js component
  dot;

  /**
   * Dot class constructor
   * @param {number} x the x coordinate of the dot
   * @param {number} y the y coordinate of the dot
   * @param {number} z the z coordinate of the dot
   * @param {any} parameters a parameters object containing properties
   * @param {'random' | 'reference'} parameters.type
   * @param {number} parameters.velocity
   * @param {number} parameters.radius
   * @param {number} parameters.apertureRadius
   */
  constructor(x, y, z, parameters) {
    // Positional coordinates
    this.x = x;
    this.y = y;
    this.z = z;
    this.initialPosition = new Vector3(x, y, z);

    // Unpack parameters
    this.type = parameters.type;
    this.velocity = parameters.velocity;
    this.radius = parameters.radius;
    this.direction = parameters.direction;
    this.apertureRadius = parameters.apertureRadius;

    this.active = true; // Activate the 'step' function and visibility
  }

  /**
   * Get the x position of the dot
   * @return {number} x position
   */
  getX() {
    return this.x;
  }

  /**
   * Get the y position of the dot
   * @return {number} y position
   */
  getY() {
    return this.y;
  }

  /**
   * Get the z position of the dot
   * @return {number} z position
   */
  getZ() {
    return this.z;
  }

  /**
   * Get the angular direction of the dot
   * @return {number} angular direction
   */
  getDirection() {
    return this.direction;
  }

  /**
   * Get the radius of the dot
   * @return {number} dot radius
   */
  getRadius() {
    return this.radius;
  }

  /**
   * Get the current position of the dot
   * @return {Vector3}
   */
  getPosition() {
    return new Vector3(this.x, this.y, this.z);
  }

  /**
   * Set the current position of the dot
   * @param {number} x x coordinate
   * @param {number} y y coordinate
   * @param {number} z z coordinate
   */
  setPosition(x, y, z) {
    this.x = x;
    this.y = y;
    this.z = z;
    this.dot.position.set(x, y, z);
  }

  /**
   * Get the type of dot
   * @return {'random' | 'reference'} the dot type
   */
  getType() {
    return this.type;
  }

  /**
   * Set the mesh component of the dot
   * @param {THREE.Mesh} dot Three.js Mesh component
   */
  setObject(dot) {
    this.dot = dot;
  }

  /**
   * Get the mesh component of the dot
   * @return {THREE.Mesh}
   */
  getObject() {
    return this.dot;
  }

  /**
   * Enable or disable the dot activity and visibility
   * @param {boolean} active activity status of the dot
   */
  setActive(active) {
    this.active = active;
    this.dot.visible = this.active;
  }

  /**
   * Replace the dot to its original position
   */
  resetPosition() {
    this.setPosition(
      this.initialPosition.x,
      this.initialPosition.y,
      this.initialPosition.z
    );
  }

  /**
   * Generic step method that is called sixty times per second
   * @param {number} frameCount the number of elapsed frames
   */
  step(frameCount) {
    if (this.dot && this.active) {
      let x = this.x;
      let y = this.y;

      if (this.type === 'random' && frameCount % 10 === 0) {
        // Adjust the direction
        const delta = Math.random();
        if (Math.random() > 0.5) {
          this.direction -= (Math.PI / 4) * delta;
        } else {
          this.direction += (Math.PI / 4) * delta;
        }
      }

      const inAperture =
        Math.sqrt(x ** 2 + y ** 2) < this.apertureRadius + this.radius / 8;

      if (this.type === 'reference') {
        // Check if outside boundary
        if (Math.abs(x) > this.apertureRadius) {
          x = x - 2 * this.apertureRadius * Math.cos(this.direction);
          y = y - 2 * this.apertureRadius * Math.sin(this.direction);
        }
      } else if (this.type === 'random' && !inAperture) {
        // Reverse the direction of "random"-type dots to avoid disappearance
        this.direction -= Math.PI;
      }

      // Update position
      x = x + this.velocity * Math.cos(this.direction);
      y = y + this.velocity * Math.sin(this.direction);

      // Show the dot only when within the aperture
      this.dot.visible =
        Math.sqrt(x ** 2 + y ** 2) <= this.apertureRadius + this.radius / 8;

      // Apply the updated dot position
      this.setPosition(x, y, this.z);
    }
  }
}

export default Dot;
