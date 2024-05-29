using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Stimuli
{
    public class UIManager : MonoBehaviour
    {
        public Canvas UICanvas;
        public float ScalingFactor = 0.1f; // Adjust scaling of stimulus to be viewable
        public float VerticalOffset = -2.0f; // Adjust the vertical positioning of the UI

        // Text UI components
        private GameObject headerContainer;
        private string headerText = "";
        private TextMeshProUGUI headerTextComponent;
        private GameObject bodyContainer;
        private string bodyText = "";
        private TextMeshProUGUI bodyTextComponent;

        // Button UI components
        private GameObject leftButton;
        private GameObject rightButton;

        // Page UI components
        private bool usePagination;
        private List<string> pageContent;
        private int activePage = 0;

        void Start()
        {
            // Run setup functions to create and position UI components
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

            // Update the UICanvas positioning to incorporate a vertical offset
            UICanvas.transform.position = new Vector3(0.0f, 10.0f * VerticalOffset, 200.0f);

            // Create GameObject for header
            headerContainer = new GameObject();
            headerContainer.name = "rdk_text_header_container";
            headerContainer.AddComponent<TextMeshProUGUI>();
            headerContainer.transform.SetParent(UICanvas.transform, false);
            headerContainer.SetActive(true);
            headerContainer.transform.localScale = new Vector3(ScalingFactor, ScalingFactor, ScalingFactor);

            // Header component (10%, top)
            headerTextComponent = headerContainer.GetComponent<TextMeshProUGUI>();
            headerTextComponent.text = headerText;
            headerTextComponent.fontStyle = FontStyles.Bold;
            headerTextComponent.fontSize = 8.0f;
            headerTextComponent.material.color = Color.white;
            headerTextComponent.alignment = TextAlignmentOptions.Center;
            headerTextComponent.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            headerTextComponent.transform.localPosition = new Vector3(0.0f, 40.0f, 0.0f);
            headerTextComponent.rectTransform.sizeDelta = new Vector2(180.0f, 14.0f);

            // Create GameObject for body
            bodyContainer = new GameObject();
            bodyContainer.name = "rdk_text_body_object";
            bodyContainer.AddComponent<TextMeshProUGUI>();
            bodyContainer.transform.SetParent(UICanvas.transform, false);
            bodyContainer.SetActive(true);
            bodyContainer.transform.localScale = new Vector3(ScalingFactor, ScalingFactor, ScalingFactor);

            // Body component (80%, below header)
            bodyTextComponent = bodyContainer.GetComponent<TextMeshProUGUI>();
            bodyTextComponent.text = bodyText;
            bodyTextComponent.fontSize = 6.0f;
            bodyTextComponent.material.color = Color.white;
            bodyTextComponent.alignment = TextAlignmentOptions.Center;
            bodyTextComponent.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            bodyTextComponent.transform.localPosition = new Vector3(0.0f, -5.0f, 0.0f);
            bodyTextComponent.rectTransform.sizeDelta = new Vector2(160.0f, 100.0f);

            // Button components (10%, below body)
            GameObject buttonBodyObject = new GameObject();
            buttonBodyObject.name = "rdk_button_body_object";
            buttonBodyObject.transform.SetParent(UICanvas.transform, false);
            buttonBodyObject.transform.localPosition = new Vector3(0.0f, -60.0f, 0.0f);

            TMP_DefaultControls.Resources ButtonResources = new TMP_DefaultControls.Resources();

            // Left button, typically "back" action
            leftButton = TMP_DefaultControls.CreateButton(ButtonResources);
            leftButton.transform.SetParent(buttonBodyObject.transform, false);
            leftButton.transform.localPosition = new Vector3(-42.5f, 0.0f, 0.0f);
            leftButton.GetComponent<RectTransform>().sizeDelta = new Vector2(28.0f, 14.0f);
            leftButton.GetComponent<Image>().sprite = Resources.Load<Sprite>("Sprites/Button");
            TextMeshProUGUI LButtonText = leftButton.GetComponentInChildren<TextMeshProUGUI>();
            LButtonText.fontStyle = FontStyles.Bold;
            LButtonText.fontSize = 5.0f;
            LButtonText.text = "Back";

            // Right button, typically "next" action
            rightButton = TMP_DefaultControls.CreateButton(ButtonResources);
            rightButton.transform.SetParent(buttonBodyObject.transform, false);
            rightButton.transform.localPosition = new Vector3(42.5f, 0.0f, 0.0f);
            rightButton.GetComponent<RectTransform>().sizeDelta = new Vector2(28.0f, 14.0f);
            rightButton.GetComponent<Image>().sprite = Resources.Load<Sprite>("Sprites/Button");
            TextMeshProUGUI RButtonText = rightButton.GetComponentInChildren<TextMeshProUGUI>();
            RButtonText.fontStyle = FontStyles.Bold;
            RButtonText.fontSize = 5.0f;
            RButtonText.text = "Next";
        }

        public void SetHeaderText(string text)
        {
            headerText = text;
            headerTextComponent.text = headerText;
        }

        public void SetBodyText(string text)
        {
            bodyText = text;
            bodyTextComponent.text = bodyText;
        }

        public void EnablePagination(bool state)
        {
            usePagination = state;
        }

        public int GetCurrentActivePage()
        {
            return activePage;
        }

        public void SetPages(List<string> pages)
        {
            pageContent = pages;
            activePage = 0; // Reset the active page index
            SetPage(activePage);
        }

        public void SetPage(int pageIndex)
        {
            if (pageIndex >= 0 && pageIndex < pageContent.Count)
            {
                activePage = pageIndex;
                SetBodyText(pageContent[activePage]);

                // Update the button state
                leftButton.GetComponent<Button>().interactable = HasPreviousPage();
                rightButton.GetComponent<Button>().interactable = HasNextPage();
            }
            else
            {
                Debug.LogWarning("Invalid page index specified");
            }
        }

        public bool HasNextPage()
        {
            return usePagination && activePage + 1 < pageContent.Count;
        }

        public void NextPage()
        {
            if (HasNextPage())
            {
                SetPage(activePage + 1);
            }
        }

        public bool HasPreviousPage()
        {
            return usePagination && activePage - 1 >= 0;
        }

        public void PreviousPage()
        {
            if (HasPreviousPage())
            {
                SetPage(activePage - 1);
            }
        }

        public void SetLeftButtonState(bool enabled, bool visible = true, string text = "Back")
        {
            leftButton.GetComponentInChildren<TextMeshProUGUI>().text = text;
            leftButton.GetComponent<Button>().interactable = enabled;
            leftButton.SetActive(visible);
        }

        public void SetRightButtonState(bool enabled, bool visible = true, string text = "Next")
        {
            rightButton.GetComponentInChildren<TextMeshProUGUI>().text = text;
            rightButton.GetComponent<Button>().interactable = enabled;
            rightButton.SetActive(visible);
        }

        public void ClickLeftButton()
        {
            ExecuteEvents.Execute(leftButton, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        }

        public void ClickRightButton()
        {
            ExecuteEvents.Execute(rightButton, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        }

        public void SetVisible(bool state)
        {
            // Text components
            headerContainer.SetActive(state);
            bodyContainer.SetActive(state);

            // Button components
            leftButton.SetActive(state);
            rightButton.SetActive(state);
        }
    }
}
