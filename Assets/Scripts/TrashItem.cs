using UnityEngine;

public class TrashItem : MonoBehaviour
{
    // This function is automatically called by Unity when 
    // the GameObject's Collider is clicked by the mouse (or tapped on mobile).
    // Requirements: 
    // 1. This script must be on the GameObject you want to click.
    // 2. The GameObject must have a Collider2D component.
    // 3. There must be an EventSystem in your scene (usually added automatically with UI Canvas, or add manually).
    // 4. The Camera viewing this object needs a Physics 2D Raycaster component if using UI elements or complex setups, 
    //    but for simple Collider clicks, it often works directly.
    void OnMouseDown()
    {
        Debug.Log($"Trash item clicked: {gameObject.name}"); // Optional: for testing

        // --- Optional Enhancements ---
        // Add points to a score manager
        // ScoreManager.Instance.AddScore(10); 

        // Play a sound effect
        // SoundManager.Instance.PlayCollectSound();

        // Spawn a particle effect at the trash's position
        // if (collectionEffectPrefab != null) {
        //     Instantiate(collectionEffectPrefab, transform.position, Quaternion.identity);
        // }
        // --- End Optional Enhancements ---

        // Destroy the GameObject this script is attached to
        Destroy(gameObject); 
    }

    // --- Optional: Add these variables if you want effects/score ---
    // [SerializeField] private GameObject collectionEffectPrefab; // Assign a particle effect prefab in the Inspector
    // public int pointsValue = 10; // Assign points value per trash type
    // --- End Optional ---
}