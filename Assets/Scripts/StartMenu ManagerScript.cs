using UnityEngine;
using UnityEngine.UI; // Required for Button
// using UnityEngine.SceneManagement; // Optional: If you want to load a different scene

public class StartMenuManager : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Assign the main panel for the start menu here.")]
    [SerializeField] private GameObject startMenuPanel;
    [Tooltip("Assign the Start Button here.")]
    [SerializeField] private Button startButton;

    [Header("Gameplay Managers to Control")]
    [Tooltip("Assign the GameObject that has the TrashSpawner script.")]
    [SerializeField] private TrashSpawner trashSpawner;
    [Tooltip("Assign the GameObject that has the RiverBackgroundGenerator script.")]
    [SerializeField] private RiverBackgroundGenerator riverBackgroundGenerator;
    [Tooltip("Assign the GameObject that has the SkyscraperSpawner script.")]
    [SerializeField] private SkyscraperSpawner skyscraperSpawner;
    [Header("Tutorial Control")]
    [SerializeField] private TutorialManager tutorialManager;
    // [SerializeField] private PlayerController playerController;


    void Start()
    {
        if (startMenuPanel != null)
        {
            Debug.Log("StartMenuManager: Activating Start Menu Panel in Start().");
            startMenuPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("StartMenuManager: Start Menu Panel is NOT ASSIGNED in the Inspector for Start()!");
        }

        Time.timeScale = 0f;

        if (startButton != null)
        {
            startButton.onClick.AddListener(StartGame);
        }
        else
        {
            Debug.LogError("StartMenuManager: Start Button is NOT ASSIGNED in the Inspector!");
        }

        SetGameplayScriptsActive(false);
    }

   public void StartGame() // This is in StartMenuManager.cs
{
    Debug.Log("StartMenuManager: Start Game button clicked! Beginning tutorial sequence.");

    if (startMenuPanel != null)
    {
        startMenuPanel.SetActive(false); // Hide the initial welcome panel
    }

    if (tutorialManager != null)
    {
        tutorialManager.BeginTutorialSequence(); // Tell TutorialManager to take over
    }
    else
    {
        Debug.LogError("StartMenuManager: TutorialManager not assigned! Cannot start tutorial.");
        // Fallback: Directly start the game if tutorial manager is missing (optional)
        // Time.timeScale = 1f;
        // SetGameplayScriptsActive(true);
    }
    // Time.timeScale and SetGameplayScriptsActive will now be handled by TutorialManager
}
    private void SetGameplayScriptsActive(bool isActive)
    {
        if (trashSpawner != null) trashSpawner.enabled = isActive;
        // else Debug.LogWarning("StartMenuManager: TrashSpawner not assigned."); // Can be spammy

        if (riverBackgroundGenerator != null) riverBackgroundGenerator.enabled = isActive;
        // else Debug.LogWarning("StartMenuManager: RiverBackgroundGenerator not assigned.");

        if (skyscraperSpawner != null) skyscraperSpawner.enabled = isActive;
        // else Debug.LogWarning("StartMenuManager: SkyscraperSpawner not assigned.");
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game button clicked!");
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
