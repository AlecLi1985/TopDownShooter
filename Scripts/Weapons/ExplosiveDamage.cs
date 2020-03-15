using UnityEngine;
using System;

[CreateAssetMenu(menuName = "3D Roguelike Shooter/Explosive Damage Settings")]
public class ExplosiveDamage : ProjectileDamage
{
    public override event Action<Transform, float, Vector3, Vector3, Transform> ProjectileDamageEvent;

    public float explosionRadius = 1f;
    public float explosionForce = 1f;
    public float explosionDamageMultiplier = 3f;

    public override void CalculateDamage(Transform owner, float damage, Vector3 hitPosition, Vector3 hitDirection, Vector3 velocity, Transform hitTransform, LayerMask collisionMask)
    {
        bool callDamageEvent = false;

        Collider[] colliders = Physics.OverlapSphere(hitPosition, explosionRadius, collisionMask);
        if(colliders.Length > 0)
        {
            callDamageEvent = true;
        }

        foreach (Collider c in colliders)
        {
            Enemy e = c.GetComponent<Enemy>();
            PlayerController p = c.GetComponent<PlayerController>(); ;
            Projectile proj = c.GetComponent<Projectile>(); ;

            if (e != null || p != null || proj != null)
            {
                float distanceFromExplosion = (hitPosition - c.transform.position).magnitude;
                if (distanceFromExplosion > 0f && distanceFromExplosion < explosionRadius)
                {
                    //float damageMultiplier = damageFalloff.Evaluate((explosionRadius - distanceFromExplosion) / explosionRadius);
                    float damageMultiplier = (explosionRadius - distanceFromExplosion) / explosionRadius;
                    float damageInflicted = (damage * explosionDamageMultiplier) * damageMultiplier;
                    if (e != null)
                    {
                        e.SetDamage(damageInflicted);
                    }
                    if(p != null)
                    {
                        p.SetDamage(damageInflicted);
                    }
                }
            }
            else
            {
                PhysicsObject physObj = c.GetComponent<PhysicsObject>();
                if (physObj != null)
                {
                    physObj.AddExplosionForce(damage, explosionForce, hitPosition, explosionRadius, ForceMode.Force);
                }
            }
        }

        if(callDamageEvent)
        {
            if (ProjectileDamageEvent != null)
            {
                ProjectileDamageEvent.Invoke(owner, damage, hitPosition, hitDirection, hitTransform);
            }
        }
        
    }
}

public class ExplosiveDamageStruct
{
    public float explosionRadius = 1f;
    public float explosionForce = 1f;
}
