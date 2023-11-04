import { Vector3 } from 'three';

/**
 * Dot class used to abstract the positioning of moving dots.
 */
class Dot {
  // Positional coordinates
  x;
  y;
  z;

  // Dot parameters
  type;
  velocity;
  radius;
  direction;
  reset;

  // Aperture parameters
  apertureRadius;

  // Dot-specific attributes
  dot;
  shown;

  /**
   * Dot class constructor
   * @param {number} x the x coordinate of the dot
   * @param {number} y the y coordinate of the dot
   * @param {number} z the z coordinate of the dot
   * @param {any} parameters a parameters object containing properties
   */
  constructor(x, y, z, parameters) {
    // Positional coordinates
    this.x = x;
    this.y = y;
    this.z = z;

    // Unpack parameters
    this.type = parameters.type;
    this.velocity = parameters.velocity;
    this.radius = parameters.radius;
    this.direction = parameters.direction;
    this.apertureRadius = parameters.apertureRadius;

    // Dot is within the aperture bounds or not
    this.reset = false;
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

  getZ() {
    return this.z;
  }

  getRadius() {
    return this.radius;
  }

  getPosition() {
    return new Vector3(this.x, this.y, this.z);
  }

  setPosition(x, y, z) {
    this.dot.position.set(new Vector3(x, y, z));
  }

  /**
   * Get the type of dot
   * @return {string} the dot type
   */
  getType() {
    return this.type;
  }

  setObject(dot) {
    this.dot = dot;
  }

  getObject() {
    return this.dot;
  }

  /**
   * Generic step method that is called sixty times per second
   * @param {number} frameCount the number of elapsed frames
   */
  step(frameCount) {
    if (this.dot) {
      let x = this.x;
      let y = this.y;

      if (this.type === 'random' && frameCount % 6 === 0) {
        // Adjust the direction
        const delta = Math.random();
        if (delta > 0.5) {
          this.direction -= (Math.PI / 8) * delta;
        } else {
          this.direction += (Math.PI / 8) * delta;
        }
      }

      const inAperture =
        Math.sqrt(x ** 2 + y ** 2) < this.apertureRadius + this.radius / 2;

      // Check if the dot needs to be translated
      if (!inAperture && !this.reset) {
        if (this.type === 'reference') {
          // Translate the location of "reference"-type dots to remain synchronized
          x = x - 2 * this.apertureRadius * Math.cos(this.direction);
          y = y - 2 * this.apertureRadius * Math.sin(this.direction);
        } else if (this.type === 'random') {
          // Reset the direction of "random"-type dots to avoid disappearance
          this.direction -= Math.PI;
        }
        this.reset = true;
      } else {
        // Calculate the updated position
        x = x + this.velocity * Math.cos(this.direction);
        y = y + this.velocity * Math.sin(this.direction);
      }

      // Update the reset toggle if required
      if (this.dot.visible) this.reset = false;

      // Show the dot only when within the aperture
      this.dot.visible =
        Math.sqrt(x ** 2 + y ** 2) <= this.apertureRadius + this.radius / 2;

      // Apply the updated dot position
      this.x = x;
      this.y = y;
      this.dot.position.set(this.x, this.y, this.z);
    }
  }
}

export default Dot;
