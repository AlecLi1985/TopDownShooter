using UnityEngine;
using System;

[CreateAssetMenu(menuName = "3D Roguelike Shooter/Ballistic Projectile Behaviour")]
public class BallisticProjectileBehaviour : ProjectileBehaviour
{
    public override void CalculatePosition(ref Vector3 transformPosition, ref Vector3 projectedPosition, 
        ref Vector3 velocity, float speed, ref Quaternion transformRotation, float currentProjectileRadius, 
        ref bool projectileHit, LayerMask collisionMask, ref Transform projectileHitTransform, ref Vector3 projectileHitNormal)
    {
        projectedPosition.x = transformPosition.x + velocity.x * Time.deltaTime;
        projectedPosition.y = transformPosition.y + velocity.y * Time.deltaTime;
        projectedPosition.z = transformPosition.z + velocity.z * Time.deltaTime;

        RaycastHit hit;
        if (Physics.SphereCast(transformPosition, currentProjectileRadius, projectedPosition - transformPosition, out hit, (projectedPosition - transformPosition).magnitude, collisionMask))
        {
            projectileHit = true;
            transformPosition = hit.point;
            projectileHitTransform = hit.transform;
            projectileHitNormal = hit.normal;
        }
        else
        {
            projectileHit = false;

            transformPosition = projectedPosition;
            transformRotation = Quaternion.LookRotation(velocity);
        }
    }

    public void OnDestroy()
    {
        Debug.Log("Destroyed BallisticProjectileBehaviour");
    }

}