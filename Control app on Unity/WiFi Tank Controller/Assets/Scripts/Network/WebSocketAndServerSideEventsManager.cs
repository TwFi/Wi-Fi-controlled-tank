using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;
using UnityEngine.Events;
using EvtSource;

public class WebSocketAndServerSideEventsManager : MonoBehaviour
{
    public static UnityEvent<LogType, string> OnLogMessageReceived = new UnityEvent<LogType, string>();
    public static UnityEvent<FastSensorData> OnFastDataReceived = new UnityEvent<FastSensorData>();
    public static UnityEvent<NanoSettings> OnNanoSettingsReceived = new UnityEvent<NanoSettings>();

    private string wsPort = ":1053";
    private string ssePort = ":1053";

    private WebSocket ws;
    private EventSourceReader sse;
    private bool wsConnectionEnabled = false;

    private void Awake()
    {
        EventManager.OnConnectionChanged.AddListener(ConnectionChange);
    }

    void Update()
    {
        #if !UNITY_WEBGL || UNITY_EDITOR
            if (ws != null)
                ws.DispatchMessageQueue();
        #endif
    }

    void ConnectionChange(ConnectionType type, string ip)
    {
        if (type == ConnectionType.Repeater || type == ConnectionType.SelfAP)
        {
            string wsURL = "ws://" + ip.ToString() + wsPort + "/ws";
            string sseURL = "http://" + ip.ToString() + ssePort + "/sse";
            WSConnect(wsURL);
            SSEConnect(sseURL);
        }
        else
        {
            WSDisconnect();
        }
    }

    private async void WSConnect(string url)
    {
        if (wsConnectionEnabled)
            WSDisconnect();
        wsConnectionEnabled = true;

        ws = new WebSocket(url);
        ws.OnOpen += WSConnectionOpen;
        ws.OnClose += WSConnectionClose;
        ws.OnError += WSConnectionError;
        ws.OnMessage += WSReceivedData;
        await ws.Connect();
    }

    private void WSDisconnect()
    {
        if (!wsConnectionEnabled)
            return;
        wsConnectionEnabled = false;
        DestroyWS();
    }

    async void WSConnectionOpen()
    {
        //Debug.Log("WS Connection is open!");
        await ws.SendText("hi__");
    }

    void WSConnectionClose(WebSocketCloseCode closeCode)
    {
        //Debug.Log("WS Connection closed");
        EventManager.Log("WS connection is disconnect");
    }

    void WSConnectionError(string errorMsg)
    {
        Debug.Log("WS Error " + errorMsg); 
    }

    void WSReceivedData(byte[] data)
    {
        string s_data = System.Text.Encoding.UTF8.GetString(data);
        string type = s_data.Substring(0, 4);
        string message = s_data.Substring(4);

        if (type == "logE")
        {
            OnLogMessageReceived.Invoke(LogType.ESP, message);
        }
        else if (type == "logN")
        {
            OnLogMessageReceived.Invoke(LogType.NANO, message);
        }
        else if (type == "nset")
        {
            NanoSettings set = JsonUtility.FromJson<NanoSettings>(message);
            OnNanoSettingsReceived.Invoke(set);
        }
        else
        {
            Debug.Log("Unknow WS type received! Type= " + type + "Mess= " + message);
        }
    }

    private void OnDestroy()
    {
        DestroyWS();
        DestroySSE();
    }

    public async void DestroyWS()
    {
        if (ws.State != WebSocketState.Closed || ws.State != WebSocketState.Closing)
        {
            await ws.Close();
        }
    }

    private void DestroySSE()
    {
        if (!sse.IsDisposed)
        {
            sse.Dispose();
        }
    }

    private void SSEConnect(string url)
    {
        sse = new EventSourceReader(new System.Uri(url));
        sse.MessageReceived += SSEMessageReceived;
        sse.Disconnected += SSEDisconnected;
        sse.Start();
    }

    private void SSEMessageReceived(object sender, EventSourceMessageEventArgs e)
    {
       if (e.Event == "fast")
        {
            FastSensorData fastData = JsonUtility.FromJson<FastSensorData>(e.Message);
            OnFastDataReceived.Invoke(fastData);
        }
        else
        {
            Debug.Log("Unknow SSE event received! Event= " + e.Event + "Mess= " + e.Message);
        }
    }

    private void SSEDisconnected(object sender, DisconnectEventArgs e)
    {
        EventManager.Log("SSE connection is disconnect");
    }
}
