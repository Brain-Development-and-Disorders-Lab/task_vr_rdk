using UnityEngine;

namespace ExperimentUtilities
{
    /// <summary>
    /// Struct to generalize a pair of vector components representing the left and right eye gaze estimates.
    /// </summary>
    public struct GazeVector
    {
        public Vector2 LeftAdjustment;
        public Vector2 RightAdjustment;

        public GazeVector(Vector2 left, Vector2 right)
        {
            LeftAdjustment = left;
            RightAdjustment = right;
        }

        public void SetAdjustments(Vector2 left, Vector2 right)
        {
            LeftAdjustment = left;
            RightAdjustment = right;
        }

        public Vector2 GetLeft()
        {
            return LeftAdjustment;
        }

        public Vector2 GetRight()
        {
            return RightAdjustment;
        }
    }
}
