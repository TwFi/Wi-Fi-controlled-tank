using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum LogType
{
    APPLICATION,
    ESP,
    NANO
}

public class LogController : MonoBehaviour
{
    public RectTransform logScrollViewContentT;
    public GridLayoutGroup gridGroup;
    public GameObject logObjectPrefub;

    private const int maxLogObjects = 200;

    private List<GameObject> logObjects = new List<GameObject>();
    private float logObjectSpacing;
    private string[] logTypes = new string[] { "APP", "ESP", "NANO" };

    private void Awake()
    {
        logObjectSpacing = gridGroup.cellSize.y + gridGroup.spacing.y;
        WebSocketAndServerSideEventsManager.OnLogMessageReceived.AddListener(AddLogObject);
        EventManager.OnLogNewObject.AddListener(AddLogObject);
    }

    private void AddLogObject(LogType type, string data)
    {
        logScrollViewContentT.sizeDelta = new Vector2(logScrollViewContentT.sizeDelta.x, logScrollViewContentT.sizeDelta.y + logObjectSpacing);
        GameObject log = Instantiate(logObjectPrefub, logScrollViewContentT);
        log.GetComponent<LogObject>().Set(logTypes[(int)type], data);
        logObjects.Add(log);

        if (logObjects.Count > maxLogObjects)
        {
            GameObject oldLog = logObjects[0];
            logObjects.RemoveAt(0);
            Destroy(oldLog);
        } 
    }
}
