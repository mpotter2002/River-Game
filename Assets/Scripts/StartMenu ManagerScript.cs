using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartMenuManager : MonoBehaviour
{
    public static StartMenuManager Instance { get; private set; }

    [Header("UI Elements")]
    [Tooltip("Assign the Start Button from the Title Screen here.")]
    [SerializeField] private Button startButton;

    [Header("Scene Settings")]
    [Tooltip("Name of the scene that contains the TutorialManager")]
    [SerializeField] private string gameSceneName = "GameScene"; // Set this in the Inspector

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Pause game logic initially
        Time.timeScale = 0f;

        // Add a listener to the start button's onClick event
        if (startButton != null)
        {
            startButton.onClick.AddListener(InitiateTutorialOrGame);
        }
        else
        {
            Debug.LogError("StartMenuManager: Start Button is not assigned!");
        }
    }

    public void InitiateTutorialOrGame()
    {
        Debug.Log("StartMenuManager: Start Button clicked!");
        
        // Load the game scene
        SceneManager.LoadScene(gameSceneName);
        
        // The TutorialManager will be found in the new scene
        // and will automatically start the tutorial sequence
    }

    public void QuitGame()
    {
        Debug.Log("StartMenuManager: Quit Game button clicked!");
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
