using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ControlButtonController : MonoBehaviour
{
    public static UnityEvent<MoveSide> OnMoveSideChanged = new UnityEvent<MoveSide>();

    private static MoveSide moveSide = MoveSide.NONE;
    private static MoveSide lastMoveSide = MoveSide.NONE;
    private static int buttonCounts = ControlButtonType.GetNames(typeof(ControlButtonType)).Length;
    private static bool[] buttonStates = new bool[buttonCounts];
    private KeyCode[] keyboardKeyCodes = {KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D, KeyCode.Space};

    private void Update()
    {
        UpdateKeyboard();
    }

    public static void ControlButtonDown(ControlButtonType button)
    {
        buttonStates[(int)button] = true;
        moveSide = UpdateMoveSide();
        MoveSideUpdate();
    }

    public static void ControlButtonUp(ControlButtonType button)
    {
        buttonStates[(int)button] = false;
        moveSide = UpdateMoveSide();
        MoveSideUpdate();
    }

    private static MoveSide UpdateMoveSide()
    {
        lastMoveSide = moveSide;
        bool forward = buttonStates[0];
        bool back = buttonStates[1];
        bool left = buttonStates[2];
        bool right = buttonStates[3];
        bool stop = buttonStates[4];

        if (stop)
        {
            lastMoveSide = MoveSide.NONE;
            return MoveSide.STOP;
        }
        else if (forward)
        {
            if (left)
                return MoveSide.FORWARD_LEFT;
            else if (right)
                return MoveSide.FORWARD_RIGHT;

            return MoveSide.FORWARD;
        }
        else if (back)
        {
            if (left)
                return MoveSide.BACK_LEFT;
            if (right)
                return MoveSide.BACK_RIGHT;

            return MoveSide.BACK;
        }
        else if (left)
            return MoveSide.LEFT;
        else if (right)
            return MoveSide.RIGHT;

        return MoveSide.STOP;
    }

    private static void MoveSideUpdate()
    {   
        if (moveSide != lastMoveSide)
            OnMoveSideChanged.Invoke(moveSide);
    }

    private void UpdateKeyboard()
    {
        for (int i = 0; i < buttonCounts; i++)
        {
            if (Input.GetKeyDown(keyboardKeyCodes[i]))
                ControlButtonDown((ControlButtonType)i);
            if (Input.GetKeyUp(keyboardKeyCodes[i]))
                ControlButtonUp((ControlButtonType)i);
        }
    }
}
