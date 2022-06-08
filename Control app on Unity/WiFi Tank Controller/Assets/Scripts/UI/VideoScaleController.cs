using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VideoScaleController : MonoBehaviour
{
    public GameObject frameGO;
    public Slider slider;

    private void Awake()
    {
        VideoScale(slider.value);
        slider.onValueChanged.AddListener(VideoScale);
    }

    private void VideoScale(float scale)
    {
        float scaleNormalized = scale / slider.maxValue;
        Vector3 scaleVector = new Vector3(scaleNormalized, scaleNormalized, 1);
        frameGO.transform.localScale = scaleVector;
    }
}
