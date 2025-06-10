using UnityEngine;
using UnityEngine.UI; // For Button
using TMPro;        // For TextMeshPro elements
using System.Collections.Generic; // For List
using UnityEngine.SceneManagement; // For restarting the scene

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Tutorial Flow Panels")]
    [SerializeField] private GameObject tutorialIntroPanel;
    [SerializeField] private GameObject itemRevealPanel;
    [SerializeField] private GameObject readyToStartPanel;
    [SerializeField] private GameObject wildlifeWarningPanel;
    [SerializeField] private GameObject keepTrashWarningPanel;

    [Header("Item Reveal Panel Elements")]
    [SerializeField] private Image itemImageReveal;
    [SerializeField] private TMP_Text itemNameReveal;
    [SerializeField] private TMP_Text itemDescriptionReveal;
    [SerializeField] private Button trashButtonReveal;
    [SerializeField] private Button keepButtonReveal;

    [Header("Tutorial Intro Panel Elements")]
    [SerializeField] private Button startTutorialButton;

    [Header("Ready To Start Panel Elements")]
    [SerializeField] private Button startRealGameButton;

    [Header("Wildlife Warning Panel Elements")]
    [SerializeField] private Button warningOkayButton;

    [Header("Keep Trash Warning Panel Elements")]
    [SerializeField] private Button keepTrashWarningOkayButton;

    [Header("Gameplay UI Elements")]
    [SerializeField] private GameObject scoreDisplayPanel;
    [SerializeField] private GameObject timerDisplayPanel;
    [SerializeField] private TMP_Text timerTextElement;


    [Header("Tutorial Item Data")]
    public List<TutorialItemData> tutorialItems = new List<TutorialItemData>();
    private int itemsProcessedInTutorial = 0;
    [SerializeField] private int tutorialItemsToComplete = 5;
    private TutorialItemData currentlyRevealedItem;
    private int currentTutorialUniqueItemIndex = 0;

    [Header("Game State Control")]
    [SerializeField] private TrashSpawner trashSpawner;
    [SerializeField] private RiverBackgroundGenerator riverGenerator;
    [SerializeField] private SkyscraperSpawner leftSkyscraperSpawner;
    [SerializeField] private SkyscraperSpawner rightSkyscraperSpawner;
    [SerializeField] private ScoreManager scoreManager;
    // [Tooltip("Assign your camera's movement script component here (e.g., drag the Main Camera GameObject here).")] // REMOVED
    // [SerializeField] private MonoBehaviour cameraScrollScript; // REMOVED

    [Header("Game Timer Settings")]
    [SerializeField] private float gameTimeLimit = 60f;
    public float divvyBikeTimeBonus = 5f;
    // [SerializeField] private float naturePhaseTriggerTime = 30f; // REMOVED

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text finalScoreTextElement;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button replayTutorialButton;

    [Header("Inactivity Settings")]
    [SerializeField] private float inactivityTimeout = 300f;
    private float timeSinceLastInput = 0f;


    private float currentTime;
    private bool isGameTimerRunning = false;
    // private bool hasEnteredNaturePhase = false; // REMOVED

    [Header("Camera Settings")]
    [SerializeField] private float cameraStartY = 0f; // Set this to your camera's starting Y position in the Inspector

    [Header("River Settings")]
    [SerializeField] private float riverStartY = 0f; // Set this in the Inspector to your river's starting Y

    [Header("Audio")]
    [SerializeField] private AudioSource musicAudioSource;
    [SerializeField] private AudioSource gameOverAudioSource; // New AudioSource for Game Over music
    [SerializeField] private AudioClip gameOverMusicClip;     // Assign in Inspector

    private bool isMusicMuted = false;

    [SerializeField] private Sprite muteIcon;
    [SerializeField] private Sprite unmuteIcon;
    [SerializeField] private Image muteButtonImage;

    [SerializeField] private Button backButton;

    [SerializeField] private Button skipTutorialButton;

    public enum GamePhase
    {
        None, ShowingTutorialIntro, TutorialPlaying,
        ShowingItemReveal, ShowingWildlifeWarning, ShowingKeepTrashWarning,
        ShowingReadyToStart, MainGamePlaying, /*NaturePhase,*/
        GameOver
    }
    public GamePhase currentPhase = GamePhase.None;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        Debug.Log("TM: Awake() called.");

        if (skipTutorialButton != null) skipTutorialButton.onClick.AddListener(SkipTutorialToReadyPanel);
        else Debug.LogWarning("TM: SkipTutorialButton not assigned!");
    }

    void Start()
    {
        Debug.Log("TM: Start() called.");
        // Null checks
        if (tutorialIntroPanel == null) Debug.LogError("TM: Tutorial Intro Panel not assigned!");
        if (itemRevealPanel == null) Debug.LogError("TM: Item Reveal Panel not assigned!");
        if (readyToStartPanel == null) Debug.LogError("TM: Ready To Start Panel not assigned!");
        if (wildlifeWarningPanel == null) Debug.LogError("TM: Wildlife Warning Panel not assigned!");
        if (keepTrashWarningPanel == null) Debug.LogError("TM: Keep Trash Warning Panel not assigned!");
        if (trashSpawner == null) Debug.LogError("TM: Trash Spawner not assigned!");
        if (scoreManager == null) Debug.LogError("TM: ScoreManager not assigned!");
        if (timerDisplayPanel == null) Debug.LogError("TM: Timer Display Panel not assigned!");
        if (timerTextElement == null) Debug.LogError("TM: Timer Text Element (child of TimerDisplayPanel) not assigned!");
        if (gameOverPanel == null) Debug.LogError("TM: Game Over Panel not assigned!");
        if (scoreDisplayPanel == null) Debug.LogError("TM: Score Display Panel not assigned!");
        if (riverGenerator == null) Debug.LogWarning("TM: RiverBackgroundGenerator not assigned.");
        if (leftSkyscraperSpawner == null) Debug.LogWarning("TM: LeftSkyscraperSpawner not assigned.");
        if (rightSkyscraperSpawner == null) Debug.LogWarning("TM: RightSkyscraperSpawner not assigned.");
        if (replayTutorialButton == null) Debug.LogWarning("TM: Replay Tutorial Button not assigned for Game Over panel.");
        if (warningOkayButton == null) Debug.LogError("TM: Wildlife Warning Okay Button not assigned!");
        if (keepTrashWarningOkayButton == null) Debug.LogError("TM: Keep Trash Warning Okay Button not assigned!");
        if (backButton == null) Debug.LogWarning("TM: BackButton not assigned in Inspector!");
        // if (cameraScrollScript == null) Debug.LogWarning("TM: CameraScrollScript not assigned in Inspector. Camera movement will not be controlled by TutorialManager."); // REMOVED
        // else Debug.Log($"TM: CameraScrollScript assigned: {cameraScrollScript.gameObject.name}, Initial intended enabled state in Start: False"); // REMOVED

        if (gameTimeLimit <= 0) Debug.LogError($"TM CRITICAL: gameTimeLimit in Inspector is {gameTimeLimit}, which is not positive. Game will likely end immediately if started!");

        // Button listeners
        if (startTutorialButton != null) startTutorialButton.onClick.AddListener(StartTutorialGameplay);
        else Debug.LogError("TM CRITICAL: Start Tutorial Button (on TutorialIntroPanel) NOT ASSIGNED in Inspector!");

        if (trashButtonReveal != null) trashButtonReveal.onClick.AddListener(OnTrashButtonClicked); else Debug.LogError("TM: TrashButtonReveal not assigned!");
        if (keepButtonReveal != null) keepButtonReveal.onClick.AddListener(OnKeepButtonClicked); else Debug.LogError("TM: KeepButtonReveal not assigned!");
        if (warningOkayButton != null) warningOkayButton.onClick.AddListener(DismissWildlifeWarning); else Debug.LogError("TM: WarningOkayButton not assigned!");
        if (keepTrashWarningOkayButton != null) keepTrashWarningOkayButton.onClick.AddListener(DismissKeepTrashWarning); else Debug.LogError("TM: KeepTrashWarningOkayButton not assigned!");
        if (startRealGameButton != null) startRealGameButton.onClick.AddListener(EndTutorialAndStartGame); else Debug.LogError("TM: StartRealGameButton not assigned!");
        if (playAgainButton != null) playAgainButton.onClick.AddListener(PrepareToPlayAgain); else Debug.LogWarning("TM: Play Again Button (on GameOverPanel) not assigned!");
        if (replayTutorialButton != null) replayTutorialButton.onClick.AddListener(ReplayTutorial); else Debug.LogWarning("TM: ReplayTutorialButton not assigned!");
        if (backButton != null) backButton.onClick.AddListener(OnBackButtonClicked);

        // Initial UI states
        SetPanelActive(tutorialIntroPanel, false, "TutorialIntroPanel (Start)");
        SetPanelActive(itemRevealPanel, false, "ItemRevealPanel (Start)");
        SetPanelActive(readyToStartPanel, false, "ReadyToStartPanel (Start)");
        SetPanelActive(gameOverPanel, false, "GameOverPanel (Start)");
        SetPanelActive(wildlifeWarningPanel, false, "WildlifeWarningPanel (Start)");
        SetPanelActive(keepTrashWarningPanel, false, "KeepTrashWarningPanel (Start)");
        SetPanelActive(timerDisplayPanel, false, "TimerDisplayPanel (Start)");
        SetPanelActive(scoreDisplayPanel, false, "ScoreDisplayPanel (Start)");

        // Initialize game state
        currentPhase = GamePhase.None;
        timeSinceLastInput = 0f;
        isGameTimerRunning = false;
        currentTime = gameTimeLimit;
        SetGameplayScriptsActive(false, true);
        if(trashSpawner != null) trashSpawner.enabled = false;
        Debug.Log($"TM: Start() finished. Initial currentPhase: {currentPhase}, gameTimeLimit: {gameTimeLimit}, isGameTimerRunning: {isGameTimerRunning}, currentTime: {currentTime}");
        SetBackButtonVisible(true);

        // Automatically start the tutorial sequence
        BeginTutorialSequence();

        if (skipTutorialButton != null)
            skipTutorialButton.gameObject.SetActive(false);
    }

    private void SetPanelActive(GameObject panel, bool isActive, string panelNameForLog = "Panel")
    {
        if (panel != null)
        {
            panel.SetActive(isActive);
            Debug.Log($"TM: SetPanelActive - '{panelNameForLog}' set to {isActive}. Actual activeSelf: {panel.activeSelf}, activeInHierarchy: {panel.activeInHierarchy}");
        }
        else
        {
            Debug.LogWarning($"TM: SetPanelActive - '{panelNameForLog}' is null, cannot set active state.");
        }
    }

    // --- THIS METHOD IS CALLED BY THE "PLAY AGAIN" BUTTON ---
public void PrepareToPlayAgain()
{
    Debug.Log("TM: PrepareToPlayAgain called. Resetting for main game (skipping tutorial).");
    SetPanelActive(gameOverPanel, false, "GameOverPanel (PrepareToPlayAgain)");

    ClearExistingSpawnedItems();

    // Reset critical game state variables
    isGameTimerRunning = false;
    // currentTime will be reset by EndTutorialAndStartGame
    // score will be reset by EndTutorialAndStartGame

    Time.timeScale = 0f; // Pause game for the ReadyToStartPanel

    if (Camera.main != null)
    {
        Camera.main.transform.position = new Vector3(
            Camera.main.transform.position.x,
            10f, // Set Y to 10 explicitly
            Camera.main.transform.position.z
        );
    }

    // Ensure all gameplay scripts are stopped/disabled before showing ReadyToStartPanel
    SetGameplayScriptsActive(false, true); // Disable River & Skyscraper spawners
    if (trashSpawner != null)
    {
        trashSpawner.StopSpawning();
        trashSpawner.enabled = false;
    }
    // Reset skyscraper modes
    if (leftSkyscraperSpawner != null) {
        leftSkyscraperSpawner.HaltSpawning(); // Ensure it's fully reset for ResumeSpawning
    }
    if (rightSkyscraperSpawner != null) {
        rightSkyscraperSpawner.HaltSpawning();
    }

    // Hide gameplay UI elements
    SetPanelActive(timerDisplayPanel, false, "TimerDisplayPanel (PrepareToPlayAgain)");
    SetPanelActive(scoreDisplayPanel, false, "ScoreDisplayPanel (PrepareToPlayAgain)");

    // Show the ReadyToStartPanel
    SetPanelActive(readyToStartPanel, true, "ReadyToStartPanel (PrepareToPlayAgain)");
    currentPhase = GamePhase.ShowingReadyToStart;
    timeSinceLastInput = 0f; // Reset inactivity timer
    Debug.Log("TM: Now showing ReadyToStartPanel. Click its button to start the main game.");

    if (riverGenerator != null)
    {
        riverGenerator.ResetRiver(riverStartY);
    }
    if (leftSkyscraperSpawner != null) leftSkyscraperSpawner.ResumeSpawning();
    if (rightSkyscraperSpawner != null) rightSkyscraperSpawner.ResumeSpawning();
    if (leftSkyscraperSpawner != null)
    {
        leftSkyscraperSpawner.transform.position = new Vector3(
            leftSkyscraperSpawner.transform.position.x,
            riverStartY,
            leftSkyscraperSpawner.transform.position.z
        );
    }
    if (rightSkyscraperSpawner != null)
    {
        rightSkyscraperSpawner.transform.position = new Vector3(
            rightSkyscraperSpawner.transform.position.x,
            riverStartY,
            rightSkyscraperSpawner.transform.position.z
        );
    }

    if (leftSkyscraperSpawner != null) leftSkyscraperSpawner.ForceSpawnNow();
    if (rightSkyscraperSpawner != null) rightSkyscraperSpawner.ForceSpawnNow();

    SetGameplayScriptsActive(true, true);

    // Stop Game Over music and resume main music
    if (gameOverAudioSource != null) gameOverAudioSource.Stop();
    if (musicAudioSource != null && musicAudioSource.clip != null)
    {
        musicAudioSource.loop = true;
        musicAudioSource.Play();
    }
}
// --- END NEW METHOD ---

    // private void SetCameraScrollActive(bool isActive) // REMOVED ENTIRE METHOD
    // {
    //     // ...
    // }

    void Update()
    {
        if (isGameTimerRunning && (currentPhase == GamePhase.MainGamePlaying /*|| currentPhase == GamePhase.NaturePhase*/) )
        {
            currentTime -= Time.deltaTime;
            UpdateTimerDisplay();

            // if (currentPhase == GamePhase.MainGamePlaying && !hasEnteredNaturePhase && currentTime <= naturePhaseTriggerTime)
            // {
            //     EnterNaturePhase();
            // }

            if (currentTime <= 0)
            {
                currentTime = 0;
                UpdateTimerDisplay();
                TriggerGameOver();
            }
        }

        if (currentPhase != GamePhase.ShowingTutorialIntro)
        {
            timeSinceLastInput += Time.unscaledDeltaTime;
            if (Input.anyKey || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2) || Input.touchCount > 0)
            {
                timeSinceLastInput = 0f;
            }
            if (timeSinceLastInput >= inactivityTimeout)
            {
                Debug.Log("TM: Inactivity timeout reached. Loading Title Screen.");
                SceneManager.LoadScene("TitleScreen"); // Loads the correct title screen scene
            }
        }
        else
        {
            timeSinceLastInput = 0f;
        }

        // --- NEW: Resume main music after Game Over soundtrack finishes ---
        if (currentPhase == GamePhase.GameOver && gameOverAudioSource != null && musicAudioSource != null)
        {
            // Only start main music if ending music is done and main music is NOT playing
            if (!gameOverAudioSource.isPlaying && !musicAudioSource.isPlaying)
            {
                musicAudioSource.loop = true;
                musicAudioSource.Play();
            }
        }
        // --- END NEW ---
    }

    private void UpdateTimerDisplay()
    {
        if (timerTextElement != null)
        {
            timerTextElement.text = "Time: " + Mathf.Max(0, Mathf.CeilToInt(currentTime));
        }
    }

    public void AddTimeClock(float timeToAdd)
    {
        if (isGameTimerRunning && (currentPhase == GamePhase.MainGamePlaying /*|| currentPhase == GamePhase.NaturePhase*/))
        {
            currentTime += timeToAdd;
            UpdateTimerDisplay();
            Debug.Log($"Added {timeToAdd}s to clock. New time: {currentTime}");
        }
        else
        {
            Debug.LogWarning($"TM: AddTimeToClock called, but conditions not met. TimerRunning: {isGameTimerRunning}, Phase: {currentPhase}");
        }
    }

    public void BeginTutorialSequence()
    {
        Debug.Log("TM: BeginTutorialSequence called. Setting phase to ShowingTutorialIntro.");
        SetPanelActive(tutorialIntroPanel, true, "TutorialIntroPanel (BeginTutorial)");
        SetBackButtonVisible(true);  // Show back button during tutorial
        currentPhase = GamePhase.ShowingTutorialIntro;
        timeSinceLastInput = 0f;

        // Enable skyscraper spawners
        if (leftSkyscraperSpawner != null)
        {
            leftSkyscraperSpawner.enabled = true;
            leftSkyscraperSpawner.ResumeSpawning();
        }
        if (rightSkyscraperSpawner != null)
        {
            rightSkyscraperSpawner.enabled = true;
            rightSkyscraperSpawner.ResumeSpawning();
        }
    }

    public void StartTutorialGameplay()
    {
        Debug.Log("TM: StartTutorialGameplay called.");
        SetPanelActive(tutorialIntroPanel, false, "TutorialIntroPanel (StartTutorialGameplay)");
        currentPhase = GamePhase.TutorialPlaying;
        SetBackButtonVisible(true);  // Keep back button visible during tutorial gameplay
        timeSinceLastInput = 0f;
        Time.timeScale = 1f;  // Ensure game is running at normal speed

        // Show skip button during tutorial gameplay
        if (skipTutorialButton != null)
            skipTutorialButton.gameObject.SetActive(true);

        // Enable trash spawner and set up first tutorial item
        if (trashSpawner != null)
        {
            trashSpawner.enabled = true;
            if (tutorialItems != null && tutorialItems.Count > 0)
            {
                currentTutorialUniqueItemIndex = 0;
                trashSpawner.SetCurrentTutorialItem(tutorialItems[0]);
                trashSpawner.StartTutorialSpawning(tutorialItems[0]);  // Start the tutorial spawning
                Debug.Log($"TM: Setting first tutorial item: {tutorialItems[0].itemName}");
            }
            else
            {
                Debug.LogError("TM: No tutorial items available to start tutorial!");
            }
        }
        else
        {
            Debug.LogError("TM: TrashSpawner not assigned!");
        }

        // Enable gameplay scripts
        SetGameplayScriptsActive(true, true);
    }

    public void ShadowClicked(TutorialItemData itemData)
    {
        if (currentPhase != GamePhase.TutorialPlaying) {
            Debug.LogWarning($"TM: ShadowClicked called in wrong phase: {currentPhase}");
            return;
        }
        currentlyRevealedItem = itemData;
        currentPhase = GamePhase.ShowingItemReveal;
        Time.timeScale = 0f;
        Debug.Log($"TM: Shadow clicked for {itemData.itemName}. Setting phase to ShowingItemReveal. Time.timeScale = 0");
        timeSinceLastInput = 0f;

        if (itemImageReveal != null) itemImageReveal.sprite = itemData.revealedSprite;
        if (itemNameReveal != null) itemNameReveal.text = itemData.itemName;
        if (itemDescriptionReveal != null) itemDescriptionReveal.text = itemData.itemDescription;

        SetPanelActive(itemRevealPanel, true, "ItemRevealPanel (ShadowClicked)");
        SetPanelActive(wildlifeWarningPanel, false, "WildlifeWarningPanel (ShadowClicked)");
        SetPanelActive(keepTrashWarningPanel, false, "KeepTrashWarningPanel (ShadowClicked)");
    }

    public void OnTrashButtonClicked()
    {
        if (currentPhase != GamePhase.ShowingItemReveal || currentlyRevealedItem == null) return;
        Debug.Log($"TM: OnTrashButtonClicked for: {currentlyRevealedItem.itemName}");
        if (currentlyRevealedItem.isWildlife) {
            SetPanelActive(wildlifeWarningPanel, true, "WildlifeWarningPanel (OnTrashButtonClicked)");
            currentPhase = GamePhase.ShowingWildlifeWarning; Time.timeScale = 0f;
        } else { ProgressTutorial(); }
    }

    public void OnKeepButtonClicked()
    {
        if (currentPhase != GamePhase.ShowingItemReveal || currentlyRevealedItem == null) return;
        Debug.Log($"TM: OnKeepButtonClicked for: {currentlyRevealedItem.itemName}");
        if (currentlyRevealedItem.isWildlife) { ProgressTutorial(); }
        else {
            SetPanelActive(keepTrashWarningPanel, true, "KeepTrashWarningPanel (OnKeepButtonClicked)");
            currentPhase = GamePhase.ShowingKeepTrashWarning; Time.timeScale = 0f;
        }
    }

    public void DismissWildlifeWarning()
    {
        if (currentPhase != GamePhase.ShowingWildlifeWarning) return;
        Debug.Log("TM: Dismissing wildlife warning. Returning to ShowingItemReveal.");
        SetPanelActive(wildlifeWarningPanel, false, "WildlifeWarningPanel (DismissWildlifeWarning)");
        currentPhase = GamePhase.ShowingItemReveal; Time.timeScale = 0f; timeSinceLastInput = 0f;
    }

    public void DismissKeepTrashWarning()
    {
        if (currentPhase != GamePhase.ShowingKeepTrashWarning) return;
        Debug.Log("TM: Dismissing keep trash warning. Returning to ShowingItemReveal.");
        SetPanelActive(keepTrashWarningPanel, false, "KeepTrashWarningPanel (DismissKeepTrashWarning)");
        currentPhase = GamePhase.ShowingItemReveal; Time.timeScale = 0f; timeSinceLastInput = 0f;
    }

    private void ProgressTutorial()
    {
        SetPanelActive(itemRevealPanel, false, "ItemRevealPanel (ProgressTutorial)");
        itemsProcessedInTutorial++;
        currentlyRevealedItem = null;
        timeSinceLastInput = 0f;
        Debug.Log($"TM: Item choice processed. Items: {itemsProcessedInTutorial}/{tutorialItemsToComplete}");

        if (itemsProcessedInTutorial >= tutorialItemsToComplete)
        {
            Debug.Log("TM: Tutorial items complete. Setting phase to ShowingReadyToStart.");
            currentPhase = GamePhase.ShowingReadyToStart;
            SetPanelActive(readyToStartPanel, true, "ReadyToStartPanel (ProgressTutorial)");
            Time.timeScale = 0f;
            if (trashSpawner != null) { trashSpawner.StopSpawning(); trashSpawner.enabled = false; }
            SetGameplayScriptsActive(false, true);
            // SetCameraScrollActive(false); // REMOVED
        }
        else
        {
            Debug.Log("TM: Continuing tutorial. Setting phase to TutorialPlaying.");
            currentPhase = GamePhase.TutorialPlaying;
            Time.timeScale = 1f;
            // SetCameraScrollActive(false); // REMOVED
            currentTutorialUniqueItemIndex++;
            if (tutorialItems != null && tutorialItems.Count > 0) {
                int itemIndexToSpawn = currentTutorialUniqueItemIndex % tutorialItems.Count;
                if (trashSpawner != null && trashSpawner.enabled) {
                    trashSpawner.SetCurrentTutorialItem(tutorialItems[itemIndexToSpawn]);
                    Debug.Log($"TM: Next tutorial item focus: {tutorialItems[itemIndexToSpawn].itemName}.");
                } else if (trashSpawner != null && !trashSpawner.enabled) {
                    Debug.LogWarning("TM: TrashSpawner is disabled, cannot set next tutorial item.");
                }
            } else { Debug.LogError("TM: No tutorial items in list to continue!");}
        }
    }

    public void EndTutorialAndStartGame()
    {
        Debug.Log("TM: EndTutorialAndStartGame called.");
        if (skipTutorialButton != null)
            skipTutorialButton.gameObject.SetActive(false);
        SetPanelActive(readyToStartPanel, false, "ReadyToStartPanel (EndTutorialAndStartGame)");
        currentPhase = GamePhase.MainGamePlaying;
        SetBackButtonVisible(true);  // Keep back button visible during main game
        timeSinceLastInput = 0f;
        isGameTimerRunning = true;
        currentTime = gameTimeLimit;
        Time.timeScale = 1f;  // Ensure game is running at normal speed
        SetGameplayScriptsActive(true, true);
        
        // Start real game spawning
        if (trashSpawner != null)
        {
            trashSpawner.enabled = true;
            trashSpawner.StartRealGameSpawning();
            Debug.Log("TM: Started real game spawning");
        }
        else
        {
            Debug.LogError("TM: TrashSpawner not assigned!");
        }
        
        SetPanelActive(timerDisplayPanel, true, "TimerDisplayPanel (EndTutorialAndStartGame)");
        SetPanelActive(scoreDisplayPanel, true, "ScoreDisplayPanel (EndTutorialAndStartGame)");
    }

    private void TriggerGameOver()
    {
        Debug.Log($"TM: !!! TriggerGameOver CALLED. CurrentTime: {currentTime}, CurrentPhase: {currentPhase}, isGameTimerRunning: {isGameTimerRunning}, Time.timeScale: {Time.timeScale} !!!");
        if (currentPhase == GamePhase.GameOver) {
            Debug.Log("TM: TriggerGameOver - Already in GameOver phase. Exiting to prevent re-trigger.");
            return;
        }
        currentPhase = GamePhase.GameOver;
        isGameTimerRunning = false;
        Time.timeScale = 0f;
        Debug.Log("TM: Game Over! Phase set. Time.timeScale = 0");
        timeSinceLastInput = 0f;

        // SetCameraScrollActive(false); // REMOVED
        SetGameplayScriptsActive(false, true);
        if (trashSpawner != null) { trashSpawner.StopSpawning(); trashSpawner.enabled = false; }

        if (leftSkyscraperSpawner != null) leftSkyscraperSpawner.HaltSpawning();
        if (rightSkyscraperSpawner != null) rightSkyscraperSpawner.HaltSpawning();

        SetPanelActive(gameOverPanel, true, "GameOverPanel (TriggerGameOver)");
        SetBackButtonVisible(true);

        SetPanelActive(timerDisplayPanel, false, "TimerDisplayPanel (TriggerGameOver)");
        SetPanelActive(scoreDisplayPanel, false, "ScoreDisplayPanel (TriggerGameOver)");

        if (finalScoreTextElement != null && scoreManager != null)
        {
            finalScoreTextElement.text = "Final Score: " + scoreManager.GetCurrentScore();
        }

        // Stop main music and play Game Over music
        if (musicAudioSource != null) {
            musicAudioSource.Stop();
            Debug.Log("Main music stopped in TriggerGameOver.");
        }
        if (gameOverAudioSource != null && gameOverMusicClip != null)
        {
            gameOverAudioSource.clip = gameOverMusicClip;
            gameOverAudioSource.loop = false;
            gameOverAudioSource.Play();
            gameOverAudioSource.mute = isMusicMuted;
        }
    }

    private void RestartGame()
    {
        Debug.Log("TM: Restarting Game (reloading scene)...");
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReplayTutorial()
    {
        Debug.Log("TM: ReplayTutorial called.");
        if (leftSkyscraperSpawner != null) leftSkyscraperSpawner.enabled = true;
        if (rightSkyscraperSpawner != null) rightSkyscraperSpawner.enabled = true;
        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(
                Camera.main.transform.position.x,
                cameraStartY,
                Camera.main.transform.position.z
            );
        }
        ClearExistingSpawnedItems();
        SetPanelActive(gameOverPanel, false, "GameOverPanel (ReplayTutorial)");
        if (scoreManager != null) scoreManager.ResetScore();
        SetPanelActive(scoreDisplayPanel, false, "ScoreDisplayPanel (ReplayTutorial)");
        SetPanelActive(timerDisplayPanel, false, "TimerDisplayPanel (ReplayTutorial)");
        isGameTimerRunning = false;
        // hasEnteredNaturePhase = false; // REMOVED
        if (leftSkyscraperSpawner != null) {leftSkyscraperSpawner.ResumeSpawning();}
        if (rightSkyscraperSpawner != null) {rightSkyscraperSpawner.ResumeSpawning();}
        BeginTutorialSequence(); // This will handle SetCameraScrollActive(false) if it were still there
        timeSinceLastInput = 0f;

        // Stop Game Over music and resume main music
        if (gameOverAudioSource != null) gameOverAudioSource.Stop();
        if (musicAudioSource != null && musicAudioSource.clip != null)
        {
            musicAudioSource.loop = true;
            musicAudioSource.Play();
        }
    }

    public void GoToStartMenu()
    {
        Debug.Log("TM: GoToStartMenu called.");
        Time.timeScale = 0f;
        isGameTimerRunning = false;
        ClearExistingSpawnedItems();
        SetPanelActive(tutorialIntroPanel, false, "TutorialIntroPanel (GoToStartMenu)");
        SetPanelActive(itemRevealPanel, false, "ItemRevealPanel (GoToStartMenu)");
        SetPanelActive(readyToStartPanel, false, "ReadyToStartPanel (GoToStartMenu)");
        SetPanelActive(gameOverPanel, false, "GameOverPanel (GoToStartMenu)");
        SetPanelActive(wildlifeWarningPanel, false, "WildlifeWarningPanel (GoToStartMenu)");
        SetPanelActive(keepTrashWarningPanel, false, "KeepTrashWarningPanel (GoToStartMenu)");
        SetPanelActive(timerDisplayPanel, false, "TimerDisplayPanel (GoToStartMenu)");
        SetPanelActive(scoreDisplayPanel, false, "ScoreDisplayPanel (GoToStartMenu)");

        SetPanelActive(tutorialIntroPanel, true, "TutorialIntroPanel (GoToStartMenu)");
        SetBackButtonVisible(true);

        SetGameplayScriptsActive(false, true);
        // SetCameraScrollActive(false); // REMOVED
        if (trashSpawner != null) { trashSpawner.StopSpawning(); trashSpawner.enabled = false; }
        if (leftSkyscraperSpawner != null) leftSkyscraperSpawner.HaltSpawning();
        if (rightSkyscraperSpawner != null) rightSkyscraperSpawner.HaltSpawning();

        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(
                Camera.main.transform.position.x,
                cameraStartY, // Reset to original starting Y
                Camera.main.transform.position.z
            );
        }
        if (leftSkyscraperSpawner != null)
        {
            leftSkyscraperSpawner.transform.position = new Vector3(
                leftSkyscraperSpawner.transform.position.x,
                riverStartY,
                leftSkyscraperSpawner.transform.position.z
            );
        }
        if (rightSkyscraperSpawner != null)
        {
            rightSkyscraperSpawner.transform.position = new Vector3(
                rightSkyscraperSpawner.transform.position.x,
                riverStartY,
                rightSkyscraperSpawner.transform.position.z
            );
        }

        currentPhase = GamePhase.ShowingTutorialIntro;
        timeSinceLastInput = 0f;
        // hasEnteredNaturePhase = false; // REMOVED
    }

    private void ClearExistingSpawnedItems()
    {
        Debug.Log("TM: Clearing existing spawned items by component type...");
        TutorialShadowItem[] tutorialShadows = FindObjectsByType<TutorialShadowItem>(FindObjectsSortMode.None);
        foreach (TutorialShadowItem shadowScript in tutorialShadows)
        {
            Destroy(shadowScript.gameObject);
        }
        Debug.Log($"TM: Destroyed {tutorialShadows.Length} tutorial shadow items.");
        TrashItem[] realTrashItems = FindObjectsByType<TrashItem>(FindObjectsSortMode.None);
        foreach (TrashItem trashScript in realTrashItems)
        {
            Destroy(trashScript.gameObject);
        }
        Debug.Log($"TM: Destroyed {realTrashItems.Length} real trash items.");
    }

    private void SetGameplayScriptsActive(bool isActive, bool includeBackgroundGenerators)
    {
        Debug.Log($"TM: SetGameplayScriptsActive called. isActive: {isActive}, includeBackgrounds: {includeBackgroundGenerators}");
        if (includeBackgroundGenerators)
        {
            if (riverGenerator != null) {
                riverGenerator.enabled = isActive;
                Debug.Log($"-- RiverGenerator.enabled set to {isActive}. Actual: {(riverGenerator != null ? riverGenerator.enabled.ToString() : "NULL")}");
            }
            if (leftSkyscraperSpawner != null) {
                leftSkyscraperSpawner.enabled = isActive;
                if(isActive && leftSkyscraperSpawner != null) leftSkyscraperSpawner.ResumeSpawning(); else if (leftSkyscraperSpawner != null) leftSkyscraperSpawner.HaltSpawning();
                Debug.Log($"-- LeftSkyscraperSpawner.enabled set to {isActive}. Actual: {(leftSkyscraperSpawner != null ? leftSkyscraperSpawner.enabled.ToString() : "NULL")}");
            }
            if (rightSkyscraperSpawner != null) {
                rightSkyscraperSpawner.enabled = isActive;
                if(isActive && rightSkyscraperSpawner != null) rightSkyscraperSpawner.ResumeSpawning(); else if (rightSkyscraperSpawner != null) rightSkyscraperSpawner.HaltSpawning();
                Debug.Log($"-- RightSkyscraperSpawner.enabled set to {isActive}. Actual: {(rightSkyscraperSpawner != null ? rightSkyscraperSpawner.enabled.ToString() : "NULL")}");
            }
        }
        if (trashSpawner != null && !isActive)
        {
            trashSpawner.enabled = false;
            Debug.Log($"-- TrashSpawner.enabled set to {isActive} (via global deactivation). Actual: {trashSpawner.enabled}");
        }
    }

    public void ToggleMusicMute()
    {
        isMusicMuted = !isMusicMuted;
        if (musicAudioSource != null)
            musicAudioSource.mute = isMusicMuted;
        if (gameOverAudioSource != null)
            gameOverAudioSource.mute = isMusicMuted;

        if (muteButtonImage != null)
            muteButtonImage.sprite = isMusicMuted ? muteIcon : unmuteIcon;
    }

    public void OnBackButtonClicked()
    {
        Debug.Log("TM: Back button clicked - returning to title screen");
        
        // Stop all game systems
        Time.timeScale = 0f;
        isGameTimerRunning = false;
        ClearExistingSpawnedItems();
        
        // Stop all audio
        if (musicAudioSource != null) musicAudioSource.Stop();
        if (gameOverAudioSource != null) gameOverAudioSource.Stop();
        
        // Disable all gameplay systems
        SetGameplayScriptsActive(false, true);
        if (trashSpawner != null) { 
            trashSpawner.StopSpawning(); 
            trashSpawner.enabled = false; 
        }
        
        // Load the title screen scene
        SceneManager.LoadScene("TitleScreen");
    }

    private void SetBackButtonVisible(bool isVisible)
    {
        Debug.Log($"[SetBackButtonVisible] Called with isVisible={isVisible} at phase {currentPhase}. backButton is {(backButton != null ? "assigned" : "null")}");
        if (backButton != null)
        {
            backButton.gameObject.SetActive(isVisible);
            Debug.Log($"[SetBackButtonVisible] backButton.gameObject.activeSelf is now: {backButton.gameObject.activeSelf}");
        }
        else
        {
            Debug.LogWarning("[SetBackButtonVisible] backButton reference is null!");
        }
    }

    private void SkipTutorialToReadyPanel()
    {
        if (skipTutorialButton != null)
            skipTutorialButton.gameObject.SetActive(false);
        // Deactivate all tutorial-related panels
        SetPanelActive(tutorialIntroPanel, false, "TutorialIntroPanel (SkipTutorial)");
        SetPanelActive(itemRevealPanel, false, "ItemRevealPanel (SkipTutorial)");
        SetPanelActive(wildlifeWarningPanel, false, "WildlifeWarningPanel (SkipTutorial)");
        SetPanelActive(keepTrashWarningPanel, false, "KeepTrashWarningPanel (SkipTutorial)");
        SetPanelActive(gameOverPanel, false, "GameOverPanel (SkipTutorial)");
        // Move camera forward by 10 units on the y-axis
        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(
                Camera.main.transform.position.x,
                Camera.main.transform.position.y + 10f,
                Camera.main.transform.position.z
            );
        }
        // Show the ReadyToStartPanel and set phase
        currentPhase = GamePhase.ShowingReadyToStart;
        SetPanelActive(readyToStartPanel, true, "ReadyToStartPanel (SkipTutorial)");
        Time.timeScale = 0f;
        if (trashSpawner != null) { trashSpawner.StopSpawning(); trashSpawner.enabled = false; }
        SetGameplayScriptsActive(false, true);
    }
}
