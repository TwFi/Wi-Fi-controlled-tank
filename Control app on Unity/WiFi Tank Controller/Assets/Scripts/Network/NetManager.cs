using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;

public class NetManager : MonoBehaviour
{
    public static UnityEvent<ConnectionStatus> OnConnectionStatusChanged = new UnityEvent<ConnectionStatus>();

    const string repeaterIP = "192.168.4.2";
    const string selfAP_IP = "192.168.6.1";
    const int searchTimeout = 1;//s
    const int connectionCheckTimeout = 1;//s
    const float connectionQualityTimeout = 1f;//s
    const uint connectionQualityPingCount = 15;

    ConnectionType currentConnectionType = ConnectionType.None;
    List<ConnectionMode> connectionModes = new List<ConnectionMode>();
    Coroutine getConnectionQuality;
    ConnectionStatus currentConnectionStatus = new ConnectionStatus();

    void Awake()
    {
        InitConnectionModes();
    }

    void Start()
    {
        ConnectionStatusChange(currentConnectionStatus);
        StartCoroutine(SearchConnectionType());  
    }

    void InitConnectionModes()
    {
        ConnectionMode repeaterMode = new ConnectionMode(ConnectionType.Repeater, repeaterIP,
            "Connection established – repeater", MessageController.MessageType.success);
        ConnectionMode selfAPMode = new ConnectionMode(ConnectionType.SelfAP, selfAP_IP,
            "Connection established – direct", MessageController.MessageType.success);
        ConnectionMode noneMode = new ConnectionMode(ConnectionType.None, "",
            "No connection!", MessageController.MessageType.fail);
        connectionModes.Add(repeaterMode);
        connectionModes.Add(selfAPMode);
        connectionModes.Add(noneMode);
    }

    IEnumerator SearchConnectionType()
    {
        string ip;
        ConnectionType searchConnectionType = ConnectionType.Repeater;
        
        while (true)
        {
            ip = connectionModes[(int)searchConnectionType].IP.ToString();
            WebSocketPing wPing = new WebSocketPing(ip);
            yield return new WaitForSeconds(searchTimeout);
    
            if (wPing.isDone)
            {
                wPing.DestroyPing();

                currentConnectionType = searchConnectionType;
                EventManager.ConnectionChanged(currentConnectionType, connectionModes[(int)searchConnectionType].IP);
                EventManager.ShowMessage(connectionModes[(int)currentConnectionType].messageText, connectionModes[(int)currentConnectionType].messageType);

                currentConnectionStatus.PingDataSetToZero();
                currentConnectionStatus.type = ConnectionStatusType.GOOD;
                ConnectionStatusChange(currentConnectionStatus);

                StartCoroutine(CheckConnectionStatus());
                getConnectionQuality = StartCoroutine(GetConnectionQuality());

                yield break;
            }

            if (searchConnectionType == ConnectionType.Repeater)
                searchConnectionType = ConnectionType.SelfAP;
            else if (searchConnectionType == ConnectionType.SelfAP)
                searchConnectionType = ConnectionType.Repeater;

            wPing.DestroyPing();
        }
    }

    IEnumerator CheckConnectionStatus()
    {
        string url = "http://" + connectionModes[(int)currentConnectionType].IP.ToString() + ":1053/ping";

        while (true)
        {
            UnityWebRequest request = UnityWebRequest.Get(url);
            request.timeout = connectionCheckTimeout;
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                if (response != "pong")
                {
                    Debug.Log("Unknow response to ping: " + response);
                }
            }
            else
            {
                currentConnectionType = ConnectionType.None;
                EventManager.ConnectionChanged(currentConnectionType, connectionModes[(int)currentConnectionType].IP);
                EventManager.ShowMessage(connectionModes[(int)currentConnectionType].messageText, connectionModes[(int)currentConnectionType].messageType);

                currentConnectionStatus.PingDataSetToZero();
                currentConnectionStatus.type = ConnectionStatusType.NONE;
                ConnectionStatusChange(currentConnectionStatus);

                StopCoroutine(getConnectionQuality);
                StartCoroutine(SearchConnectionType());
                yield break;
            }
            yield return new WaitForSeconds(connectionCheckTimeout);
        }
    }

    private void ConnectionStatusChange(ConnectionStatus status)
    {
        OnConnectionStatusChanged.Invoke(status);
    }

    IEnumerator GetConnectionQuality()
    {
        PingsData pingsData = new PingsData();
        Ping pingCheck = null;
        string tankIP = connectionModes[(int)currentConnectionType].IP;

        pingsData.receivedToSentRatio[1] = (int)connectionQualityPingCount;

        long countSentPings = 0;
        int max_value = 0;
        int min_value = int.MaxValue;
        int[] pings_values = new int[connectionQualityPingCount];
        bool[] pings_received = new bool[connectionQualityPingCount];
        uint index_pings_v = 0;
        uint index_pings_rec = 0;

        while (true)
        {
            if (pingCheck == null)
            {
                pingCheck = new Ping(tankIP);
                countSentPings++;
            }
            yield return new WaitForSeconds(connectionQualityTimeout);

            if (pingCheck.isDone)
            {
                pings_received[index_pings_rec] = true;

                //last latency
                int value = pingCheck.time;
                pings_values[index_pings_v] = value;
                index_pings_v++;
                if (index_pings_v == connectionQualityPingCount)
                    index_pings_v = 0;
                pingsData.lastLatency = value;

                //max min
                if (value > max_value)
                    max_value = value;
                if (value < min_value)
                    min_value = value;
                pingsData.maxLatency = max_value;
                pingsData.minLatency = min_value;

                //avg
                if (countSentPings > connectionQualityPingCount)
                {
                    int avg_value = 0;
                    for (int i = 0; i < connectionQualityPingCount; i++)
                    {
                        avg_value += pings_values[i];
                    }
                    avg_value /= (int)connectionQualityPingCount;
                    pingsData.avgLatency = avg_value;
                }
            }
            else
            {
                pings_received[index_pings_rec] = false;
            }

            index_pings_rec++;
            if (index_pings_rec == connectionQualityPingCount)
                index_pings_rec = 0;

            //ratio
            int ratio = 0;
            for (int i = 0; i < connectionQualityPingCount; i++)
            {
                if (pings_received[i] == true)
                {
                    ratio++;
                }
            }
            pingsData.receivedToSentRatio[0] = ratio;

            pingCheck.DestroyPing();
            pingCheck = null;

            if (countSentPings > connectionQualityPingCount)
            {
                if (currentConnectionStatus.type == ConnectionStatusType.GOOD)
                {
                    if (pingsData.receivedToSentRatio[0] < pingsData.receivedToSentRatio[1] || pingsData.lastLatency >= 250)
                        currentConnectionStatus.type = ConnectionStatusType.LOSS;    
                }
                else if (currentConnectionStatus.type == ConnectionStatusType.LOSS)
                {
                    if (pingsData.receivedToSentRatio[0] == pingsData.receivedToSentRatio[1] || pingsData.lastLatency <= 200)
                        currentConnectionStatus.type = ConnectionStatusType.GOOD;
                }     
            }
            currentConnectionStatus.pingData = pingsData.GetCopy();
            ConnectionStatusChange(currentConnectionStatus);

            //pingsData.DebugPrintValues();
            //EventManager.ShowMessage(pingsData.lastLatency.ToString(), MessageController.MessageType.warning);
        }
    }
}