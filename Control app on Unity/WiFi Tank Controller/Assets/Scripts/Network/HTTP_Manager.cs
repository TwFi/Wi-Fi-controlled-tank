using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;

public class HTTP_Manager : MonoBehaviour
{
    public static UnityEvent<string> OnDataReceived = new UnityEvent<string>();
    public static UnityEvent<string> OnControlDataSended = new UnityEvent<string>();

    const string httpPrefix = "http://";
    const string httpServerPort = ":1053";
    const string dataPath = "/data";
    const string getCameraSettingsPath = "/g-cam";
    const string controlPath = "/?c=";
    const string beepPath = "/?b=";
    const string videoResolutionPatch = "cam-r=";
    const string videoQualityPatch = "cam-q=";
    const string videoGainPatch = "cam-g=";
    const string protectionOverloadPatch = "/?n-p=";
    const string getNanoSettingsPatch = "/g-n";
    const string responseAnswer = "OK";
    const float getDataRequestTimeout = 1;//s
    const float sendDataRequestTimeout = 0.5f;//s
    const int retryCount = 3;

    string tankIP;
    bool connectionEstablished = false;
    Coroutine sendControlDataGetRequest;
    Coroutine sendBeepDataGetRequest;
    Coroutine sendVideoSettingsGetRequest;
    Coroutine readVideoSettingsGetRequest;
    Coroutine sendNanoSettingsGetRequest;
    Coroutine readNanoSettingsGetRequest;

    private void Awake()
    {
        EventManager.OnConnectionChanged.AddListener(ConnectionChanged);
        ControlDataManager.OnControlDataUpdate.AddListener(SendControlData);
        BuzzerConroller.OnBeep.AddListener(SendBeepData);
        SettingsManager.OnVideoSettingsChanged.AddListener(SendVideoSettings);
        SettingsManager.OnReadVideoSettings.AddListener(ReadVideoSettings);
        SettingsManager.OnNanoSettingsChanged.AddListener(SendNanoSettings);
        SettingsManager.OnReadNanoSettings.AddListener(ReadNanoSettings);
    }

    private void Start()
    {
        StartCoroutine(GetDataGetRequest());
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    public void ConnectionChanged(ConnectionType connectionType, string IP)
    {
        if (connectionType == ConnectionType.Repeater || connectionType == ConnectionType.SelfAP)
        {
            connectionEstablished = true;
            tankIP = IP;

            EventManager.Log("Connection established via " + connectionType.ToString());
        }
        else
        {
            connectionEstablished = false;
            EventManager.Log("Connection has been disconnect");
        }
    }

    private void DataReceived(string data)
    {
        OnDataReceived.Invoke(data);
    }

    IEnumerator GetDataGetRequest()
    {
        UnityWebRequest request = null;
        while (true)
        {
            if (connectionEstablished)
            {
                string url = httpPrefix + tankIP + httpServerPort + dataPath;
                request = UnityWebRequest.Get(url);
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    DataReceived(request.downloadHandler.text);
                }
            }
            else
            {
                if (request != null)
                {
                    request.Abort();
                    request = null;
                }
            }

            yield return new WaitForSeconds(getDataRequestTimeout);
        }
    }

    void SendControlData(string control, bool repeat)
    {
        if (sendControlDataGetRequest != null)
        {
            StopCoroutine(sendControlDataGetRequest);
            sendControlDataGetRequest = null;
        }
        sendControlDataGetRequest = StartCoroutine(SendControlDataGetRequest(control, repeat));
    }

    IEnumerator SendControlDataGetRequest(string control, bool repeat)
    {
        int retry = 0;
        while (true)
        {
            if (connectionEstablished)
            {
                string url = httpPrefix + tankIP + httpServerPort + controlPath + control;
                UnityWebRequest request = UnityWebRequest.Get(url);
                ControlDataSended(control);
                yield return request.SendWebRequest();

                if (!repeat && retry < retryCount)
                {
                    retry++;

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string response = request.downloadHandler.text;
                        if (response == responseAnswer)
                        {
                            ControlDataSended(((int)MoveSide.NONE).ToString());
                            yield break;
                        }
                    }
                }
            }
            else
            {
                ControlDataSended(((int)MoveSide.NONE).ToString());
                yield break;
            }
            yield return new WaitForSeconds(sendDataRequestTimeout);
        }
    }

    private void ControlDataSended(string control)
    {
        OnControlDataSended.Invoke(control);
    }

    void SendBeepData(string signal)
    {
        if (sendBeepDataGetRequest != null)
        {
            StopCoroutine(sendBeepDataGetRequest);
            sendBeepDataGetRequest = null;
        }
        sendBeepDataGetRequest = StartCoroutine(SendBeepDataGetRequest(signal));
    }

    IEnumerator SendBeepDataGetRequest(string signal)
    {
        int retry = 0;
        while (true)
        {
            if (connectionEstablished)
            {
                string url = httpPrefix + tankIP + httpServerPort + beepPath + signal;
                UnityWebRequest request = UnityWebRequest.Get(url);
                yield return request.SendWebRequest();

                if (retry < retryCount)
                {
                    retry++;

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string response = request.downloadHandler.text;
                        if (response == responseAnswer)
                        {
                            EventManager.Log("Beep signal has been received on ESP");
                            yield break;
                        }
                    }
                }
            }
            else
            {
                yield break;
            }
            yield return new WaitForSeconds(sendDataRequestTimeout);
        }
    }

    void SendNanoSettings(NanoSettings nanoSettings)
    {
        if (sendNanoSettingsGetRequest != null)
        {
            StopCoroutine(sendNanoSettingsGetRequest);
            sendNanoSettingsGetRequest = null;
        }
        sendNanoSettingsGetRequest = StartCoroutine(SendNanoSettingGetRequest(nanoSettings));
    }

    IEnumerator SendNanoSettingGetRequest(NanoSettings nanoSettings)
    {
        int retry = 0;
        while (true)
        {
            if (connectionEstablished)
            {
                int protection = 0;
                if (nanoSettings.protectionOverload)
                    protection = 1;
                string url = httpPrefix + tankIP + httpServerPort + protectionOverloadPatch + protection.ToString();
                UnityWebRequest request = UnityWebRequest.Get(url);
                yield return request.SendWebRequest();

                if (retry < retryCount)
                {
                    retry++;

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string response = request.downloadHandler.text;
                        if (response == responseAnswer)
                        {
                            EventManager.Log("Nano settings has been received on ESP");
                            yield break;
                        }
                    }
                }
            }
            else
            {
                yield break;
            }
            yield return new WaitForSeconds(sendDataRequestTimeout);
        }
    }

    private void SendVideoSettings(CameraSettings cameraSettings, bool messageEnable)
    {
        if (sendVideoSettingsGetRequest != null)
        {
            StopCoroutine(sendVideoSettingsGetRequest);
            sendVideoSettingsGetRequest = null;
        }
        sendVideoSettingsGetRequest = StartCoroutine(SendVideSettingsGetRequest(cameraSettings, messageEnable));
    }

    IEnumerator SendVideSettingsGetRequest(CameraSettings cameraSettings, bool messageEnable)
    {
        int retry = 0;
        while (true)
        {
            if (connectionEstablished)
            {
                string url = httpPrefix + tankIP + httpServerPort + "/?" + videoResolutionPatch + cameraSettings.frame.ToString() +
                    "&" + videoQualityPatch + cameraSettings.quality.ToString() + "&" + videoGainPatch + cameraSettings.gain.ToString();
                UnityWebRequest request = UnityWebRequest.Get(url);
                yield return request.SendWebRequest();

                if (retry < retryCount)
                {
                    retry++;

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string response = request.downloadHandler.text;
                        if (response == responseAnswer)
                        {
                            if (messageEnable)
                                EventManager.ShowMessage("Settings has been received", MessageController.MessageType.success);
                            EventManager.Log("Video settings is aplly on ESP");
                            yield break;
                        }
                    }
                }
                else
                {
                    if (messageEnable)
                        EventManager.ShowMessage("Settings not received!", MessageController.MessageType.fail);
                }
            }
            else
            {
                if (messageEnable)
                    EventManager.ShowMessage("Settings not sended, no connection!", MessageController.MessageType.fail);
                yield break;
            }
            yield return new WaitForSeconds(sendDataRequestTimeout);
        }
    }

    public void ReadVideoSettings(SettingsManager settingsManager)
    {
        if (readVideoSettingsGetRequest != null)
        {
            StopCoroutine(readVideoSettingsGetRequest);
            readVideoSettingsGetRequest = null;
        }
        readVideoSettingsGetRequest = StartCoroutine(ReadVideoSettingsGetRequest(settingsManager));
    }

    IEnumerator ReadVideoSettingsGetRequest(SettingsManager settingsManager)
    {
        while (true)
        {
            if (connectionEstablished)
            {
                string url = httpPrefix + tankIP + httpServerPort + getCameraSettingsPath;
                UnityWebRequest request = UnityWebRequest.Get(url);
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;

                    CameraSettings camSettings = new CameraSettings();
                    camSettings = JsonUtility.FromJson<CameraSettings>(response);
                    settingsManager.SetSlidersCameraSettings(camSettings);

                    //EventManager.ShowMessage("Settings has been readed", MessageController.MessageType.success);
                    EventManager.Log("Video settings is readed");
                    yield break;
                }
            }
            else
            {
                EventManager.ShowMessage("No connection!", MessageController.MessageType.fail);
                yield break;
            }
            yield return new WaitForSeconds(sendDataRequestTimeout);
        }
    }

    public void ReadNanoSettings()
    {
        if (readNanoSettingsGetRequest != null)
        {
            StopCoroutine(readNanoSettingsGetRequest);
            readNanoSettingsGetRequest = null;
        }
        readNanoSettingsGetRequest = StartCoroutine(ReadNanoSettingsGetRequest());
    }

    IEnumerator ReadNanoSettingsGetRequest()
    {
        while (true)
        {
            if (connectionEstablished)
            {
                string url = httpPrefix + tankIP + httpServerPort + getNanoSettingsPatch;
                UnityWebRequest request = UnityWebRequest.Get(url);
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;
                    yield break;
                }
            }
            else
            {
                yield break;
            }
            yield return new WaitForSeconds(sendDataRequestTimeout);
        }
    }
}
