using UnityEngine;
using System;
using System.Collections;
using Random = UnityEngine.Random;

public class Weapon : MonoBehaviour
{
    public static event Action<int, int> OnUpdateWeaponEvent;
    public static event Func<int> OnGetGlobalCarriedRoundsEvent;
    public static event Action<int> OnSetGlobalCarriedRoundsEvent;
    public static event Action<int> OnPlayerShootWeapon;

    public static event Action ShakeCameraEvent;

    public Projectile projectile;
    public bool useTrailRenderer = false;
    public Material tracerMaterial;
    public Color projectileColor;
    public float tracerWidth;
    public float tracerUpdateRate = 0.1f;
    public Light muzzleFlash;
    public GameObject muzzleParticles;
    public float muzzleEffectDuration = .025f;

    public WeaponSettings weaponSettings;
    public bool useGlobalCarriedRounds = false;
    public bool isEnemyWeapon = false;
    [HideInInspector]
    public bool isReloading = false;

    //[HideInInspector]
    //public WeaponSettingsStruct weaponSettings;

    public int currentRound = 30;
    public int weaponCarriedRounds = 100;

    public Vector3 firePosition;
    public Vector3 fireDirection { get; set; }

    public Transform targetTransform;
    public Vector3 targetTransformOffset;

   // [Range(.00001f, .1f)]
    //public float spreadAngleOffset = 2.0f;

    //public bool isFullAuto = false;
    //public float rateOfFire = 0.25f;

    [HideInInspector]
    public bool fired = false;

    float currentTime = 0f;
    float currentMuzzleEffectTime = 0f;
    bool muzzleEffectComplete = false;

    float pullTriggerTime = 0f;
    float releaseTriggerTime = 0f;
    float totalTimeBetweenTriggerPull = 0f;

    Vector3 initialDirection = Vector3.zero;

    // Start is called before the first frame update
    void Start()
    {
        SetWeaponSettings(weaponSettings);

        if (muzzleFlash != null)
        {
            muzzleFlash.transform.localPosition = firePosition;
            muzzleFlash.color = projectileColor;
            muzzleFlash.enabled = false;
        }
    }

    public void PullTrigger(float pullTime)
    {
        pullTriggerTime = pullTime;

        totalTimeBetweenTriggerPull +=  pullTriggerTime - releaseTriggerTime;
        //Debug.Log(totalTimeBetweenTriggerPull + " " + weaponSettings.rateOfFire + " " + (totalTimeBetweenTriggerPull > weaponSettings.rateOfFire));
        if (totalTimeBetweenTriggerPull > weaponSettings.rateOfFire)
        {
            //Debug.Log("trigger reset");
            releaseTriggerTime = 0f;
        }

        if (releaseTriggerTime == 0f)
        {
            fired = true;
            totalTimeBetweenTriggerPull = 0f;

            if(isEnemyWeapon == false)
            {
                if (OnPlayerShootWeapon != null)
                {
                    OnPlayerShootWeapon.Invoke(weaponSettings.projectileAmount);
                }
            }
        }
       
        if(isEnemyWeapon)
        {
            fired = true;
        }
    }

    public void UpdateWeapon(Vector3 direction)
    {
        if (currentTime == 0f)
        {
            if (currentRound > 0 && isReloading == false)
            {
                if (SoundManager.instance != null)
                {
                    SoundManager.instance.PlaySound(weaponSettings.weaponSound);
                }

                for (int i = 0; i < weaponSettings.projectileAmount; i++)
                {
                    Vector3 firePos = transform.TransformVector(firePosition + Random.insideUnitSphere * .1f);
                    fireDirection = direction + Random.onUnitSphere * weaponSettings.spreadAmount;

                    Projectile p = Instantiate(projectile,
                        transform.position + firePos,
                        Quaternion.LookRotation(fireDirection));

                    p.owner = transform;

                    if (targetTransform != null)
                    {
                        p.SetTargetPosition(targetTransform);
                        p.SetTargetPositionOffset(targetTransformOffset);
                    }

                    p.SetVelocity(fireDirection);

                    //p.GetComponentInChildren<MeshRenderer>().material.SetColor("_BaseColor", projectileColor);

                    if (tracerMaterial != null)
                    {
                        p.useTrailRenderer = useTrailRenderer;

                        if (useTrailRenderer)
                        {
                            p.tracerTrail = p.gameObject.AddComponent<TrailRenderer>();
                        }
                        else
                        {
                            p.tracer = p.gameObject.AddComponent<LineRenderer>();
                            p.tracer.useWorldSpace = true;
                        }

                        p.tracerMaterial = tracerMaterial;
                        p.tracerColor = projectileColor;
                        p.tracerWidth = tracerWidth;
                        p.tracerUpdateRate = tracerUpdateRate;

                        if (useTrailRenderer)
                        {
                            p.SetTracerTrailParamaters();
                        }
                        else
                        {
                            p.SetTracerParamaters();
                        }
                    }
                }

                currentRound--;
                if (OnUpdateWeaponEvent != null && isEnemyWeapon == false)
                {
                    OnUpdateWeaponEvent.Invoke(currentRound, weaponCarriedRounds);
                }


                if (isEnemyWeapon == false)
                {
                    if (ShakeCameraEvent != null)
                    {
                        ShakeCameraEvent.Invoke();
                    }
                }

            }
        }

        currentTime += Time.deltaTime;

        if(weaponSettings.fullAuto)
        {
            if (currentTime > weaponSettings.rateOfFire)
            {
                RecycleWeapon();


                if (isEnemyWeapon == false)
                {
                    if (OnPlayerShootWeapon != null)
                    {
                        OnPlayerShootWeapon.Invoke(weaponSettings.projectileAmount);
                    }
                }
            }
        }

        //semi auto weapon fire for enemies
        if (isEnemyWeapon && weaponSettings.fullAuto == false)
        {
            if (currentTime > weaponSettings.rateOfFire)
            {
                ReleaseTrigger(0f);
                PullTrigger(0f);
            }
        }

        if (isReloading == false)
        {
            if (currentMuzzleEffectTime == 0f)
            {
                EnableMuzzleEffects();
            }

            currentMuzzleEffectTime += Time.deltaTime;

            if(muzzleEffectComplete == false)
            {
                if (currentMuzzleEffectTime > muzzleEffectDuration)
                {
                    DisableMuzzleEffects();
                }
                else if (currentRound == 0)
                {
                    DisableMuzzleEffects();
                }
            }

        }
    }

    public void ReleaseTrigger(float releaseTime)
    {
        releaseTriggerTime = releaseTime;

        fired = false;
        RecycleWeapon();

        if (muzzleEffectComplete == false)
        {
            DisableMuzzleEffects();
        }
    }

    void RecycleWeapon()
    {
        currentTime = 0.0f;
        currentMuzzleEffectTime = 0f;

    }

    //public void Fire(Vector3 direction)
    //{
    //    if (currentRound > 0 && isReloading == false)
    //    {
    //        if (fired == false)
    //        {
    //            if (SoundManager.instance != null)
    //            {
    //                SoundManager.instance.PlaySound(weaponSettings.weaponSound);
    //            }

    //            for (int i = 0; i < weaponSettings.projectileAmount; i++)
    //            {
    //                Vector3 firePos = transform.TransformVector(firePosition + Random.insideUnitSphere * .1f);
    //                fireDirection = direction + Random.onUnitSphere * weaponSettings.spreadAmount;

    //                Projectile p = Instantiate(projectile,
    //                    transform.position + firePos,
    //                    Quaternion.LookRotation(fireDirection));

    //                if (targetTransform != null)
    //                {
    //                    p.SetTargetPosition(targetTransform);
    //                    p.SetTargetPositionOffset(targetTransformOffset);
    //                }

    //                p.SetVelocity(fireDirection);

    //                //p.GetComponentInChildren<MeshRenderer>().material.SetColor("_BaseColor", projectileColor);

    //                if (tracerMaterial != null)
    //                {
    //                    p.useTrailRenderer = useTrailRenderer;

    //                    if (useTrailRenderer)
    //                    {
    //                        p.tracerTrail = p.gameObject.AddComponent<TrailRenderer>();
    //                    }
    //                    else
    //                    {
    //                        p.tracer = p.gameObject.AddComponent<LineRenderer>();
    //                        p.tracer.useWorldSpace = true;
    //                    }

    //                    p.tracerMaterial = tracerMaterial;
    //                    p.tracerColor = projectileColor;
    //                    p.tracerWidth = tracerWidth;
    //                    p.tracerUpdateRate = tracerUpdateRate;

    //                    if(useTrailRenderer)
    //                    {
    //                        p.SetTracerTrailParamaters();
    //                    }
    //                    else
    //                    {
    //                        p.SetTracerParamaters();
    //                    }
    //                }
    //            }

    //            fired = true;

    //            currentRound--;
    //            if(OnUpdateWeaponEvent != null && isEnemyWeapon == false)
    //            {
    //                OnUpdateWeaponEvent.Invoke(currentRound, weaponCarriedRounds);
    //            }

    //            if(ShakeCameraEvent != null)
    //            {
    //                if(isEnemyWeapon == false)
    //                {
    //                    ShakeCameraEvent.Invoke();
    //                }
    //            }

    //        }

    //        if (weaponSettings.fullAuto)
    //        {
    //            currentTime += Time.deltaTime;
    //            if (currentTime > weaponSettings.rateOfFire)
    //            {
    //                ResetFire();
    //            }
    //        }

    //        //semi auto weapon fire for enemies
    //        if (isEnemyWeapon && weaponSettings.fullAuto == false)
    //        {
    //            currentTime += Time.deltaTime;
    //            if (currentTime > weaponSettings.rateOfFire * 1.5f)
    //            {
    //                ResetFire();
    //            }
    //        }

    //    }

    //    if(isReloading == false)
    //    {
    //        if (currentMuzzleEffectTime == 0f)
    //        {
    //            EnableMuzzleEffects();
    //        }

    //        currentMuzzleEffectTime += Time.deltaTime;

    //        if (currentMuzzleEffectTime > muzzleEffectDuration)
    //        {
    //            DisableMuzzleEffects();
    //        }

    //        if (currentRound == 0)
    //        {
    //            DisableMuzzleEffects();
    //        }
    //    }

    //}

    //public void ResetFire()
    //{
    //    fired = false;
    //    currentTime = 0.0f;
    //    currentMuzzleEffectTime = 0f;

    //    DisableMuzzleEffects();
    //}

    public void EnableMuzzleEffects()
    {
        //Debug.Log("enable muzzle effects");
        muzzleEffectComplete = false;

        if (muzzleFlash != null)
        {
            muzzleFlash.enabled = true;
        }
        if (muzzleParticles != null)
        {
            muzzleParticles.SetActive(true);
            Vector3 randomScale = Vector3.one * (Random.value + .3f);
            randomScale.x += Random.Range(-Random.value, Random.value);
            randomScale.y += Random.Range(-Random.value, Random.value);
            randomScale.z = 1f;
            muzzleParticles.transform.localScale = randomScale;
        }
    }

    public void DisableMuzzleEffects()
    {
        //Debug.Log("disable muzzle effects");

        muzzleEffectComplete = true;
        if (muzzleFlash != null)
        {
            muzzleFlash.enabled = false;
        }
        if (muzzleParticles != null)
        {
            muzzleParticles.SetActive(false);
        }
    }

    public void ReloadWeapon()
    {
        if(isEnemyWeapon)
        {
            weaponCarriedRounds = 1000;
        }

        if (useGlobalCarriedRounds)
        {
            if (OnGetGlobalCarriedRoundsEvent != null)
            {
                weaponCarriedRounds = OnGetGlobalCarriedRoundsEvent.Invoke();
            }
        }

        if (isReloading == false && currentRound < weaponSettings.maxClip && weaponCarriedRounds > 0)
        {
            SoundManager.instance.PlaySound(weaponSettings.weaponReloadSound);

            StopCoroutine(StartReloadWeapon());
            StartCoroutine(StartReloadWeapon());
        }

    }

    IEnumerator StartReloadWeapon()
    {
        //Debug.Log("reloading");
        isReloading = true;
        yield return new WaitForSeconds(weaponSettings.reloadTime);

        Reload();
        isReloading = false;
        //Debug.Log("reloading complete");
    }

    void Reload()
    {
        int rounds = weaponSettings.maxClip - currentRound;
        if(rounds <= weaponCarriedRounds)
        {
            currentRound += rounds;
            weaponCarriedRounds -= rounds;
        }
        else
        {
            currentRound += weaponCarriedRounds;
            weaponCarriedRounds = 0;
        }

        if (isEnemyWeapon == false)
        {
            if (OnUpdateWeaponEvent != null)
            {
                OnUpdateWeaponEvent.Invoke(currentRound, weaponCarriedRounds);
            }
        }

        if (useGlobalCarriedRounds)
        {
            if (OnSetGlobalCarriedRoundsEvent != null)
            {
                OnSetGlobalCarriedRoundsEvent.Invoke(weaponCarriedRounds);
            }
        }
    }

    public void SetWeaponSettings(WeaponSettings weaponSettingsObject)
    {
        if(weaponSettingsObject != null)
        {
            weaponSettings.weaponName = weaponSettingsObject.weaponName;
            weaponSettings.weaponSound = weaponSettingsObject.weaponSound;
            weaponSettings.weaponReloadSound = weaponSettingsObject.weaponReloadSound;

            weaponSettings.fullAuto = weaponSettingsObject.fullAuto;
            weaponSettings.rateOfFire = weaponSettingsObject.rateOfFire;
            weaponSettings.projectileAmount = weaponSettingsObject.projectileAmount;
            weaponSettings.maxClip = weaponSettingsObject.maxClip;

            weaponSettings.spreadAmount = weaponSettingsObject.spreadAmount;
            weaponSettings.reloadTime = weaponSettingsObject.reloadTime;
            weaponSettings.criticalHitChance = weaponSettingsObject.criticalHitChance;
            weaponSettings.critcalHitMultiplier = weaponSettingsObject.critcalHitMultiplier;

            currentRound = weaponSettings.maxClip;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.TransformVector(firePosition), .1f);

        //Gizmos.color = Color.cyan;
        //Gizmos.DrawLine(firePosition, firePosition + fireDirection);
    }
}
