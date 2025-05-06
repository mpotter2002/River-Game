using UnityEngine;

public class SkyscraperSpawner : MonoBehaviour
{
    [Header("Skyscraper Sequence")]
    [Tooltip("Assign the UNIQUE skyscraper chunk prefabs HERE, in the order they should appear vertically.")]
    [SerializeField] private GameObject[] skyscraperSequencePrefabs; // Array holds the sequence to spawn

    [Header("Placement Settings")]
    [Tooltip("The vertical height of ONE skyscraper chunk prefab. Assumes ALL skyscraper prefabs in the sequence have the SAME height.")]
    [SerializeField] private float chunkHeight = 20f; // <<< SET THIS ACCURATELY!
    [Tooltip("The vertical distance (in world units) AFTER one sequence ends BEFORE the next sequence starts.")]
    [SerializeField] private float distanceBetweenSequences = 100f; // <<< The gap between sequences
    [Tooltip("The world Y-coordinate where the very FIRST sequence should start spawning.")]
    [SerializeField] private float firstSequenceStartY = 50f; // Where the bottom of the first sequence appears

    [Header("References")]
    [Tooltip("Assign the main camera here (or leave empty if tagged 'MainCamera').")]
    [SerializeField] private Camera mainCamera;

    // Internal variables
    private float nextSequenceStartY; // Tracks the calculated Y position for the bottom of the next sequence
    private float sequenceTotalHeight; // Calculated total height of one full sequence
    private float cameraHalfHeight; // To calculate camera view edges

    void Start()
    {
        // --- Initial Setup and Error Checking ---
        if (skyscraperSequencePrefabs == null || skyscraperSequencePrefabs.Length == 0) {
            Debug.LogWarning("SkyscraperSpawner: No Skyscraper Chunk Prefabs assigned. Nothing will be spawned.");
            enabled = false; return;
        }
        if (chunkHeight <= 0) {
            Debug.LogError("SkyscraperSpawner: Chunk Height must be a positive value!"); enabled = false; return;
        }
        if (mainCamera == null) {
            mainCamera = Camera.main;
            if (mainCamera == null) {
                 Debug.LogError("SkyscraperSpawner: Main Camera reference not set/found!"); enabled = false; return;
            }
        }

        // Calculate the total height of one sequence stack
        sequenceTotalHeight = skyscraperSequencePrefabs.Length * chunkHeight;
         Debug.Log($"Calculated sequenceTotalHeight = {sequenceTotalHeight} ({skyscraperSequencePrefabs.Length} prefabs * {chunkHeight} height)");
        if (sequenceTotalHeight <= 0) {
             Debug.LogError("SkyscraperSpawner: Sequence total height is zero or negative. Check chunkHeight and array size.");
             enabled = false; return;
        }

        // Initialize where the first sequence should start
        nextSequenceStartY = firstSequenceStartY;
        cameraHalfHeight = mainCamera.orthographicSize;

        // Optional: Spawn initial sequence immediately if camera starts high enough
        // CheckInitialSpawn(); // You could add this if needed
        
    }


    void Update()
    {
        // Check if the camera is high enough to trigger the *next* sequence spawn
        CheckAndSpawnSequence();
    }

    void CheckAndSpawnSequence()
    {
        // Calculate a trigger point based on camera view - when camera top edge reaches the next spawn point
        // Adding cameraHalfHeight means trigger when center reaches it, +2*cameraHalfHeight means when bottom edge reaches it.
        // Let's trigger when the camera's center is approaching the spawn point. Add lookahead?
        // Simpler: Trigger when the *calculated start Y* enters the camera's view + a buffer.
        float cameraTopEdge = mainCamera.transform.position.y + cameraHalfHeight;
        // Add a small buffer so it spawns slightly before it's needed on screen
        float triggerPointY = cameraTopEdge + chunkHeight; // Spawn when the target Y is roughly one chunk height above the view

        if (triggerPointY >= nextSequenceStartY)
 {
     float spawnStart = nextSequenceStartY; // Store the Y where this sequence starts       

     // Spawn the sequence starting at the calculated Y
     SpawnSkyscraperSequence(spawnStart);

     // --- ADD THIS LOG ---
     float oldNextY = nextSequenceStartY; // Store old value for logging
     // Calculate the starting Y position for the sequence AFTER this one
     nextSequenceStartY += sequenceTotalHeight + distanceBetweenSequences;
     Debug.Log($"SPAWNED Sequence starting at {spawnStart:F2}. Updated nextSequenceStartY from {oldNextY:F2} to {nextSequenceStartY:F2} (SeqHeight={sequenceTotalHeight:F2}, Gap={distanceBetweenSequences:F2})");
     // --------------------
 }
    }

    // Spawns all prefabs in the sequence, stacked vertically starting at startY
    void SpawnSkyscraperSequence(float startY)
    {
         Debug.Log($"SkyscraperSpawner: Spawning sequence starting at Y: {startY}");
        for (int i = 0; i < skyscraperSequencePrefabs.Length; i++)
        {
            GameObject prefabToSpawn = skyscraperSequencePrefabs[i];

            if (prefabToSpawn == null) {
                Debug.LogWarning($"SkyscraperSpawner: Prefab at index {i} is null. Skipping.");
                continue; // Skip this iteration
            }

            // Calculate the spawn position for this chunk in the sequence
            // Stacks them upwards from the startY based on index and chunkHeight
            float spawnY = startY + (i * chunkHeight);
            Vector3 spawnPos = new Vector3(transform.position.x, spawnY, transform.position.z);

            // Debug.Log($"-- Spawning Skyscraper {i + 1}/{skyscraperSequencePrefabs.Length}: {prefabToSpawn.name} at Y: {spawnY}");
            Instantiate(prefabToSpawn, spawnPos, Quaternion.identity, transform); // Instantiate as child of spawner
        }
    }
    

}

