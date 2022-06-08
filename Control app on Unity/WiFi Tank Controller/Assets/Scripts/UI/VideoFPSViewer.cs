using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VideoFPSViewer : MonoBehaviour
{
    public Text fpsText;

    private void Awake()
    {
        if (fpsText == null)
            Debug.Log("FPS text is null!");

        VideoReceiver.OnVideoFPSChanged.AddListener(VideoFPSChanged);
    }

    private void VideoFPSChanged(int fps)
    {
        fpsText.text = "FPS: " + fps.ToString();
    }
}
