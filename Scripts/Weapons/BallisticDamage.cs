using UnityEngine;
using System;

[CreateAssetMenu(menuName = "3D Roguelike Shooter/Ballistic Damage Settings")]
public class BallisticDamage : ProjectileDamage
{
    public override event Action<Transform, float, Vector3, Vector3, Transform> ProjectileDamageEvent;

    public float impactForce = 1f;

    public override void CalculateDamage(Transform owner, float damage, float lifetime, float currentLifetime, Vector3 hitPosition, Vector3 hitDirection, Vector3 velocity, Transform hitTransform, LayerMask collisionMask)
    {
        bool callDamageEvent = false;

        Enemy e;
        PlayerController p;
        Projectile proj;
        if (hitTransform.TryGetComponent(out e) || hitTransform.TryGetComponent(out p) || hitTransform.TryGetComponent(out proj))
        {
            callDamageEvent = true;
            //float damageMultiplier = damageFalloff.Evaluate((lifetime - currentLifetime) / lifetime);
            //damage = damage * damageMultiplier;
        }
        else
        {
            PhysicsObject physObj;
            if (hitTransform.TryGetComponent(out physObj))
            {
                physObj.AddForce(impactForce * velocity.magnitude, hitDirection, hitPosition, ForceMode.Force);
            }
        }

        if(callDamageEvent)
        {
            if (ProjectileDamageEvent != null)
            {
                //Debug.Log("calling damage event");
                ProjectileDamageEvent.Invoke(owner, damage, hitPosition, hitDirection, hitTransform);
            }
        }
    }
}
