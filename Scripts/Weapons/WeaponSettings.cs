using UnityEngine;
using System;

[CreateAssetMenu(menuName = "3D Roguelike Shooter/Weapon Settings")]
public class WeaponSettings : ScriptableObject
{
    public string weaponName;
    public string weaponSound;
    public string weaponReloadSound;

    public bool fullAuto;
    public float rateOfFire;
    public int projectileAmount;
    public int maxClip;
    [Range(.00001f, .5f)]
    public float spreadAmount;
    public float reloadTime;
    public float recoilAmount;
    public float criticalHitChance;
    public float critcalHitMultiplier;

    private void OnDestroy()
    {
        Debug.Log("Destroy weapon settings");
    }
}

[Serializable]
public class WeaponSettingsStruct
{
    public string weaponName;
    public string weaponSound;
    public string weaponReloadSound;

    public bool fullAuto;
    public float rateOfFire;
    public int projectileAmount;
    public int maxClip;

    public float spreadAmount;
    public float reloadTime;
    public float criticalHitChance;
    public float critcalHitMultiplier;
}