using UnityEngine;
using System;
using System.Collections;

public class FloorTrap : MonoBehaviour
{
    public static event Action<float> OnTrapActivateEvent;

    public float damage = 5f;
    public float repeatDelay = .25f;
    bool trapActive = false;

    private void Start()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        StartCoroutine(TrapReactivate());
    }

    private void OnTriggerStay(Collider other)
    {
        if(trapActive == false)
        {
            StartCoroutine(TrapReactivate());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        StopAllCoroutines();
        trapActive = false;
    }

    IEnumerator TrapReactivate()
    {
        if (OnTrapActivateEvent != null)
        {
            OnTrapActivateEvent.Invoke(damage);
        }
        trapActive = true;

        yield return new WaitForSeconds(repeatDelay);

        trapActive = false;

    }
}
