using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
  [SerializeField]
  private Canvas UICanvas;
  [SerializeField]
  private float ScalingFactor = 0.1f; // Adjust scaling of stimulus to be viewable

  // Calculated dimensions of stimuli
  private float StimulusDistance;

  // Text UI components
  private GameObject HeaderContainer;
  private string HeaderText = "";
  private TextMeshProUGUI HeaderTextComponent;
  private GameObject BodyContainer;
  private string BodyText = "";
  private TextMeshProUGUI BodyTextComponent;

  // Button UI components
  private GameObject LButton;
  private GameObject RButton;

  // Page UI components
  private bool UsePagination;
  private List<string> PageContent;
  private int ActivePage = 0;

  void Start()
  {
    // Run setup functions to create and position UI components
    StimulusDistance = Mathf.Abs(transform.position.z - UICanvas.transform.position.z);
    SetupUI();
    SetVisible(false);
  }

  public void SetupUI()
  {
    if (UICanvas == null)
    {
      Debug.LogError("No UICanvas specified. UI will not appear!");
      SetVisible(false);
      return;
    }

    // Create GameObject for header
    HeaderContainer = new GameObject();
    HeaderContainer.name = "rdk_text_header_container";
    HeaderContainer.AddComponent<TextMeshProUGUI>();
    HeaderContainer.transform.SetParent(UICanvas.transform, false);
    HeaderContainer.SetActive(true);
    HeaderContainer.transform.localScale = new Vector3(ScalingFactor, ScalingFactor, ScalingFactor);

    // Header component (10%, top)
    HeaderTextComponent = HeaderContainer.GetComponent<TextMeshProUGUI>();
    HeaderTextComponent.text = HeaderText;
    HeaderTextComponent.fontStyle = FontStyles.Bold;
    HeaderTextComponent.fontSize = 7.5f;
    HeaderTextComponent.material.color = Color.white;
    HeaderTextComponent.alignment = TextAlignmentOptions.Center;
    HeaderTextComponent.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
    HeaderTextComponent.transform.localPosition = new Vector3(0.0f, 40.0f, 0.0f);
    HeaderTextComponent.rectTransform.sizeDelta = new Vector2(140.0f, 10.0f);

    // Create GameObject for body
    BodyContainer = new GameObject();
    BodyContainer.name = "rdk_text_body_object";
    BodyContainer.AddComponent<TextMeshProUGUI>();
    BodyContainer.transform.SetParent(UICanvas.transform, false);
    BodyContainer.SetActive(true);
    BodyContainer.transform.localScale = new Vector3(ScalingFactor, ScalingFactor, ScalingFactor);

    // Body component (80%, below header)
    BodyTextComponent = BodyContainer.GetComponent<TextMeshProUGUI>();
    BodyTextComponent.text = BodyText;
    BodyTextComponent.fontSize = 4.5f;
    BodyTextComponent.material.color = Color.white;
    BodyTextComponent.alignment = TextAlignmentOptions.TopJustified;
    BodyTextComponent.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
    BodyTextComponent.transform.localPosition = new Vector3(0.0f, -5.0f, 0.0f);
    BodyTextComponent.rectTransform.sizeDelta = new Vector2(120.0f, 80.0f);

    // Button components (10%, below body)
    GameObject buttonBodyObject = new GameObject();
    buttonBodyObject.name = "rdk_button_body_object";
    buttonBodyObject.transform.SetParent(UICanvas.transform, false);
    buttonBodyObject.transform.localPosition = new Vector3(0.0f, -50.0f, 0.0f);

    TMP_DefaultControls.Resources ButtonResources = new TMP_DefaultControls.Resources();

    // Left button, typically "back" action
    LButton = TMP_DefaultControls.CreateButton(ButtonResources);
    LButton.transform.SetParent(buttonBodyObject.transform, false);
    LButton.transform.localPosition = new Vector3(-42.5f, 0.0f, 0.0f);
    LButton.GetComponent<RectTransform>().sizeDelta = new Vector2(24.0f, 12.0f);
    TextMeshProUGUI LButtonText = LButton.GetComponentInChildren<TextMeshProUGUI>();
    LButtonText.fontStyle = FontStyles.Bold;
    LButtonText.fontSize = 4.0f;
    LButtonText.text = "Back";

    // Right button, typically "next" action
    RButton = TMP_DefaultControls.CreateButton(ButtonResources);
    RButton.transform.SetParent(buttonBodyObject.transform, false);
    RButton.transform.localPosition = new Vector3(40.0f, 0.0f, 0.0f);
    RButton.GetComponent<RectTransform>().sizeDelta = new Vector2(24.0f, 12.0f);
    TextMeshProUGUI RButtonText = RButton.GetComponentInChildren<TextMeshProUGUI>();
    RButtonText.fontStyle = FontStyles.Bold;
    RButtonText.fontSize = 4.0f;
    RButtonText.text = "Next";
  }

  public void SetHeader(string headerText)
  {
    HeaderText = headerText;
    HeaderTextComponent.text = HeaderText;
  }

  public void SetBody(string bodyText)
  {
    BodyText = bodyText;
    BodyTextComponent.text = BodyText;
  }

  public void EnablePagination(bool state)
  {
    UsePagination = state;
  }

  public int GetCurrentActivePage()
  {
    return ActivePage;
  }

  public void SetPages(List<string> pageContent)
  {
    PageContent = pageContent;
    SetPage(ActivePage);
  }

  public void SetPage(int pageIndex)
  {
    if (UsePagination && pageIndex >= 0 && pageIndex < PageContent.Count)
    {
      ActivePage = pageIndex;
      SetBody(PageContent[ActivePage]);

      // Update the button state
      LButton.GetComponent<Button>().interactable = HasPreviousPage();
      RButton.GetComponent<Button>().interactable = HasNextPage();
    }
    else
    {
      Debug.LogWarning("Pagination not enabled or invalid page index specified");
    }
  }

  public bool HasNextPage()
  {
    return UsePagination && ActivePage + 1 < PageContent.Count;
  }

  public void NextPage()
  {
    if (HasNextPage())
    {
      SetPage(ActivePage + 1);
    }
  }

  public bool HasPreviousPage()
  {
    return UsePagination && ActivePage - 1 >= 0;
  }

  public void PreviousPage()
  {
    if (HasPreviousPage())
    {
      SetPage(ActivePage - 1);
    }
  }

  public void SetLeftButton(bool enabled, bool visible = true, string text = "Back")
  {
    LButton.GetComponentInChildren<TextMeshProUGUI>().text = text;
    LButton.GetComponent<Button>().interactable = enabled;
    LButton.SetActive(visible);
  }

  public void SetRightButton(bool enabled, bool visible = true, string text = "Next")
  {
    RButton.GetComponentInChildren<TextMeshProUGUI>().text = text;
    RButton.GetComponent<Button>().interactable = enabled;
    RButton.SetActive(visible);
  }

  public void SetVisible(bool state)
  {
    // Text components
    HeaderContainer.SetActive(state);
    BodyContainer.SetActive(state);

    // Button components
    LButton.SetActive(state);
    RButton.SetActive(state);
  }
}
