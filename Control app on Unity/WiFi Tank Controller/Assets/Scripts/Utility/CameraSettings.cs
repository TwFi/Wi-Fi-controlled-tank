public class CameraSettings
{
    public int frame;
    public int quality;
    public int gain;

    public CameraSettings()
    {
        frame = 1 + SettingsManager.sliderVideoResolutionOffset;
        quality = 12;
        gain = 0;
    }
}
