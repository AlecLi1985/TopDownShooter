using UnityEngine;

[CreateAssetMenu(menuName = "3D Roguelike Shooter/Smart Projectile Behaviour")]
public class SmartProjectileBehaviour : ProjectileBehaviour
{
    //public override event Action<int, Vector3, Vector3> HitEvent;
    [HideInInspector]
    public Vector3 targetPosition = Vector3.zero;

    public float turnSpeed = 10f;

    Vector3 targetDirection = Vector3.zero;

    public override void CalculatePosition(ref Vector3 transformPosition, ref Vector3 projectedPosition, ref Vector3 velocity, 
        float speed, ref Quaternion transformRotation, float currentProjectileRadius, 
        ref bool projectileHit, LayerMask collisionMask, ref Transform projectileHitTransform, ref Vector3 projectileHitNormal)
    {
        if(targetPosition != Vector3.zero)
        {
            velocity = Vector3.RotateTowards(velocity, (targetPosition - transformPosition).normalized, turnSpeed, 0.0f);
            velocity = velocity.normalized * speed;
        }

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
        Debug.Log("Destroyed SmartProjectileBehaviour");
    }

}

public class SmartProjectileBehaviourStruct
{
    public float turnSpeed = 10f;
}
