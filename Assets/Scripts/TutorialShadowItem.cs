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
        if (!enabled) return; // Do nothing if the script component is disabled

        if (itemData == null)
        {
            Debug.LogError("TutorialShadowItem clicked but has no itemData on " + gameObject.name + "!");
            return;
        }

        // Notify the TutorialManager that this shadow was clicked
        if (TutorialManager.Instance != null)
        {
            // Check if the TutorialManager is in the correct state to process this click
            if (TutorialManager.Instance.currentState == TutorialManager.TutorialState.TutorialPlaying)
            {
                TutorialManager.Instance.ShadowClicked(itemData);
            }
            else
            {
                Debug.LogWarning("TutorialShadowItem: Clicked, but TutorialManager is not in TutorialPlaying state. Current state: " + TutorialManager.Instance.currentState);
            }
        }
        else
        {
            Debug.LogError("TutorialShadowItem: TutorialManager.Instance is null! Cannot process click for " + gameObject.name);
        }

        // Destroy the shadow object after it's clicked and processed
        // Ensure it's only destroyed if the click was valid and processed by the manager
        // Or, let the TutorialManager decide when to destroy it after showing the reveal panel.
        // For now, let's destroy it immediately after notifying the manager.
        Destroy(gameObject);
    }

    // Optional: Add a visual cue if the mouse hovers over it
    void OnMouseEnter()
    {
        if (!enabled) return;
        // Example: Slightly change color or scale if you have a SpriteRenderer
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // sr.color = Color.gray; // Be careful with direct color changes if you have other effects
        }
    }

    void OnMouseExit()
    {
        if (!enabled) return;
        // Example: Revert color or scale
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // sr.color = Color.white; // Revert to original
        }
    }
}
