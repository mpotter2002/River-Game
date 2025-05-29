using UnityEngine;
using UnityEngine.UI; // For Button
using TMPro;        // For TextMeshPro elements
using System.Collections.Generic; // For List
using UnityEngine.SceneManagement; // For restarting the scene

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Tutorial Flow Panels")]
    [SerializeField] private GameObject welcomePanel;
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
    [SerializeField] private float naturePhaseTriggerTime = 30f;

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
    private bool hasEnteredNaturePhase = false;


    public enum GamePhase
    {
        None, ShowingWelcome, ShowingTutorialIntro, TutorialPlaying,
        ShowingItemReveal, ShowingWildlifeWarning, ShowingKeepTrashWarning,
        ShowingReadyToStart, MainGamePlaying, NaturePhase,
        GameOver
    }
    public GamePhase currentPhase = GamePhase.None;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        Debug.Log("TM: Awake() called.");
    }

    void Start()
    {
        Debug.Log("TM: Start() called.");
        // Null checks
        if (welcomePanel == null) Debug.LogError("TM: Welcome Panel not assigned!");
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


        // Initial UI states
        SetPanelActive(tutorialIntroPanel, false, "TutorialIntroPanel (Start)");
        SetPanelActive(itemRevealPanel, false, "ItemRevealPanel (Start)");
        SetPanelActive(readyToStartPanel, false, "ReadyToStartPanel (Start)");
        SetPanelActive(gameOverPanel, false, "GameOverPanel (Start)");
        SetPanelActive(wildlifeWarningPanel, false, "WildlifeWarningPanel (Start)");
        SetPanelActive(keepTrashWarningPanel, false, "KeepTrashWarningPanel (Start)");
        SetPanelActive(timerDisplayPanel, false, "TimerDisplayPanel (Start)");
        SetPanelActive(scoreDisplayPanel, false, "ScoreDisplayPanel (Start)");

        currentPhase = GamePhase.ShowingWelcome;
        timeSinceLastInput = 0f;
        isGameTimerRunning = false;
        currentTime = gameTimeLimit;
        hasEnteredNaturePhase = false;

        // SetCameraScrollActive(false); // REMOVED
        SetGameplayScriptsActive(false, true);
        if(trashSpawner != null) trashSpawner.enabled = false;

        Debug.Log($"TM: Start() finished. Initial currentPhase: {currentPhase}, gameTimeLimit: {gameTimeLimit}, isGameTimerRunning: {isGameTimerRunning}, currentTime: {currentTime}");
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
    Debug.Log("TM: PrepareToPlayAgain called. Resetting for main game (skipping tutorial intro).");
    SetPanelActive(gameOverPanel, false, "GameOverPanel (PrepareToPlayAgain)");

    ClearExistingSpawnedItems();

    // Reset critical game state variables
    isGameTimerRunning = false;
    hasEnteredNaturePhase = false;
    // currentTime will be reset by EndTutorialAndStartGame
    // score will be reset by EndTutorialAndStartGame

    Time.timeScale = 0f; // Pause game for the ReadyToStartPanel

    // Ensure all gameplay scripts are stopped/disabled before showing ReadyToStartPanel
    SetGameplayScriptsActive(false, true); // Disable River & Skyscraper spawners
    if (trashSpawner != null)
    {
        trashSpawner.StopSpawning();
        trashSpawner.enabled = false;
    }
    // Reset skyscraper modes
    if (leftSkyscraperSpawner != null) {
        leftSkyscraperSpawner.SwitchToNatureMode(false);
        leftSkyscraperSpawner.HaltSpawning(); // Ensure it's fully reset for ResumeSpawning
    }
    if (rightSkyscraperSpawner != null) {
        rightSkyscraperSpawner.SwitchToNatureMode(false);
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
}
// --- END NEW METHOD ---

    // private void SetCameraScrollActive(bool isActive) // REMOVED ENTIRE METHOD
    // {
    //     // ...
    // }

    void Update()
    {
        if (isGameTimerRunning && (currentPhase == GamePhase.MainGamePlaying || currentPhase == GamePhase.NaturePhase) )
        {
            currentTime -= Time.deltaTime;
            UpdateTimerDisplay();

            if (currentPhase == GamePhase.MainGamePlaying && !hasEnteredNaturePhase && currentTime <= naturePhaseTriggerTime)
            {
                EnterNaturePhase();
            }

            if (currentTime <= 0)
            {
                currentTime = 0;
                UpdateTimerDisplay();
                TriggerGameOver();
            }
        }

        if ((currentPhase == GamePhase.TutorialPlaying || currentPhase == GamePhase.MainGamePlaying || currentPhase == GamePhase.NaturePhase) && Time.timeScale > 0f)
        {
            timeSinceLastInput += Time.unscaledDeltaTime;
            if (Input.anyKey || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2) || Input.touchCount > 0)
            {
                timeSinceLastInput = 0f;
            }
            if (timeSinceLastInput >= inactivityTimeout)
            {
                Debug.Log("TM: Inactivity timeout reached. Returning to Start Menu.");
                GoToStartMenu();
            }
        } else if (currentPhase != GamePhase.ShowingWelcome && currentPhase != GamePhase.None) {
            timeSinceLastInput = 0f;
        }
    }

    private void UpdateTimerDisplay()
    {
        if (timerTextElement != null)
        {
            timerTextElement.text = "Time: " + Mathf.Max(0, Mathf.CeilToInt(currentTime));
        }
    }

    public void AddTimeToClock(float timeToAdd)
    {
        if (isGameTimerRunning && (currentPhase == GamePhase.MainGamePlaying || currentPhase == GamePhase.NaturePhase))
        {
            currentTime += timeToAdd;
            UpdateTimerDisplay();
            Debug.Log($"Added {timeToAdd}s to clock. New time: {currentTime}");
        }
    }

    public void BeginTutorialSequence()
    {
        Debug.Log("TM: BeginTutorialSequence called. Setting phase to ShowingTutorialIntro.");
        SetPanelActive(welcomePanel, false, "WelcomePanel (BeginTutorial)");
        SetPanelActive(tutorialIntroPanel, true, "TutorialIntroPanel (BeginTutorial)");

        if (tutorialIntroPanel != null && tutorialIntroPanel.activeSelf && startTutorialButton != null)
        {
            Debug.Log($"TM: In BeginTutorialSequence - TutorialIntroPanel is active. StartTutorialButton name: '{startTutorialButton.gameObject.name}', IsActive: {startTutorialButton.gameObject.activeSelf}, IsInteractable: {startTutorialButton.interactable}");
        }
        else if (startTutorialButton == null)
        {
            Debug.LogError("TM: In BeginTutorialSequence - startTutorialButton reference is NULL!");
        }

        currentPhase = GamePhase.ShowingTutorialIntro;
        Time.timeScale = 0f;
        SetGameplayScriptsActive(false, true);
        // SetCameraScrollActive(false); // REMOVED
        if (trashSpawner != null) trashSpawner.enabled = false;
        SetPanelActive(scoreDisplayPanel, false, "ScoreDisplayPanel (BeginTutorial)");
        SetPanelActive(timerDisplayPanel, false, "TimerDisplayPanel (BeginTutorial)");
        timeSinceLastInput = 0f;
        hasEnteredNaturePhase = false;
        isGameTimerRunning = false;
    }

    public void StartTutorialGameplay()
    {
        Debug.Log("TM: !!! StartTutorialGameplay method CALLED !!!");
        ClearExistingSpawnedItems();
        SetPanelActive(tutorialIntroPanel, false, "TutorialIntroPanel (StartTutorialGameplay)");
        currentPhase = GamePhase.TutorialPlaying;
        Time.timeScale = 1f;
        Debug.Log("TM: Starting Tutorial Gameplay. Time.timeScale = 1");
        itemsProcessedInTutorial = 0;
        currentTutorialUniqueItemIndex = 0;
        timeSinceLastInput = 0f;
        hasEnteredNaturePhase = false;
        isGameTimerRunning = false;

        if (trashSpawner != null)
        {
            trashSpawner.enabled = true;
            Debug.Log("TM: Enabling TrashSpawner for tutorial.");
            if (tutorialItems != null && tutorialItems.Count > 0)
            {
                trashSpawner.StartTutorialSpawning(tutorialItems[currentTutorialUniqueItemIndex]);
            } else Debug.LogError("TM: TutorialItems list is empty!");
        }
        SetGameplayScriptsActive(true, true);
        // SetCameraScrollActive(false); // REMOVED
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
        Debug.Log("TM: EndTutorialAndStartGame called. Setting phase to MainGamePlaying.");
        ClearExistingSpawnedItems();
        SetPanelActive(readyToStartPanel, false, "ReadyToStartPanel (EndTutorialAndStartGame)");
        currentPhase = GamePhase.MainGamePlaying;
        Time.timeScale = 1f;
        timeSinceLastInput = 0f;
        hasEnteredNaturePhase = false;

        if (scoreManager != null) scoreManager.ResetScore();
        currentTime = gameTimeLimit;
        isGameTimerRunning = true;
        Debug.Log($"TM: currentTime initialized to: {currentTime} (from gameTimeLimit: {gameTimeLimit})");

        SetPanelActive(timerDisplayPanel, true, "TimerDisplayPanel (EndTutorialAndStartGame)");
        SetPanelActive(scoreDisplayPanel, true, "ScoreDisplayPanel (EndTutorialAndStartGame)");
        UpdateTimerDisplay();

        SetGameplayScriptsActive(true, true);
        // SetCameraScrollActive(true); // REMOVED
        if (leftSkyscraperSpawner != null) leftSkyscraperSpawner.ResumeSpawning();
        if (rightSkyscraperSpawner != null) rightSkyscraperSpawner.ResumeSpawning();

        if (trashSpawner != null)
        {
            trashSpawner.enabled = true;
            trashSpawner.StartRealGameSpawning();
        }
    }

    private void EnterNaturePhase()
    {
        Debug.Log("TM: EnterNaturePhase called. Setting phase to NaturePhase.");
        currentPhase = GamePhase.NaturePhase;
        hasEnteredNaturePhase = true;
        // SetCameraScrollActive(true); // REMOVED (camera movement is now independent)
        if (leftSkyscraperSpawner != null) leftSkyscraperSpawner.SwitchToNatureMode(true);
        if (rightSkyscraperSpawner != null) rightSkyscraperSpawner.SwitchToNatureMode(true);
        Debug.Log("TM: Skyscraper spawners switched to Nature mode.");
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

        if (leftSkyscraperSpawner != null) leftSkyscraperSpawner.SwitchToNatureMode(false);
        if (rightSkyscraperSpawner != null) rightSkyscraperSpawner.SwitchToNatureMode(false);

        SetPanelActive(gameOverPanel, true, "GameOverPanel (TriggerGameOver)");
        if (gameOverPanel != null && !gameOverPanel.activeSelf) Debug.LogError("TM CRITICAL: gameOverPanel FAILED to activate in TriggerGameOver!");

        SetPanelActive(timerDisplayPanel, false, "TimerDisplayPanel (TriggerGameOver)");
        SetPanelActive(scoreDisplayPanel, false, "ScoreDisplayPanel (TriggerGameOver)");

        if (finalScoreTextElement != null && scoreManager != null)
        {
            finalScoreTextElement.text = "Final Score: " + scoreManager.GetCurrentScore();
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
        ClearExistingSpawnedItems();
        SetPanelActive(gameOverPanel, false, "GameOverPanel (ReplayTutorial)");
        if (scoreManager != null) scoreManager.ResetScore();
        SetPanelActive(scoreDisplayPanel, false, "ScoreDisplayPanel (ReplayTutorial)");
        SetPanelActive(timerDisplayPanel, false, "TimerDisplayPanel (ReplayTutorial)");
        isGameTimerRunning = false;
        hasEnteredNaturePhase = false;
        if (leftSkyscraperSpawner != null) {leftSkyscraperSpawner.SwitchToNatureMode(false); leftSkyscraperSpawner.ResumeSpawning();}
        if (rightSkyscraperSpawner != null) {rightSkyscraperSpawner.SwitchToNatureMode(false); rightSkyscraperSpawner.ResumeSpawning();}
        BeginTutorialSequence(); // This will handle SetCameraScrollActive(false) if it were still there
        timeSinceLastInput = 0f;
    }

    private void GoToStartMenu()
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

        SetPanelActive(welcomePanel, true, "WelcomePanel (GoToStartMenu)");

        SetGameplayScriptsActive(false, true);
        // SetCameraScrollActive(false); // REMOVED
        if (trashSpawner != null) { trashSpawner.StopSpawning(); trashSpawner.enabled = false; }
        if (leftSkyscraperSpawner != null) leftSkyscraperSpawner.SwitchToNatureMode(false);
        if (rightSkyscraperSpawner != null) rightSkyscraperSpawner.SwitchToNatureMode(false);

        currentPhase = GamePhase.ShowingWelcome;
        timeSinceLastInput = 0f;
        hasEnteredNaturePhase = false;
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
}
