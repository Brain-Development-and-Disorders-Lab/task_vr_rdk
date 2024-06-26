using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Utility class attached to the ButtonSlider prefab to manage slider behaviour
/// </summary>
public class ButtonSliderInput : MonoBehaviour
{
    public GameObject ButtonBackground;
    public GameObject ButtonFill;

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
        if (value < 0.0f)
        {
            // Value must be positive
            ButtonSliderComponent.value = 0.0f;
        }
        else if (value > 1.0f)
        {
            // Value must be less than or equal to `1.0f`
            ButtonSliderComponent.value = 1.0f;
        }
        else
        {
            ButtonSliderComponent.value = value;
        }
    }

    public void SetBackgroundColor(Color color)
    {
        ButtonBackground.GetComponentInChildren<Image>().color = color;
    }

    public void SetFillColor(Color color)
    {
        ButtonFill.GetComponentInChildren<Image>().color = color;
    }
}
