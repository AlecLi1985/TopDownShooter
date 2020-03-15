using UnityEngine;
using TMPro;
using System;

public class ConsoleScript : MonoBehaviour
{
    public static event Action ActivateConsoleEvent;

    public string activateConsoleSound;

    public void ActivateConsole()
    {
        SoundManager.instance.PlaySound(activateConsoleSound);

        if (ActivateConsoleEvent != null)
        {
            ActivateConsoleEvent.Invoke();
        }
    }
}
