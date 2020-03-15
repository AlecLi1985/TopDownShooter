using UnityEngine;

[CreateAssetMenu(menuName = "3D Roguelike Shooter/Projectile Object")]
public class ProjectileObject : ScriptableObject
{
    public ProjectileSettings settings;
    public ProjectileBehaviour behaviour;
    public ProjectileDamage damage;
}
