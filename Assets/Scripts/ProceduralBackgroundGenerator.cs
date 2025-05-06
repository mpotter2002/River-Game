using UnityEngine;
using System.Collections.Generic; // Required for List

public class ProceduralBackgroundGenerator : MonoBehaviour
{
    [Header("Chunk Prefabs")]
    [Tooltip("The standard repeating background/river chunk prefab.")]
    [SerializeField] private GameObject riverChunkPrefab;
    [Tooltip("The unique skyscraper chunk prefab.")]
    [SerializeField] private GameObject skyscraperChunkPrefab;

    [Header("Generation Settings")]
    [Tooltip("The exact height of ONE chunk prefab in Unity units. Assumes both prefabs have the SAME height for this basic version.")]
    [SerializeField] private float chunkHeight = 20f; // <<< SET THIS ACCURATELY in the Inspector!
    [Tooltip("How far above the camera's view edge should we spawn the next chunk?")]
    [SerializeField] private float spawnAheadDistance = 5f;
    [Tooltip("How far below the camera's view edge should a chunk be before we destroy it?")]
    [SerializeField] private float despawnDistanceBelowCamera = 10f;

    [Header("Skyscraper Control")]
    [Tooltip("The world Y-coordinate the camera must reach (or exceed) to trigger spawning the skyscraper chunk.")]
    [SerializeField] private float skyscraperSpawnTriggerY = 500f;

    [Header("References")]
    [Tooltip("Assign the main camera here (or leave empty if tagged 'MainCamera').")]
    [SerializeField] private Camera mainCamera;

    // Internal variables used by the script
    private List<GameObject> activeChunks = new List<GameObject>();
    private float nextSpawnY; // Tracks where the next chunk should be placed vertically
    private bool skyscraperHasSpawned = false; // Flag to ensure skyscraper only spawns once
    private float cameraHalfHeight; // Calculated vertical size of half the camera's view

    void Start()
    {
        // --- Initial Setup and Error Checking ---
        // Ensure prefabs are assigned in the Inspector
        if (riverChunkPrefab == null || skyscraperChunkPrefab == null)
        {
            Debug.LogError("ProceduralBackgroundGenerator: Assign both River and Skyscraper chunk prefabs in the Inspector!");
            enabled = false; // Disable this script if setup is incomplete
            return;
        }
        // Ensure chunk height is valid
        if (chunkHeight <= 0)
        {
            Debug.LogError("ProceduralBackgroundGenerator: Chunk Height must be a positive value!");
            enabled = false;
            return;
        }
        // Ensure camera reference is valid
        if (mainCamera == null)
        {
            mainCamera = Camera.main; // Attempt to find camera tagged "MainCamera"
            if (mainCamera == null)
            {
                 Debug.LogError("ProceduralBackgroundGenerator: Main Camera reference not set and 'MainCamera' tag not found!");
                 enabled = false;
                 return;
            }
        }

        // Calculate half the camera's vertical view size (for orthographic)
        cameraHalfHeight = mainCamera.orthographicSize;
        // Initialize the Y position for the very first spawn, starting relative to the camera
        nextSpawnY = mainCamera.transform.position.y - cameraHalfHeight;

        // Spawn enough initial chunks to fill the screen and the area above it right at the start
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
        // --- Logic to decide which prefab to spawn ---
        GameObject prefabToSpawn = riverChunkPrefab; // Default to the river chunk

        // Check if the skyscraper hasn't spawned yet AND if the current spawn position meets/exceeds the trigger Y
        if (!skyscraperHasSpawned && spawnY >= skyscraperSpawnTriggerY)
        {
            prefabToSpawn = skyscraperChunkPrefab; // Switch to the skyscraper prefab
            skyscraperHasSpawned = true; // Set the flag so it doesn't spawn again
            Debug.Log($"ProceduralBackgroundGenerator: Spawning Skyscraper Chunk at Y: {spawnY}"); // Optional message
        }
        // ---------------------------------------------

        // Double-check if the chosen prefab is valid before trying to instantiate
        if (prefabToSpawn == null)
        {
            Debug.LogError("ProceduralBackgroundGenerator: Prefab to spawn is null! Check assignments.");
            return; // Exit the function to prevent errors
        }

        // --- Instantiate the chosen chunk ---
        // Calculate the spawn position vector (using the Generator's X/Z for alignment)
        Vector3 spawnPos = new Vector3(transform.position.x, spawnY, transform.position.z);

        // Create the new chunk instance as a child of this generator object (for organization in Hierarchy)
        GameObject newChunk = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity, transform);
        // Add the newly created chunk to our list of active chunks
        activeChunks.Add(newChunk);

        // --- Update the Y position for the *next* potential spawn ---
        // !! IMPORTANT ASSUMPTION: This assumes riverChunkPrefab and skyscraperChunkPrefab have the SAME height (chunkHeight) !!
        // If their heights differ, you'll need more complex logic here to get the height of the 'prefabToSpawn'.
        nextSpawnY += chunkHeight;
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
            // Debug.Log("ProceduralBackgroundGenerator: Despawned chunk."); // Optional message
        }
    }
}