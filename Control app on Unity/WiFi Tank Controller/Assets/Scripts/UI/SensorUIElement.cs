using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SensorUIElement : MonoBehaviour
{
    public SensorType sensorType;

    protected Text valueText;
    protected bool isHead = false;

    protected void Awake()
    {
        valueText = this.transform.Find("Value").GetComponent<Text>(); 
        if (valueText == null)
        {
            Debug.Log("Value text is null!");
        }

        if (isHead)
        {
            return;
        }
        else if (sensorType == SensorType.connection)
        {
            NetManager.OnConnectionStatusChanged.AddListener(ConnectionUpdate);
        }
        else if (sensorType != SensorType.moveSide)
        {
            DataManager.OnSensorDataUpdated.AddListener(DataUpdate);

            if (sensorType == SensorType.ultasonicDistance || sensorType == SensorType.angle)
                WebSocketAndServerSideEventsManager.OnFastDataReceived.AddListener(FastDataUpdate);   
        }
    }

    private void ConnectionUpdate(ConnectionStatus status)
    {
        string value = "";

        switch(status.type)
        {
            case ConnectionStatusType.GOOD:
                value += "[GOOD] ";
                break;
            case ConnectionStatusType.LOSS:
                value += "[LOSS] ";
                break;
            case ConnectionStatusType.NONE:
                value += "[NO] ";
                break;
        }

        value += "Ping: " + status.pingData.lastLatency.ToString() + ", ";
        value += "min: " + status.pingData.minLatency.ToString() + ", ";
        value += "max: " + status.pingData.maxLatency.ToString() + ", ";
        value += "average: " + status.pingData.avgLatency.ToString() + ". ";
        value += "Packets: " + status.pingData.receivedToSentRatio[0].ToString() + "/" + status.pingData.receivedToSentRatio[1].ToString();

        SetValue(value);
    }

    void FastDataUpdate (FastSensorData fastData)
    {
        if (sensorType == SensorType.angle)
        {
            string value = "X:" + fastData.x.ToString() + "; Y: " + fastData.y.ToString() + "; Z: " + fastData.z.ToString();
            SetValue(value);
        }
        else if (sensorType == SensorType.angle)
        {
            SetValue(fastData.ultrasonic.ToString());
        }
    }

    void DataUpdate(SensorData data)
    {
        string value = "";
        float[] xyz;

        switch (sensorType)
        {
            case SensorType.none:
                Debug.Log("Sensor type is none");
                return;

            case SensorType.accelerometer:
                xyz = data.acc;
                value = "X:" + xyz[0].ToString() + "; Y: " + xyz[1].ToString() + "; Z: " + xyz[2].ToString();
                break;

            case SensorType.altitude:
                value = data.alt.ToString();
                break;

            case SensorType.angle:
                xyz = data.ang;
                value = "X:" + xyz[0].ToString() + "; Y: " + xyz[1].ToString() + "; Z: " + xyz[2].ToString();
                break;

            case SensorType.batteryPercente:
                value = data.bpc.ToString() + "% " + data.b_vlt.ToString() + "V";
                break;

            case SensorType.batteryVoltage:
                value = data.bpc.ToString() + "% " + data.b_vlt.ToString() + "V";
                break;

            case SensorType.gyroscop:
                xyz = data.gyr;
                value = "X:" + xyz[0].ToString() + "; Y: " + xyz[1].ToString() + "; Z: " + xyz[2].ToString();
                break;

            case SensorType.humidity:
                value = data.hum.ToString();
                break;

            case SensorType.mileage:
                value = data.mil.ToString();
                break;

            case SensorType.pressure:
                value = data.prs.ToString();
                break;

            case SensorType.repeaterBattery:
                value = data.r_bat_p.ToString() + "% " + data.r_bat.ToString() + "V";
                break;

            case SensorType.repeaterBatteryPercente:
                value = data.r_bat_p.ToString() + "% " + data.r_bat.ToString() + "V";
                break;

            case SensorType.rotatePerSecond:
                value = data.rps.ToString();
                break;

            case SensorType.signal:
                value = data.sig.ToString();
                break;

            case SensorType.speed:
                value = data.spd.ToString();
                break;

            case SensorType.temperature:
                value = data.tmp.ToString();
                break;

            case SensorType.ultasonicDistance:
                value = data.u_dst.ToString();
                break;

            default:
                return;
        }
        SetValue(value);
    }

    void SetValue(string value)
    {
        valueText.text = value;
    }
}
