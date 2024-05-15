using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Utility class attached to the ButtonSlider prefab to manage slider behaviour
/// </summary>
public class ButtonSliderInput : MonoBehaviour
{
    GameObject ButtonSliderParent;
    TextMeshProUGUI ButtonSliderText;
    Slider ButtonSliderComponent;

    void Start()
    {
        // Get references to all required components
        ButtonSliderParent = gameObject;
        ButtonSliderText = ButtonSliderParent.GetComponentInChildren<TextMeshProUGUI>();
        ButtonSliderComponent = ButtonSliderParent.GetComponent<Slider>();
    }

    /// <summary>
    /// Update the text displayed on the button
    /// </summary>
    /// <param name="buttonText">Button text</param>
    public void SetButtonText(string buttonText)
    {
        ButtonSliderText.text = buttonText;
    }

    public float GetSliderValue()
    {
        return ButtonSliderComponent.value;
    }

    /// <summary>
    /// Update the value of the slider, displayed as a fill
    /// </summary>
    /// <param name="value">[0.0, 1.0]</param>
    public void SetSliderValue(float value)
    {
        if (value >= 0.0f && value <= 1.0f)
        {
            ButtonSliderComponent.value = value;
        }
    }
}
