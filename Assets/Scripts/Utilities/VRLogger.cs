using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Utilities
{
    public class VRLogger : MonoBehaviour
    {
        [SerializeField]
        private GameObject logCanvas;

        [SerializeField]
        private Font font;

        [SerializeField]
        private bool showLogger = true;

        // Data collections
        private List<string> messages;
        private List<GameObject> messageContainers;

        public int MessageLimit = 20; // Set the number of message rows displayed
        public bool UseHeadTracking = true; // Fix the logger to headset head tracking position

        private void CreateLoggerRows()
        {
            // Iterate over the list of messages
            for (int i = 0; i < MessageLimit; i++)
            {
                GameObject textContainer = new GameObject("logger_message_" + i.ToString());
                textContainer.transform.position = new Vector3(150.0f, 0.0f - 15.0f * i, 0.0f);
                textContainer.layer = 5; // UI layer
                textContainer.transform.SetParent(logCanvas.transform, false);

                Text text = textContainer.AddComponent<Text>();
                text.font = font;
                text.color = Color.green;
                text.alignment = TextAnchor.MiddleLeft;
                text.fontSize = 8;
                RectTransform textTransform = textContainer.GetComponent<RectTransform>();
                textTransform.sizeDelta = new Vector2(300.0f, 15.0f);
                textTransform.anchorMin = new Vector2(0, 1);
                textTransform.anchorMax = new Vector2(0, 1);

                messageContainers.Add(textContainer);
            }
        }

        private void RenderMessages()
        {
            for (int i = 0; i < messages.Count; i++)
            {
                GameObject textContainer = messageContainers[i];
                Text messageText = textContainer.GetComponent<Text>();
                messageText.text = DateTime.Now.ToString("T") + ": " + messages[i];

                // Toggle visibility
                textContainer.SetActive(showLogger);
            }
        }

        void Start()
        {
            // Create new arrays
            messages = new List<string>();
            messageContainers = new List<GameObject>();

            // Instatiate the rows
            CreateLoggerRows();

            // Fix to head movement if in VR context
            if (UseHeadTracking)
            {
                OVRCameraRig cameraRig = FindObjectOfType<OVRCameraRig>();
                if (cameraRig)
                {
                    logCanvas.transform.SetParent(cameraRig.centerEyeAnchor.transform, false);
                }
            }
        }

        void Update()
        {
            RenderMessages();
        }

        public void Log(string message)
        {
            messages.Add(message);
            if (messages.Count > MessageLimit)
            {
                messages.RemoveAt(0);
                messages.TrimExcess();
            }
        }
    }
}
