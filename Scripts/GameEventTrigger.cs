using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameEventTrigger : MonoBehaviour
{
    public UnityEvent OnEnterTriggerEvents;
    public UnityEvent OnExitTriggerEvents;

    private void OnTriggerEnter(Collider other)
    {
        if(OnEnterTriggerEvents != null)
        {
            OnEnterTriggerEvents.Invoke();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (OnExitTriggerEvents != null)
        {
            OnExitTriggerEvents.Invoke();
        }
    }

}
