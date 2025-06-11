using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ClickableKayak : MonoBehaviour
{
    [Header("Popup Settings")]
    [SerializeField] private GameObject popupPrefab;
    [SerializeField] private float popupDuration = 3f;

    private bool hasShownPopup = false;

    private void OnMouseDown()
    {
        // Check if we're in the main game phase
        if (TutorialManager.Instance != null && TutorialManager.Instance.currentPhase == TutorialManager.GamePhase.MainGamePlaying)
        {
            Debug.Log("Kayak clicked!"); // Debug log for click
            if (!hasShownPopup)
            {
                Debug.Log("Showing popup..."); // Debug log for popup
                ShowPopup();
                hasShownPopup = true;
            }
            else
            {
                Debug.Log("Popup already shown for this kayak"); // Debug log if already shown
            }
        }
        else
        {
            Debug.Log("Kayak clicked but not in main game phase - ignoring click"); // Debug log for ignored click
        }
    }

    private void ShowPopup()
    {
        if (popupPrefab != null)
        {
            Debug.Log("Popup prefab found, creating popup"); // Debug log for prefab check
            // Create the popup
            GameObject popup = Instantiate(popupPrefab);
            
            // Find the canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                Debug.Log("Canvas found, setting up popup"); // Debug log for canvas
                // Set the popup as a child of the canvas
                popup.transform.SetParent(canvas.transform, false);
                
                // Position the popup above the kayak
                Vector3 worldPosition = transform.position + Vector3.up * 2.0f;
                Vector3 screenPosition = Camera.main.WorldToScreenPoint(worldPosition);
                
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvas.transform as RectTransform,
                    screenPosition,
                    canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                    out Vector2 localPoint
                );
                
                RectTransform popupRect = popup.GetComponent<RectTransform>();
                popupRect.anchoredPosition = localPoint;
                // Do NOT set localScale or sizeDelta here if your prefab is already correct
                // LayoutRebuilder.ForceRebuildLayoutImmediate(popupRect); // Optional: only if you have layout issues

                // Add the KayakPopup component to handle the animation
                KayakPopup kayakPopup = popup.AddComponent<KayakPopup>();
                kayakPopup.SetDuration(popupDuration);

                // Destroy the popup after duration
                Destroy(popup, popupDuration);
                Debug.Log("Popup created and set up successfully"); // Debug log for success
            }
            else
            {
                Debug.LogWarning("No Canvas found in scene!");
            }
        }
        else
        {
            Debug.LogWarning("Popup prefab not assigned!");
        }
    }

    // Reset the hasShownPopup flag when the kayak is respawned
    private void OnEnable()
    {
        hasShownPopup = false;
        Debug.Log("Kayak enabled, hasShownPopup reset to false"); // Debug log for enable
    }
} 