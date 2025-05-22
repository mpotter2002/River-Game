using UnityEngine;
using System.Collections.Generic; // Required for List
using System.Linq; // Required for OrderBy

public class SkyscraperSpawner : MonoBehaviour
{
    [Header("Regular Skyscraper Sequence")]
    [Tooltip("Assign the repeating skyscraper chunk prefabs HERE, in the order they should appear vertically.")]
    [SerializeField] private GameObject[] skyscraperSequencePrefabs;
    [Tooltip("The vertical height of ONE regular skyscraper chunk prefab. Assumes ALL regular sequence prefabs have this SAME height.")]
    [SerializeField] private float regularChunkHeight = 20f;
    [Tooltip("The vertical distance (in world units) AFTER one regular sequence ends BEFORE the next regular sequence (or special building) starts.")]
    [SerializeField] private float distanceBetweenElements = 100f;
    [Tooltip("The world Y-coordinate where the very FIRST regular sequence (or special building if its trigger is earlier) should start spawning.")]
    [SerializeField] private float firstElementStartY = 50f;

    [Header("Special One-Off Buildings")]
    [Tooltip("List of unique buildings to spawn at specific Y-coordinates.")]
    public List<SpecialBuildingInfo> specialBuildings = new List<SpecialBuildingInfo>();

    [Header("References")]
    [Tooltip("Assign the main camera here (or leave empty if tagged 'MainCamera').")]
    [SerializeField] private Camera mainCamera;

    // Internal variables
    private float nextElementStartY; // Tracks the Y position for the bottom of the next element (regular or special)
    private float regularSequenceTotalHeight;
    private float cameraHalfHeight;

    void Start()
    {
        // --- Initial Setup and Error Checking ---
        if ((skyscraperSequencePrefabs == null || skyscraperSequencePrefabs.Length == 0) && (specialBuildings == null || specialBuildings.Count == 0)) {
            Debug.LogWarning("SkyscraperSpawner: No Regular OR Special Building Prefabs assigned. Nothing will be spawned.");
            enabled = false; return;
        }
        if (regularChunkHeight <= 0 && (skyscraperSequencePrefabs != null && skyscraperSequencePrefabs.Length > 0) ) { // Only error if regular prefabs exist
            Debug.LogError("SkyscraperSpawner: Regular Chunk Height must be a positive value if regular sequences are used!"); enabled = false; return;
        }
        if (mainCamera == null) {
            mainCamera = Camera.main;
            if (mainCamera == null) {
                 Debug.LogError("SkyscraperSpawner: Main Camera reference not set/found!"); enabled = false; return;
            }
        }

        // Calculate the total height of one regular sequence stack
        if (skyscraperSequencePrefabs != null && skyscraperSequencePrefabs.Length > 0)
        {
            regularSequenceTotalHeight = skyscraperSequencePrefabs.Length * regularChunkHeight;
            if (regularSequenceTotalHeight <= 0) {
                 Debug.LogError("SkyscraperSpawner: Regular sequence total height is zero or negative. Check chunkHeight and array size.");
                 // Allow to continue if only special buildings are used
            }
        } else {
            regularSequenceTotalHeight = 0; // No regular sequences
        }


        // Sort special buildings by their trigger Y so we process them in order
        if (specialBuildings != null)
        {
            specialBuildings = specialBuildings.OrderBy(sb => sb.triggerWorldY).ToList();
            // Reset hasSpawned flag for all special buildings at game start
            foreach (var sb in specialBuildings) {
                sb.hasSpawned = false;
            }
        }

        nextElementStartY = firstElementStartY;
        cameraHalfHeight = mainCamera.orthographicSize;
    }

    void Update()
    {
        CheckAndSpawnNextElement();
    }

    void CheckAndSpawnNextElement()
    {
        // Calculate a trigger point based on camera view - when camera top edge is approaching where the next element should start
        float cameraVisibleTopY = mainCamera.transform.position.y + cameraHalfHeight;
        // Add a lookahead buffer (e.g., one chunk height, or a fixed value)
        // This ensures we spawn things before they are strictly needed on screen.
        float spawnLookaheadBuffer = regularChunkHeight > 0 ? regularChunkHeight : 20f; // Use regular chunk height or a default
        float cameraTriggerY = cameraVisibleTopY + spawnLookaheadBuffer;


        if (cameraTriggerY >= nextElementStartY)
        {
            bool specialBuildingSpawnedThisCycle = false;

            // --- 1. Check for Special Buildings first ---
            if (specialBuildings != null)
            {
                foreach (SpecialBuildingInfo specialBuilding in specialBuildings)
                {
                    // Check if this special building hasn't spawned, and if its trigger Y
                    // is at or before where the next element is due to start.
                    // We also check if the camera has actually reached its trigger Y.
                    if (!specialBuilding.hasSpawned && mainCamera.transform.position.y >= specialBuilding.triggerWorldY && specialBuilding.triggerWorldY <= nextElementStartY + (specialBuilding.buildingHeight/2f) /*Ensure it's relevant now*/)
                    {
                        if (specialBuilding.prefab == null) {
                            Debug.LogError($"SkyscraperSpawner: Special building '{specialBuilding.buildingName}' has no prefab assigned!");
                            specialBuilding.hasSpawned = true; // Mark as "handled" to avoid repeated errors
                            continue;
                        }
                        if (specialBuilding.buildingHeight <= 0) {
                             Debug.LogError($"SkyscraperSpawner: Special building '{specialBuilding.buildingName}' has invalid height {specialBuilding.buildingHeight}! Skipping.");
                             specialBuilding.hasSpawned = true; // Mark as "handled"
                             continue;
                        }


                        Debug.Log($"SkyscraperSpawner: Triggering SPECIAL Building '{specialBuilding.buildingName}' at Y: {nextElementStartY}");
                        Vector3 spawnPos = new Vector3(transform.position.x, nextElementStartY, transform.position.z);
                        Instantiate(specialBuilding.prefab, spawnPos, Quaternion.identity, transform);

                        specialBuilding.hasSpawned = true;
                        nextElementStartY += specialBuilding.buildingHeight + distanceBetweenElements;
                        specialBuildingSpawnedThisCycle = true;
                        break; // Spawn only one special building per spawn cycle opportunity
                    }
                }
            }

            // --- 2. If no special building was spawned this cycle, try spawning a regular sequence ---
            if (!specialBuildingSpawnedThisCycle)
            {
                if (skyscraperSequencePrefabs != null && skyscraperSequencePrefabs.Length > 0 && regularSequenceTotalHeight > 0)
                {
                    Debug.Log($"SkyscraperSpawner: Spawning REGULAR sequence starting at Y: {nextElementStartY}");
                    SpawnRegularSkyscraperSequence(nextElementStartY);
                    nextElementStartY += regularSequenceTotalHeight + distanceBetweenElements;
                }
                else if (specialBuildings == null || specialBuildings.All(sb => sb.hasSpawned))
                {
                    // No regular sequences defined, and all special buildings (if any) have been spawned.
                    // Spawner can effectively stop or just keep advancing nextElementStartY to avoid constant checks.
                    // For now, let it advance so it doesn't spam checks if camera stays high.
                    // Or disable the component:
                    // Debug.Log("SkyscraperSpawner: All special buildings spawned and no regular sequences. Disabling spawner.");
                    // this.enabled = false;
                    // For now, just advance Y to prevent tight loops if camera is static high up
                    if (specialBuildings == null || specialBuildings.Count == 0) // Only advance if no special buildings were ever there
                         nextElementStartY = cameraTriggerY + spawnLookaheadBuffer; // Push it far ahead
                }
            }
        }
    }

    void SpawnRegularSkyscraperSequence(float startY)
    {
        for (int i = 0; i < skyscraperSequencePrefabs.Length; i++)
        {
            GameObject prefabToSpawn = skyscraperSequencePrefabs[i];
            if (prefabToSpawn == null) {
                Debug.LogWarning($"SkyscraperSpawner: Regular sequence prefab at index {i} is null. Skipping.");
                continue;
            }
            float spawnY = startY + (i * regularChunkHeight);
            Vector3 spawnPos = new Vector3(transform.position.x, spawnY, transform.position.z);
            Instantiate(prefabToSpawn, spawnPos, Quaternion.identity, transform);
        }
    }
}
