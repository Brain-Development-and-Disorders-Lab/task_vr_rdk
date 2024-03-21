using UnityEngine;
using System;

public class Dot
{
  private GameObject DotAnchor;
  private float DotRadius;
  private float ApertureRadius;
  private string DotBehavior;
  private float DotX;
  private float DotY;
  private float DotDirection;
  private GameObject DotObject;

  // Visibility and activity state
  private bool DotVisible;
  private bool DotActive = true;

  public Dot(GameObject anchor, float radius, float aperture, string behavior, float x = 0.0f, float y = 0.0f, bool visible = true)
  {
    DotAnchor = anchor;
    DotRadius = radius;
    ApertureRadius = aperture;
    DotBehavior = behavior;
    DotX = x;
    DotY = y;
    DotDirection = Mathf.PI;
    DotVisible = visible;

    CreateGameObject();
  }

  private GameObject CreateGameObject()
  {
    // Create base GameObject
    DotObject = new GameObject();
    DotObject.name = "rdk_dot_object";
    DotObject.transform.SetParent(DotAnchor.transform, false);
    DotObject.AddComponent<SpriteRenderer>();
    DotObject.transform.localPosition = new Vector3(DotX, DotY, 0.0f);

    // Create SpriteRenderer
    SpriteRenderer dotRenderer = DotObject.GetComponent<SpriteRenderer>();
    dotRenderer.drawMode = SpriteDrawMode.Sliced;
    dotRenderer.sprite = Resources.Load<Sprite>("Sprites/Circle");
    dotRenderer.size = new Vector2(DotRadius * 2.0f, DotRadius * 2.0f);
    dotRenderer.enabled = DotVisible;

    return DotObject;
  }

  public GameObject GetGameObject()
  {
    return DotObject;
  }

  public void SetActive(bool state)
  {
    DotActive = state;
    DotObject.SetActive(DotActive);
  }

  public void Update()
  {
    if (DotActive == true)
    {
      // Create and store positions
      Vector3 originalPosition = DotObject.transform.localPosition;
      float updatedX = originalPosition.x;
      float updatedY = originalPosition.y;

      // Update visibility
      bool visibility = Mathf.Sqrt(Mathf.Pow(updatedX, 2.0f) + Mathf.Pow(updatedY, 2.0f)) <= ApertureRadius;
      DotObject.GetComponent<SpriteRenderer>().enabled = visibility;

      if (DotBehavior == "reference")
      {
        if (Mathf.Abs(updatedX) > ApertureRadius)
        {
          updatedX -= 2.0f * ApertureRadius * Mathf.Cos(DotDirection);
          updatedY -= 2.0f * ApertureRadius * Mathf.Sin(DotDirection);
        }
      }
      else if (DotBehavior == "random" & visibility)
      {
        DotDirection -= Mathf.PI;
      }

      updatedX += 0.01f * Mathf.Cos(DotDirection);
      updatedY += 0.01f * Mathf.Sin(DotDirection);

      DotObject.transform.localPosition = new Vector3(updatedX, updatedY, originalPosition.z);
    }
  }
}
