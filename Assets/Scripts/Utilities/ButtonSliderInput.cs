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
    GameObject ButtonSlider;
    TextMeshProUGUI ButtonSliderText;
    Slider ButtonSliderComponent;
    bool HasSetup = false;

    public void Setup()
    {
        // Get references to all required components
        ButtonSlider = gameObject;
        ButtonSliderComponent = ButtonSlider.GetComponent<Slider>();
        ButtonSliderText = ButtonSlider.GetComponentInChildren<TextMeshProUGUI>();
        HasSetup = true;
    }

    /// <summary>
    /// Update the text displayed on the button
    /// </summary>
    /// <param name="buttonText">Button text</param>
    public void SetButtonText(string buttonText)
    {
        if (HasSetup)
        {
            ButtonSliderText.text = buttonText;
        }
    }

    public float GetSliderValue()
    {
        if (HasSetup)
        {
            return ButtonSliderComponent.value;
        }
        return 0.0f;
    }

    /// <summary>
    /// Update the value of the slider, displayed as a fill
    /// </summary>
    /// <param name="value">[0.0, 1.0]</param>
    public void SetSliderValue(float value)
    {
        if (HasSetup && value >= 0.0f && value <= 1.0f)
        {
            ButtonSliderComponent.value = value;
        }
    }
}
