using UnityEngine;
using System.Collections.Generic; // Required for List

public class RiverBackgroundGenerator : MonoBehaviour
{
    [Header("Chunk Prefabs")]
    [Tooltip("Assign your two different river background chunk prefabs here. They must be the same height.")]
    [SerializeField] private GameObject[] riverChunkPrefabs; // Array to hold the two different river prefabs

    [Header("Generation Settings")]
    [Tooltip("The exact height of ONE River Chunk prefab in Unity units. Both prefabs must share this height.")]
    [SerializeField] private float chunkHeight = 20f; // <<< SET THIS ACCURATELY for the RIVER prefabs!
    [Tooltip("How far above the camera's view edge should we spawn the next chunk?")]
    [SerializeField] private float spawnAheadDistance = 5f;
    [Tooltip("How far below the camera's view edge should a chunk be before we destroy it?")]
    [SerializeField] private float despawnDistanceBelowCamera = 10f;

    [Header("References")]
    [Tooltip("Assign the main camera here (or leave empty if tagged 'MainCamera').")]
    [SerializeField] private Camera mainCamera;

    // Internal variables
    private List<GameObject> activeChunks = new List<GameObject>();
    private float nextSpawnY;
    private float cameraHalfHeight;

    void Start()
    {
        // --- Initial Setup and Error Checking ---
        if (riverChunkPrefabs == null || riverChunkPrefabs.Length == 0)
        {
            Debug.LogError("RiverBackgroundGenerator: No River Chunk Prefabs assigned in the array!");
            enabled = false; // Disable script if not set up
            return;
        }
        // Check if any assigned prefab in the array is null
        for (int i = 0; i < riverChunkPrefabs.Length; i++)
        {
            if (riverChunkPrefabs[i] == null)
            {
                Debug.LogError($"RiverBackgroundGenerator: River Chunk Prefab at index {i} is not assigned!");
                enabled = false;
                return;
            }
        }

        if (chunkHeight <= 0)
        {
            Debug.LogError("RiverBackgroundGenerator: Chunk Height must be a positive value!");
            enabled = false;
            return;
        }
        if (mainCamera == null)
        {
            mainCamera = Camera.main; // Attempt to find camera tagged "MainCamera"
            if (mainCamera == null)
            {
                 Debug.LogError("RiverBackgroundGenerator: Main Camera reference not set and 'MainCamera' tag not found!");
                 enabled = false;
                 return;
            }
        }

        cameraHalfHeight = mainCamera.orthographicSize;
        // Start spawning from slightly below the camera's initial position
        nextSpawnY = mainCamera.transform.position.y - cameraHalfHeight;

        // Spawn initial chunks to fill the screen and the area above
        SpawnInitialChunks();
    }

    // Called from Start to ensure the screen is filled initially
    void SpawnInitialChunks()
    {
        // Determine the highest Y position needed initially to cover the view plus the spawn-ahead distance
        float initialSpawnCeiling = mainCamera.transform.position.y + cameraHalfHeight + spawnAheadDistance;
        // Spawn chunks starting from below the camera up to the ceiling
        while (nextSpawnY < initialSpawnCeiling)
        {
            SpawnChunk(nextSpawnY);
        }
    }

    // Called every frame
    void Update()
    {
        // Continuously check if we need to spawn more chunks ahead of the moving camera
        TrySpawnNextChunk();

        // Continuously check if we need to despawn chunks that are far behind the camera
        DespawnOldChunks();
    }

    // Checks if the camera has moved high enough to warrant spawning the next chunk
    void TrySpawnNextChunk()
    {
        // Calculate the Y position threshold (camera's top edge + buffer) that triggers a new spawn
        float spawnTriggerY = mainCamera.transform.position.y + cameraHalfHeight + spawnAheadDistance;

        // Use a 'while' loop instead of 'if'. This handles cases where the camera might move
        // faster than one chunk height in a single frame, ensuring we spawn enough chunks to catch up.
        while (nextSpawnY < spawnTriggerY)
        {
            SpawnChunk(nextSpawnY);
        }
    }

    // Handles the logic for spawning a single chunk at the specified Y position
    void SpawnChunk(float spawnY)
    {
        // --- Randomly select one of the river prefabs ---
        if (riverChunkPrefabs == null || riverChunkPrefabs.Length == 0)
        {
            Debug.LogError("RiverBackgroundGenerator: riverChunkPrefabs array is null or empty in SpawnChunk. Cannot spawn.");
            return; // Critical error, cannot proceed
        }
        int randomIndex = Random.Range(0, riverChunkPrefabs.Length); // Generates an index from 0 to Length-1
        GameObject prefabToSpawn = riverChunkPrefabs[randomIndex];

        if (prefabToSpawn == null)
        {
            Debug.LogError($"RiverBackgroundGenerator: Prefab at random index {randomIndex} is null. Check array assignments.");
            return; // Cannot proceed
        }
        // --- End random selection ---

        // --- Instantiate the chosen chunk ---
        // Calculate the spawn position vector (using the Generator's X/Z for alignment)
        Vector3 spawnPos = new Vector3(transform.position.x, spawnY, transform.position.z);

        // Create the new chunk instance as a child of this generator object (for organization in Hierarchy)
        GameObject newChunk = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity, transform);
        // Add the newly created chunk to our list of active chunks
        activeChunks.Add(newChunk);

        // --- Update the Y position for the *next* potential spawn ---
        nextSpawnY += chunkHeight; // Always use the shared river chunk height
    }

    // Handles cleaning up chunks that are far below the camera's view
    void DespawnOldChunks()
    {
        // Calculate the Y position threshold below which chunks should be destroyed
        float despawnTriggerY = mainCamera.transform.position.y - cameraHalfHeight - despawnDistanceBelowCamera;

        // Create a temporary list to store chunks that need to be removed.
        // This is safer than modifying the 'activeChunks' list while iterating through it.
        List<GameObject> chunksToRemove = new List<GameObject>();

        foreach (GameObject chunk in activeChunks)
        {
            // Calculate the position of the top edge of this chunk.
            // This assumes the chunk's pivot point is in its center.
            // If pivot is at the bottom, use: chunk.transform.position.y + chunkHeight
            float chunkTopEdgeY = chunk.transform.position.y + (chunkHeight / 2f);

            // If the chunk's top edge is below the despawn line...
            if (chunkTopEdgeY < despawnTriggerY)
            {
                // Mark it for removal
                chunksToRemove.Add(chunk);
            }
        }

        // Now, safely remove and destroy the marked chunks
        foreach (GameObject chunkToRemove in chunksToRemove)
        {
            activeChunks.Remove(chunkToRemove); // Remove from the tracking list
            Destroy(chunkToRemove); // Destroy the actual GameObject from the scene
            // Debug.Log("RiverBackgroundGenerator: Despawned chunk."); // Optional message
        }
    }

    public void ResetRiver(float newY)
    {
        // Destroy all active chunks
        foreach (var chunk in activeChunks)
        {
            if (chunk != null) Destroy(chunk);
        }
        activeChunks.Clear();

        // Reset position and nextSpawnY
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        cameraHalfHeight = mainCamera != null ? mainCamera.orthographicSize : 0f;
        nextSpawnY = newY;

        // Respawn initial chunks to fill the screen
        SpawnInitialChunks();
    }
}
