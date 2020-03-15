using UnityEngine;
using System;

[CreateAssetMenu(menuName = "3D Roguelike Shooter/Projectile Settings")]
public class ProjectileSettings : ScriptableObject
{
    public string projectileName;
    public string projectileTravelSound;
    public string projectileDeathSound;

    public float minDamage;
    public float maxDamage;

    public float minSpeed;
    public float maxSpeed;

    public float lifeTime;
    public float hitLifeTime; //how long the projectile lasts for after it has hit something
    public float health;

    [Range(0, 100)]
    public int penetrationCount; //how many extra enemies can this projectile pass through before being destroyed

    public float trailDistance;

    public bool adjustProjectileSphereCast;
    public float startSphereCastRadius;
    public float endSphereCastRadius;

    //[HideInInspector]
    public ProjectileDamageType damageType;
   // [HideInInspector]
    public ProjectileBehaviourType behaviourType;

    private void OnDestroy()
    {
        Debug.Log("Destroy projectile settings");
    }
}

[Serializable]
public class ProjectileSettingsStruct
{
    public string projectileName;

    public float minDamage;
    public float maxDamage;

    public float minSpeed;
    public float maxSpeed;

    public float lifeTime;
    public float hitLifeTime; //how long the projectile lasts for after it has hit something

    public int penetrationCount; //how many extra enemies can this projectile pass through before being destroyed

    public bool adjustProjectileSphereCast;
    public float startSphereCastRadius;
    public float endSphereCastRadius;

    public ProjectileDamageType damageType;
    public ProjectileBehaviourType behaviourType;
}