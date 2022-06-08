using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BuzzerConroller : MonoBehaviour
{
    public static UnityEvent<string> OnBeep = new UnityEvent<string>();

    public void BeepButtonPressed()
    {
        EventManager.ShowMessage("Beep signal sended", MessageController.MessageType.success);
        Beep(BuzzerSignal.NOTIFICATION);
    }

    public static void Beep(BuzzerSignal signal)
    {
        OnBeep.Invoke(((int)signal).ToString());
    }
}
