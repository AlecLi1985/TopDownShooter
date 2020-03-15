using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GUIManager : MonoBehaviour
{
    public static event Action OnFadeBlackMaskCompleteEvent;

    public GameManager gameManager;

    [Header("In-game UI")]
    public GameObject gameHUD;
    public Image mouseCrosshair;
    public float showNotificationPanelDuration;
    public TMP_Text healthText;
    public TMP_Text currentRoundsText;
    public TMP_Text carriedRoundsText;
    public TMP_Text weaponNameText;
    public GameObject notificationPanel;
    public TMP_Text notificationText;
    public GameObject bossInfoPanel;
    public Slider bossHealthSlider;
    public GameObject selfDestructPanel;
    public TMP_Text selfDestructTimeText;

    public Image blackMask;
    public float fadeBlackMaskDelay;
    public float fadeBlackMaskDuration;

    public Image damageMask;
    public float fadeDamageMaskDuration;

    [Header("Background")]
    public GameObject backgroundPanel;
    public CanvasGroup backgroundCanvasGroup;

    [Header("Pause Menu")]
    public GameObject pauseMenu;
    public GameObject pauseOptionsGroup;
    public GameObject quitVerificationGroup;

    [Header("Game Won")]
    public GameObject gameWonCanvas;
    public GameObject missionAccomplished;
    public GameObject TimeToCompleteText;
    public TMP_Text TimeToCompleteTimeText;
    public GameObject ShotsFiredText;
    public TMP_Text ShotsFiredNumberText;
    public GameObject AccuracyText;
    public TMP_Text AccuracyPercentText;
    public GameObject mainMenuButton;

    public Color mouseDefaultColor;
    public Color mouseOverEnemyColor;

    Vector3 mousePos;

    float blackMaskAlpha = 0f;
    Color blackMaskColor;

    float damageMaskAlpha = 0f;
    Color damageMaskColor;

    // Start is called before the first frame update
    void Start()
    {
        HideCursor();

        mouseCrosshair.gameObject.SetActive(true);
        mouseCrosshair.rectTransform.localPosition = Vector3.zero;



        if (gameHUD != null)
        {
            gameHUD.SetActive(true);
        }

        if(backgroundPanel != null)
        {
            HideBackgroundPanel();
        }

        if (gameManager != null)
        {
            if (healthText != null)
            {
                healthText.text = gameManager.player.playerHealth.ToString();

                PlayerController.OnPlayerDamagedEvent -= UpdateHealthText;
                PlayerController.OnPlayerDamagedEvent += UpdateHealthText;
                PlayerController.OnPlayerDamagedEvent -= AnimateDamageMask;
                PlayerController.OnPlayerDamagedEvent += AnimateDamageMask;
            }

            if (currentRoundsText != null)
            {
                currentRoundsText.text = gameManager.player.weapon.currentRound.ToString();

                Weapon.OnUpdateWeaponEvent -= UpdateCurrentRoundsText;
                Weapon.OnUpdateWeaponEvent += UpdateCurrentRoundsText;
            }

            if (carriedRoundsText != null)
            {
                carriedRoundsText.text = gameManager.player.weapon.weaponCarriedRounds.ToString();

                Weapon.OnUpdateWeaponEvent -= UpdateCarriedRoundsText;
                Weapon.OnUpdateWeaponEvent += UpdateCarriedRoundsText;

                GameManager.OnUpdateGlobalCarriedRoundsEvent -= UpdateCarriedRoundsText;
                GameManager.OnUpdateGlobalCarriedRoundsEvent += UpdateCarriedRoundsText;

            }

            if (weaponNameText != null)
            {
                weaponNameText.text = gameManager.player.weapon.weaponSettings.weaponName;
            }

            GameManager.OnSwitchWeaponEvent -= UpdateWeaponInfoText;
            GameManager.OnSwitchWeaponEvent += UpdateWeaponInfoText;

            if (notificationPanel != null)
            {
                if (notificationText != null)
                {
                    GameManager.OnAddAmmoEvent -= UpdateAmmoNotificationText;
                    GameManager.OnAddAmmoEvent += UpdateAmmoNotificationText;
                    GameManager.OnAddWeaponEvent -= UpdateWeaponNotificationText;
                    GameManager.OnAddWeaponEvent += UpdateWeaponNotificationText;
                    GameManager.OnGiveHealthEvent -= UpdateHealthText;
                    GameManager.OnGiveHealthEvent += UpdateHealthText;
                    GameManager.OnUpdateHealthEvent -= UpdateHealthNotificationText;
                    GameManager.OnUpdateHealthEvent += UpdateHealthNotificationText;
                    //GameManager.OnSaveStatusEvent -= UpdateSaveStatusNotificationText;
                    //GameManager.OnSaveStatusEvent += UpdateSaveStatusNotificationText;

                    ConsoleScript.ActivateConsoleEvent -= UpdateClearanceLevelNotificationText;
                    ConsoleScript.ActivateConsoleEvent += UpdateClearanceLevelNotificationText;
                    DoorScript.OnUnlockDoorTriggerEvent -= ClearanceLevelInvalidNotificationText;
                    DoorScript.OnUnlockDoorTriggerEvent += ClearanceLevelInvalidNotificationText;
                }
            }

            if (bossInfoPanel != null)
            {
                bossInfoPanel.SetActive(false);

                if (bossHealthSlider != null)
                {
                    Enemy.OnEnemyHitEvent -= UpdateBossHealthSlider;
                    Enemy.OnEnemyHitEvent += UpdateBossHealthSlider;
                }
            }

            if (pauseMenu != null)
            {
                pauseMenu.SetActive(false);

                if (pauseOptionsGroup != null && quitVerificationGroup != null)
                {
                    GameManager.OnPauseGameEvent -= ShowPauseMenu;
                    GameManager.OnPauseGameEvent += ShowPauseMenu;
                    GameManager.OnUnpauseGameEvent -= HidePauseMenu;
                    GameManager.OnUnpauseGameEvent += HidePauseMenu;
                }
            }

            if (blackMask != null)
            {
                blackMaskColor = blackMask.color;

                PlayerController.OnPlayerDeathEvent -= AnimateBlackMask;
                PlayerController.OnPlayerDeathEvent += AnimateBlackMask;
            }

            GameManager.OnGameWonEvent -= AnimateGameWonPanel;
            GameManager.OnGameWonEvent += AnimateGameWonPanel;

        }

        if(damageMask != null)
        {
            damageMaskColor = damageMask.color;
        }
    }

    // Update is called once per frame
    void Update()
    {
        mousePos = Input.mousePosition;
        mousePos.z = 0;
        mouseCrosshair.rectTransform.position = mousePos;

        if (gameManager.selfDestruct)
        {
            UpdateSelfDestructTimeText();
        }

    }

    public void HideCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.lockState = CursorLockMode.Confined;
        mouseCrosshair.gameObject.SetActive(true);
    }

    public void ShowCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.lockState = CursorLockMode.Confined;
        mouseCrosshair.gameObject.SetActive(false);
    }

    void ShowGameHUD()
    {
        gameHUD.SetActive(true);
    }

    void HideGameHUD()
    {
        gameHUD.SetActive(false);
    }

    void ShowPauseMenu()
    {
        ShowCursor();

        HideGameHUD();
        ShowBackgroundPanel();
        pauseOptionsGroup.SetActive(true);
        quitVerificationGroup.SetActive(false);
        pauseMenu.SetActive(true);
    }

    void HidePauseMenu()
    {
        HideCursor();

        ShowGameHUD();
        HideBackgroundPanel();
        pauseMenu.SetActive(false);
    }

    void ShowBackgroundPanel()
    {
        backgroundPanel.SetActive(true);
    }

    void HideBackgroundPanel()
    {
        backgroundPanel.SetActive(false);
    }

    void UpdateWeaponName(string weaponName)
    {
        weaponNameText.text = weaponName;
    }

    void UpdateHealthText(float playerHealth)
    {
        healthText.text = playerHealth.ToString();
    }

    void UpdateCurrentRoundsText(int currentRounds, int carriedRounds)
    {
        currentRoundsText.text = currentRounds.ToString();
    }

    void UpdateCarriedRoundsText(int currentRounds, int carriedRounds)
    {
        carriedRoundsText.text = carriedRounds.ToString();
    }

    void UpdateWeaponInfoText(string weaponName, int currentRounds, int carriedRounds)
    {
        UpdateWeaponName(weaponName);
        UpdateCurrentRoundsText(currentRounds, carriedRounds);
        UpdateCarriedRoundsText(currentRounds, carriedRounds);
    }

    void UpdateAmmoNotificationText(int addedRounds)
    {
        StopAllCoroutines();
        notificationPanel.SetActive(true);
        notificationText.text = "Picked up " + addedRounds + " rounds";
        StartCoroutine(HideNotificationPanel());
    }

    void UpdateWeaponNotificationText(string weaponName)
    {
        StopAllCoroutines();
        notificationPanel.SetActive(true);
        notificationText.text = "Picked up " + weaponName;
        StartCoroutine(HideNotificationPanel());
    }

    public void ClearanceLevelInvalidNotificationText(int requiredLevel)
    {
        StopAllCoroutines();
        notificationPanel.SetActive(true);
        notificationText.text = "Current Clearance Level " + gameManager.clearanceLevel + " insufficient.\nClearance Level " + requiredLevel + " required";
        StartCoroutine(HideNotificationPanel());
    }

    public void UpdateClearanceLevelNotificationText()
    {
        StopAllCoroutines();
        notificationPanel.SetActive(true);
        notificationText.text = "Clearance Level raised to \nLevel " + gameManager.clearanceLevel;
        StartCoroutine(HideNotificationPanel());
    }

    public void UpdateHealthNotificationText(float healthAmount)
    {
        StopAllCoroutines();
        notificationPanel.SetActive(true);
        notificationText.text = "Healed " + healthAmount + " damage";
        StartCoroutine(HideNotificationPanel());
    }

    public void UpdateSaveStatusNotificationText()
    {
        StopAllCoroutines();
        notificationPanel.SetActive(true);
        notificationText.text = "Checkpoint reached";
        StartCoroutine(HideNotificationPanel());
    }

    IEnumerator HideNotificationPanel()
    {
        yield return new WaitForSeconds(showNotificationPanelDuration);
        notificationPanel.SetActive(false);
    }

    public void ShowSelfDestructPanel()
    {
        selfDestructPanel.SetActive(true);
    }

    public void HideSelfDestructPanel()
    {
        selfDestructPanel.SetActive(false);
    }

    void UpdateSelfDestructTimeText()
    {
        if (selfDestructPanel != null)
        {
            if (selfDestructPanel.activeInHierarchy)
            {
                if (selfDestructTimeText != null)
                {
                    int selfDestructTime = (int)gameManager.currentSelfDestructTimer;
                    if (selfDestructTime > 10)
                    {
                        selfDestructTimeText.text = selfDestructTime.ToString() + " seconds";
                    }
                    else
                    {
                        selfDestructTimeText.text = selfDestructTime.ToString();
                    }
                }
            }
            else
            {
                selfDestructPanel.SetActive(true);
            }
        }
    }

    //void ShowBossInfoPanel()
    //{
    //    bossInfoPanel.SetActive(true);
    //}

    //void HideBossInfoPanel()
    //{
    //    bossInfoPanel.SetActive(false);
    //}

    //void ResetBossHealthSlider()
    //{
    //    bossHealthSlider.value = 1f;
    //}

    void UpdateBossHealthSlider(float bossHealth, float currentBossHealth)
    {
        bossHealthSlider.value = (1f / bossHealth) * currentBossHealth;
    }

    void AnimateDamageMask(float damage)
    {
        StartCoroutine(FadeDamageMask());
    }

    IEnumerator FadeDamageMask()
    {
        float damageMaskCurrentFadeTime;
        for (damageMaskCurrentFadeTime = fadeDamageMaskDuration; damageMaskCurrentFadeTime > 0f; damageMaskCurrentFadeTime -= Time.deltaTime)
        {
            float normalizedTime = (1f / fadeDamageMaskDuration) * damageMaskCurrentFadeTime;
            damageMaskAlpha = Mathf.Clamp(1f - normalizedTime, 0f, 0.5f);
            damageMaskColor.a = damageMaskAlpha;
            damageMask.color = damageMaskColor;

            yield return null;
        }

        damageMaskColor.a = 0f;
        damageMask.color = damageMaskColor;
    }

    void AnimateBlackMask()
    {
        StartCoroutine(FadeBlackMask());
    }

    IEnumerator FadeBlackMask()
    {
        HideGameHUD();
        HideCursor();

        yield return new WaitForSeconds(1f);

        float blackMaskCurrentFadeTime;
        for (blackMaskCurrentFadeTime = 0f; blackMaskCurrentFadeTime < fadeBlackMaskDuration; blackMaskCurrentFadeTime += Time.deltaTime)
        {
            float normalizedTime = (1f / fadeBlackMaskDuration) * blackMaskCurrentFadeTime;
            blackMaskAlpha = normalizedTime;
            blackMaskColor.a = blackMaskAlpha;
            blackMask.color = blackMaskColor;

            yield return null;
        }

        blackMaskColor.a = 1f;
        blackMask.color = blackMaskColor;

        StartCoroutine(LoadCheckpointRoutine());
    }

    IEnumerator LoadCheckpointRoutine()
    {
        yield return new WaitForSeconds(fadeBlackMaskDuration);

        blackMaskColor.a = 0f;
        blackMask.color = blackMaskColor;

        ShowGameHUD();

        if (OnFadeBlackMaskCompleteEvent != null)
        {
            OnFadeBlackMaskCompleteEvent.Invoke();
        }

        StopAllCoroutines();
    }

    void ShowGameWonPanel()
    {
        gameWonCanvas.SetActive(true);
    }

    void HideGameWonPanel()
    {
        gameWonCanvas.SetActive(false);
    }

    void AnimateGameWonPanel()
    {
        StartCoroutine(AnimateGameWonPanelRoutine());
    }

    IEnumerator AnimateGameWonPanelRoutine()
    {
        HideGameHUD();

        float blackMaskCurrentFadeTime;
        for (blackMaskCurrentFadeTime = 0f; blackMaskCurrentFadeTime < fadeBlackMaskDuration; blackMaskCurrentFadeTime += Time.deltaTime)
        {
            float normalizedTime = (1f / fadeBlackMaskDuration) * blackMaskCurrentFadeTime;
            blackMaskAlpha = normalizedTime;
            blackMaskColor.a = blackMaskAlpha;
            blackMask.color = blackMaskColor;

            yield return null;
        }

        blackMaskColor.a = 1f;
        blackMask.color = blackMaskColor;

        yield return new WaitForSeconds(fadeBlackMaskDuration);

        ShowGameWonPanel();

        yield return new WaitForSeconds(.5f);

        TimeSpan ts = TimeSpan.FromSeconds(gameManager.completeTime);

        TimeToCompleteTimeText.text = ts.ToString("m\\:ss\\.fff");
        TimeToCompleteText.SetActive(true);
        TimeToCompleteTimeText.gameObject.SetActive(true);

        yield return new WaitForSeconds(.5f);

        ShotsFiredNumberText.text = gameManager.totalShotsFired.ToString("0");
        ShotsFiredNumberText.gameObject.SetActive(true);
        ShotsFiredText.SetActive(true);

        yield return new WaitForSeconds(.5f);

        AccuracyPercentText.text = gameManager.totalAccuracy.ToString("0.00") + "%";
        AccuracyPercentText.gameObject.SetActive(true);
        AccuracyText.SetActive(true);

        yield return new WaitForSeconds(1f);

        mainMenuButton.SetActive(true);

        ShowCursor();

        StopAllCoroutines();
    }

    private void OnDestroy()
    {
        PlayerController.OnPlayerDamagedEvent -= UpdateHealthText;
        PlayerController.OnPlayerDeathEvent -= AnimateBlackMask;
        PlayerController.OnPlayerDamagedEvent -= AnimateDamageMask;

        Weapon.OnUpdateWeaponEvent -= UpdateCurrentRoundsText;
        Weapon.OnUpdateWeaponEvent -= UpdateCarriedRoundsText;

        GameManager.OnSwitchWeaponEvent -= UpdateWeaponInfoText;
        GameManager.OnUpdateGlobalCarriedRoundsEvent -= UpdateCarriedRoundsText;
        GameManager.OnAddAmmoEvent -= UpdateAmmoNotificationText;
        GameManager.OnAddWeaponEvent -= UpdateWeaponNotificationText;
        GameManager.OnGiveHealthEvent -= UpdateHealthText;
        GameManager.OnUpdateHealthEvent -= UpdateHealthNotificationText;
        GameManager.OnPauseGameEvent -= ShowPauseMenu;
        GameManager.OnUnpauseGameEvent -= HidePauseMenu;
        //GameManager.OnSaveStatusEvent -= UpdateSaveStatusNotificationText;
        GameManager.OnGameWonEvent -= AnimateGameWonPanel;

        Enemy.OnEnemyHitEvent -= UpdateBossHealthSlider;

        ConsoleScript.ActivateConsoleEvent -= UpdateClearanceLevelNotificationText;

        DoorScript.OnUnlockDoorTriggerEvent -= ClearanceLevelInvalidNotificationText;
    }
}
