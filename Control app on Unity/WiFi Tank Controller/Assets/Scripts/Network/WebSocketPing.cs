using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NativeWebSocket;

public class WebSocketPing
{
    public bool isDone { get => _isDone; }

    const string port = ":1053";
    const string pingMessage = "ping";
    const string pongMessage = "pong";
    WebSocket ws;
    bool _isDone = false;

    public WebSocketPing(string ip)
    {
        string url = "ws://" + ip.ToString() + port + "/ws";
        ws = new WebSocket(url);
        Begin();
    }

    private async void Begin()
    {
        EventManager.OnUpdate.AddListener(Update);
        ws.OnMessage += ConnectionReceiveData;
        ws.OnOpen += ConnectionOpen;
        await ws.Connect();
    }

    private void Update()
    {
        #if !UNITY_WEBGL || UNITY_EDITOR
            ws.DispatchMessageQueue();
        #endif
    }

    private async void ConnectionOpen()
    {
        await ws.SendText(pingMessage);
    }

    private async void ConnectionReceiveData(byte[] data)
    {
        string message = System.Text.Encoding.UTF8.GetString(data);
        if (message == pongMessage)
        {
            _isDone = true;
            if (ws.State != WebSocketState.Closed || ws.State != WebSocketState.Closing)
            {
                await ws.Close();
            }
        }
    }

    public async void DestroyPing()
    {
        if (ws.State != WebSocketState.Closed || ws.State != WebSocketState.Closing)
        {
            await ws.Close();
        }

        EventManager.OnUpdate.RemoveListener(Update);
    }
}
