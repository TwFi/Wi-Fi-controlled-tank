using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SensorData
{
    public int hum;
    public float tmp;
    public int u_dst;
    public float[] acc;
    public float[] gyr;
    public float[] ang;
    public float prs;
    public float alt;
    public float b_vlt;
    public int bpc;
    public int rps;
    public float spd;
    public float mil;
    public int sig;
    public float r_bat;
    public int r_bat_p;
}
