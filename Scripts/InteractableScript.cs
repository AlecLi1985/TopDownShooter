using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class InteractableScript : MonoBehaviour
{

    public UnityEvent activateCallbacks;

    public bool canActivate = false;
    public bool isOpen = false;
    public bool deactivateAfterUsed = true;

    public TMP_Text interactText;

    // Start is called before the first frame update
    void Start()
    {
        interactText.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            //show contents of container in HUD
            if (isOpen == false)
            {
                interactText.gameObject.SetActive(true);
                canActivate = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            //hide contents of container in HUD
            if (isOpen == false)
            {
                interactText.gameObject.SetActive(false);
                canActivate = false;
            }
        }
    }

    public void Activate()
    {
        activateCallbacks.Invoke();

        if(deactivateAfterUsed)
        {
            isOpen = true;
            canActivate = false;
            gameObject.layer = LayerMask.NameToLayer("Default");
            interactText.gameObject.SetActive(false);
        }
    }

    public void Reactivate()
    {
        isOpen = false;
        gameObject.layer = LayerMask.NameToLayer("Interactable");
    }

    public bool CanActivate()
    {
        return canActivate && isOpen == false;
    }
}
