using UnityEngine;
using System;
using Random = UnityEngine.Random;

public class PlayerController : MonoBehaviour
{
    public static event Action<float> OnPlayerDamagedEvent;
    public static event Action OnPlayerDeathEvent;

    [HideInInspector]
    public bool isGamePaused = false;

    public float playerHealth = 250f;
    [HideInInspector]
    public bool isDead = false;
    [HideInInspector]
    public bool isGameOver = false;
    [HideInInspector]
    public bool isGameWon = false;

    public float moveSpeed = 0.5f;

    public float turnSpeed = 5f;
    public float walkForceMultiplier = 4f;
    public float runForceMultiplier = 5f;
    public float sprintForceMultiplier = 7f;

    [HideInInspector]
    public bool isAiming = false;
    [HideInInspector]
    public bool isSprinting = false;

    public float animationSpeedXSmoothTime;
    public float animationSpeedZSmoothTime;
    public float gravity = -5f;
    public float groundedRayDistance = .2f;
    public Vector3 mouseWorldPointOffset = Vector3.zero;
    public LayerMask mouseCheckMask;
    public LayerMask interactablesMask;

    public bool useLaserSight = false;
    public bool isGrounded = true;

    public string deathSound;
    public string[] damagedSounds;

    public bool useAudioSources = true;
    public AudioSource playerHitSound1;
    public AudioSource playerHitSound2;
    public AudioSource playerDeathSound;

    [HideInInspector]
    public bool damageInflicted = false;
    [HideInInspector]
    public float damageInflictedAmount = 0f;

    PlayerControls controls;

    Animator playerAnimator;
    Rigidbody rb;
    Camera viewCamera;
    LineRenderer lineRenderer;

    Vector2 mousePos;
    RaycastHit hitInfo;
    Vector3 lookDir;

    Vector3 targetInput = Vector3.zero;
    Vector3 velInput = Vector3.zero;
    Vector3 velTransformedInput = Vector3.zero;
    Vector3 mouseWorldPoint = Vector3.zero;

    Vector2 controllerPos;
    bool convertMousePosX = false;
    bool convertMousePosY = false;

    Vector3 appliedVelocity;

    [HideInInspector]
    public Weapon weapon;

    bool isOverEnemy = false;

    float cachedDeltaTime = 0f;


    // Start is called before the first frame update
    void Awake()
    {
        controls = new PlayerControls();

        //controls.Gameplay.Horizontal.performed += ctx => targetInput.x = ctx.ReadValue<float>();

        HandleInput();

        playerAnimator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
        viewCamera = Camera.main;

        weapon = GetComponent<Weapon>();
        //appliedVelocity.y = gravity;

        Enemy.OnPlayerHitEvent -= OnPlayerHit;
        Enemy.OnPlayerHitEvent += OnPlayerHit;
        FloorTrap.OnTrapActivateEvent -= OnPlayerHit;
        FloorTrap.OnTrapActivateEvent += OnPlayerHit;
        Projectile.ProjectileHitEvent -= OnPlayerShot;
        Projectile.ProjectileHitEvent += OnPlayerShot;
    }

    void OnEnable()
    {
        controls.Gameplay.Enable();
    }

    void OnDisable()
    {
        controls.Gameplay.Disable();
    }

    private void Start()
    {
        if(useLaserSight)
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.enabled = true;
            lineRenderer.positionCount = 2;
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (cachedDeltaTime == 0f)
        {
            cachedDeltaTime = Time.deltaTime;
        }

        if (isGamePaused == false && isDead == false)
        {
            float deltaTime = Time.deltaTime == 0f ? cachedDeltaTime == 0f ? 0.02f : cachedDeltaTime : Time.deltaTime;

            HandleMovement(deltaTime);
            HandleShooting(deltaTime);
        }

        if (isDead && isGameOver == false)
        {
            isGameOver = true;
            OnDeath();
        }

        if(isGameWon)
        {
            targetInput = Vector3.zero;
            playerHealth = 1000f;
        }

        cachedDeltaTime = Time.deltaTime;

    }

    private void HandleInput()
    {
        controls.Gameplay.Horizontal.performed += ctx =>
        {
            targetInput.x = ctx.ReadValue<float>();
        };
        controls.Gameplay.Horizontal.canceled += ctx => targetInput.x = 0f;

        controls.Gameplay.Vertical.performed += ctx =>
        {
            targetInput.z = ctx.ReadValue<float>();
        };
        controls.Gameplay.Vertical.canceled += ctx => targetInput.z = 0f;

        controls.Gameplay.Looking.performed += ctx =>
        {
            mousePos = ctx.ReadValue<Vector2>();
        };

        //controller stick input
        controls.Gameplay.LookingX.started += ctx => convertMousePosX = true;
        controls.Gameplay.LookingX.performed += ctx => controllerPos.x = ctx.ReadValue<float>();
        controls.Gameplay.LookingX.canceled += ctx =>
        {
            convertMousePosX = false;
            //mousePos.x = Screen.width * 0.5f;
        };

        controls.Gameplay.LookingY.started += ctx => convertMousePosY = true;
        controls.Gameplay.LookingY.performed += ctx => controllerPos.y = ctx.ReadValue<float>();
        controls.Gameplay.LookingY.canceled += ctx =>
        {
            convertMousePosY = false;
            //mousePos.y = Screen.height * 0.5f;
        };

        controls.Gameplay.Fire.performed += ctx =>
        {
            playerAnimator.SetBool("shooting", true);
            weapon.PullTrigger(Time.time);
        };

        controls.Gameplay.Fire.canceled += ctx =>
        {
            playerAnimator.SetBool("shooting", false);
            weapon.ReleaseTrigger(Time.time);
        };

        controls.Gameplay.Reload.performed += ctx =>
        {
            weapon.ReloadWeapon();
        };

        controls.Gameplay.Sprint.performed += ctx =>
        {
            isSprinting = true;
            weapon.ReleaseTrigger(0f);
        };
        controls.Gameplay.Sprint.canceled += ctx =>
        {
            isSprinting = false;
        };

        controls.Gameplay.Interact.performed += ctx =>
        {
            HandleInteractable();
        };
    }


    private void HandleMovement(float deltaTime)
    {
        isOverEnemy = false;

        //MOVEMENT
        //mousePos.x = Input.mousePosition.x;
        //mousePos.y = Input.mousePosition.y;

        if (convertMousePosX)
        {
            mousePos.x = Screen.width * 0.5f + (controllerPos.x * Screen.width * 0.5f);
        }

        if (convertMousePosY)
        {
            mousePos.y = Screen.height * 0.5f + (controllerPos.y * Screen.height * 0.5f);
        }

        Ray mouseRay = viewCamera.ScreenPointToRay(mousePos);
        if (Physics.Raycast(mouseRay, out hitInfo, 1000f, mouseCheckMask))
        {
            Enemy e = hitInfo.transform.GetComponent<Enemy>();
            if (e != null)
            {
                //Debug.Log("mouse over enemy");
                isOverEnemy = true;
            }

            mouseWorldPoint = hitInfo.point + mouseWorldPointOffset;

            lookDir = hitInfo.point - transform.position;
            lookDir.y = 0f;

            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), turnSpeed * deltaTime);
        }

        //keeping this for reference
        //if (Input.GetAxisRaw("Horizontal") > 0f)
        //{
        //    targetInput.x = 1f;
        //}
        //else if (Input.GetAxisRaw("Horizontal") < 0f)
        //{
        //    targetInput.x = -1f;
        //}
        //else
        //{
        //    targetInput.x = 0f;
        //}

        //if (Input.GetAxisRaw("Vertical") > 0f)
        //{
        //    targetInput.z = 1f;
        //}
        //else if (Input.GetAxisRaw("Vertical") < 0f)
        //{
        //    targetInput.z = -1f;
        //}
        //else
        //{
        //    targetInput.z = 0f;
        //}

        targetInput.Normalize();

        velInput.x = Mathf.MoveTowards(velInput.x, targetInput.x, deltaTime * moveSpeed);
        velInput.z = Mathf.MoveTowards(velInput.z, targetInput.z, deltaTime * moveSpeed);

        appliedVelocity.x = velInput.x;
        appliedVelocity.z = velInput.z;

        RaycastHit groundHit;
        if (Physics.Raycast(transform.position + transform.up * groundedRayDistance, Vector3.down, out groundHit, groundedRayDistance * 2f, mouseCheckMask))
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }

        //if (Input.GetKey(KeyCode.LeftShift))
        //{
        //    isSprinting = true;
        //    weapon.ReleaseTrigger(0f);
        //}
        //else
        //{
        //    isSprinting = false;
        //}

        if (isAiming)
        {
            velTransformedInput = transform.InverseTransformVector(velInput) * 0.35f;
        }
        else if (isSprinting)
        {
            velTransformedInput = transform.InverseTransformVector(velInput) * 1.35f;
        }
        else
        {
            velTransformedInput = transform.InverseTransformVector(velInput);
        }

        playerAnimator.SetFloat("velX", velTransformedInput.x, animationSpeedXSmoothTime, deltaTime);
        playerAnimator.SetFloat("velZ", velTransformedInput.z, animationSpeedZSmoothTime, deltaTime);
        //playerAnimator.SetFloat("speedMultiplier", velTransformedInput.magnitude, animationSpeedSmoothTime, Time.deltaTime);

    }

    private void HandleShooting(float deltaTime)
    {
        //SHOOTING
        if(isGamePaused == false)
        {
            if (isSprinting == false)
            {
                //if (Input.GetMouseButtonDown(0))
                //{
                //    playerAnimator.SetBool("shooting", true);
                //    weapon.PullTrigger(Time.time);
                //}

                //update weapon
                if (weapon.fired)
                {
                    if (isOverEnemy)
                    {
                        weapon.UpdateWeapon((mouseWorldPoint - transform.TransformPoint(weapon.firePosition)).normalized);
                    }
                    else
                    {
                        weapon.UpdateWeapon(transform.forward);
                    }
                }

                //if (Input.GetMouseButtonUp(0))
                //{
                //    playerAnimator.SetBool("shooting", false);
                //    weapon.ReleaseTrigger(Time.time);
                //}

                //if (Input.GetKeyDown(KeyCode.R))
                //{
                //    weapon.ReloadWeapon();
                //}
            }

            if (useLaserSight)
            {
                lineRenderer.SetPosition(0, transform.position + transform.TransformVector(weapon.firePosition));
                if (isOverEnemy)
                {
                    lineRenderer.SetPosition(1, mouseWorldPoint);
                }
                else
                {
                    lineRenderer.SetPosition(1, transform.position + (lookDir.normalized * 50f));
                }
            }
        }

    }

    void HandleInteractable()
    {
        //CHECK ACTIVATE INTERACTABLES
        Collider[] interactableColliders = Physics.OverlapSphere(transform.position, 0.5f, interactablesMask);
        if (interactableColliders.Length > 0)
        {
            for (int i = 0; i < interactableColliders.Length; i++)
            {
                InteractableScript interactable;
                if (interactableColliders[i].transform.TryGetComponent(out interactable))
                {
                    if (interactable.CanActivate())
                    {
                        interactable.Activate();
                    }
                }
            }
        }
    }

    private void FixedUpdate()
    {
        Vector3 appliedVelocityMultiplied;

        if (isAiming)
        {
            appliedVelocityMultiplied = appliedVelocity * walkForceMultiplier;
        }
        else if (isSprinting)
        {
            appliedVelocityMultiplied = appliedVelocity * sprintForceMultiplier;
        }
        else
        {
            appliedVelocityMultiplied = appliedVelocity * runForceMultiplier;
        }

        if (isGrounded == false)
        {
            appliedVelocityMultiplied.y += gravity;
        }
        else
        {
            appliedVelocityMultiplied.y = 0f;
        }

        rb.velocity = appliedVelocityMultiplied;
    }

    void OnPlayerHit(float damage)
    {

        if (isDead == false)
        {
            PlayHitSound();
        }

        playerHealth -= damage;
        UpdateHealth();

    }

    void OnPlayerShot(Transform owner, float damage, Vector3 hitPosition, Vector3 hitDirection, Transform hitTransform)
    {
        if (transform == hitTransform)
        {
            //Debug.Log("damaged player for " + damage.ToString());

            if (isDead == false)
            {
                PlayHitSound();


            }

            playerHealth -= Mathf.Round(damage);
            UpdateHealth();
        }

        if (damageInflicted)
        {
            //Debug.Log("inflicted damage on player for " + damageInflictedAmount.ToString());

            playerHealth -= Mathf.Round(damageInflictedAmount);
            damageInflictedAmount = 0f;
            damageInflicted = false;
            UpdateHealth();
        }
    }

    public void SetDamage(float damage)
    {
        damageInflictedAmount = damage;
        damageInflicted = true;
    }

    void UpdateHealth()
    {
        if (playerHealth <= 0f)
        {
            playerHealth = 0f;
            isDead = true;
        }

        if(OnPlayerDamagedEvent != null)
        {
            OnPlayerDamagedEvent.Invoke(playerHealth);
        }
    }

    void PlayHitSound()
    {
        if(useAudioSources == false)
        {
            int randomID = Random.Range(0, damagedSounds.Length);
            if (damagedSounds.Length > 0)
            {
                SoundManager.instance.PlaySound(damagedSounds[randomID]);
            }
        }
        else
        {
            int randomID = Random.Range(0, 2);
            if(randomID == 0)
            {
                playerHitSound1.Play();
            }
            else
            {
                playerHitSound2.Play();
            }
        }
    }



    void OnDeath()
    {
        appliedVelocity = Vector3.zero;

        playerAnimator.SetFloat("velX", 0f);
        playerAnimator.SetFloat("velY", 0f);

        playerAnimator.SetBool("isDead", true);

        if(useAudioSources == false)
        {
            SoundManager.instance.PlaySound(deathSound);
        }
        else
        {
            playerDeathSound.Play();
        }

        if (OnPlayerDeathEvent != null)
        {
            OnPlayerDeathEvent.Invoke();
        }
    }

    private void OnDestroy()
    {
        Enemy.OnPlayerHitEvent -= OnPlayerHit;
        FloorTrap.OnTrapActivateEvent -= OnPlayerHit;
        Projectile.ProjectileHitEvent -= OnPlayerShot;
    }

    public void ResetPlayerAnimator()
    {
        playerAnimator.SetBool("isDead", false);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(hitInfo.point, transform.position);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position + transform.up * groundedRayDistance, (transform.position + transform.up * groundedRayDistance) + Vector3.down * groundedRayDistance * 2);

        if(isGrounded)
        {
            Gizmos.color = Color.blue;
        }
        else
        {
            Gizmos.color = Color.red;
        }
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.2f);
    }
}
