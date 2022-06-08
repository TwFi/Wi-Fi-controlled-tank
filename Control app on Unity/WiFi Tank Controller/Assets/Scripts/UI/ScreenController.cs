using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ScreenController : MonoBehaviour
{
    public static UnityEvent<ScreenType> OnScreenChanged = new UnityEvent<ScreenType>();

    public GameObject mainScreen;
    public GameObject settingsScreen;
    public GameObject sensorScreen;
    public GameObject logScreen;
    public SettingsManager settingsManager;

    private GameObject currentScreenGObject;

    void Start()
    {
        currentScreenGObject = mainScreen;
    }

    public void ScreenChanged(ScreenType screenType)
    {
        OnScreenChanged.Invoke(screenType);
    }

    public void SetScreen(int _screen)
    {
        ScreenType screen = (ScreenType)_screen;
        currentScreenGObject.SetActive(false);

        switch (screen)
        {
            case ScreenType.Main:
                currentScreenGObject = mainScreen;
                break;
            case ScreenType.Settings:
                settingsManager.ReadSettings();
                currentScreenGObject = settingsScreen;
                break;
            case ScreenType.Sensors:
                currentScreenGObject = sensorScreen;
                break;
            case ScreenType.Log:
                currentScreenGObject = logScreen;
                break;
        }

        currentScreenGObject.SetActive(true);
        ScreenChanged(screen);
    }

    public void BackToMainScreen()
    {
        SetScreen((int)ScreenType.Main);
    }
}
