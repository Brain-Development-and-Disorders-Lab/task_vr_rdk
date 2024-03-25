using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class UIManager : MonoBehaviour
{
  [SerializeField]
  private Canvas TextCanvas;
  [SerializeField]
  private float ScalingFactor = 0.1f; // Adjust scaling of stimulus to be viewable

  // Calculated dimensions of stimuli
  private float StimulusDistance;

  // Text UI components
  private string HeaderText = "";
  private TextMeshProUGUI HeaderComponent;
  private string BodyText = "";
  private TextMeshProUGUI BodyComponent;

  void Start()
  {
    StimulusDistance = Mathf.Abs(transform.position.z - TextCanvas.transform.position.z);
    SetupUI();
  }

  public void SetupUI()
  {
    // Create GameObject for header
    GameObject textHeaderObject = new GameObject();
    textHeaderObject.name = "rdk_text_header_object";
    textHeaderObject.AddComponent<TextMeshProUGUI>();
    textHeaderObject.transform.SetParent(TextCanvas.transform, false);
    textHeaderObject.SetActive(true);
    textHeaderObject.transform.localScale = new Vector3(ScalingFactor, ScalingFactor, ScalingFactor);

    // Header component (20%, top)
    HeaderComponent = textHeaderObject.GetComponent<TextMeshProUGUI>();
    HeaderComponent.text = HeaderText;
    HeaderComponent.fontStyle = FontStyles.Bold;
    HeaderComponent.fontSize = 10;
    HeaderComponent.material.color = Color.white;
    HeaderComponent.alignment = TextAlignmentOptions.Center;
    HeaderComponent.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
    HeaderComponent.transform.localPosition = new Vector3(0.0f, 40.0f, 0.0f);
    HeaderComponent.rectTransform.sizeDelta = new Vector2(140.0f, 20.0f);

    // Create GameObject for body
    GameObject textBodyObject = new GameObject();
    textBodyObject.name = "rdk_text_body_object";
    textBodyObject.AddComponent<TextMeshProUGUI>();
    textBodyObject.transform.SetParent(TextCanvas.transform, false);
    textBodyObject.SetActive(true);
    textBodyObject.transform.localScale = new Vector3(ScalingFactor, ScalingFactor, ScalingFactor);

    // Body component (80%, below header)
    BodyComponent = textBodyObject.GetComponent<TextMeshProUGUI>();
    BodyComponent.text = BodyText;
    BodyComponent.fontSize = 7.5f;
    BodyComponent.material.color = Color.white;
    BodyComponent.alignment = TextAlignmentOptions.TopJustified;
    BodyComponent.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
    BodyComponent.transform.localPosition = new Vector3(0.0f, -10.0f, 0.0f);
    BodyComponent.rectTransform.sizeDelta = new Vector2(120.0f, 80.0f);
  }

  public void SetHeader(string headerText)
  {
    HeaderText = headerText;
    HeaderComponent.text = HeaderText;
  }

  public void SetBody(string bodyText)
  {
    BodyText = bodyText;
    BodyComponent.text = BodyText;
  }

  public void SetVisible(bool state)
  {

  }
}
