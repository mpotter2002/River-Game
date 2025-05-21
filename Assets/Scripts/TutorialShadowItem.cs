using UnityEngine;

public class TutorialShadowItem : MonoBehaviour
{
    private TutorialItemData itemData;

    // Call this method after instantiating the shadow item to give it its data
    public void Initialize(TutorialItemData data)
    {
        itemData = data;
        if (itemData == null)
        {
            Debug.LogError("TutorialShadowItem initialized with null data for " + gameObject.name + "! Destroying shadow object.");
            Destroy(gameObject); // Destroy if no data, to prevent further errors
        }
    }

    void OnMouseDown()
    {
        // Ensure the script component is enabled before processing clicks
        if (!enabled) return;

        if (itemData == null)
        {
            Debug.LogError("TutorialShadowItem clicked but has no itemData on " + gameObject.name + "!");
            return;
        }

        // Notify the TutorialManager that this shadow was clicked
        if (TutorialManager.Instance != null)
        {
            // Check if the TutorialManager is in the correct state to process this click
            // This uses the 'currentPhase' and 'GamePhase' enum from the TutorialManager
            if (TutorialManager.Instance.currentPhase == TutorialManager.GamePhase.TutorialPlaying)
            {
                TutorialManager.Instance.ShadowClicked(itemData);
            }
            else
            {
                Debug.LogWarning("TutorialShadowItem: Clicked, but TutorialManager is not in TutorialPlaying state. Current phase: " + TutorialManager.Instance.currentPhase);
            }
        }
        else
        {
            Debug.LogError("TutorialShadowItem: TutorialManager.Instance is null! Cannot process click for " + gameObject.name);
        }

        // Destroy the shadow object after it's clicked and processed.
        // This ensures it doesn't remain clickable after the reveal panel is shown.
        Destroy(gameObject);
    }

    // Optional: Add a visual cue if the mouse hovers over it
    void OnMouseEnter()
    {
        if (!enabled) return;
        // Example: Slightly change color or scale if you have a SpriteRenderer
        // SpriteRenderer sr = GetComponent<SpriteRenderer>();
        // if (sr != null)
        // {
        //     // sr.color = Color.gray; // Be careful with direct color changes
        // }
    }

    void OnMouseExit()
    {
        if (!enabled) return;
        // Example: Revert color or scale
        // SpriteRenderer sr = GetComponent<SpriteRenderer>();
        // if (sr != null)
        // {
        //     // sr.color = Color.white; // Revert to original
        // }
    }
}
