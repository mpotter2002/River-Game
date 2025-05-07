using UnityEngine;
using TMPro; // Required for TextMeshPro UI elements

public class ScoreManager : MonoBehaviour
{
    // --- Singleton Pattern ---
    // This makes the ScoreManager easily accessible from any other script
    // without needing a direct reference (e.g., ScoreManager.Instance.AddScore(10);)
    public static ScoreManager Instance { get; private set; }
    // --- End Singleton Pattern ---

    [Header("UI Settings")]
    [Tooltip("Assign the TextMeshPro UI element that will display the score.")]
    [SerializeField] private TMP_Text scoreTextElement; // Drag your score display Text (TMP) object here

    private int currentScore = 0;

    // Awake is called when the script instance is being loaded (before Start)
    void Awake()
    {
        // --- Singleton Implementation ---
        if (Instance == null)
        {
            // If no instance exists, this becomes the instance
            Instance = this;
            // Optional: if you want the ScoreManager to persist across scene loads
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            // If an instance already exists, destroy this duplicate one
            // to ensure there's only ever one ScoreManager.
            Debug.LogWarning("Duplicate ScoreManager instance found. Destroying this one.");
            Destroy(gameObject);
        }
        // --- End Singleton Implementation ---
    }

    // Start is called before the first frame update
    void Start()
    {
        // Initialize the score to 0 when the game starts
        currentScore = 0;
        UpdateScoreDisplay(); // Update the UI text to show "Score: 0"
    }

    // Public method that other scripts (like TrashItem.cs) can call to add points
    public void AddScore(int pointsToAdd)
    {
        if (pointsToAdd > 0) // Optional: only add positive points
        {
            currentScore += pointsToAdd;
            UpdateScoreDisplay(); // Refresh the UI text with the new score
            // Debug.Log($"Score is now: {currentScore}"); // Optional: for testing
        }
    }

    // Updates the assigned TextMeshPro UI element with the current score value
    private void UpdateScoreDisplay()
    {
        if (scoreTextElement != null)
        {
            scoreTextElement.text = "Score: " + currentScore;
        }
        else
        {
            // This warning helps if you forget to link the UI element in the Inspector
            Debug.LogWarning("ScoreManager: ScoreTextElement is not assigned in the Inspector!");
        }
    }

    // Optional: A method to get the current score if other scripts need to read it
    public int GetCurrentScore()
    {
        return currentScore;
    }

    // Optional: A method to reset the score (e.g., when starting a new game or from the menu)
    public void ResetScore()
    {
        currentScore = 0;
        UpdateScoreDisplay();
        Debug.Log("Score has been reset to 0.");
    }
}

