using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoggerManager : MonoBehaviour
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

    // Set the number of message rows displayed
    private int DefaultMessageLimit = 20;

    private void CreateLoggerRows()
    {
        // Iterate over the list of messages
        for (int i = 0; i < DefaultMessageLimit + 1; i++)
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

            if (i == 0)
            {
                text.text = "Logger Output:";
            }

            messageContainers.Add(textContainer);
        }
    }

    private void RenderMessages()
    {
        for (int i = 0; i < messages.Count; i++)
        {
            GameObject textContainer = messageContainers[i + 1];
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
    }

    void Update()
    {
        RenderMessages();
    }

    public void Log(string message)
    {
        messages.Add(message);
        if (messages.Count > DefaultMessageLimit)
        {
            messages.RemoveAt(0);
            messages.TrimExcess();
        }
    }
}
