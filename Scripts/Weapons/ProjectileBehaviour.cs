using UnityEngine;

public enum ProjectileBehaviourType
{
    NONE,
    BALLISTIC,
    SMART,
    TIMED,
    TIMEDPHYSICS
}

public abstract class ProjectileBehaviour : ScriptableObject
{
    public ProjectileBehaviourType type;

    public virtual void CalculatePosition(ref Vector3 transformPosition, ref Vector3 projectedPosition,
        ref Vector3 velocity, float speed, ref Quaternion transformRotation, float currentProjectileRadius,
        ref bool projectileHit, LayerMask collisionMask, ref Transform projectileHitTransform, ref Vector3 projectileHitNormal)
    { }

    public virtual void UpdateProjectile(Vector3 transformPosition, ref Transform projectileHitTransform, LayerMask collisionMask, ref bool projectileHit) { }

}
