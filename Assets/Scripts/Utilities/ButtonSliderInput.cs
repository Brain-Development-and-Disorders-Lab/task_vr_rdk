using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Utility class attached to the _buttonSlider prefab to manage slider behaviour
/// </summary>
public class ButtonSliderInput : MonoBehaviour
{
    public GameObject buttonBackground;
    public GameObject buttonFill;

    private GameObject _buttonSlider;
    private TextMeshProUGUI _buttonSliderText;
    private Slider _buttonSliderComponent;
    private bool _hasSetup = false;

    public void Setup()
    {
        // Get references to all required components
        _buttonSlider = gameObject;
        _buttonSliderComponent = _buttonSlider.GetComponent<Slider>();
        _buttonSliderText = _buttonSlider.GetComponentInChildren<TextMeshProUGUI>();
        _hasSetup = true;
    }

    /// <summary>
    /// Update the text displayed on the button
    /// </summary>
    /// <param name="buttonText">Button text</param>
    public void SetButtonText(string buttonText)
    {
        if (_hasSetup)
        {
            _buttonSliderText.text = buttonText;
        }
    }

    public float GetSliderValue()
    {
        if (_hasSetup)
        {
            return _buttonSliderComponent.value;
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
            _buttonSliderComponent.value = 0.0f;
        }
        else if (value > 1.0f)
        {
            // Value must be less than or equal to `1.0f`
            _buttonSliderComponent.value = 1.0f;
        }
        else
        {
            _buttonSliderComponent.value = value;
        }
    }

    public void SetBackgroundColor(Color color) => buttonBackground.GetComponentInChildren<Image>().color = color;

    public void SetFillColor(Color color) => buttonFill.GetComponentInChildren<Image>().color = color;
}
