using UnityEngine;

[CreateAssetMenu(menuName = "3D Roguelike Shooter/Timed Physics Projectile Behaviour")]
public class TimedPhysicsProjectileBehaviour : ProjectileBehaviour
{

    public override void UpdateProjectile(Vector3 transformPosition, ref Transform projectileHitTransform, LayerMask collisionMask, ref bool projectileHit)
    {
        Collider[] colliders = Physics.OverlapSphere(transformPosition, 0.1f, collisionMask);
        foreach (Collider c in colliders)
        {
            Enemy e = c.GetComponent<Enemy>();

            if (e != null)
            {
                projectileHit = true;
                projectileHitTransform = c.transform;
                break;
            }
        }
    }

    public void OnDestroy()
    {
        Debug.Log("Destroyed TimedProjectileBehaviour");
    }

}