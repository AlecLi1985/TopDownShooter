using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class Projectile : MonoBehaviour
{
    public static event Action<Transform, float, Vector3, Vector3, Transform> ProjectileHitEvent;

    [HideInInspector]
    public Transform owner;

    public bool useTrailRenderer = false;
    public LineRenderer tracer { get; set; }
    public TrailRenderer tracerTrail { get; set; }

    public Material tracerMaterial { get; set; }
    public Color tracerColor { get; set; }
    public float tracerWidth { get; set; }
    public float tracerUpdateRate { get; set; }

    public ProjectileSettings projectileSettings;
    public ProjectileBehaviour projectileBehaviour;
    public ProjectileDamage projectileDamage;

    public GameObject explosionObject;
    public GameObject bulletImpactMetalObject;
    public GameObject bulletImpactSoftObject;

    public LayerMask collisionMask;
    public LayerMask damageMask;
    public bool drawDebugLines = false;

    public Transform targetTransform;
    public Vector3 targetTransformOffset;

    bool isInitialized = false;

    Vector3 transformPosition;
    Quaternion transformRotation;
    Vector3 projectedPosition = Vector3.zero;
    Vector3 velocity = Vector3.zero;
    float speed = 0f;
    float currentLifeTime = 0.0f;
    float currentHitLifeTime = 0.0f;

    Vector3 startPosition = Vector3.zero;
    float currentSphereCastRadius = 0f;
    float scaleStartTime = 0f;
    float currentTracerUpdateTime = 0f;

    bool projectileHit;
    Transform projectileHitTransform;
    Vector3 projectileHitNormal;
    bool isDeflected = false;

    Rigidbody rb;
    SphereCollider collider;

    float health;

    int count = 0;

    // Start is called before the first frame update
    void Start()
    {
        startPosition = transform.position;

        scaleStartTime = Time.time;
        currentSphereCastRadius = projectileSettings.startSphereCastRadius;

        health = projectileSettings.health;

        if(projectileBehaviour is TimedPhysicsProjectileBehaviour)
        {
            collider = gameObject.AddComponent<SphereCollider>();
            collider.center = Vector3.zero;
            collider.radius = projectileSettings.startSphereCastRadius;
            //collider.enabled = false;

            rb = gameObject.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            //transform.GetChild(0).gameObject.SetActive(true);
        }

        ProjectileHitEvent -= OnProjectileHit;
        ProjectileHitEvent += OnProjectileHit;

        if(projectileDamage != null)
        {
            projectileDamage.ProjectileDamageEvent -= OnDamage;
            projectileDamage.ProjectileDamageEvent += OnDamage;
        }
    }

    public void SetTracerParamaters()
    {
        tracer.material = tracerMaterial;
        tracer.startColor = tracerColor;
        tracer.endColor = Color.clear;
        tracer.startWidth = tracerWidth;
        tracer.endWidth = tracerWidth * .25f;
    }

    public void SetTracerTrailParamaters()
    {
        tracerTrail.material = tracerMaterial;
        tracerTrail.startColor = tracerColor;
        tracerTrail.endColor = Color.clear;
        tracerTrail.startWidth = tracerWidth;
        tracerTrail.endWidth = tracerWidth * .25f;
        tracerTrail.minVertexDistance = tracerUpdateRate;
        tracerTrail.time = projectileSettings.lifeTime;
        tracerTrail.alignment = LineAlignment.View;
        tracerTrail.receiveShadows = false;
        tracerTrail.generateLightingData = false;
        tracerTrail.emitting = true;
    }

    // Update is called once per frame
    void Update()
    {
        if(isInitialized == false)
        {
            if(rb != null)
            {
                rb.AddForce(velocity, ForceMode.Force);
            }

            isInitialized = true;
        }

        if(currentLifeTime < projectileSettings.lifeTime)
        {
            transformPosition = transform.position;
            transformRotation = transform.rotation;

            if (projectileBehaviour is SmartProjectileBehaviour)
            {
                if (targetTransform != null)
                {
                    (projectileBehaviour as SmartProjectileBehaviour).targetPosition = targetTransform.position + targetTransformOffset;
                }
                else
                {
                    (projectileBehaviour as SmartProjectileBehaviour).targetPosition = Vector3.zero;
                }
            }

            if(isDeflected)
            {
                velocity += Random.onUnitSphere * Random.Range(projectileSettings.minDamage, projectileSettings.maxDamage) * 0.5f;
            }

            projectileBehaviour.CalculatePosition(ref transformPosition, ref projectedPosition, ref velocity, speed,
                                                    ref transformRotation, currentSphereCastRadius,
                                                    ref projectileHit, collisionMask, ref projectileHitTransform, ref projectileHitNormal);


            transform.position = transformPosition;
            transform.rotation = transformRotation;

            if (projectileSettings.adjustProjectileSphereCast)
            {
                float currentScaleTime = (Time.time - scaleStartTime) / projectileSettings.lifeTime;
                currentSphereCastRadius = Mathf.Lerp(projectileSettings.startSphereCastRadius, projectileSettings.endSphereCastRadius, currentScaleTime);
            }
        }

        if(useTrailRenderer == false)
        {
            if (currentTracerUpdateTime == 0f)
            {
                tracer.SetPosition(0, projectedPosition - transform.position * -2f);
            }
            tracer.SetPosition(1, projectedPosition);

            currentTracerUpdateTime += Time.deltaTime;
            if (currentTracerUpdateTime > tracerUpdateRate)
            {
                currentTracerUpdateTime = 0f;
            }
        }
        
        if (projectileHit)
        {
            //OnDamage(transform.position, (projectedPosition - transform.position).normalized);
            OnHit(transform.position, (projectedPosition - transform.position).normalized);
        }
        else
        {
            currentLifeTime += Time.deltaTime;
            currentHitLifeTime += Time.deltaTime;

            if (currentLifeTime > projectileSettings.lifeTime && projectileSettings.hitLifeTime == 0)
            {
                if (projectileBehaviour is TimedPhysicsProjectileBehaviour && projectileDamage is ExplosiveDamage)
                {
                    OnHit(transform.position, (projectedPosition - transform.position).normalized);
                }
                else if(projectileDamage is ExplosiveDamage)
                {
                    OnHit(transform.position, (projectedPosition - transform.position).normalized);

                    if (explosionObject != null)
                    {
                        Instantiate(explosionObject, transform.position, Quaternion.identity);
                    }
                }
                else
                {
                    //Debug.Log("destroying projectile");
                    DestroyProjectile();
                }

            }
        }


    }

    private void FixedUpdate()
    {
        if(projectileBehaviour is TimedPhysicsProjectileBehaviour)
        {
            projectileBehaviour.UpdateProjectile(transform.position, ref projectileHitTransform, collisionMask, ref projectileHit);
        }

    }

    public void SetVelocity(Vector3 dir)
    {
        speed = Random.Range(projectileSettings.minSpeed, projectileSettings.maxSpeed);
        velocity = dir * speed;
    }

    void OnHit(Vector3 hitPosition, Vector3 hitDirection)
    {
        if(projectileHitTransform != null)
        {
            if (projectileHitTransform.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
            {
                if (bulletImpactMetalObject != null)
                {
                    Quaternion hitImpactDirection = Quaternion.LookRotation(projectileHitNormal);
                    Instantiate(bulletImpactMetalObject, hitPosition, hitImpactDirection);
                }
            }
            else
            {
                float damage = Random.Range(projectileSettings.minDamage, projectileSettings.maxDamage);

                if (projectileDamage != null)
                {
                    if (projectileDamage is BallisticDamage)
                    {
                        projectileDamage.CalculateDamage(owner, damage, projectileSettings.lifeTime, currentLifeTime, hitPosition, hitDirection, velocity, projectileHitTransform, damageMask);

                        if (bulletImpactSoftObject != null)
                        {
                            Quaternion hitImpactDirection = Quaternion.LookRotation(projectileHitNormal);
                            Instantiate(bulletImpactSoftObject, hitPosition, hitImpactDirection);
                        }
                    }
                    else
                    {
                        //Debug.Log("calculate explosive damage 1");

                        projectileDamage.CalculateDamage(owner, damage, hitPosition, hitDirection, velocity, projectileHitTransform, damageMask);
                        if (explosionObject != null)
                        {
                            Instantiate(explosionObject, transform.position, Quaternion.identity);
                        }
                    }
                }
            }
        }


        if(projectileSettings.hitLifeTime > 0f)
        {
            if (currentHitLifeTime > projectileSettings.hitLifeTime)
            {
                //Debug.Log("destroying projectile 1");
                DestroyProjectile();
            }
        }
        else
        {
            //Debug.Log("destroying projectile 2");

            if (projectileDamage != null)
            {
                if (projectileDamage is ExplosiveDamage)
                {
                   // Debug.Log("calculate explosive damage 2");

                    float damage = Random.Range(projectileSettings.minDamage, projectileSettings.maxDamage);
                    projectileDamage.CalculateDamage(owner, damage, hitPosition, hitDirection, velocity, projectileHitTransform, damageMask);

                    if (explosionObject != null)
                    { 
                        Instantiate(explosionObject, transform.position, Quaternion.identity);
                    }
                }
            }

            DestroyProjectile();
        }
    }

    void OnDamage(Transform owner, float damage, Vector3 hitPosition, Vector3 hitDirection, Transform hitTransform)
    {
        if (ProjectileHitEvent != null)
        {
            if(projectileDamage is BallisticDamage)
            {
                if (projectileHitTransform != null)
                {
                    //Debug.Log("calling projectile hit event");

                    ProjectileHitEvent.Invoke(owner, damage, hitPosition, hitDirection, hitTransform);
                    //count++;
                    //Debug.Log("invoke count " + count + " Projectile Name: " + projectileSettings.name);
                }
            }
            else if(projectileDamage is ExplosiveDamage)
            {
                //Debug.Log("calling projectile hit event 1");

                ProjectileHitEvent.Invoke(owner, damage, hitPosition, hitDirection, hitTransform);
            }

        }
    }

    void OnProjectileHit(Transform owner, float damage, Vector3 hitPosition, Vector3 hitDirection, Transform hitTransform)
    {
        if(hitTransform == transform)
        {
            health -= damage;
            if (health <= 0f)
            {
                Destroy(gameObject);
            }

            isDeflected = true;
            currentLifeTime = projectileSettings.lifeTime * .75f;

            //Debug.Log("projectile deflected");
        }

    }

    public void SetTargetPosition(Transform target)
    {
        targetTransform = target;
    }

    public void SetTargetPositionOffset(Vector3 offset)
    {
        targetTransformOffset = offset;
    }


    public void SetProjectileSettings()
    {

    }

    public void SetProjectileBehaviour()
    {

    }

    public void SetProjectileDamage()
    {

    }

    private void DestroyProjectile()
    {
        if(projectileDamage is ExplosiveDamage)
        {
            SoundManager.instance.PlaySound(projectileSettings.projectileDeathSound);
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {

        projectileDamage.ProjectileDamageEvent -= OnDamage;
        ProjectileHitEvent -= OnProjectileHit;
    }

    private void OnDrawGizmos()
    {
        if(drawDebugLines)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(startPosition, projectedPosition);
            Gizmos.DrawWireSphere(projectedPosition, currentSphereCastRadius);

        }
    }

}
