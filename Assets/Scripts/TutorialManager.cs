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

    [Header("Gameplay UI Elements")]
    [Tooltip("The parent Panel GameObject that contains the score text and its background.")]
    [SerializeField] private GameObject scoreDisplayPanel;
    [Tooltip("The parent Panel GameObject that contains the timer text and its background.")]
    [SerializeField] private GameObject timerDisplayPanel;
    [Tooltip("UI Text to display the game timer (child of TimerDisplayPanel).")]
    [SerializeField] private TMP_Text timerTextElement;


    [Header("Tutorial Item Data")]
    [Tooltip("Create a list of items that will appear in the tutorial.")]
    public List<TutorialItemData> tutorialItems = new List<TutorialItemData>();
    private int itemsProcessedInTutorial = 0;
    [Tooltip("How many items the player needs to interact with before the tutorial round ends.")]
    [SerializeField] private int tutorialItemsToComplete = 5;

    [Header("Game State Control")]
    [SerializeField] private TrashSpawner trashSpawner;
    [SerializeField] private RiverBackgroundGenerator riverGenerator;
    // --- MODIFIED for Left and Right Skyscraper Spawners ---
    [SerializeField] private SkyscraperSpawner leftSkyscraperSpawner;
    [SerializeField] private SkyscraperSpawner rightSkyscraperSpawner;
    // ---------------------------------------------------------
    [SerializeField] private ScoreManager scoreManager;

    [Header("Game Timer Settings")]
    [SerializeField] private float gameTimeLimit = 30f;
    [Tooltip("Time bonus in seconds for collecting a Divvy Bike.")]
    public float divvyBikeTimeBonus = 5f;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text finalScoreTextElement;
    [SerializeField] private Button playAgainButton;

    private float currentTime;
    private bool isGameTimerRunning = false;


    public enum GamePhase
    {
        None, ShowingWelcome, ShowingTutorialIntro, TutorialPlaying,
        ShowingItemReveal, ShowingReadyToStart, MainGamePlaying, GameOver
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
        if (trashSpawner == null) Debug.LogError("TM: Trash Spawner not assigned!");
        if (scoreManager == null) Debug.LogError("TM: ScoreManager not assigned!");
        if (timerDisplayPanel == null) Debug.LogError("TM: Timer Display Panel not assigned!");
        if (timerTextElement == null) Debug.LogError("TM: Timer Text Element (child of TimerDisplayPanel) not assigned!");
        if (gameOverPanel == null) Debug.LogError("TM: Game Over Panel not assigned!");
        if (scoreDisplayPanel == null) Debug.LogError("TM: Score Display Panel not assigned!");
        if (riverGenerator == null) Debug.LogWarning("TM: RiverBackgroundGenerator not assigned.");
        // --- Check new assignments ---
        if (leftSkyscraperSpawner == null) Debug.LogWarning("TM: LeftSkyscraperSpawner not assigned.");
        if (rightSkyscraperSpawner == null) Debug.LogWarning("TM: RightSkyscraperSpawner not assigned.");
        // -----------------------------


        // Button listeners
        if (startTutorialButton != null) startTutorialButton.onClick.AddListener(StartTutorialGameplay);
        else Debug.LogError("TM: Start Tutorial Button not assigned!");
        if (trashButtonReveal != null) trashButtonReveal.onClick.AddListener(HandleItemChoice);
        else Debug.LogError("TM: Trash Button Reveal not assigned!");
        if (keepButtonReveal != null) keepButtonReveal.onClick.AddListener(HandleItemChoice);
        else Debug.LogError("TM: Keep Button Reveal not assigned!");
        if (startRealGameButton != null) startRealGameButton.onClick.AddListener(EndTutorialAndStartGame);
        else Debug.LogError("TM: Start Real Game Button not assigned!");
        if (playAgainButton != null) playAgainButton.onClick.AddListener(RestartGame);
        else Debug.LogWarning("TM: Play Again Button not assigned for Game Over panel.");


        // Initial UI states
        if(tutorialIntroPanel != null) tutorialIntroPanel.SetActive(false);
        if(itemRevealPanel != null) itemRevealPanel.SetActive(false);
        if(readyToStartPanel != null) readyToStartPanel.SetActive(false);
        if(gameOverPanel != null) gameOverPanel.SetActive(false);
        if(timerDisplayPanel != null) timerDisplayPanel.SetActive(false);
        if(scoreDisplayPanel != null) scoreDisplayPanel.SetActive(false);

        currentPhase = GamePhase.ShowingWelcome;
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
    }

    public void StartTutorialGameplay()
    {
        if(tutorialIntroPanel != null) tutorialIntroPanel.SetActive(false);
        currentPhase = GamePhase.TutorialPlaying;
        Time.timeScale = 1f;
        Debug.Log("TM: Starting Tutorial Gameplay. Time.timeScale = 1");
        itemsProcessedInTutorial = 0;

        if (trashSpawner != null)
        {
            trashSpawner.enabled = true;
            trashSpawner.StartTutorialSpawning(tutorialItems);
        }
        // Enable backgrounds for tutorial gameplay
        if (riverGenerator != null) riverGenerator.enabled = true;
        // --- Enable both skyscraper spawners ---
        if (leftSkyscraperSpawner != null)
        {
            leftSkyscraperSpawner.enabled = true;
            Debug.Log("TutorialManager: Enabling LeftSkyscraperSpawner for tutorial.");
        }
        if (rightSkyscraperSpawner != null)
        {
            rightSkyscraperSpawner.enabled = true;
            Debug.Log("TutorialManager: Enabling RightSkyscraperSpawner for tutorial.");
        }
        // ---------------------------------------
    }

    public void ShadowClicked(TutorialItemData itemData)
    {
        if (currentPhase != GamePhase.TutorialPlaying) return;
        currentPhase = GamePhase.ShowingItemReveal;
        Time.timeScale = 0f;
        if (itemImageReveal != null) itemImageReveal.sprite = itemData.revealedSprite;
        if (itemNameReveal != null) itemNameReveal.text = itemData.itemName;
        if (itemDescriptionReveal != null) itemDescriptionReveal.text = itemData.itemDescription;
        if(itemRevealPanel != null) itemRevealPanel.SetActive(true);
    }

    private void HandleItemChoice()
    {
        if (currentPhase != GamePhase.ShowingItemReveal) return;
        if(itemRevealPanel != null) itemRevealPanel.SetActive(false);
        itemsProcessedInTutorial++;

        if (itemsProcessedInTutorial >= tutorialItemsToComplete)
        {
            currentPhase = GamePhase.ShowingReadyToStart;
            if(readyToStartPanel != null) readyToStartPanel.SetActive(true);
            Time.timeScale = 0f;
            if (trashSpawner != null)
            {
                trashSpawner.StopSpawning();
                trashSpawner.enabled = false;
            }
            // Backgrounds will pause due to Time.timeScale = 0f
        }
        else
        {
            currentPhase = GamePhase.TutorialPlaying;
            Time.timeScale = 1f;
        }
    }

    public void EndTutorialAndStartGame()
    {
        if(readyToStartPanel != null) readyToStartPanel.SetActive(false);
        currentPhase = GamePhase.MainGamePlaying;
        Time.timeScale = 1f;
        Debug.Log("TM: Tutorial Finished. Starting Real Game! Time.timeScale = 1");

        if (scoreManager != null) scoreManager.ResetScore();

        currentTime = gameTimeLimit;
        isGameTimerRunning = true;
        if(timerDisplayPanel != null) timerDisplayPanel.SetActive(true);
        if(scoreDisplayPanel != null) scoreDisplayPanel.SetActive(true);
        UpdateTimerDisplay();

        SetGameplayScriptsActive(true, true); // This will now enable both skyscraper spawners

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
        Debug.Log("TM: Restarting Game...");
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Helper function to enable/disable main gameplay-related scripts
    private void SetGameplayScriptsActive(bool isActive, bool includeBackgroundGenerators)
    {
        Debug.Log($"TM: Setting gameplay scripts active: {isActive}, Include Backgrounds: {includeBackgroundGenerators}");

        if (includeBackgroundGenerators)
        {
            if (riverGenerator != null)
            {
                riverGenerator.enabled = isActive;
                Debug.Log($"-- RiverBackgroundGenerator.enabled set to {isActive}");
            }
            // --- MODIFIED to handle both spawners ---
            if (leftSkyscraperSpawner != null)
            {
                leftSkyscraperSpawner.enabled = isActive;
                Debug.Log($"-- LeftSkyscraperSpawner.enabled set to {isActive}");
            }
            if (rightSkyscraperSpawner != null)
            {
                rightSkyscraperSpawner.enabled = isActive;
                Debug.Log($"-- RightSkyscraperSpawner.enabled set to {isActive}");
            }
            // ----------------------------------------
        }
        if (trashSpawner != null && !isActive)
        {
            trashSpawner.enabled = false;
             Debug.Log($"-- TrashSpawner.enabled set to {isActive} (via global deactivation)");
        }
    }
}
