using UnityEngine;
using UnityEngine.UI; // For Button
using TMPro;        // For TextMeshPro elements
using System.Collections.Generic; // For List

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Tutorial Flow Panels")]
    [SerializeField] private GameObject welcomePanel; // Your existing initial welcome panel
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
    [SerializeField] private Button startTutorialButton; // Button on TutorialIntroPanel

    [Header("Ready To Start Panel Elements")]
    [SerializeField] private Button startRealGameButton; // Button on ReadyToStartPanel

    [Header("Tutorial Item Data")]
    [Tooltip("Create a list of items that will appear in the tutorial.")]
    public List<TutorialItemData> tutorialItems = new List<TutorialItemData>();
    private int currentTutorialItemIndex = 0; // Note: This was not used in the provided TrashSpawner logic for tutorial items.
                                            // TrashSpawner currently iterates through the whole list provided to it.
    private int itemsProcessedInTutorial = 0;
    [SerializeField] private int tutorialItemsToComplete = 5;

    [Header("Game State Control")]
    [SerializeField] private TrashSpawner trashSpawner; // Assign your TrashSpawner
    // Add references to other game managers if they need to be controlled
    // [SerializeField] private RiverBackgroundGenerator riverGenerator;
    // [SerializeField] private SkyscraperSpawner skyscraperSpawner;


    // Enum to manage tutorial states
    public enum TutorialState
    {
        None,
        ShowingWelcome, // This state might be managed by StartMenuManager primarily
        ShowingTutorialIntro,
        TutorialPlaying,
        ShowingItemReveal,
        ShowingReadyToStart,
        TutorialFinished
    }
    public TutorialState currentState = TutorialState.None;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Initial setup: Hide all tutorial panels except the first one (welcome panel)
        if (welcomePanel == null) Debug.LogError("TutorialManager: Welcome Panel not assigned!");
        if (tutorialIntroPanel == null) Debug.LogError("TutorialManager: Tutorial Intro Panel not assigned!");
        if (itemRevealPanel == null) Debug.LogError("TutorialManager: Item Reveal Panel not assigned!");
        if (readyToStartPanel == null) Debug.LogError("TutorialManager: Ready To Start Panel not assigned!");
        if (trashSpawner == null) Debug.LogError("TutorialManager: Trash Spawner not assigned!");


        // Listeners for buttons
        if (startTutorialButton != null) startTutorialButton.onClick.AddListener(StartTutorialGameplay);
        else Debug.LogError("TutorialManager: Start Tutorial Button (on TutorialIntroPanel) not assigned!");

        if (trashButtonReveal != null) trashButtonReveal.onClick.AddListener(HandleItemChoice);
        else Debug.LogError("TutorialManager: Trash Button Reveal not assigned!");

        if (keepButtonReveal != null) keepButtonReveal.onClick.AddListener(HandleItemChoice); // For now, keep also just advances
        else Debug.LogError("TutorialManager: Keep Button Reveal not assigned!");

        if (startRealGameButton != null) startRealGameButton.onClick.AddListener(EndTutorialAndStartGame);
        else Debug.LogError("TutorialManager: Start Real Game Button (on ReadyToStartPanel) not assigned!");


        // Initially, all tutorial-specific panels should be off.
        // The StartMenuManager will handle showing the first welcomePanel.
        if(tutorialIntroPanel != null) tutorialIntroPanel.SetActive(false);
        if(itemRevealPanel != null) itemRevealPanel.SetActive(false);
        if(readyToStartPanel != null) readyToStartPanel.SetActive(false);

        // The game starts paused by StartMenuManager, TutorialManager doesn't manage Time.timeScale directly here
        // but will coordinate with StartMenuManager or a GameManager for actual game start.
    }

    // Called by StartMenuManager when the initial "Start" button (on Welcome Panel) is clicked
    public void BeginTutorialSequence()
    {
        if (welcomePanel != null) welcomePanel.SetActive(false); // Hide the main welcome panel
        if (tutorialIntroPanel != null) tutorialIntroPanel.SetActive(true);
        currentState = TutorialState.ShowingTutorialIntro;
        Time.timeScale = 0f; // Ensure game is paused for tutorial intro
        Debug.Log("TutorialManager: Showing Tutorial Intro Panel.");

        // Disable main game spawners if they are active
        SetGameplayScriptsActive(false); // Spawners, player, etc.
        if (trashSpawner != null) trashSpawner.enabled = false; // Specifically ensure trash spawner is off
    }


    // Called when "Let's Go!" on TutorialIntroPanel is clicked
    public void StartTutorialGameplay()
    {
        if(tutorialIntroPanel != null) tutorialIntroPanel.SetActive(false);
        currentState = TutorialState.TutorialPlaying;
        Time.timeScale = 1f; // Unpause for tutorial gameplay (clicking shadows)
        Debug.Log("TutorialManager: Starting Tutorial Gameplay. Time.timeScale = 1");

        itemsProcessedInTutorial = 0;
        // currentTutorialItemIndex = 0; // Reset for multiple tutorial runs if needed

        // Tell TrashSpawner to start spawning tutorial shadow items
        if (trashSpawner != null)
        {
            trashSpawner.enabled = true;
            trashSpawner.StartTutorialSpawning(tutorialItems); // This method was added to TrashSpawner
        }
        // Other game elements for tutorial can be enabled here
    }

    // Called by TutorialShadowItem when a shadow is clicked
    public void ShadowClicked(TutorialItemData itemData)
    {
        if (currentState != TutorialState.TutorialPlaying) return;

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
            Debug.Log("TutorialManager: Tutorial items complete. Showing ReadyToStartPanel. Time.timeScale remains 0.");
            if (trashSpawner != null)
            {
                trashSpawner.StopSpawning(); // This method was added to TrashSpawner
                trashSpawner.enabled = false;
            }
        }
        else
        {
            // Continue tutorial gameplay
            currentState = TutorialState.TutorialPlaying;
            Time.timeScale = 1f; // Unpause to find next shadow
            Debug.Log("TutorialManager: Continuing tutorial. Time.timeScale = 1");
            // TrashSpawner continues spawning tutorial items if more are needed
        }
    }

    // Called when "Start Game!" on ReadyToStartPanel is clicked
    public void EndTutorialAndStartGame()
    {
        if(readyToStartPanel != null) readyToStartPanel.SetActive(false);
        currentState = TutorialState.TutorialFinished;
        Time.timeScale = 1f; // Ensure game is running
        Debug.Log("TutorialManager: Tutorial Finished. Starting Real Game!");

        // Enable main game systems
        SetGameplayScriptsActive(true);
        if (trashSpawner != null)
        {
            trashSpawner.enabled = true;
            trashSpawner.StartRealGameSpawning(); // This method was added to TrashSpawner
        }

        // Potentially notify a GameManager to take over
        // FindObjectOfType<GameManager>()?.StartMainGame();
    }

    // Helper to enable/disable main gameplay scripts
    private void SetGameplayScriptsActive(bool isActive)
    {
        // Example: Enable/disable your actual game spawners here
        // if (riverGenerator != null) riverGenerator.enabled = isActive;
        // if (skyscraperSpawner != null) skyscraperSpawner.enabled = isActive;
        // Note: trashSpawner is handled more specifically above for tutorial vs real game
    }
}
