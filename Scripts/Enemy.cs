using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public enum EnemyType
{
    NONE,
    SOLDIER,
    SPIDERBOT,
    DRONE,
    BOSS
}

public class Enemy : MonoBehaviour
{
    public static event Action<float> OnPlayerHitEvent;
    public static event Action<float, float> OnEnemyHitEvent;
    public static event Action OnPlayerHitEnemyEvent;

    public EnemyType enemyType;
    public float health;
    [HideInInspector]
    public bool isDead = false;
    public bool disableOnDeath = false;

    [Header("Weapons Library")]
    public List<WeaponSettings> weaponSettingsLibrary;

    [Header("Behaviour Settings")]
    public LayerMask attackLayerMask;
    public LayerMask sightLayerMask;

    public bool trackPlayer = false;
    public float alertPlayerDistance = 10f;
    public float retrackPlayerDistance = 10f;
    bool destinationSet = false;

    [Header("Equipped Weapon")]
    public Weapon[] weapons;

    [Header("Attack Settings")]
    public float attackRadius = 25f;
    public float attackSphereCastCheckRadius = .1f;
    [Range(-1f, 1f)]
    public float engageTargetMinimumThreshold = 0.98f;
    public bool fireEverything = false;
    public float meleeAttackDelay = 2f;
    public float meleeAttackRadius = 2f;
    public float meleeDamage = 10f;

    [Header("Move Settings")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float moveDelay = 2f;
    public float reachedDistance = .5f;
    public Vector3 heightOffset;
    public bool updateNavAgentRotation = true;

    public Transform moveTransform;
    Vector3 movePosition;

    [Header("Animation Settings")]
    public float animationSpeedXSmoothTime;
    public float animationSpeedZSmoothTime;

    [Header("Sound Settings")]
    public string moveSound;
    public string hitSound;
    public string deathSound;

    [Header("UI Settings")]
    public TextMesh healthText;

    [Header("Misc")]
    public Light flashlight;
    public GameObject explosionObject;

    [HideInInspector]
    public bool damageInflicted = false;
    [HideInInspector]
    public float damageInflictedAmount = 0f;

    public UnityEvent OnDeathEvent;

    PlayerController player;
    Transform playerTransform;
    NavMeshAgent navAgent;
    Animator enemyAnimator;
    Rigidbody rb;
    CapsuleCollider capsuleCollider;

    Vector3 directionToTarget;
    Vector3 directionToTargetFlat;
    Vector3 targetAimPosition;
    Vector3 randomPointInSphere;
    Vector3 transformedVelocity;

    Quaternion playerLookRotation;
    Quaternion playerLookRotationFlat;

    float distanceFromPlayer = 0f;
    float distanceFromTarget = 0f;

    bool targetInSight = false;
    float currentMeleeAttackDelay = 0f;

    float currentAttackDelay = 0f;
    float attackDelay = 2f;
    float currentMoveDelay = 0f;

    bool parentNavAgent = false;

    float startHealth;
    Vector3 startPosition;
    Quaternion startRotation;

    bool ragdollCharacter = false;
    Rigidbody[] rbs;

    // Start is called before the first frame update
    void Awake()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if(playerObject != null)
        {
            player = playerObject.GetComponent<PlayerController>();
            playerTransform = playerObject.transform;
        }

        enemyAnimator = GetComponent<Animator>();

        if(enemyAnimator != null)
        {
            enemyAnimator.SetFloat("cycleOffset", Random.value);
        }

        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        if (weapons.Length > 0)
        {
            for(int i = 0; i < weapons.Length; i++)
            {
                Weapon weapon = weapons[i];

                weapon.isEnemyWeapon = true;
                weapon.targetTransform = playerTransform;

                if (weaponSettingsLibrary.Count > 0)
                {
                    weapon.weaponSettings = weaponSettingsLibrary[Random.Range(0, weaponSettingsLibrary.Count)];
                    weapon.currentRound = weapon.weaponSettings.maxClip;
                }
            }
        }

        navAgent = GetComponent<NavMeshAgent>();
        if(navAgent == null)
        {
            navAgent = GetComponentInParent<NavMeshAgent>();
            parentNavAgent = true;
        }

        if (healthText != null)
        {
            healthText.text = health.ToString();
        }

        rbs = GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rbs)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.mass = 1;
            rb.drag = 0;
        }


        Projectile.ProjectileHitEvent -= OnEnemyShot;
        Projectile.ProjectileHitEvent += OnEnemyShot;
    }

    private void Start()
    {
        //if(SoundManager.instance != null)
        //{
        //    SoundManager.instance.PlaySound(moveSound);
        //}

        AudioSource source;
        if(TryGetComponent(out source))
        {
            source.pitch += Random.Range(-.1f, .1f);
        }

        startHealth = health;
        if (parentNavAgent)
        {
            startPosition = transform.parent.position;
            startRotation = transform.parent.rotation;
        }
        else
        {
            startPosition = transform.position;
            startRotation = transform.rotation;
        }
    }

    public void ResetEnemy()
    {
        health = startHealth;

        if (parentNavAgent)
        {
            transform.parent.position = startPosition;
            transform.parent.rotation = startRotation;

            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
        else
        {
            transform.position = startPosition;
            transform.rotation = startRotation;
        }

        trackPlayer = false;
        distanceFromPlayer = 0f;
        navAgent.destination = transform.position;

        transformedVelocity = Vector3.zero;

        enemyAnimator.SetFloat("velX", 0f);
        enemyAnimator.SetFloat("velZ", 0f);
    }

    // Update is called once per frame
    void Update()
    {
        if (isDead == false)
        {
            if (navAgent != null)
            {
                navAgent.updateRotation = updateNavAgentRotation;
            }

            if (playerTransform != null && player.isDead == false)
            {
                targetAimPosition = playerTransform.position; //have this be the player's future position so they can lead them
                directionToTarget = targetAimPosition - transform.position;
                distanceFromPlayer = (playerTransform.position - transform.position).magnitude;
                playerLookRotation = Quaternion.LookRotation(directionToTarget + Vector3.up);

                directionToTargetFlat = directionToTarget;
                directionToTargetFlat.y = 0f;
                playerLookRotationFlat = Quaternion.LookRotation(directionToTargetFlat);

                if (enemyType == EnemyType.DRONE)
                {
                    MoveDrone();

                    //currentMoveDelay += Time.deltaTime;
                    //currentAttackDelay += Time.deltaTime;

                    targetInSight = CheckIsPlayerInFOV();

                    if (targetInSight)
                    {
                        AttackTarget();
                    }
                }

                if (enemyType == EnemyType.BOSS)
                {
                    MoveBoss();

                    //currentMoveDelay += Time.deltaTime;
                    //currentAttackDelay += Time.deltaTime;

                    targetInSight = CheckIsPlayerInFOV();

                    if (targetInSight)
                    {
                        AttackTarget();
                    }
                }

                if (trackPlayer)
                {
                    if (enemyType == EnemyType.SOLDIER)
                    {
                        MoveSoldier();

                        targetInSight = CheckIsPlayerInFOV();

                        if (targetInSight)
                        {
                            AttackTarget();
                        }
                    }
                    else if (enemyType == EnemyType.SPIDERBOT)
                    {
                        MoveSpiderBot();
                    }
                    else if (enemyType == EnemyType.DRONE)
                    {
                        targetInSight = CheckIsPlayerInFOV();

                        if (targetInSight)
                        {
                            AttackTarget();
                        }
                    }
                    else if (enemyType == EnemyType.BOSS)
                    {
                        targetInSight = CheckIsPlayerInFOV();

                        if (targetInSight)
                        {
                            AttackTarget();
                        }
                    }
                }

                if (distanceFromPlayer < alertPlayerDistance)
                {
                    trackPlayer = true;
                }

            }
        }
    }

    void MoveSoldier()
    {
        if (navAgent != null)
        {
            if (distanceFromPlayer > retrackPlayerDistance)
            {
                if(destinationSet == false)
                {
                    Vector3 destination = Vector3.zero;
                    int loopCount = 0;

                    while (loopCount < 100)
                    {
                        randomPointInSphere = Random.onUnitSphere;
                        randomPointInSphere.y = 0f;

                        destination = playerTransform.position + Vector3.up + randomPointInSphere * navAgent.stoppingDistance;

                        RaycastHit hit;
                        if (Physics.Raycast(destination, (playerTransform.position + Vector3.up) - destination, out hit, 100f, sightLayerMask))
                        {
                            if(hit.transform.gameObject.layer == LayerMask.NameToLayer("Player"))
                            {
                                break;
                            }
                        }

                        loopCount++;
                    }
                    
                    navAgent.SetDestination(playerTransform.position + randomPointInSphere * navAgent.stoppingDistance);

                    destinationSet = true;
                }

                distanceFromPlayer = 0f;

                enemyAnimator.SetBool("meleeAttacking", false);
            }

            if(distanceFromPlayer < attackRadius)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, playerLookRotationFlat, rotationSpeed * Time.deltaTime);
            }

            if (navAgent.remainingDistance <= navAgent.stoppingDistance)
            {
                destinationSet = false;
                distanceFromPlayer = (playerTransform.position - transform.position).magnitude;

                if (distanceFromPlayer < meleeAttackRadius)
                {
                    enemyAnimator.SetBool("meleeAttacking", true);

                    if (currentMeleeAttackDelay == 0f)
                    {
                        MeleeTarget();
                    }

                    currentMeleeAttackDelay += Time.deltaTime;

                    if (currentMeleeAttackDelay > meleeAttackDelay)
                    {
                        currentMeleeAttackDelay = 0f;
                    }
                }

                transform.rotation = Quaternion.Slerp(transform.rotation, playerLookRotationFlat, rotationSpeed * Time.deltaTime);
            }
        }

        transformedVelocity = transform.InverseTransformVector(navAgent.velocity);

        enemyAnimator.SetFloat("velX", transformedVelocity.x, animationSpeedXSmoothTime, Time.deltaTime);
        enemyAnimator.SetFloat("velZ", transformedVelocity.z, animationSpeedZSmoothTime, Time.deltaTime);
    }

    void MoveSpiderBot()
    {
        if (navAgent != null)
        {
            if (distanceFromPlayer > retrackPlayerDistance)
            {
                randomPointInSphere = Random.onUnitSphere;
                randomPointInSphere.y = 0f;
                navAgent.SetDestination(playerTransform.position + randomPointInSphere * navAgent.stoppingDistance);

                distanceFromPlayer = 0f;

                enemyAnimator.SetBool("meleeAttacking", false);
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, playerLookRotationFlat, rotationSpeed * Time.deltaTime);
            }

            if (navAgent.remainingDistance <= navAgent.stoppingDistance)
            {
                distanceFromPlayer = (playerTransform.position - transform.position).magnitude;

                if (distanceFromPlayer < meleeAttackRadius)
                {
                    enemyAnimator.SetBool("meleeAttacking", true);

                    if (currentMeleeAttackDelay == 0f)
                    {
                        MeleeTarget();
                    }

                    currentMeleeAttackDelay += Time.deltaTime;

                    if (currentMeleeAttackDelay > meleeAttackDelay)
                    {
                        currentMeleeAttackDelay = 0f;
                    }
                }

                transform.rotation = Quaternion.Slerp(transform.rotation, playerLookRotationFlat, rotationSpeed * Time.deltaTime);
            }
        }

        transformedVelocity = transform.InverseTransformVector(navAgent.velocity);

        enemyAnimator.SetFloat("velX", transformedVelocity.x, animationSpeedXSmoothTime, Time.deltaTime);
        enemyAnimator.SetFloat("velZ", transformedVelocity.z, animationSpeedZSmoothTime, Time.deltaTime);
    }

    void MoveDrone()
    {
        if (navAgent != null)
        {
            if (distanceFromPlayer > retrackPlayerDistance)
            {
                if (destinationSet == false)
                {
                    randomPointInSphere = Random.onUnitSphere * attackRadius;
                    navAgent.SetDestination(playerTransform.position + randomPointInSphere + heightOffset);

                    destinationSet = true;
                }

                distanceFromPlayer = 0f;
            }


            if (navAgent.remainingDistance <= navAgent.stoppingDistance)
            {
                destinationSet = false;
                distanceFromPlayer = (playerTransform.position - transform.position).magnitude;

            }
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, playerLookRotation, rotationSpeed * Time.deltaTime);


        //if(destinationSet == false && currentMoveDelay > moveDelay)
        //{
        //    Vector3 destination = transform.position;

        //    for(int i = 0; i < 10f; i++)
        //    {
        //        Vector3 randomPoint = playerTransform.position + Random.onUnitSphere * 15f;

        //        NavMeshHit navHit;
        //        if(NavMesh.SamplePosition(randomPoint, out navHit, 1.0f, NavMesh.AllAreas))
        //        {
        //            movePosition = navHit.position;
        //            destinationSet = true;
        //            break;
        //        }
        //    }
        //}

        //if(destinationSet)
        //{
        //    //move, don't attack
        //    distanceFromTarget = (movePosition - transform.position).sqrMagnitude;

        //    transform.position = Vector3.Slerp(transform.position, movePosition + heightOffset, moveSpeed * Time.deltaTime);

        //    if(distanceFromTarget < reachedDistance)
        //    {
        //        //reahed destination, start attacking
        //        destinationSet = false;
        //        currentMoveDelay = 0f;
        //    }
        //}


        //if (trackPlayer)
        //{
        //    transform.rotation = Quaternion.Slerp(transform.rotation, playerLookRotation, rotationSpeed * Time.deltaTime);
        //}
        //else
        //{
        //    Quaternion lookRotation = Quaternion.identity;
        //    if (movePosition - transform.position != Vector3.zero)
        //    {
        //        lookRotation = Quaternion.LookRotation(movePosition - transform.position, Vector3.up);
        //    }
        //    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        //}

    }

    void MoveBoss()
    {
        if (navAgent != null)
        {
            if (distanceFromPlayer > retrackPlayerDistance)
            {
                if (destinationSet == false)
                {
                    randomPointInSphere = Random.onUnitSphere * attackRadius;
                    navAgent.SetDestination(playerTransform.position + randomPointInSphere + heightOffset);

                    destinationSet = true;
                }

                distanceFromPlayer = 0f;
            }


            if (navAgent.remainingDistance <= navAgent.stoppingDistance)
            {
                destinationSet = false;
                distanceFromPlayer = (playerTransform.position - transform.position).magnitude;

            }
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, playerLookRotation, rotationSpeed * Time.deltaTime);


        //if(destinationSet == false && currentMoveDelay > moveDelay)
        //{
        //    Vector3 destination = transform.position;

        //    for(int i = 0; i < 10f; i++)
        //    {
        //        Vector3 randomPoint = playerTransform.position + Random.onUnitSphere * 15f;

        //        NavMeshHit navHit;
        //        if(NavMesh.SamplePosition(randomPoint, out navHit, 1.0f, NavMesh.AllAreas))
        //        {
        //            movePosition = navHit.position;
        //            destinationSet = true;
        //            break;
        //        }
        //    }
        //}

        //if(destinationSet)
        //{
        //    //move, don't attack
        //    distanceFromTarget = (movePosition - transform.position).sqrMagnitude;

        //    transform.position = Vector3.Slerp(transform.position, movePosition + heightOffset, moveSpeed * Time.deltaTime);

        //    if(distanceFromTarget < reachedDistance)
        //    {
        //        //reahed destination, start attacking
        //        destinationSet = false;
        //        currentMoveDelay = 0f;
        //    }
        //}


        //if (trackPlayer)
        //{
        //    transform.rotation = Quaternion.Slerp(transform.rotation, playerLookRotation, rotationSpeed * Time.deltaTime);
        //}
        //else
        //{
        //    Quaternion lookRotation = Quaternion.identity;
        //    if (movePosition - transform.position != Vector3.zero)
        //    {
        //        lookRotation = Quaternion.LookRotation(movePosition - transform.position, Vector3.up);
        //    }
        //    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        //}

    }

    bool CheckIsPlayerInFOV()
    {
        float Result = 0f;
        Result = Vector3.Dot(transform.forward, directionToTarget.normalized);
        return Result > engageTargetMinimumThreshold;
    }

    bool CheckIsPlayerSighted()
    {
        bool Result = false;
        RaycastHit hit;
        if (Physics.SphereCast(transform.position /*+ Vector3.up + (transform.forward * 0.5f)*/, attackSphereCastCheckRadius, transform.forward, out hit, attackRadius, sightLayerMask))
        {
            if (hit.transform.gameObject.layer != LayerMask.NameToLayer("Obstacle"))
            {
                Result = true;
            }
        }

        return Result;
    }

    void AttackTarget()
    {
        if(weapons.Length > 0)
        {
            for(int i = 0; i < weapons.Length; i++)
            {
                Weapon weapon = weapons[i];

                if (weapon.currentRound == 0 && weapon.isReloading == false)
                {
                    weapon.ReloadWeapon();
                }

                if (weapon.isReloading == false)
                {
                    if (fireEverything)
                    {
                        weapon.PullTrigger(0f);
                    }
                    else
                    {
                        if(CheckIsPlayerSighted())
                        {
                            weapon.PullTrigger(0f);
                        }
                        else
                        {
                            weapon.ReleaseTrigger(0f);
                        }
                    }

                    if(weapon.fired)
                    {
                        weapon.UpdateWeapon(transform.forward);
                    }
                }
            }
        }
    }

    void MeleeTarget()
    {
        if(Physics.CheckSphere(transform.position, meleeAttackRadius, attackLayerMask))
        {
            if(OnPlayerHitEvent != null)
            {
                OnPlayerHitEvent.Invoke(meleeDamage);
            }
        }
    }

    void OnEnemyShot(Transform owner, float damage, Vector3 hitPosition, Vector3 hitDirection, Transform hitTransform)
    {
        if (isDead == false)
        {
            if (transform == hitTransform)
            {
                //Debug.Log("damaged enemy for " + damage.ToString());
                health -= damage;

                if (SoundManager.instance != null)
                {
                    SoundManager.instance.PlaySound(hitSound);
                }

                if (OnEnemyHitEvent != null)
                {
                    OnEnemyHitEvent.Invoke(startHealth, health);
                }

                if(OnPlayerHitEnemyEvent != null)
                {
                    OnPlayerHitEnemyEvent.Invoke();
                }
            }

            if (damageInflicted)
            {
                //Debug.Log("inflicted damage on enemy for " + damageInflictedAmount.ToString());

                health -= damageInflictedAmount;
                damageInflictedAmount = 0f;
                damageInflicted = false;

                if (OnEnemyHitEvent != null)
                {
                    OnEnemyHitEvent.Invoke(startHealth, health);
                }
            }

            if (health <= 0)
            {
                if (SoundManager.instance != null)
                {
                    //SoundManager.instance.StopSound(moveSound);
                    SoundManager.instance.PlaySound(deathSound);
                }
                if(disableOnDeath)
                {
                    DisableEnemy();
                }
                else
                {
                   OnDeath();
                }
            }

            if (healthText != null)
            {
                healthText.text = health.ToString();
            }
        }
    }

    public void SetDamage(float damage)
    {
        damageInflictedAmount = damage;
        damageInflicted = true;
    }

    //this is specfically for the boss so i'm not checking for the general case
    void DisableEnemy()
    {
        trackPlayer = false;
        distanceFromPlayer = 0f;
        navAgent.destination = transform.position;

        if (OnDeathEvent != null)
        {
            OnDeathEvent.Invoke();
        }

        if (explosionObject != null)
        {
            Instantiate(explosionObject, transform.position, Quaternion.identity);
        }

        ResetEnemy();
        transform.parent.gameObject.SetActive(false);
    }

    void OnDeath()
    {
        isDead = true;

        trackPlayer = false;
        distanceFromPlayer = 0f;
        navAgent.destination = transform.position;

        if (weapons.Length > 0)
        {
            for (int i = 0; i < weapons.Length; i++)
            {
                Weapon weapon = weapons[i];
                weapon.ReleaseTrigger(0f);
            }
        }

        transformedVelocity = Vector3.zero;

        if(enemyAnimator != null)
        {
            enemyAnimator.SetFloat("velX", 0f);
            enemyAnimator.SetFloat("velZ", 0f);

            enemyAnimator.enabled = false;
        }

        AudioSource source;
        if (TryGetComponent(out source))
        {
            source.Stop();
        }

        if(rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.drag = 1000; //setting this seems to make the ragdolls react to gravity properly???!!
                            //rb.AddTorque(Random.onUnitSphere * 300.0f, ForceMode.Force);
        }

        if (capsuleCollider != null)
        {
            capsuleCollider.enabled = false;
        }

        if(rbs.Length > 0)
        {
            foreach (Rigidbody rb in rbs)
            {
                rb.useGravity = true;
                rb.isKinematic = false;
            }
        }

        if(flashlight != null)
        {
            flashlight.enabled = false;
        }

        if (OnDeathEvent != null)
        {
            OnDeathEvent.Invoke();
        }

        if(enemyType == EnemyType.SOLDIER || enemyType == EnemyType.SPIDERBOT)
        {
            StartCoroutine(DestroyEnemy());
        }
        else 
        {
            if (explosionObject != null)
            {
                Instantiate(explosionObject, transform.position, Quaternion.identity);
            }

            if (parentNavAgent)
            {
                Destroy(transform.parent.gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    IEnumerator DestroyEnemy()
    {
        yield return new WaitForSeconds(1f);

        Renderer[] renderers = transform.GetComponentsInChildren<Renderer>();

        foreach(Renderer renderer in renderers)
        {
            Material[] materials = renderer.materials;
            MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

            foreach (Material m in materials)
            {
                if (m.HasProperty("_DissolveTime"))
                {
                    float dissolveTime;
                    for (dissolveTime = 0f; dissolveTime < 1f; dissolveTime += Time.deltaTime)
                    {
                        propertyBlock.SetFloat("_DissolveTime", dissolveTime);
                        renderer.SetPropertyBlock(propertyBlock);

                        yield return null;
                    }

                    if(dissolveTime >= 1f)
                    {
                        m.SetFloat("_DissolveTime", 1f);
                        break;
                    }
                }
            }
        }

        yield return new WaitForSeconds(1f);

        if (parentNavAgent)
        {
            Destroy(transform.parent.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        Projectile.ProjectileHitEvent -= OnEnemyShot;
    }

    bool IsVector3Valid(Vector3 v)
    {
        return float.IsNaN(v.x) == false &&
                float.IsNaN(v.y) == false &&
                float.IsNaN(v.z) == false;
    }

    private void OnDrawGizmos()
    {
        //Gizmos.color = Color.red;
        //if (navAgent != null)
        //{
        //    Gizmos.DrawWireSphere(navAgent.destination, .2f);
        //}
        //Gizmos.DrawLine(transform.position, transform.position + transform.forward * attackRadius);
    }
}
