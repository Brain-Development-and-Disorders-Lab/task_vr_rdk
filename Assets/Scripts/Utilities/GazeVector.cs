using UnityEngine;

namespace Utilities
{
    /// <summary>
    /// Struct to generalize a pair of vector components representing the left and right eye gaze estimates.
    /// </summary>
    public struct GazeVector
    {
        public Vector2 leftAdjustment;
        public Vector2 rightAdjustment;

        public GazeVector(Vector2 left, Vector2 right)
        {
            leftAdjustment = left;
            rightAdjustment = right;
        }

        public void SetAdjustments(Vector2 left, Vector2 right)
        {
            leftAdjustment = left;
            rightAdjustment = right;
        }

        public readonly Vector2 GetLeft() => leftAdjustment;

        public readonly Vector2 GetRight() => rightAdjustment;
    }
}
