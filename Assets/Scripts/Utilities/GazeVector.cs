using UnityEngine;

namespace Utilities
{
    /// <summary>
    /// Struct to generalize a pair of vector components representing the left and right eye gaze estimates.
    /// </summary>
    public struct GazeVector
    {
        public Vector3 left;
        public Vector3 right;

        public GazeVector(Vector3 left, Vector3 right)
        {
            this.left = left;
            this.right = right;
        }

        public void SetAdjustments(Vector3 left, Vector3 right)
        {
            this.left = left;
            this.right = right;
        }

        public readonly Vector3 GetLeft() => left;

        public readonly Vector3 GetRight() => right;
    }
}
