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
    [Tooltip("The parent Panel GameObject that contains the score text and its background.")]
    [SerializeField] private GameObject scoreDisplayPanel;
    [Tooltip("The parent Panel GameObject that contains the timer text and its background.")]
    [SerializeField] private GameObject timerDisplayPanel;
    [Tooltip("UI Text to display the game timer (child of TimerDisplayPanel).")]
    [SerializeField] private TMP_Text timerTextElement;


    [Header("Tutorial Item Data")]
    [Tooltip("Create a list of items that will appear in the tutorial. Ensure 'Is Wildlife' is checked for wildlife items.")]
    public List<TutorialItemData> tutorialItems = new List<TutorialItemData>();
    private int itemsProcessedInTutorial = 0;
    [Tooltip("How many items the player needs to interact with before the tutorial round ends.")]
    [SerializeField] private int tutorialItemsToComplete = 5;
    private TutorialItemData currentlyRevealedItem;

    [Header("Game State Control")]
    [SerializeField] private TrashSpawner trashSpawner;
    [SerializeField] private RiverBackgroundGenerator riverGenerator;
    [SerializeField] private SkyscraperSpawner leftSkyscraperSpawner;
    [SerializeField] private SkyscraperSpawner rightSkyscraperSpawner;
    [SerializeField] private ScoreManager scoreManager;

    [Header("Game Timer Settings")]
    [SerializeField] private float gameTimeLimit = 60f;
    [Tooltip("Time bonus in seconds for collecting a Divvy Bike.")]
    public float divvyBikeTimeBonus = 5f;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text finalScoreTextElement;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button replayTutorialButton;

    [Header("Inactivity Settings")]
    [Tooltip("Time in seconds before game resets to start menu due to inactivity.")]
    [SerializeField] private float inactivityTimeout = 300f; // 5 minutes
    private float timeSinceLastInput = 0f;


    private float currentTime;
    private bool isGameTimerRunning = false;


    public enum GamePhase
    {
        None, ShowingWelcome, ShowingTutorialIntro, TutorialPlaying,
        ShowingItemReveal, ShowingWildlifeWarning, ShowingKeepTrashWarning,
        ShowingReadyToStart, MainGamePlaying, GameOver
    }
    public GamePhase currentPhase = GamePhase.None;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
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


        // Button listeners
        if (startTutorialButton != null) startTutorialButton.onClick.AddListener(StartTutorialGameplay);
        else Debug.LogError("TM: Start Tutorial Button not assigned!");
        if (trashButtonReveal != null) trashButtonReveal.onClick.AddListener(OnTrashButtonClicked);
        else Debug.LogError("TM: Trash Button Reveal not assigned!");
        if (keepButtonReveal != null) keepButtonReveal.onClick.AddListener(OnKeepButtonClicked);
        else Debug.LogError("TM: Keep Button Reveal not assigned!");
        if (warningOkayButton != null) warningOkayButton.onClick.AddListener(DismissWildlifeWarning);
        else Debug.LogError("TM: Wildlife Warning Okay Button not assigned!");
        if (keepTrashWarningOkayButton != null) keepTrashWarningOkayButton.onClick.AddListener(DismissKeepTrashWarning);
        else Debug.LogError("TM: Keep Trash Warning Okay Button not assigned!");
        if (startRealGameButton != null) startRealGameButton.onClick.AddListener(EndTutorialAndStartGame);
        else Debug.LogError("TM: Start Real Game Button not assigned!");
        if (playAgainButton != null) playAgainButton.onClick.AddListener(RestartGame);
        else Debug.LogWarning("TM: Play Again Button not assigned for Game Over panel.");
        if (replayTutorialButton != null) replayTutorialButton.onClick.AddListener(ReplayTutorial);


        // Initial UI states
        if(tutorialIntroPanel != null) tutorialIntroPanel.SetActive(false);
        if(itemRevealPanel != null) itemRevealPanel.SetActive(false);
        if(readyToStartPanel != null) readyToStartPanel.SetActive(false);
        if(gameOverPanel != null) gameOverPanel.SetActive(false);
        if(wildlifeWarningPanel != null) wildlifeWarningPanel.SetActive(false);
        if(keepTrashWarningPanel != null) keepTrashWarningPanel.SetActive(false);
        if(timerDisplayPanel != null) timerDisplayPanel.SetActive(false);
        if(scoreDisplayPanel != null) scoreDisplayPanel.SetActive(false);

        currentPhase = GamePhase.ShowingWelcome;
        timeSinceLastInput = 0f;
    }

    void Update()
    {
        if (isGameTimerRunning && currentPhase == GamePhase.MainGamePlaying)
        {
            currentTime -= Time.deltaTime;
            UpdateTimerDisplay();
            if (currentTime <= 0)
            {
                currentTime = 0;
                UpdateTimerDisplay();
                TriggerGameOver();
            }
        }

        if ((currentPhase == GamePhase.TutorialPlaying || currentPhase == GamePhase.MainGamePlaying) && Time.timeScale > 0f)
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
        if (isGameTimerRunning && currentPhase == GamePhase.MainGamePlaying)
        {
            currentTime += timeToAdd;
            UpdateTimerDisplay();
            Debug.Log($"Added {timeToAdd}s to clock. New time: {currentTime}");
        }
    }

    public void BeginTutorialSequence()
    {
        if (welcomePanel != null) welcomePanel.SetActive(false);
        if (tutorialIntroPanel != null) tutorialIntroPanel.SetActive(true);
        currentPhase = GamePhase.ShowingTutorialIntro;
        Time.timeScale = 0f;
        Debug.Log("TM: Showing Tutorial Intro Panel. Time.timeScale = 0");
        SetGameplayScriptsActive(false, false);
        if (trashSpawner != null) trashSpawner.enabled = false;
        if (scoreDisplayPanel != null) scoreDisplayPanel.SetActive(false);
        if (timerDisplayPanel != null) timerDisplayPanel.SetActive(false);
        timeSinceLastInput = 0f;
    }

    public void StartTutorialGameplay()
    {
        Debug.Log("TM: StartTutorialGameplay called.");
        ClearExistingSpawnedItems();

        if(tutorialIntroPanel != null) tutorialIntroPanel.SetActive(false);
        currentPhase = GamePhase.TutorialPlaying;
        Time.timeScale = 1f;
        Debug.Log("TM: Starting Tutorial Gameplay. Time.timeScale = 1");
        itemsProcessedInTutorial = 0;
        timeSinceLastInput = 0f;

        if (trashSpawner != null)
        {
            trashSpawner.enabled = true;
            trashSpawner.StartTutorialSpawning(tutorialItems);
        }
        if (riverGenerator != null) riverGenerator.enabled = true;
        if (leftSkyscraperSpawner != null) leftSkyscraperSpawner.enabled = true;
        if (rightSkyscraperSpawner != null) rightSkyscraperSpawner.enabled = true;
    }

    public void ShadowClicked(TutorialItemData itemData)
    {
        if (currentPhase != GamePhase.TutorialPlaying) return;

        currentlyRevealedItem = itemData;
        currentPhase = GamePhase.ShowingItemReveal;
        Time.timeScale = 0f;
        Debug.Log($"TM: Shadow clicked for {itemData.itemName}. Time.timeScale = 0");
        timeSinceLastInput = 0f;

        if (itemImageReveal != null) itemImageReveal.sprite = itemData.revealedSprite;
        if (itemNameReveal != null) itemNameReveal.text = itemData.itemName;
        if (itemDescriptionReveal != null) itemDescriptionReveal.text = itemData.itemDescription;
        if(itemRevealPanel != null) itemRevealPanel.SetActive(true);
        if(wildlifeWarningPanel != null) wildlifeWarningPanel.SetActive(false);
        if(keepTrashWarningPanel != null) keepTrashWarningPanel.SetActive(false);
    }

    public void OnTrashButtonClicked()
    {
        if (currentPhase != GamePhase.ShowingItemReveal || currentlyRevealedItem == null)
        {
            Debug.LogWarning("TM: TrashButtonClicked called at wrong time or no item revealed.");
            return;
        }

        Debug.Log($"TM: Trash button clicked for item: {currentlyRevealedItem.itemName}");
        if (currentlyRevealedItem.isWildlife)
        {
            Debug.Log("TM: This is wildlife! Showing wildlife warning panel.");
            // if(itemRevealPanel != null) itemRevealPanel.SetActive(false); // <<< DO NOT HIDE ItemRevealPanel
            if(wildlifeWarningPanel != null) wildlifeWarningPanel.SetActive(true);
            currentPhase = GamePhase.ShowingWildlifeWarning;
            Time.timeScale = 0f; // Ensure game remains paused
        }
        else
        {
            Debug.Log("TM: Item is trash and 'Trash It!' clicked. Progressing tutorial.");
            ProgressTutorial();
        }
    }

    public void OnKeepButtonClicked()
    {
        if (currentPhase != GamePhase.ShowingItemReveal || currentlyRevealedItem == null)
        {
            Debug.LogWarning("TM: KeepButtonClicked called at wrong time or no item revealed.");
            return;
        }

        Debug.Log($"TM: Keep button clicked for item: {currentlyRevealedItem.itemName}");
        if (currentlyRevealedItem.isWildlife)
        {
            Debug.Log("TM: Wildlife correctly chosen to be kept. Progressing tutorial.");
            ProgressTutorial();
        }
        else
        {
            Debug.Log("TM: This is trash, not wildlife! Showing keep trash warning panel.");
            // if(itemRevealPanel != null) itemRevealPanel.SetActive(false); // <<< DO NOT HIDE ItemRevealPanel
            if(keepTrashWarningPanel != null) keepTrashWarningPanel.SetActive(true);
            currentPhase = GamePhase.ShowingKeepTrashWarning;
            Time.timeScale = 0f; // Ensure game remains paused
        }
    }

    public void DismissWildlifeWarning()
    {
        if (currentPhase != GamePhase.ShowingWildlifeWarning) return;

        Debug.Log("TM: Dismissing wildlife warning.");
        if(wildlifeWarningPanel != null) wildlifeWarningPanel.SetActive(false);
        // if(itemRevealPanel != null) itemRevealPanel.SetActive(true); // <<< ItemRevealPanel should still be active
        currentPhase = GamePhase.ShowingItemReveal; // Return to making a choice on the still-visible ItemRevealPanel
        Time.timeScale = 0f; // Keep game paused
        timeSinceLastInput = 0f;
    }

    public void DismissKeepTrashWarning()
    {
        if (currentPhase != GamePhase.ShowingKeepTrashWarning) return;

        Debug.Log("TM: Dismissing keep trash warning.");
        if(keepTrashWarningPanel != null) keepTrashWarningPanel.SetActive(false);
        // if(itemRevealPanel != null) itemRevealPanel.SetActive(true); // <<< ItemRevealPanel should still be active
        currentPhase = GamePhase.ShowingItemReveal; // Return to making a choice on the still-visible ItemRevealPanel
        Time.timeScale = 0f; // Keep game paused
        timeSinceLastInput = 0f;
    }

    private void ProgressTutorial()
    {
        if(itemRevealPanel != null) itemRevealPanel.SetActive(false); // Now hide ItemRevealPanel as we progress
        itemsProcessedInTutorial++;
        currentlyRevealedItem = null;
        timeSinceLastInput = 0f;
        Debug.Log($"TutorialManager: Item choice processed. Items processed: {itemsProcessedInTutorial}/{tutorialItemsToComplete}");

        if (itemsProcessedInTutorial >= tutorialItemsToComplete)
        {
            currentPhase = GamePhase.ShowingReadyToStart;
            if(readyToStartPanel != null) readyToStartPanel.SetActive(true);
            Time.timeScale = 0f;
            Debug.Log("TutorialManager: Tutorial items complete. Showing ReadyToStartPanel.");
            if (trashSpawner != null)
            {
                trashSpawner.StopSpawning();
                trashSpawner.enabled = false;
            }
            if (riverGenerator != null) riverGenerator.enabled = false;
            if (leftSkyscraperSpawner != null) leftSkyscraperSpawner.enabled = false;
            if (rightSkyscraperSpawner != null) rightSkyscraperSpawner.enabled = false;
        }
        else
        {
            currentPhase = GamePhase.TutorialPlaying;
            Time.timeScale = 1f;
            Debug.Log("TutorialManager: Continuing tutorial. Time.timeScale = 1");
        }
    }


    public void EndTutorialAndStartGame()
    {
        Debug.Log("TM: EndTutorialAndStartGame called.");
        ClearExistingSpawnedItems();

        if(readyToStartPanel != null) readyToStartPanel.SetActive(false);
        currentPhase = GamePhase.MainGamePlaying;
        Time.timeScale = 1f;
        Debug.Log("TM: Tutorial Finished. Starting Real Game! Time.timeScale = 1");
        timeSinceLastInput = 0f;

        if (scoreManager != null) scoreManager.ResetScore();

        currentTime = gameTimeLimit;
        isGameTimerRunning = true;
        if(timerDisplayPanel != null) timerDisplayPanel.SetActive(true);
        if(scoreDisplayPanel != null) scoreDisplayPanel.SetActive(true);
        UpdateTimerDisplay();

        SetGameplayScriptsActive(true, true);

        if (trashSpawner != null)
        {
            trashSpawner.enabled = true;
            trashSpawner.StartRealGameSpawning();
        }
    }
    private void TriggerGameOver()
    {
        if (currentPhase == GamePhase.GameOver) return;
        currentPhase = GamePhase.GameOver;
        isGameTimerRunning = false;
        Time.timeScale = 0f;
        Debug.Log("TM: Game Over! Time.timeScale = 0");
        timeSinceLastInput = 0f;

        SetGameplayScriptsActive(false, false);
        if (trashSpawner != null) trashSpawner.StopSpawning();

        if (gameOverPanel != null)
        {
            if (finalScoreTextElement != null && scoreManager != null)
            {
                finalScoreTextElement.text = "Final Score: " + scoreManager.GetCurrentScore();
            }
            gameOverPanel.SetActive(true);
        }
        if(timerDisplayPanel != null) timerDisplayPanel.SetActive(false);
        if(scoreDisplayPanel != null) scoreDisplayPanel.SetActive(false);
    }
    private void RestartGame()
    {
        Debug.Log("TM: Restarting Game (reloading scene)...");
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public void ReplayTutorial()
    {
        Debug.Log("TM: Replay Tutorial button clicked.");
        ClearExistingSpawnedItems();

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (scoreManager != null) scoreManager.ResetScore();

        if(scoreDisplayPanel != null) scoreDisplayPanel.SetActive(false);
        if(timerDisplayPanel != null) timerDisplayPanel.SetActive(false);
        isGameTimerRunning = false;

        BeginTutorialSequence();
        timeSinceLastInput = 0f;
    }
    private void GoToStartMenu()
    {
        Time.timeScale = 0f;
        isGameTimerRunning = false;
        ClearExistingSpawnedItems();

        if(tutorialIntroPanel != null) tutorialIntroPanel.SetActive(false);
        if(itemRevealPanel != null) itemRevealPanel.SetActive(false);
        if(readyToStartPanel != null) readyToStartPanel.SetActive(false);
        if(gameOverPanel != null) gameOverPanel.SetActive(false);
        if(wildlifeWarningPanel != null) wildlifeWarningPanel.SetActive(false);
        if(keepTrashWarningPanel != null) keepTrashWarningPanel.SetActive(false);
        if(timerDisplayPanel != null) timerDisplayPanel.SetActive(false);
        if(scoreDisplayPanel != null) scoreDisplayPanel.SetActive(false);

        if (welcomePanel != null)
        {
            welcomePanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("TM: WelcomePanel not assigned for GoToStartMenu.");
        }

        SetGameplayScriptsActive(false, false);
        if (trashSpawner != null)
        {
            trashSpawner.StopSpawning();
            trashSpawner.enabled = false;
        }
        currentPhase = GamePhase.ShowingWelcome;
        timeSinceLastInput = 0f;
        Debug.Log("TM: Returned to Start Menu due to inactivity.");
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
        Debug.Log($"TM: Setting gameplay scripts active: {isActive}, Include Backgrounds: {includeBackgroundGenerators}");
        if (includeBackgroundGenerators)
        {
            if (riverGenerator != null) riverGenerator.enabled = isActive;
            if (leftSkyscraperSpawner != null) leftSkyscraperSpawner.enabled = isActive;
            if (rightSkyscraperSpawner != null) rightSkyscraperSpawner.enabled = isActive;
        }
        if (trashSpawner != null && !isActive)
        {
            trashSpawner.enabled = false;
        }
    }
}
