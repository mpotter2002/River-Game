using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TrashItem : MonoBehaviour
{
    [Header("Item Properties")]
    [Tooltip("Check this if this trash item is a special Divvy Bike that adds time.")]
    public bool isDivvyBike = false;
    // If you want each Divvy Bike (or other special items) to give a different time bonus,
    // you could add a variable here like:
    // public float specificTimeBonus = 5f;
    // And then use that in OnMouseDown instead of TutorialManager.Instance.divvyBikeTimeBonus

    [Tooltip("Check this if this trash item is a GOOD ITEM that DEDUCTS points if clicked.")]
    public bool isGoodItem = false;
    [Tooltip("How many points to deduct if this is a good item. Usually set by TrashSpawner.")]
    public int goodItemDeductionAmount = 3;

    [Header("Score Popup")]
    [SerializeField] private GameObject scorePopupPrefab;

    // --- Cooldown Logic ---
    private static float lastClickTime = 0f;
    private static readonly float clickCooldown = 0.25f; // Cooldown in seconds (e.g., 250ms)
    // --------------------

    void OnMouseDown()
    {
        // Ensure the script component is enabled before processing clicks
        if (!enabled) return;

        if (Time.unscaledTime < lastClickTime + clickCooldown)
        {
            return; // Cooldown active, ignore this click
        }
        lastClickTime = Time.unscaledTime; // Update the last click time

        Debug.Log($"Trash item clicked: {gameObject.name}");

        // --- Good Item Deduction ---
        if (isGoodItem)
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(-goodItemDeductionAmount);
                SpawnScorePopup(-goodItemDeductionAmount);
            }
            else
            {
                Debug.LogWarning($"TrashItem ({gameObject.name}): ScoreManager.Instance is null! Cannot deduct score for good item.");
            }
            Destroy(gameObject);
            return;
        }

        // --- Add Score ---
        if (ScoreManager.Instance != null)
        {
            if (isDivvyBike)
            {
                // Only show the +15 popup for Divvy Bikes
                ScoreManager.Instance.AddScore(15);
                SpawnScorePopup(15);
            }
            else
            {
                ScoreManager.Instance.AddScore(1); // Add 1 point for any trash
                SpawnScorePopup(1);
            }
        }

        // --- Handle Divvy Bike Time Bonus ---
        if (isDivvyBike)
        {
            if (TutorialManager.Instance != null)
            {
                // Check if the game is actually in the MainGamePlaying phase before adding time
                if (TutorialManager.Instance.currentPhase == TutorialManager.GamePhase.MainGamePlaying)
                {
                    TutorialManager.Instance.AddTimeClock(TutorialManager.Instance.divvyBikeTimeBonus);
                    SpawnScorePopup(15); // Show +15 for Divvy Bike
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

    private void SpawnScorePopup(int score)
    {
        Debug.Log($"[TrashItem] Attempting to spawn score popup for score: {score}");
        if (scorePopupPrefab == null)
        {
            Debug.LogWarning("[TrashItem] scorePopupPrefab is not assigned! Cannot spawn score popup.");
            return;
        }
        Debug.Log("[TrashItem] Using assigned prefab for popup.");
        GameObject popup = Instantiate(scorePopupPrefab);
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            popup.transform.SetParent(canvas.transform, false);
            Vector3 worldPosition = transform.position + Vector3.up * 0.5f;
            Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                screenPosition,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out Vector2 localPoint
            );
            RectTransform popupRect = popup.GetComponent<RectTransform>();
            popupRect.anchoredPosition = localPoint;
            popupRect.localScale = Vector3.one;
            Debug.Log($"[Popup] Local anchored position: {localPoint}");
        }
        else
        {
            Debug.LogWarning("No Canvas found in scene!");
        }
        FloatingScorePopup scorePopup = popup.GetComponent<FloatingScorePopup>();
        if (scorePopup != null)
        {
            scorePopup.SetScore(score);
            Debug.Log("[TrashItem] Set score on popup prefab.");
        }
        else
        {
            Debug.LogWarning("[TrashItem] FloatingScorePopup component missing on prefab!");
        }
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
