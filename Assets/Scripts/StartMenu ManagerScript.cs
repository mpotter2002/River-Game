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

    public void StartGame()
    {
        Debug.Log("Start Game button clicked! Attempting to hide panel and start game.");

        if (startMenuPanel != null)
        {
            Debug.Log($"StartMenuManager: Found startMenuPanel '{startMenuPanel.name}'. Setting it to inactive.");
            startMenuPanel.SetActive(false);
            if (!startMenuPanel.activeSelf) // Check if it actually became inactive
            {
                Debug.Log("StartMenuManager: startMenuPanel successfully set to inactive.");
            }
            else
            {
                Debug.LogWarning("StartMenuManager: Tried to set startMenuPanel inactive, but it's still active. Check for other scripts controlling it or if it's a prefab instance issue.");
            }
        }
        else
        {
            Debug.LogError("StartMenuManager: startMenuPanel reference is NULL when trying to hide it in StartGame()! Assign it in the Inspector.");
        }

        Time.timeScale = 1f;
        SetGameplayScriptsActive(true);
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
