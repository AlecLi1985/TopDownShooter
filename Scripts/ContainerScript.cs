using System;
using UnityEngine;
using TMPro;

public class ContainerScript : MonoBehaviour
{
    public static event Action<bool, bool, int, int, int, int> ActivateContainerEvent;

    public bool giveWeapon = false;
    public bool giveHealth = false;

    [Range(-1,5)]
    public int specificWeaponID = -1;

    public int minAmmoAmount;
    public int maxAmmoAmount;
    public int healthAmount;
    public string openContainerSound;
    public string giveHealthSound;
    public AudioSource giveWeaponSound;

    private void Start()
    {
        if(giveHealth)
        {
            InteractableScript interactable = GetComponent<InteractableScript>();
            if (interactable != null)
            {
                interactable.interactText.text += " " + healthAmount;
            }
        }
        
    }

    public void OpenContainer()
    {
        if (ActivateContainerEvent != null)
        {
            ActivateContainerEvent.Invoke(giveWeapon, giveHealth, specificWeaponID, minAmmoAmount, maxAmmoAmount, healthAmount);
        }
    }

    public void PlayOpenContainerSound()
    {
        SoundManager.instance.PlaySound(openContainerSound);
    }

    public void PlayGiveHealthSound()
    {
        SoundManager.instance.PlaySound(giveHealthSound);
    }

    public void PlayGiveWeaponSound()
    {
        giveWeaponSound.Play();
    }
}
