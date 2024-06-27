using UnityEngine;
using System;

namespace Stimuli
{
    public class Dot
    {
        private GameObject DotAnchor;
        private float DotRadius;
        private float ApertureWidth;
        private float ApertureHeight;
        private string DotBehavior;
        private float DotX;
        private float DotY;
        private float DotDirection;
        private GameObject DotObject;
        private SpriteRenderer DotRenderer;

        // Visibility and activity state
        private bool DotVisible;
        private bool DotActive = true;

        public Dot(GameObject anchor, float radius, float apertureWidth, float apertureHeight, string behavior, float x = 0.0f, float y = 0.0f, bool visible = true)
        {
            DotAnchor = anchor;
            DotRadius = radius;
            ApertureWidth = apertureWidth;
            ApertureHeight = apertureHeight;
            DotBehavior = behavior;
            DotX = x;
            DotY = y;
            DotDirection = Mathf.PI / 2; // Default direction is up
            DotVisible = visible;

            // Update direction depending on initial behaviour
            if (behavior == "random")
            {
                DotDirection = (float)(2.0f * Math.PI * UnityEngine.Random.value);
            }

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

        public bool GetVisible()
        {
            return DotVisible;
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

        public string GetBehavior()
        {
            return DotBehavior;
        }

        public void SetBehavior(string behavior)
        {
            if (behavior == "random" || behavior == "reference")
            {
                DotBehavior = behavior;
            }
            else
            {
                Debug.LogWarning("Invalid dot behavior: " + behavior);
            }
        }

        /// <summary>
        /// Utility function to check the visibility of a dot with specific coordinates
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <returns></returns>
        private bool IsVisible(float x, float y)
        {
            float halfWidth = ApertureWidth / 2.0f;
            float halfHeight = ApertureHeight / 2.0f;

            float leftBound = -halfWidth;
            float rightBound = halfWidth;
            float topBound = halfHeight;
            float bottomBound = -halfHeight;

            return x >= leftBound && x <= rightBound && y >= bottomBound && y <= topBound;
        }

        public void Update()
        {
            if (DotActive == true)
            {
                // Create and store positions
                Vector3 originalPosition = DotObject.transform.localPosition;
                float updatedX = originalPosition.x;
                float updatedY = originalPosition.y;

                // Get and apply visibility state
                bool visibility = IsVisible(updatedX, updatedY);
                SetVisible(visibility);

                // Random direction adjustment every 12 frames
                if (DotBehavior == "random" && Time.frameCount % 12 == 0)
                {
                    // Adjust the direction
                    float delta = UnityEngine.Random.value;
                    if (UnityEngine.Random.value > 0.5f)
                    {
                        DotDirection -= Mathf.PI / 4 * delta;
                    }
                    else
                    {
                        DotDirection += Mathf.PI / 4 * delta;
                    }
                }

                if (DotBehavior == "reference")
                {
                    // Reset depending on which edge the dot reached
                    if (updatedY > ApertureHeight / 2)
                    {
                        updatedY -= ApertureHeight;
                    }
                    else if (updatedY < -ApertureHeight / 2)
                    {
                        updatedY += ApertureHeight;
                    }
                }
                else if (DotBehavior == "random")
                {
                    // Reset depending on which edge the dot reached, adding a padding distance to ensure continued
                    // dot visibility
                    if (updatedY > ApertureHeight / 2)
                    {
                        updatedY -= ApertureHeight;
                        updatedY += DotRadius * 2.0f;
                    }
                    else if (updatedY < -ApertureHeight / 2)
                    {
                        updatedY += ApertureHeight;
                        updatedY -= DotRadius * 2.0f;
                    }
                    else if (updatedX > ApertureWidth / 2)
                    {
                        updatedX -= ApertureWidth;
                        updatedX += DotRadius * 2.0f;
                    }
                    else if (updatedX < -ApertureWidth / 2)
                    {
                        updatedX += ApertureWidth;
                        updatedX -= DotRadius * 2.0f;
                    }
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
}
