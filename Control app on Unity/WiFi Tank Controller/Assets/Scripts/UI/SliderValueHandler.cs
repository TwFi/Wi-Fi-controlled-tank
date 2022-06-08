using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderValueHandler : MonoBehaviour
{
    public Text valueText;
    public Slider slider;
    public float multiplayer;
    public string postfix;

    public void Awake()
    {
        SliderValueChanged(slider.value);
        slider.onValueChanged.AddListener(SliderValueChanged);
    }

    private void SliderValueChanged(float value)
    {
        valueText.text = (value * multiplayer).ToString() + postfix;
    }
}
