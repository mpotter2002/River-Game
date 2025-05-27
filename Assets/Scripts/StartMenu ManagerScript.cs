using UnityEngine;
using UnityEngine.UI; // Required for Button

public class StartMenuManager : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Assign the main panel for the start menu/welcome screen here.")]
    [SerializeField] private GameObject welcomePanel; // This is the first panel the player sees
    [Tooltip("Assign the Start Button from the Welcome Panel here.")]
    [SerializeField] private Button startButton;

    [Header("Tutorial Integration")]
    [Tooltip("Assign your TutorialManager GameObject here.")]
    [SerializeField] private TutorialManager tutorialManager;

    // Note: References to individual gameplay managers (TrashSpawner, RiverBackgroundGenerator, etc.)
    // have been removed as TutorialManager will now handle their states.

    void Start()
    {
        // Ensure the welcome panel is visible when the game begins
        if (welcomePanel != null)
        {
            welcomePanel.SetActive(true);
        }
        else
        {
            Debug.LogError("StartMenuManager: Welcome Panel is not assigned!");
        }

        // Pause game logic initially
        Time.timeScale = 0f; // Pauses physics, FixedUpdate, and time-dependent operations

        // Add a listener to the start button's onClick event
        if (startButton != null)
        {
            startButton.onClick.AddListener(InitiateTutorialOrGame);
        }
        else
        {
            Debug.LogError("StartMenuManager: Start Button is not assigned!");
        }

        // Ensure TutorialManager is assigned
        if (tutorialManager == null)
        {
            Debug.LogError("StartMenuManager: TutorialManager is not assigned! Tutorial sequence cannot be started.");
        }
    }

    public void InitiateTutorialOrGame()
    {
        Debug.Log("StartMenuManager: Start Button clicked!");

        if (tutorialManager != null)
        {
            // Hide the welcome panel (TutorialManager might also do this, but good to be explicit)
            if (welcomePanel != null)
            {
                welcomePanel.SetActive(false);
            }
            tutorialManager.BeginTutorialSequence(); // Hand off to TutorialManager
        }
        else
        {
            Debug.LogError("StartMenuManager: TutorialManager not assigned! Cannot begin tutorial. Check Inspector assignments.");
            // As a fallback, if no tutorial manager, you might want to directly start the game
            // For example:
            // Time.timeScale = 1f;
            // if (welcomePanel != null) welcomePanel.SetActive(false);
            // Enable your main game scripts here if TutorialManager is missing
        }
    }

    // Optional: If you add a Quit button to this initial welcome panel
    public void QuitGame()
    {
        Debug.Log("StartMenuManager: Quit Game button clicked!");
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Stops play mode in the editor
        #endif
    }
}
