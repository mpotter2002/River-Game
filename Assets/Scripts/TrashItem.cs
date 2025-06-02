using UnityEngine;

public class TrashItem : MonoBehaviour
{
    [Header("Item Properties")]
    [Tooltip("Check this if this trash item is a special Divvy Bike that adds time.")]
    public bool isDivvyBike = false;
    // If you want each Divvy Bike (or other special items) to give a different time bonus,
    // you could add a variable here like:
    // public float specificTimeBonus = 5f;
    // And then use that in OnMouseDown instead of TutorialManager.Instance.divvyBikeTimeBonus

    // --- Cooldown Logic ---
    private static float lastClickTime = 0f;
    private static readonly float clickCooldown = 0.25f; // Cooldown in seconds (e.g., 250ms)
    // --------------------

    void OnMouseDown()
    {
        // Ensure the script component is enabled before processing clicks
        if (!enabled) return;

        Debug.Log($"Trash item clicked: {gameObject.name}");

         if (Time.unscaledTime < lastClickTime + clickCooldown)
        {
            // Debug.Log($"TrashItem Click Cooldown Active. Time since last click: {Time.unscaledTime - lastClickTime}");
            return; // Cooldown active, ignore this click
        }
        lastClickTime = Time.unscaledTime; // Update the last click time
        // ----------------------

        Debug.Log($"Trash item clicked: {gameObject.name}");


        // --- Add Score ---
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(1); // Add 1 point for any trash
        }
        else
        {
             Debug.LogWarning($"TrashItem ({gameObject.name}): ScoreManager.Instance is null! Cannot add score.");
        }

        // --- Handle Divvy Bike Time Bonus ---
        if (isDivvyBike)
        {
            if (TutorialManager.Instance != null)
            {
                // Check if the game is actually in the MainGamePlaying phase before adding time
                // This uses the 'currentPhase' and 'GamePhase' enum from the TutorialManager
                if (TutorialManager.Instance.currentPhase == TutorialManager.GamePhase.MainGamePlaying)
                {
                    TutorialManager.Instance.AddTimeClock(TutorialManager.Instance.divvyBikeTimeBonus);
                }
                else
                {
                    Debug.LogWarning($"TrashItem ({gameObject.name}): Divvy Bike clicked, but game is not in MainGamePlaying phase. Current phase: {TutorialManager.Instance.currentPhase}. No time added.");
                }
            }
            else
            {
                Debug.LogWarning($"TrashItem ({gameObject.name}): TutorialManager.Instance is null! Cannot add time for Divvy Bike.");
            }
        }

        // Destroy the GameObject this script is attached to
        Destroy(gameObject);
    }

    // Optional: Visual feedback on hover
    void OnMouseEnter()
    {
        // Example: Change scale or color slightly if you have a SpriteRenderer
        // SpriteRenderer sr = GetComponent<SpriteRenderer>();
        // if (sr != null) { sr.color = Color.yellow; }
    }

    void OnMouseExit()
    {
        // Example: Revert visual feedback
        // SpriteRenderer sr = GetComponent<SpriteRenderer>();
        // if (sr != null) { sr.color = Color.white; }
    }
}
