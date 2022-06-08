using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventManager : MonoBehaviour 
{
    public static UnityEvent<string, MessageController.MessageType> OnMessage = new UnityEvent<string, MessageController.MessageType>();
    public static UnityEvent<ConnectionType, string> OnConnectionChanged = new UnityEvent<ConnectionType, string>();
    public static UnityEvent OnUpdate = new UnityEvent();
    public static UnityEvent<LogType, string> OnLogNewObject = new UnityEvent<LogType, string>();

    public static void ShowMessage(string message)
    {
        OnMessage.Invoke(message, MessageController.MessageType.none);
    }

    public static void ShowMessage (string message, MessageController.MessageType mType)
    {
        OnMessage.Invoke(message, mType);
    }

    public static void ConnectionChanged(ConnectionType connectionType, string IP)
    {
        OnConnectionChanged.Invoke(connectionType, IP);
    }

    public static void Log(string message)
    {
        OnLogNewObject.Invoke(LogType.APPLICATION, message);
    }

    private void Update()
    {
        OnUpdate.Invoke();
    }
}
