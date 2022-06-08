using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Networking;

public class VideoReceiver : MonoBehaviour
{
    public static UnityEvent<int> OnVideoFPSChanged = new UnityEvent<int>();

    [SerializeField] RawImage rawImage;

    string streamPort = "1051";

    string streamURL;
    Vector2 currentImageResolution;
    Vector2 screenResolution;
    Texture2D currentTexture;
    Texture2D newTexture;
    GameObject frameGO;
    int fps = 0;
    float fpsTimer = 0;
    bool streamEnabled = false;
    bool connectionEstablished = false;
    UnityWebRequest webRequest;
    MJPEG_DownloadWebHandler dHandler;

    private void Awake()
    {
        EventManager.OnConnectionChanged.AddListener(ConnectionChanged);
        SettingsManager.OnVideoSettingsChanged.AddListener(VideoSettingsChanged);
        frameGO = rawImage.transform.gameObject;
    }

    void Start()
    {
        newTexture = new Texture2D(2, 2);
        currentTexture = new Texture2D(2, 2);

        if (rawImage == null)
            Debug.Log("Raw Image is null!");
        screenResolution = new Vector2(Screen.width, Screen.height);
    }

    private void VideoSettingsChanged(CameraSettings set, bool message)
    {
        RestartVideoStream();
    }

    public void RestartVideoStream()
    {
        EndStream();
        if (connectionEstablished)
            StartStream();
    }

    private void ConnectionChanged(ConnectionType type, string IP)
    {
        if(type == ConnectionType.Repeater || type == ConnectionType.SelfAP)
        {
            connectionEstablished = true;

            streamURL = "http://" + IP + ":" + streamPort;
            RestartVideoStream();
        }
        else
        {
            connectionEstablished = false;
            streamURL = "";
            EndStream();
        }
    }

    private void Update()
    {
        UpdateVideoFPS();
    }

    private void OnDestroy()
    {
        EndStream();
    }

    public void StartStream()
    {
        if (streamEnabled)
            return;
        streamEnabled = true;

        frameGO.SetActive(true);

        fpsTimer = Time.unscaledTime + 1;
        fps = 0;

        webRequest = UnityWebRequest.Get(streamURL);
        webRequest.downloadHandler = new MJPEG_DownloadWebHandler();
        dHandler = (MJPEG_DownloadWebHandler)webRequest.downloadHandler;
        dHandler.OnMJPEG_Received.AddListener(FrameReceived);
        webRequest.SendWebRequest();
    }

    public void EndStream()
    {
        if (!streamEnabled)
            return;
        streamEnabled = false;

        dHandler.OnMJPEG_Received.RemoveListener(FrameReceived);
        webRequest.Abort();
        webRequest.Dispose();

        frameGO.SetActive(false);

        OnVideoFPSChanged.Invoke(0);
    }

    void FrameReceived(byte[] bytes)
    {
        newTexture.LoadImage(bytes);

        //if texture have a red question mark (unity bad texture) – texture is bad
        if (newTexture.width == 8)
            return;

        currentTexture.LoadImage(bytes);
        SetImageResolution(new Vector2(currentTexture.width, currentTexture.height));
        rawImage.texture = currentTexture;

        fps++;
    }

    void SetImageResolution(Vector2 resolution)
    {
        if(currentImageResolution != resolution)
        {
            float scaleFactor = screenResolution.x / resolution.x;
            Vector2 realImageResolution = resolution * scaleFactor;

            rawImage.rectTransform.sizeDelta = realImageResolution;
            currentImageResolution = resolution;
        } 
    }

    void UpdateVideoFPS()
    {
        if (Time.unscaledTime >= fpsTimer)
        {
            fpsTimer = Time.unscaledTime + 1;

            OnVideoFPSChanged.Invoke(fps);
            fps = 0;
        }
    }
}