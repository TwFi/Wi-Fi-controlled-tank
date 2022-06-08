using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ControlDataManager : MonoBehaviour
{
    public static UnityEvent<string, bool> OnControlDataUpdate = new UnityEvent<string, bool>();

    private void Awake()
    {
        ControlButtonController.OnMoveSideChanged.AddListener(ControlDataUpdate);
    }

    void ControlDataUpdate(MoveSide moveSide)
    {
        bool repeat = true;
        if (moveSide == MoveSide.STOP)
            repeat = false;

        OnControlDataUpdate.Invoke(((int)moveSide).ToString(), repeat);
    }
}
