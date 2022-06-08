using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class DataManager : MonoBehaviour
{
    public static UnityEvent<SensorData> OnSensorDataUpdated = new UnityEvent<SensorData>();

    SensorData data = new SensorData();
    private void Awake()
    {
        HTTP_Manager.OnDataReceived.AddListener(DataReceived);
    }

    public void SensorDataUpdate(SensorData data)
    {
        OnSensorDataUpdated.Invoke(data);
    }

    void DataReceived(string jsonData)
    {
        //Debug.Log(jsonData);
        data =  JsonUtility.FromJson<SensorData>(jsonData);

        SensorDataUpdate(data);
    }
}
