using UnityEngine;
using System;

public enum ProjectileDamageType
{
    NONE,
    BALLISTIC,
    EXPLOSIVE
}

public abstract class ProjectileDamage : ScriptableObject
{
    public abstract event Action<Transform, float, Vector3, Vector3, Transform> ProjectileDamageEvent;

    public ProjectileDamageType type;

    public AnimationCurve damageFalloff;

    public virtual void CalculateDamage(Transform owner, float damage, Vector3 hitPosition, Vector3 hitDirection, Vector3 velocity, Transform hitTransform, LayerMask collisionMask) { }
    public virtual void CalculateDamage(Transform owner, float damage, float lifetime, float currentLifetime, Vector3 hitPosition, Vector3 hitDirection, Vector3 velocity, Transform hitTransform, LayerMask collisionMask) { }

}
