using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ControlButtonHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public ControlButtonType buttonType;

    public void OnPointerDown(PointerEventData eventData)
    {
        ControlButtonController.ControlButtonDown(buttonType);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ControlButtonController.ControlButtonUp(buttonType);
    }
}
