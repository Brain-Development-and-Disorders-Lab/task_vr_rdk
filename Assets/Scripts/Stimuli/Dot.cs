using UnityEngine;
using System;

namespace Stimuli
{
    public class Dot
    {
        private readonly GameObject _dotAnchor;
        private readonly float _dotRadius;
        private readonly float _apertureWidth;
        private readonly float _apertureHeight;
        private string _dotBehavior;
        private float _dotX;
        private float _dotY;
        private float _dotDirection;
        private GameObject _dotObject;
        private SpriteRenderer _dotRenderer;

        // Visibility and activity state
        private bool _dotVisible;
        private bool _dotActive = true;

        public Dot(GameObject anchor, float radius, float apertureWidth, float apertureHeight, string behavior, float x = 0.0f, float y = 0.0f, bool visible = true)
        {
            _dotAnchor = anchor;
            _dotRadius = radius;
            _apertureWidth = apertureWidth;
            _apertureHeight = apertureHeight;
            _dotBehavior = behavior;
            _dotX = x;
            _dotY = y;
            _dotDirection = Mathf.PI / 2; // Default direction is up
            _dotVisible = visible;

            // Update direction depending on initial behaviour
            if (behavior == "random")
            {
                _dotDirection = (float)(2.0f * Math.PI * UnityEngine.Random.value);
            }

            CreateGameObject();
        }

        private GameObject CreateGameObject()
        {
            // Create base GameObject
            _dotObject = new GameObject("rdk_dot_object");
            _dotObject.transform.SetParent(_dotAnchor.transform, false);
            _dotObject.AddComponent<SpriteRenderer>();
            _dotObject.transform.localPosition = new Vector3(_dotX, _dotY, 0.0f);

            // Create SpriteRenderer
            _dotRenderer = _dotObject.GetComponent<SpriteRenderer>();
            _dotRenderer.drawMode = SpriteDrawMode.Sliced;
            _dotRenderer.sprite = Resources.Load<Sprite>("Sprites/Circle");
            _dotRenderer.size = new Vector2(_dotRadius * 2.0f, _dotRadius * 2.0f);
            _dotRenderer.enabled = _dotVisible;

            return _dotObject;
        }

        public GameObject GetGameObject() => _dotObject;

        public void SetActive(bool state)
        {
            _dotActive = state;
            _dotObject.SetActive(_dotActive);
        }

        public void SetVisible(bool state)
        {
            _dotVisible = state;
            _dotRenderer.enabled = _dotVisible;
        }

        public bool GetVisible() => _dotVisible;

        public void SetDirection(float direction) => _dotDirection = direction;

        public Vector2 GetPosition() => new(_dotX, _dotY);

        public void SetPosition(Vector2 position)
        {
            _dotX = position.x;
            _dotY = position.y;
        }

        public string GetBehavior() => _dotBehavior;

        public void SetBehavior(string behavior)
        {
            if (behavior is "random" or "reference")
            {
                _dotBehavior = behavior;
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
            float halfWidth = _apertureWidth / 2.0f;
            float halfHeight = _apertureHeight / 2.0f;

            float leftBound = -halfWidth;
            float rightBound = halfWidth;
            float topBound = halfHeight;
            float bottomBound = -halfHeight;

            return x >= leftBound && x <= rightBound && y >= bottomBound && y <= topBound;
        }

        public void Update()
        {
            if (_dotActive)
            {
                // Create and store positions
                var originalPosition = _dotObject.transform.localPosition;
                float updatedX = originalPosition.x;
                float updatedY = originalPosition.y;

                // Get and apply visibility state
                bool visibility = IsVisible(updatedX, updatedY);
                SetVisible(visibility);

                // Random direction adjustment every 12 frames
                if (_dotBehavior == "random" && Time.frameCount % 12 == 0)
                {
                    // Adjust the direction
                    float delta = UnityEngine.Random.value;
                    if (UnityEngine.Random.value > 0.5f)
                    {
                        _dotDirection -= Mathf.PI / 4 * delta;
                    }
                    else
                    {
                        _dotDirection += Mathf.PI / 4 * delta;
                    }
                }

                if (_dotBehavior == "reference")
                {
                    // Reset depending on which edge the dot reached
                    if (updatedY > _apertureHeight / 2)
                    {
                        updatedY -= _apertureHeight;
                    }
                    else if (updatedY < -_apertureHeight / 2)
                    {
                        updatedY += _apertureHeight;
                    }
                }
                else if (_dotBehavior == "random")
                {
                    // Reset depending on which edge the dot reached, adding a padding distance to ensure continued
                    // dot visibility
                    if (updatedY > _apertureHeight / 2)
                    {
                        updatedY -= _apertureHeight;
                        updatedY += _dotRadius * 2.0f;
                    }
                    else if (updatedY < -_apertureHeight / 2)
                    {
                        updatedY += _apertureHeight;
                        updatedY -= _dotRadius * 2.0f;
                    }
                    else if (updatedX > _apertureWidth / 2)
                    {
                        updatedX -= _apertureWidth;
                        updatedX += _dotRadius * 2.0f;
                    }
                    else if (updatedX < -_apertureWidth / 2)
                    {
                        updatedX += _apertureWidth;
                        updatedX -= _dotRadius * 2.0f;
                    }
                }

                // Update overall position
                updatedX += 0.01f * Mathf.Cos(_dotDirection);
                updatedY += 0.01f * Mathf.Sin(_dotDirection);

                _dotX = updatedX;
                _dotY = updatedY;

                // Apply transform
                _dotObject.transform.localPosition = new Vector3(updatedX, updatedY, originalPosition.z);
            }
        }
    }
}
