using Cinemachine;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public struct WeaponEntry
{
    public WeaponSettingsStruct weaponSettings;
    public ProjectileSettingsStruct projectileSettings;

    public int currentRounds;
    public int carriedRounds;
}
[Serializable]
public class WeaponObject
{
    public WeaponSettings weaponSettings;
    public ProjectileSettings projectileSettings;
    public ProjectileBehaviour projectileBehaviour;
    public ProjectileDamage projectileDamage;

    public int currentRounds;
    public int carriedRounds;
}

public class GameManager : MonoBehaviour
{
    public static event Action<string, int, int> OnSwitchWeaponEvent;
    public static event Action<int, int> OnUpdateGlobalCarriedRoundsEvent;
    public static event Action<string> OnAddWeaponEvent;
    public static event Action<int> OnAddAmmoEvent;
    public static event Action<float> OnGiveHealthEvent;
    public static event Action<float> OnUpdateHealthEvent;
    public static event Action OnPauseGameEvent;
    public static event Action OnUnpauseGameEvent;
    public static event Action OnSaveStatusEvent;
    public static event Action<int> OnLoadCheckpointEvent;
    public static event Action OnGameWonEvent;

    public string musicLoop;
    public string ambientLoop;

    public CinemachineBrain brain;
    public CinemachineVirtualCamera playerCam;
    public CinemachineVirtualCamera playerCamZoom;
    public Camera referenceCamera;
    public float cameraShiftAmount = 2f;
    public float cameraShakeAmplitude = 1f;
    public float cameraShakeFrequency = 1f;
    public Light playerFlashlight;
    public float playerFlashlightZoomRangeMultiplier = 2f;

    public GameObject mousePositionObject;

    public int bypassShunts = 0;
    public int clearanceLevel = 0;

    public PlayerController player;
    public int startingWeapons = 1;
    public int weaponSlots = 4;
    public bool useGlobalCarriedRounds = true;
    public int globalCarriedRounds = 0;

    public GameObject allEnemies;

    public List<WeaponSettings> weaponSettingsLibrary;
    public List<ProjectileSettings> projectileSettingsLibrary;
    public List<ProjectileBehaviour> projectileBehaviourLibrary;
    public List<ProjectileDamage> projectileDamageLibrary;

    public List<WeaponObject> weaponObjects;
    public int currentWeaponIndex = 0;

    [HideInInspector]
    public ScriptableObject[] scriptableObjects;

    //save data
    float savedHealth;
    int savedCarriedRounds;
    int savedCurrentWeaponIndex;
    public List<WeaponObject> savedWeaponObjects;

    bool gameOver = false;
    //[HideInInspector]
    public bool gameWon = false;
    [HideInInspector]
    public bool gameComplete = false;

    public float completeTime;
    public float totalShotsFired;
    public float enemiesHit;
    public float totalAccuracy;

    public Transform bossLocation;

    PlayerControls controls;

    Vector3 mousePosition = Vector3.zero;

    bool shakeTheCamera = false;
    float currentCameraShakeTime = 0f; //will reset with each shot
    float currentCameraShakeAmplitude = 0f;
    float currentCameraShakeFrequency = 0f;

    float flashlightDefaultRange;
    float flashlightDefaultInnerSpotAngle;
    float flashlightDefaultOuterSpotAngle;

    //[HideInInspector]
    public bool selfDestruct = false;
    public float selfDestructTimer = 60f;
    [HideInInspector]
    public float currentSelfDestructTimer;

    public GameObject explosionObject;
    public string explosionSound;
    public float minExplosionDelay = 1f;
    public float maxExplosionDelay = 2f;
    public float spawnExplosionRadius = 10f;

    bool startSpawningTheExplosions = false;

    float currentExplosionTime = 0f;
    float currentExplosionDelay = 0f;

    bool pauseGame = false;

    int savePointID = 0;

    private void Awake()
    {
        controls = new PlayerControls();

        controls.Gameplay.Pause.performed += ctx =>
        {
            pauseGame = !pauseGame;

            if (pauseGame)
            {
                PauseGame();
            }
            else
            {
                UnpauseGame();
            }
        };

        controls.Gameplay.CycleWeaponBack.performed += ctx =>
        {
            CycleWeaponDown();
        };

        controls.Gameplay.CycleWeaponForward.performed += ctx =>
        {
            CycleWeaponUp();
        };

        controls.Gameplay.Aiming.performed += ctx =>
        {
            if(brain != null)
            {
                playerCam.m_Priority = 0;
                playerCamZoom.m_Priority = 1;
                player.isAiming = true;
            }
        };

        controls.Gameplay.Aiming.canceled += ctx =>
        {
            if(brain != null)
            {
                playerCam.m_Priority = 1;
                playerCamZoom.m_Priority = 0;
                player.isAiming = false;
            }
        };

    }

    void OnEnable()
    {
        controls.Gameplay.Enable();
    }

    void OnDisable()
    {
        controls.Gameplay.Disable();
    }

    void Start()
    {
        scriptableObjects = FindObjectsOfType<ScriptableObject>();
        Debug.Log(scriptableObjects.Length + " scriptable objects in scene");

        player.weapon.useGlobalCarriedRounds = useGlobalCarriedRounds;

        WeaponObject weaponObject = new WeaponObject();
        weaponObject.weaponSettings = weaponSettingsLibrary[0];
        weaponObject.projectileSettings = projectileSettingsLibrary[0];
        weaponObject.projectileBehaviour = projectileBehaviourLibrary[0];
        weaponObject.projectileDamage = projectileDamageLibrary[0];
        weaponObject.currentRounds = weaponObject.weaponSettings.maxClip;
        weaponObject.carriedRounds = globalCarriedRounds;

        weaponObjects.Add(weaponObject);

        for (int i = 0; i < startingWeapons; i++)
        {
            AddRandomWeaponObject();
        }

        SwitchWeapon();

        if (playerFlashlight != null)
        {
            flashlightDefaultRange = playerFlashlight.range;
            flashlightDefaultInnerSpotAngle = playerFlashlight.innerSpotAngle;
            flashlightDefaultOuterSpotAngle = playerFlashlight.spotAngle;
        }

        savedWeaponObjects = new List<WeaponObject>();
        SaveStatus(0);

        player.isGamePaused = false;

        playerCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain = 0f;
        playerCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_FrequencyGain = 0f;

        playerCamZoom.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain = 0f;
        playerCamZoom.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_FrequencyGain = 0f;

        currentSelfDestructTimer = selfDestructTimer;

        completeTime = 0f;
        totalShotsFired = 0f;
        totalAccuracy = 0f;
        enemiesHit = 0f;

        SoundManager.instance.PlaySound(musicLoop);
        SoundManager.instance.PlaySound(ambientLoop);

        Weapon.ShakeCameraEvent -= ShakeCamera;
        Weapon.ShakeCameraEvent += ShakeCamera;
        Weapon.OnGetGlobalCarriedRoundsEvent -= GetGlobalCarriedRounds;
        Weapon.OnGetGlobalCarriedRoundsEvent += GetGlobalCarriedRounds;
        Weapon.OnSetGlobalCarriedRoundsEvent -= SetGlobalCarriedRounds;
        Weapon.OnSetGlobalCarriedRoundsEvent += SetGlobalCarriedRounds;
        Weapon.OnPlayerShootWeapon -= UpdateShotsFired;
        Weapon.OnPlayerShootWeapon += UpdateShotsFired;

        ContainerScript.ActivateContainerEvent -= GiveWeaponHealthOrAmmo;
        ContainerScript.ActivateContainerEvent += GiveWeaponHealthOrAmmo;

        Enemy.OnPlayerHitEnemyEvent -= UpdateEnemiesHit;
        Enemy.OnPlayerHitEnemyEvent += UpdateEnemiesHit;

        DoorScript.OnEnterDoorTriggerEvent -= GetClearanceLevel;
        DoorScript.OnEnterDoorTriggerEvent += GetClearanceLevel;

        GUIManager.OnFadeBlackMaskCompleteEvent -= LoadCheckpoint;
        GUIManager.OnFadeBlackMaskCompleteEvent += LoadCheckpoint;

        GUIManager.OnFadeBlackMaskCompleteEvent -= GameWon;
        GUIManager.OnFadeBlackMaskCompleteEvent += GameWon;

        //Application.targetFrameRate = 10;

        //Cursor.visible = false;
        //Cursor.lockState = CursorLockMode.Confined;
    }

    private void Update()
    {
        if(Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.M))
        {
            ResetPlayerLocation(bossLocation);
            for (int i = 0; i < 5; i++)
            {
                AddRandomWeaponObject();
            }
            player.playerHealth += 1000;
        }

        if(player.isDead == false)
        {
            if (pauseGame == false)
            {
                if(gameWon == false)
                {
                    completeTime += Time.deltaTime;
                }

                if (gameOver == false)
                {
                    //if (player != null)
                    //{
                    //    if (Input.GetKeyDown(KeyCode.C))
                    //    {
                    //        SaveAmmoState();
                    //        currentWeaponIndex--;
                    //        SwitchWeapon();
                    //    }
                    //    else if (Input.GetKeyDown(KeyCode.V))
                    //    {
                    //        SaveAmmoState();
                    //        currentWeaponIndex++;
                    //        SwitchWeapon();
                    //    }
                    //}

                    //if (brain != null)
                    //{
                    //    if (Input.GetMouseButton(1))
                    //    {
                    //        playerCam.m_Priority = 0;
                    //        playerCamZoom.m_Priority = 1;
                    //        player.isAiming = true;

                    //        //if(playerFlashlight != null)
                    //        //{
                    //        //    playerFlashlight.range = flashlightDefaultRange * playerFlashlightZoomRangeMultiplier;
                    //        //    playerFlashlight.innerSpotAngle = flashlightDefaultInnerSpotAngle * playerFlashlightZoomRangeMultiplier;
                    //        //    playerFlashlight.spotAngle = flashlightDefaultOuterSpotAngle * playerFlashlightZoomRangeMultiplier;
                    //        //}
                    //    }
                    //    else if (Input.GetMouseButtonUp(1))
                    //    {
                    //        playerCam.m_Priority = 1;
                    //        playerCamZoom.m_Priority = 0;
                    //        player.isAiming = false;

                    //        //if (playerFlashlight != null)
                    //        //{
                    //        //    playerFlashlight.range = flashlightDefaultRange;
                    //        //    playerFlashlight.innerSpotAngle = flashlightDefaultInnerSpotAngle ;
                    //        //    playerFlashlight.spotAngle = flashlightDefaultOuterSpotAngle;
                    //        //}
                    //    }
                    //}

                    if (referenceCamera != null && player != null)
                    {
                        Vector3 referenceCameraPosition = player.transform.position;
                        referenceCameraPosition.y = referenceCamera.transform.position.y;
                        referenceCamera.transform.position = referenceCameraPosition;

                        if (mousePositionObject != null)
                        {
                            //jumping through extra hoops because the level is rotated 180 degrees
                            mousePosition = Input.mousePosition;
                            mousePosition.z = cameraShiftAmount;

                            Vector3 mouseWorldPos = referenceCamera.ScreenToWorldPoint(mousePosition);
                            mouseWorldPos.y = player.transform.position.y;
                            mousePositionObject.transform.position = mouseWorldPos;
                        }
                    }

                    if (shakeTheCamera)
                    {
                        currentCameraShakeTime += Time.deltaTime;

                        if (currentCameraShakeTime < player.weapon.weaponSettings.rateOfFire)
                        {
                            currentCameraShakeAmplitude = Mathf.Lerp(currentCameraShakeAmplitude, 0f, currentCameraShakeTime / player.weapon.weaponSettings.rateOfFire);
                            currentCameraShakeFrequency = Mathf.Lerp(currentCameraShakeFrequency, 0f, currentCameraShakeTime / player.weapon.weaponSettings.rateOfFire);

                            playerCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain = currentCameraShakeAmplitude;
                            playerCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_FrequencyGain = currentCameraShakeFrequency;

                            playerCamZoom.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain = currentCameraShakeAmplitude;
                            playerCamZoom.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_FrequencyGain = currentCameraShakeFrequency;
                        }
                        else
                        {
                            shakeTheCamera = false;
                            currentCameraShakeTime = 0f;

                            playerCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain = 0f;
                            playerCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_FrequencyGain = 0f;

                            playerCamZoom.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain = 0f;
                            playerCamZoom.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_FrequencyGain = 0f;
                        }
                    }

                    if (selfDestruct)
                    {
                        if (startSpawningTheExplosions)
                        {
                            SpawnExplosions();
                        }

                        currentSelfDestructTimer -= Time.deltaTime;

                        if (currentSelfDestructTimer <= 0f && gameWon == false)
                        {
                            selfDestruct = false;
                            player.isDead = true;
                        }
                    }

                    if(gameWon && gameComplete == false)
                    {
                        selfDestruct = false;
                        player.isGamePaused = true;
                        player.isGameWon = true;

                        gameComplete = true;

                        allEnemies.SetActive(false);

                        if (OnGameWonEvent != null)
                        {
                            OnGameWonEvent.Invoke();
                        }
                    }
                }
            }
        }

    }

    public void UnpauseGame()
    {
        pauseGame = false;
        player.isGamePaused = false;
        player.weapon.ReleaseTrigger(0f);

        Time.timeScale = 1f;

        if (OnUnpauseGameEvent != null)
        {
            OnUnpauseGameEvent.Invoke();
        }
    }

    public void PauseGame()
    {
        pauseGame = true;
        player.isGamePaused = true;

        Time.timeScale = 0f;

        if(OnPauseGameEvent != null)
        {
            OnPauseGameEvent.Invoke();
        }
    }

    void ShakeCamera()
    {
        shakeTheCamera = true;
        playerCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().ReSeed();
        playerCamZoom.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().ReSeed();

        currentCameraShakeAmplitude = cameraShakeAmplitude * player.weapon.weaponSettings.recoilAmount;
        currentCameraShakeFrequency = cameraShakeFrequency * player.weapon.weaponSettings.recoilAmount;
       
    }

    public void SaveStatus(int saveID)
    {
        savePointID = saveID;

        savedHealth = player.playerHealth;
        savedCarriedRounds = globalCarriedRounds;
        savedCurrentWeaponIndex = currentWeaponIndex;

        savedWeaponObjects.Clear();

        foreach(WeaponObject weapon in weaponObjects)
        {
            WeaponObject savedWeaponObject = new WeaponObject();
            savedWeaponObject.weaponSettings = weapon.weaponSettings;
            savedWeaponObject.projectileSettings = weapon.projectileSettings;
            savedWeaponObject.projectileBehaviour = weapon.projectileBehaviour;
            savedWeaponObject.projectileDamage = weapon.projectileDamage;
            savedWeaponObject.currentRounds = weapon.weaponSettings.maxClip;

            savedWeaponObjects.Add(savedWeaponObject);
        }

        if(OnSaveStatusEvent != null)
        {
            OnSaveStatusEvent.Invoke();
        }
    }

    public void LoadCheckpoint()
    {
        if(gameWon == false)
        {
            player.isDead = false;
            player.isGameOver = false;

            player.playerHealth = savedHealth;
            globalCarriedRounds = savedCarriedRounds;
            currentWeaponIndex = savedCurrentWeaponIndex;

            OnGiveHealthEvent.Invoke(player.playerHealth);
            OnUpdateGlobalCarriedRoundsEvent.Invoke(globalCarriedRounds, globalCarriedRounds);

            weaponObjects.Clear();

            foreach (WeaponObject savedWeapon in savedWeaponObjects)
            {
                weaponObjects.Add(savedWeapon);
            }

            player.weapon.weaponSettings = weaponObjects[savedCurrentWeaponIndex].weaponSettings;
            player.weapon.projectile.projectileSettings = weaponObjects[savedCurrentWeaponIndex].projectileSettings;
            player.weapon.projectile.projectileBehaviour = weaponObjects[savedCurrentWeaponIndex].projectileBehaviour;
            player.weapon.projectile.projectileDamage = weaponObjects[savedCurrentWeaponIndex].projectileDamage;
            player.weapon.currentRound = player.weapon.weaponSettings.maxClip;

            Enemy[] enemies = FindObjectsOfType<Enemy>();
            foreach (Enemy e in enemies)
            {
                e.ResetEnemy();
            }

            if (OnLoadCheckpointEvent != null)
            {
                OnLoadCheckpointEvent.Invoke(savePointID);
            }
        }
    }

    public void ResetPlayerLocation(Transform resetLocation)
    {
        if (referenceCamera != null && player != null)
        {
            player.transform.position = resetLocation.position;
            player.transform.rotation = resetLocation.rotation;

            player.ResetPlayerAnimator();

            Vector3 referenceCameraPosition = player.transform.position;
            referenceCameraPosition.y = referenceCamera.transform.position.y;
            referenceCamera.transform.position = referenceCameraPosition;

            if (mousePositionObject != null)
            {
                //jumping through extra hoops because the level is rotated 180 degrees
                mousePosition = Input.mousePosition;
                mousePosition.z = cameraShiftAmount;

                Vector3 mouseWorldPos = referenceCamera.ScreenToWorldPoint(mousePosition);
                mouseWorldPos.y = .5f;
                mousePositionObject.transform.position = mouseWorldPos;
            }
        }
    }

    void GameWon()
    {
        //calculate some stats 
    }

    void CycleWeaponDown()
    {
        if (player != null)
        {
            SaveAmmoState();
            currentWeaponIndex--;
            SwitchWeapon();
        }
    }

    void CycleWeaponUp()
    {
        if (player != null)
        {
            SaveAmmoState();
            currentWeaponIndex++;
            SwitchWeapon();
        }
    }

    void SaveAmmoState()
    {
        //save state of weapon ammo before switching
        WeaponObject weaponObject = weaponObjects[currentWeaponIndex];
        weaponObject.currentRounds = player.weapon.currentRound;
        weaponObject.carriedRounds = player.weapon.weaponCarriedRounds;
        weaponObjects[currentWeaponIndex] = weaponObject;
    }

    void SwitchWeapon()
    {
        int tempIndex = currentWeaponIndex;

        if (currentWeaponIndex < 0)
            currentWeaponIndex = weaponObjects.Count - 1;

        currentWeaponIndex = currentWeaponIndex % weaponObjects.Count;

        player.weapon.weaponSettings = weaponObjects[currentWeaponIndex].weaponSettings;
        player.weapon.projectile.projectileSettings = weaponObjects[currentWeaponIndex].projectileSettings;
        player.weapon.projectile.projectileBehaviour = weaponObjects[currentWeaponIndex].projectileBehaviour;
        player.weapon.projectile.projectileDamage = weaponObjects[currentWeaponIndex].projectileDamage;

        player.weapon.currentRound = weaponObjects[currentWeaponIndex].currentRounds;

        if (useGlobalCarriedRounds)
        {
            player.weapon.weaponCarriedRounds = globalCarriedRounds;
        }
        else
        {
            player.weapon.weaponCarriedRounds = weaponObjects[currentWeaponIndex].carriedRounds;
        }

        if (OnSwitchWeaponEvent != null)
        {
            OnSwitchWeaponEvent.Invoke(player.weapon.weaponSettings.weaponName, player.weapon.currentRound, player.weapon.weaponCarriedRounds);
        }

        //if (OnUpdateGlobalCarriedRoundsEvent != null)
        //{
        //    OnUpdateGlobalCarriedRoundsEvent.Invoke(globalCarriedRounds, globalCarriedRounds);
        //}
    }

    void AddWeaponObject(WeaponObject weaponObject)
    {
        weaponObjects.Add(weaponObject);
    }

    void AddRandomWeaponObject()
    {
        WeaponObject weaponObject = new WeaponObject();
        weaponObject.weaponSettings = weaponSettingsLibrary[Random.Range(0, weaponSettingsLibrary.Count)];
        weaponObject.projectileSettings = projectileSettingsLibrary[Random.Range(0, projectileSettingsLibrary.Count)];
        weaponObject.projectileBehaviour = projectileBehaviourLibrary[Random.Range(0, projectileBehaviourLibrary.Count)];
        weaponObject.projectileDamage = projectileDamageLibrary[Random.Range(0, projectileDamageLibrary.Count)];
        weaponObject.currentRounds = weaponObject.weaponSettings.maxClip;
        if (useGlobalCarriedRounds)
        {
            weaponObject.carriedRounds = globalCarriedRounds;
        }
        else
        {
            weaponObject.carriedRounds = 0;
        }

        weaponObjects.Add(weaponObject);
    }

    void AddWeaponEntry(WeaponSettings weaponSettingsObject, ProjectileSettings projectileSettingsObject, int currentRounds, int carriedRounds)
    {
        WeaponEntry weaponEntry = new WeaponEntry();

        WeaponSettingsStruct weaponSettings = new WeaponSettingsStruct();

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

        ProjectileSettingsStruct projectileSettings = new ProjectileSettingsStruct();

        projectileSettings.projectileName = projectileSettingsObject.projectileName;

        projectileSettings.minDamage = projectileSettingsObject.minDamage;
        projectileSettings.maxDamage = projectileSettingsObject.maxDamage;

        projectileSettings.minSpeed = projectileSettingsObject.minSpeed;
        projectileSettings.maxSpeed = projectileSettingsObject.maxSpeed;

        projectileSettings.lifeTime = projectileSettingsObject.lifeTime;
        projectileSettings.hitLifeTime = projectileSettingsObject.hitLifeTime; //how long the projectile lasts for after it has hit something

        projectileSettings.penetrationCount = projectileSettingsObject.penetrationCount; //how many extra enemies can this projectile pass through before being destroyed

        projectileSettings.adjustProjectileSphereCast = projectileSettingsObject.adjustProjectileSphereCast;
        projectileSettings.startSphereCastRadius = projectileSettingsObject.startSphereCastRadius;
        projectileSettings.endSphereCastRadius = projectileSettingsObject.endSphereCastRadius;

        projectileSettings.damageType = projectileSettingsObject.damageType;
        projectileSettings.behaviourType = projectileSettingsObject.behaviourType;

        weaponEntry.weaponSettings = weaponSettings;
        weaponEntry.projectileSettings = projectileSettings;
        weaponEntry.currentRounds = currentRounds;
        weaponEntry.carriedRounds = carriedRounds;

        //weapons.Add(weaponEntry);
    }

    void RemoveWeapon()
    {

    }

    void GiveWeaponHealthOrAmmo(bool giveWeapon, bool giveHealth, int specificWeaponID, int minAmmoAmount, int maxAmmoAmount, int healthAmount)
    {
        if (giveWeapon)
        {
            if (specificWeaponID != -1)
            {
                WeaponObject weaponObject = new WeaponObject();
                weaponObject.weaponSettings = weaponSettingsLibrary[specificWeaponID];
                weaponObject.projectileSettings = projectileSettingsLibrary[specificWeaponID];
                weaponObject.projectileBehaviour = projectileBehaviourLibrary[specificWeaponID];
                weaponObject.projectileDamage = projectileDamageLibrary[specificWeaponID];
                weaponObject.currentRounds = weaponObject.weaponSettings.maxClip;
                weaponObject.carriedRounds = globalCarriedRounds;

                weaponObjects.Add(weaponObject);

                SaveStatus(savePointID);
            }
            else
            {
                AddRandomWeaponObject();
            }

            if (OnAddWeaponEvent != null)
            {
                OnAddWeaponEvent.Invoke(weaponObjects[weaponObjects.Count - 1].weaponSettings.weaponName);
            }
        }
        else if (giveHealth)
        {
            player.playerHealth += healthAmount;
            if (OnGiveHealthEvent != null)
            {
                OnGiveHealthEvent.Invoke(player.playerHealth);
            }
            if (OnUpdateHealthEvent != null)
            {
                OnUpdateHealthEvent.Invoke(healthAmount);
            }
        }
        else
        {
            int randomNumRounds = Random.Range(minAmmoAmount, maxAmmoAmount);

            if (useGlobalCarriedRounds)
            {
                int newGlobalCarriedRounds = globalCarriedRounds + randomNumRounds;
                SetGlobalCarriedRounds(newGlobalCarriedRounds);

                if (OnAddAmmoEvent != null)
                {
                    OnAddAmmoEvent.Invoke(randomNumRounds);
                }
            }
            else
            {
                player.weapon.weaponCarriedRounds += randomNumRounds;
            }
        }
    }

    int GetGlobalCarriedRounds()
    {
        return globalCarriedRounds;
    }

    void SetGlobalCarriedRounds(int carriedRounds)
    {
        globalCarriedRounds = carriedRounds;
        if (OnUpdateGlobalCarriedRoundsEvent != null)
        {
            OnUpdateGlobalCarriedRoundsEvent.Invoke(globalCarriedRounds, globalCarriedRounds);
        }
    }

    int GetClearanceLevel()
    {
        return clearanceLevel;
    }

    public void SetClearanceLevel(int level)
    {
        clearanceLevel = level;
    }

    public void BeginSelfDestructSequence()
    {
        selfDestruct = true;
    }

    public void ResetSelfDestructSequence()
    {
        selfDestruct = false;
        currentSelfDestructTimer = selfDestructTimer;
    }

    public void OpenAllDoors()
    {
        DoorScript[] doors = FindObjectsOfType<DoorScript>();
        foreach (DoorScript door in doors)
        {
            door.SetLockPermenantly(false);
            door.OpenPermenantly();
        }
    }

    public void StartSpawningTheExplosions(bool start)
    {
        startSpawningTheExplosions = start;
    }

    public void SpawnExplosions()
    {
        if (currentExplosionTime == 0f)
        {
            ShakeCamera();
            currentExplosionDelay = Random.Range(minExplosionDelay, maxExplosionDelay);

            if (explosionObject != null)
            {
                SoundManager.instance.PlaySound(explosionSound);
                Instantiate(explosionObject, player.transform.position + (Random.insideUnitSphere * spawnExplosionRadius), Quaternion.identity);
            }
        }

        currentExplosionTime += Time.deltaTime;

        if (currentExplosionTime > currentExplosionDelay)
        {
            currentExplosionTime = 0f;
        }
    }

    public void SetGameWon(bool win)
    {
        gameWon = win;
    }

    public void UpdateShotsFired(int shots)
    {
        totalShotsFired += shots;
    }

    public void UpdateEnemiesHit()
    {
        enemiesHit++;
        UpdateAccuracyPercentage();
    }

    public void UpdateAccuracyPercentage()
    {
        totalAccuracy = enemiesHit / totalShotsFired * 100f;
    }

    public void PlaySound(string sound)
    {
        SoundManager.instance.PlaySound(sound);
    }

    public void StopSound(string sound)
    {
        SoundManager.instance.StopSound(sound);
    }

    private void OnDestroy()
    {
        ContainerScript.ActivateContainerEvent -= GiveWeaponHealthOrAmmo;

        Weapon.ShakeCameraEvent -= ShakeCamera;
        Weapon.OnGetGlobalCarriedRoundsEvent -= GetGlobalCarriedRounds;
        Weapon.OnSetGlobalCarriedRoundsEvent -= SetGlobalCarriedRounds;
        Weapon.OnPlayerShootWeapon -= UpdateShotsFired;

        Enemy.OnPlayerHitEnemyEvent -= UpdateEnemiesHit;

        DoorScript.OnEnterDoorTriggerEvent -= GetClearanceLevel;

        GUIManager.OnFadeBlackMaskCompleteEvent -= LoadCheckpoint;
        GUIManager.OnFadeBlackMaskCompleteEvent -= GameWon;

        scriptableObjects = FindObjectsOfType<ScriptableObject>();
        Debug.Log(scriptableObjects.Length + " scriptable objects in scene");
    }
}
