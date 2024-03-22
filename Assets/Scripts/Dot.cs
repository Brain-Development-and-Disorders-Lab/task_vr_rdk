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
  private SpriteRenderer DotRenderer;

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
    DotRenderer = DotObject.GetComponent<SpriteRenderer>();
    DotRenderer.drawMode = SpriteDrawMode.Sliced;
    DotRenderer.sprite = Resources.Load<Sprite>("Sprites/Circle");
    DotRenderer.size = new Vector2(DotRadius * 2.0f, DotRadius * 2.0f);
    DotRenderer.enabled = DotVisible;

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

  public void SetVisible(bool state)
  {
    DotVisible = state;
    DotRenderer.enabled = DotVisible;
  }

  public void SetDirection(float direction)
  {
    DotDirection = direction;
  }

  public Vector2 GetPosition()
  {
    return new Vector2(DotX, DotY);
  }

  public void SetPosition(Vector2 position)
  {
    DotX = position.x;
    DotY = position.y;
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

      // Random direction adjustment every 12 frames
      if (DotBehavior == "random" & Time.frameCount % 12 == 0) {
        // Adjust the direction
        float delta = UnityEngine.Random.value;
        if (UnityEngine.Random.value > 0.5f) {
          DotDirection -= Mathf.PI / 4 * delta;
        } else {
          DotDirection += Mathf.PI / 4 * delta;
        }
      }

      // Behaviour around aperture radius
      if (DotBehavior == "reference" & Mathf.Abs(updatedX) > ApertureRadius)
      {
        // Reset position
        updatedX -= 2.0f * ApertureRadius * Mathf.Cos(DotDirection);
        updatedY -= 2.0f * ApertureRadius * Mathf.Sin(DotDirection);
      }
      else if (DotBehavior == "random" & !visibility)
      {
        // Oppose current position, "bounce" effect
        DotDirection -= Mathf.PI;
      }

      // Update overall position
      updatedX += 0.01f * Mathf.Cos(DotDirection);
      updatedY += 0.01f * Mathf.Sin(DotDirection);

      DotX = updatedX;
      DotY = updatedY;

      // Apply transform
      DotObject.transform.localPosition = new Vector3(updatedX, updatedY, originalPosition.z);
    }
  }
}
