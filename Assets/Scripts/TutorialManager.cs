using UnityEngine;
using UnityEngine.UI; // For Button
using TMPro;        // For TextMeshPro elements
using System.Collections.Generic; // For List

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

    [Header("Tutorial Item Data")]
    [Tooltip("Create a list of items that will appear in the tutorial.")]
    public List<TutorialItemData> tutorialItems = new List<TutorialItemData>();
    private int itemsProcessedInTutorial = 0;
    [Tooltip("How many items the player needs to interact with before the tutorial round ends.")]
    [SerializeField] private int tutorialItemsToComplete = 5;

    [Header("Game State Control")]
    [SerializeField] private TrashSpawner trashSpawner;
    // --- References for your background generators ---
    [SerializeField] private RiverBackgroundGenerator riverGenerator;
    [SerializeField] private SkyscraperSpawner skyscraperSpawner;
    // ----------------------------------------------------


    public enum TutorialState
    {
        None, ShowingWelcome, ShowingTutorialIntro, TutorialPlaying,
        ShowingItemReveal, ShowingReadyToStart, TutorialFinished
    }
    public TutorialState currentState = TutorialState.None;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Null checks for essential components
        if (welcomePanel == null) Debug.LogError("TM: Welcome Panel not assigned!");
        if (tutorialIntroPanel == null) Debug.LogError("TM: Tutorial Intro Panel not assigned!");
        if (itemRevealPanel == null) Debug.LogError("TM: Item Reveal Panel not assigned!");
        if (readyToStartPanel == null) Debug.LogError("TM: Ready To Start Panel not assigned!");
        if (trashSpawner == null) Debug.LogError("TM: Trash Spawner not assigned!");
        if (riverGenerator == null) Debug.LogWarning("TM: RiverBackgroundGenerator not assigned. It will not be controlled by the tutorial manager.");
        if (skyscraperSpawner == null) Debug.LogWarning("TM: SkyscraperSpawner not assigned. It will not be controlled by the tutorial manager.");

        // Assign button listeners
        if (startTutorialButton != null) startTutorialButton.onClick.AddListener(StartTutorialGameplay);
        else Debug.LogError("TM: Start Tutorial Button not assigned!");

        if (trashButtonReveal != null) trashButtonReveal.onClick.AddListener(HandleItemChoice);
        else Debug.LogError("TM: Trash Button Reveal not assigned!");

        if (keepButtonReveal != null) keepButtonReveal.onClick.AddListener(HandleItemChoice);
        else Debug.LogError("TM: Keep Button Reveal not assigned!");

        if (startRealGameButton != null) startRealGameButton.onClick.AddListener(EndTutorialAndStartGame);
        else Debug.LogError("TM: Start Real Game Button not assigned!");

        // Ensure tutorial-specific panels are initially inactive
        if(tutorialIntroPanel != null) tutorialIntroPanel.SetActive(false);
        if(itemRevealPanel != null) itemRevealPanel.SetActive(false);
        if(readyToStartPanel != null) readyToStartPanel.SetActive(false);
    }

    // Called by StartMenuManager to begin the whole tutorial flow
    public void BeginTutorialSequence()
    {
        if (welcomePanel != null) welcomePanel.SetActive(false);
        if (tutorialIntroPanel != null) tutorialIntroPanel.SetActive(true);
        currentState = TutorialState.ShowingTutorialIntro;
        Time.timeScale = 0f; // Pause game for intro text
        Debug.Log("TutorialManager: Showing Tutorial Intro Panel. Time.timeScale = 0");

        SetGameplayScriptsActive(false); // Disable all gameplay scripts initially
        if (trashSpawner != null) trashSpawner.enabled = false; // Explicitly ensure trash spawner is off
    }

    // Called when "Let's Go!" on TutorialIntroPanel is clicked
    public void StartTutorialGameplay()
    {
        if(tutorialIntroPanel != null) tutorialIntroPanel.SetActive(false);
        currentState = TutorialState.TutorialPlaying;
        Time.timeScale = 1f; // Unpause for interactive tutorial part
        Debug.Log("TutorialManager: Starting Tutorial Gameplay. Time.timeScale = 1");
        itemsProcessedInTutorial = 0;

        // Enable Trash Spawner for tutorial items
        if (trashSpawner != null)
        {
            trashSpawner.enabled = true;
            trashSpawner.StartTutorialSpawning(tutorialItems);
        }

        // --- MODIFIED: Enable background generators for the tutorial phase ---
        if (riverGenerator != null)
        {
            riverGenerator.enabled = true;
            Debug.Log("TutorialManager: Enabling RiverGenerator for tutorial.");
        }
        if (skyscraperSpawner != null)
        {
            skyscraperSpawner.enabled = true;
            Debug.Log("TutorialManager: Enabling SkyscraperSpawner for tutorial.");
            // If you need SkyscraperSpawner to reset to its initial state for the tutorial:
            // skyscraperSpawner.ResetToFirstSequence(); // Example: You would need to implement this method in SkyscraperSpawner
        }
        // ---------------------------------------------------------------------
    }

    // Called by TutorialShadowItem when a shadow is clicked
    public void ShadowClicked(TutorialItemData itemData)
    {
        if (currentState != TutorialState.TutorialPlaying) return; // Only process clicks during active tutorial play

        currentState = TutorialState.ShowingItemReveal;
        Time.timeScale = 0f; // Pause game while item reveal popup is shown
        Debug.Log($"TutorialManager: Shadow clicked for {itemData.itemName}. Time.timeScale = 0");

        // Populate and show ItemRevealPanel
        if (itemImageReveal != null) itemImageReveal.sprite = itemData.revealedSprite;
        if (itemNameReveal != null) itemNameReveal.text = itemData.itemName;
        if (itemDescriptionReveal != null) itemDescriptionReveal.text = itemData.itemDescription;
        if(itemRevealPanel != null) itemRevealPanel.SetActive(true);
    }

    // Called when "Trash It!" or "Keep It?" on ItemRevealPanel is clicked
    private void HandleItemChoice()
    {
        if (currentState != TutorialState.ShowingItemReveal) return;

        if(itemRevealPanel != null) itemRevealPanel.SetActive(false);
        itemsProcessedInTutorial++;
        Debug.Log($"TutorialManager: Item choice made. Items processed: {itemsProcessedInTutorial}/{tutorialItemsToComplete}");

        if (itemsProcessedInTutorial >= tutorialItemsToComplete)
        {
            // Tutorial round complete
            currentState = TutorialState.ShowingReadyToStart;
            if(readyToStartPanel != null) readyToStartPanel.SetActive(true);
            Time.timeScale = 0f; // Keep game paused for "Ready to Start" panel
            Debug.Log("TutorialManager: Tutorial items complete. Showing ReadyToStartPanel. Time.timeScale remains 0.");
            if (trashSpawner != null)
            {
                trashSpawner.StopSpawning();
                trashSpawner.enabled = false;
            }
            // Background generators will pause due to Time.timeScale = 0f
        }
        else
        {
            // Continue tutorial gameplay
            currentState = TutorialState.TutorialPlaying;
            Time.timeScale = 1f; // Unpause to find next shadow
            Debug.Log("TutorialManager: Continuing tutorial. Time.timeScale = 1");
            // TrashSpawner continues spawning tutorial items if more are needed
            // Background generators will resume
        }
    }

    // Called when "Start Game!" on ReadyToStartPanel is clicked
    public void EndTutorialAndStartGame()
    {
        if(readyToStartPanel != null) readyToStartPanel.SetActive(false);
        currentState = TutorialState.TutorialFinished;
        Time.timeScale = 1f; // CRUCIAL: Ensure game is running for the real game
        Debug.Log("TutorialManager: Tutorial Finished. Starting Real Game! Time.timeScale = 1");

        // Enable all main game systems
        SetGameplayScriptsActive(true); // This will enable river/skyscraper spawners

        // Specifically configure TrashSpawner for the real game
        if (trashSpawner != null)
        {
            trashSpawner.enabled = true;
            trashSpawner.StartRealGameSpawning();
        }
    }

    // Helper function to enable/disable main gameplay-related scripts
    private void SetGameplayScriptsActive(bool isActive)
    {
        Debug.Log($"TutorialManager: Setting gameplay scripts active state to: {isActive}");
        if (riverGenerator != null)
        {
            riverGenerator.enabled = isActive;
            Debug.Log($"-- RiverBackgroundGenerator.enabled set to {isActive}");
        }
        // else Debug.LogWarning("TutorialManager: RiverBackgroundGenerator not assigned, cannot control its state."); // Can be spammy

        if (skyscraperSpawner != null)
        {
            skyscraperSpawner.enabled = isActive;
            Debug.Log($"-- SkyscraperSpawner.enabled set to {isActive}");
        }
        // else Debug.LogWarning("TutorialManager: SkyscraperSpawner not assigned, cannot control its state."); // Can be spammy

        // Note: TrashSpawner's enabled state is more specifically managed by the methods
        // that call StartTutorialSpawning or StartRealGameSpawning.
        // However, this function ensures it's part of the general enable/disable sweep if needed.
        if (trashSpawner != null)
        {
            // If we are globally activating scripts for the real game, ensure trash spawner is enabled.
            // If we are globally deactivating, it should also be deactivated.
            // The specific spawning mode is set by StartTutorialSpawning/StartRealGameSpawning.
            if (isActive && currentState == TutorialState.TutorialFinished) {
                 // Already handled by EndTutorialAndStartGame
            } else {
                 // trashSpawner.enabled = isActive; // Already handled by specific calls
            }
        }
    }
}
