using UnityEngine;

namespace Calibration
{
  public struct CalibrationVector
  {
    public Vector2 LeftAdjustment;
    public Vector2 RightAdjustment;

    public CalibrationVector(Vector2 left, Vector2 right)
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
