using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public static UnityEvent<CameraSettings, bool> OnVideoSettingsChanged = new UnityEvent<CameraSettings, bool>();
    public static UnityEvent<NanoSettings> OnNanoSettingsChanged = new UnityEvent<NanoSettings>();
    public static UnityEvent<SettingsManager> OnReadVideoSettings = new UnityEvent<SettingsManager>();
    public static UnityEvent OnReadNanoSettings = new UnityEvent();

    public static int sliderVideoResolutionOffset = 6;
    private const int FPSThresholdMin = 8;
    private const int FPSThresholdMax = 25;
    private const int qualityMax = 63;
    private const int qualityMaxForIntermediateResolution = 45;
    private const int qualityMin = 16;
    private const int qualityStep = 4;
    private const int resolutionMax = 2;
    private const int resolutionMin = 0;

    public Slider videoResolutionSlider;
    public Slider videoQualitySlider;
    public Slider videoGainSlider;
    public Toggle videoAutoSetToggle;
    public Toggle protectionOverloadToggle;

    static CameraSettings camSettings = new CameraSettings();
    static NanoSettings nanoSettings = new NanoSettings();
    bool videoAutoSetEnabled;
    bool connectionIsEstablished = false;
    static int lastFPS = -1;
    
    private void Awake()
    {
        videoResolutionSlider.onValueChanged.AddListener(VideoResolutionUpdate);
        videoQualitySlider.onValueChanged.AddListener(VideoQualityUpdate);
        videoGainSlider.onValueChanged.AddListener(VideoGainUpdate);
        videoAutoSetToggle.onValueChanged.AddListener(VideoAutoSetToggleUpdate);
        protectionOverloadToggle.onValueChanged.AddListener(ProtectionOverloadToggleUpdate);
        VideoReceiver.OnVideoFPSChanged.AddListener(VideoAutoSettingsUpdate);
        EventManager.OnConnectionChanged.AddListener(ConnectionChanged);
        WebSocketAndServerSideEventsManager.OnNanoSettingsReceived.AddListener(ReadNanoSettings);

        videoAutoSetEnabled = videoAutoSetToggle.isOn;
        nanoSettings.protectionOverload = protectionOverloadToggle.isOn;
        GetSlidersCameraSettings();
    }

    private void ConnectionChanged(ConnectionType type, string ip)
    {
        if (type == ConnectionType.Repeater || type == ConnectionType.SelfAP)
        {
            connectionIsEstablished = true;
            lastFPS = -1;
        }
        else
            connectionIsEstablished = false;
    }

    private void VideoResolutionUpdate(float value)
    {
        camSettings.frame = (int)value + sliderVideoResolutionOffset;
    }

    private void VideoQualityUpdate(float value)
    {
        camSettings.quality = (int)value;
    }
    private void VideoGainUpdate(float value)
    {
        camSettings.gain = (int)value;
    }

    private void VideoAutoSetToggleUpdate(bool toogle)
    {
        videoAutoSetEnabled = toogle;
    }

    private void ProtectionOverloadToggleUpdate(bool toogle)
    {
        nanoSettings.protectionOverload = toogle;
    }

    public static void ApplyVideoSettings(bool messageEnable)
    {
        OnVideoSettingsChanged.Invoke(camSettings, messageEnable);
    }

    public static void ApplySettings(bool messageEnable)
    {
        ApplyVideoSettings(messageEnable);
        OnNanoSettingsChanged.Invoke(nanoSettings);
        lastFPS = -1;
    }

    public void ReadSettings()
    {
        OnReadVideoSettings.Invoke(this);
        OnReadNanoSettings.Invoke();
    }

    public void SetSlidersCameraSettings(CameraSettings _cameraSettings)
    {
        camSettings = _cameraSettings;

        videoResolutionSlider.value = camSettings.frame - sliderVideoResolutionOffset;
        videoQualitySlider.value = camSettings.quality;
        videoGainSlider.value = camSettings.gain;
    }

    private void GetSlidersCameraSettings()
    {
        camSettings.frame = (int)videoResolutionSlider.value + sliderVideoResolutionOffset;
        camSettings.quality = (int)videoQualitySlider.value;
        camSettings.gain = (int)videoGainSlider.value;
    }

    private void ReadNanoSettings(NanoSettings newSet)
    {
        nanoSettings.protectionOverload = newSet.protectionOverload;
        protectionOverloadToggle.isOn = nanoSettings.protectionOverload;
        EventManager.Log("Nano settings has been received");
    }

    private void VideoAutoSettingsUpdate(int fps)
    {
        if (videoAutoSetEnabled && connectionIsEstablished)
        {
            bool settingsIsChanged = false;
            int resolution = camSettings.frame - sliderVideoResolutionOffset;
            int quality = camSettings.quality;

            if (lastFPS != -1)//last fps is ready 
            {
                if (lastFPS < FPSThresholdMin && fps < FPSThresholdMin)
                {
                    if (resolution > resolutionMin)
                    {
                        if (quality < qualityMaxForIntermediateResolution)
                        {
                            quality += qualityStep;
                            if (quality > qualityMax)
                                quality = qualityMax;
                        }
                        else
                        {
                            resolution--;
                        }

                        settingsIsChanged = true;
                    }
                    else if (quality < qualityMax)
                    {
                        quality += qualityStep;
                        if (quality > qualityMax)
                            quality = qualityMax;

                        settingsIsChanged = true;
                    }
                }
                else if (lastFPS > FPSThresholdMax && fps > FPSThresholdMax)
                {
                    if (resolution < resolutionMax)
                    {
                        if (quality > qualityMin)
                        {
                            quality -= qualityStep;
                            if (quality < qualityMin)
                                quality = qualityMin;
                        }
                        else
                        {
                            resolution++;
                        }

                        settingsIsChanged = true;
                    }
                    else if (quality > qualityMin)
                    {
                        quality -= qualityStep;
                        if (quality < qualityMin)
                            quality = qualityMin;

                        settingsIsChanged = true;
                    }
                }

                if (settingsIsChanged)
                {
                    camSettings.frame = resolution + sliderVideoResolutionOffset;
                    camSettings.quality = quality;

                    ApplyVideoSettings(false);
                    SetSlidersCameraSettings(camSettings);

                    EventManager.ShowMessage("Auto video setup");
                    //Debug.Log("R = " + camSettings.frame.ToString() + " Q = " + camSettings.quality.ToString());
                }
            }
            lastFPS = fps;
        }
    }
}
