using UnityEngine;
using System.Collections; // Required for Coroutines

public class TrashSpawner : MonoBehaviour
{
    [Header("Trash Prefabs")]
    [SerializeField] private GameObject[] trashPrefabs; // Array to hold different trash prefabs

    // --- Values below are now LOCAL OFFSETS from this object's position (which should be parented to the camera) ---
    [Header("Spawning Area (Local Offsets from Camera)")] 
    [Tooltip("Minimum X offset from the camera's center.")]
    [SerializeField] private float spawnAreaMinX = -5f;
    [Tooltip("Maximum X offset from the camera's center.")]
    [SerializeField] private float spawnAreaMaxX = 5f;
    
    [Tooltip("Minimum Y offset from the camera's center.")]
    [SerializeField] private float spawnAreaMinY = 6f; // Example: Start spawning just ABOVE camera view
    [Tooltip("Maximum Y offset from the camera's center.")]
    [SerializeField] private float spawnAreaMaxY = 10f;// Example: Spawn range above camera view
    // --- End Local Offset Variables ---

    [Header("Spawning Timing")]
    [Tooltip("Minimum time delay (seconds) between spawns.")]
    [SerializeField] private float minSpawnDelay = 0.5f;
    [Tooltip("Maximum time delay (seconds) between spawns.")]
    [SerializeField] private float maxSpawnDelay = 2.0f;
    [Tooltip("Initial delay (seconds) before the first spawn.")]
    [SerializeField] private float initialDelay = 1.0f;

    void Start()
    {
        // Basic check to ensure prefabs are assigned
        if (trashPrefabs == null || trashPrefabs.Length == 0)
        {
            Debug.LogError("Trash Spawner: No trash prefabs assigned in the Inspector!");
            enabled = false; // Disable the script if no prefabs
            return;
        }
        
        // Check if parented, suggest parenting to camera if not (optional warning)
        if (transform.parent == null || Camera.main == null || transform.parent != Camera.main.transform)
        {
             Debug.LogWarning("TrashSpawner: For Option 1 setup, this GameObject should be parented to the Main Camera.");
        }

        // Start the spawning loop as a Coroutine
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        // Wait for the initial delay before starting
        yield return new WaitForSeconds(initialDelay);

        // Infinite loop to keep spawning trash while the script is active
        while (true) 
        {
            // Wait for a random amount of time before spawning the next trash
            float randomDelay = Random.Range(minSpawnDelay, maxSpawnDelay);
            yield return new WaitForSeconds(randomDelay);

            // Spawn a single piece of trash
            SpawnTrash();
        }
    }

    void SpawnTrash()
    {
        // 1. Select a random trash prefab from the array
        int randomIndex = Random.Range(0, trashPrefabs.Length);
        GameObject prefabToSpawn = trashPrefabs[randomIndex];

        // 2. Calculate random LOCAL offset position relative to the spawner
        float randomXOffset = Random.Range(spawnAreaMinX, spawnAreaMaxX);
        float randomYOffset = Random.Range(spawnAreaMinY, spawnAreaMaxY);
        Vector3 localSpawnOffset = new Vector3(randomXOffset, randomYOffset, 0f); 

        // 3. Calculate the WORLD spawn position by adding the offset to the spawner's current world position
        //    Since the spawner is parented to the camera, transform.position IS the camera's world position 
        //    (assuming spawner's local position is 0,0,0 relative to camera).
        Vector3 spawnPosition = transform.position + localSpawnOffset; 

        // 4. Instantiate (create) the selected trash prefab at the calculated position
        GameObject newTrash = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);

        // Debug.Log($"Spawned {newTrash.name} at {spawnPosition}"); // Optional: for testing
    }

    // Optional: Visualize the spawn area RECTANGLE relative to this object's position
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        
        // Use the spawner's current world position as the center for drawing offsets
        Vector3 center = transform.position; 

        // Define the 4 corners using the center and the local offsets
        Vector3 topLeft = center + new Vector3(spawnAreaMinX, spawnAreaMaxY, 0);
        Vector3 topRight = center + new Vector3(spawnAreaMaxX, spawnAreaMaxY, 0);
        Vector3 bottomLeft = center + new Vector3(spawnAreaMinX, spawnAreaMinY, 0);
        Vector3 bottomRight = center + new Vector3(spawnAreaMaxX, spawnAreaMinY, 0);

        // Draw the 4 lines connecting the corners
        Gizmos.DrawLine(topLeft, topRight);     // Top edge
        Gizmos.DrawLine(topRight, bottomRight); // Right edge
        Gizmos.DrawLine(bottomRight, bottomLeft);// Bottom edge
        Gizmos.DrawLine(bottomLeft, topLeft);   // Left edge
    }
}