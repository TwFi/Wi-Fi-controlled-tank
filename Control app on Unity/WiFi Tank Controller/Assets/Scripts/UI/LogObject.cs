using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LogObject : MonoBehaviour
{
    public Text timeText;
    public Text dataText;

    public void Set(string type, string data)
    {
        timeText.text = System.DateTime.Now.Hour.ToString() + ":" + System.DateTime.Now.Minute.ToString() + ":" + System.DateTime.Now.Second.ToString();
        dataText.text = "[" + type + "]\t" + data;
    }
}
