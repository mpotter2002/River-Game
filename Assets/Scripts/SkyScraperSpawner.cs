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
    [Tooltip("The vertical distance (in world units) AFTER one element ends BEFORE the next element starts.")]
    [SerializeField] private float distanceBetweenElements = 100f;
    [Tooltip("The world Y-coordinate where the very FIRST element (regular sequence or special building if its trigger is earlier) should start spawning.")]
    [SerializeField] private float firstElementStartY = 50f;

    [Header("Special One-Off Buildings")]
    [Tooltip("List of unique buildings to spawn at specific Y-coordinates.")]
    public List<SpecialBuildingInfo> specialBuildings = new List<SpecialBuildingInfo>();

    [Header("Despawning Settings")]
    [Tooltip("How far below the camera's bottom view edge a chunk's top edge should be before we destroy it.")]
    [SerializeField] private float despawnDistanceBelowCamera = 20f; // Example value

    [Header("References")]
    [Tooltip("Assign the main camera here (or leave empty if tagged 'MainCamera').")]
    [SerializeField] private Camera mainCamera;

    // Internal variables
    private float nextElementStartY;
    private float regularSequenceTotalHeight;
    private float cameraHalfHeight;
    private List<GameObject> activeSkyscraperChunks = new List<GameObject>(); // <<< NEW: To track spawned chunks

    void Start()
    {
        Debug.Log($"SkyscraperSpawner '{gameObject.name}': Start() initiated.");

        if ((skyscraperSequencePrefabs == null || skyscraperSequencePrefabs.Length == 0) && (specialBuildings == null || specialBuildings.Count == 0)) {
            Debug.LogWarning("SkyscraperSpawner: No Regular OR Special Building Prefabs assigned. Spawner will be disabled.");
            enabled = false; return;
        }
        if (regularChunkHeight <= 0 && (skyscraperSequencePrefabs != null && skyscraperSequencePrefabs.Length > 0) ) {
            Debug.LogError("SkyscraperSpawner: Regular Chunk Height must be a positive value if regular sequences are used! Disabling spawner."); enabled = false; return;
        }
        if (mainCamera == null) {
            mainCamera = Camera.main;
            if (mainCamera == null) {
                 Debug.LogError("SkyscraperSpawner: Main Camera reference not set/found! Disabling spawner."); enabled = false; return;
            }
        }

        if (skyscraperSequencePrefabs != null && skyscraperSequencePrefabs.Length > 0) {
            regularSequenceTotalHeight = skyscraperSequencePrefabs.Length * regularChunkHeight;
            Debug.Log($"SkyscraperSpawner: Regular Sequence Total Height calculated: {regularSequenceTotalHeight} (Count: {skyscraperSequencePrefabs.Length}, ChunkHeight: {regularChunkHeight})");
            if (regularSequenceTotalHeight <= 0) {
                 Debug.LogWarning("SkyscraperSpawner: Regular sequence total height is zero or negative. Regular sequences may not spawn correctly.");
            }
        } else {
            regularSequenceTotalHeight = 0;
            Debug.Log("SkyscraperSpawner: No regular skyscraper sequence prefabs assigned.");
        }

        if (specialBuildings != null && specialBuildings.Count > 0) {
            specialBuildings = specialBuildings.OrderBy(sb => sb.triggerWorldY).ToList();
            Debug.Log($"SkyscraperSpawner: Sorted {specialBuildings.Count} special buildings by trigger Y.");
            foreach (var sb in specialBuildings) {
                sb.hasSpawned = false;
                Debug.Log($"  - Special Building: {sb.buildingName}, TriggerY: {sb.triggerWorldY}, Height: {sb.buildingHeight}");
            }
        } else {
            Debug.Log("SkyscraperSpawner: No special buildings assigned.");
        }

        nextElementStartY = firstElementStartY;
        cameraHalfHeight = mainCamera.orthographicSize;
        Debug.Log($"SkyscraperSpawner: Initial nextElementStartY = {nextElementStartY}, CameraHalfHeight = {cameraHalfHeight}");
        Debug.Log($"SkyscraperSpawner '{gameObject.name}': Start() completed successfully.");
    }

    void Update()
    {
        CheckAndSpawnNextElement();
        DespawnOldSkyscrapers(); // <<< NEW: Call despawn logic
    }

    void CheckAndSpawnNextElement()
    {
        float cameraY = mainCamera.transform.position.y;
        float cameraVisibleTopY = cameraY + cameraHalfHeight;
        float spawnLookaheadBuffer = regularChunkHeight > 0 ? regularChunkHeight : (specialBuildings.Count > 0 && specialBuildings.FirstOrDefault(sb => !sb.hasSpawned && sb.buildingHeight > 0) != null ? specialBuildings.First(sb => !sb.hasSpawned && sb.buildingHeight > 0).buildingHeight : 20f);
        float cameraTriggerY = cameraVisibleTopY + spawnLookaheadBuffer;

        if (cameraTriggerY >= nextElementStartY)
        {
            Debug.Log($"SkyscraperSpawner '{gameObject.name}': SPAWN OPPORTUNITY! CameraTriggerY ({cameraTriggerY:F2}) >= nextElementStartY ({nextElementStartY:F2})");
            bool specialBuildingSpawnedThisCycle = false;

            if (specialBuildings != null)
            {
                for (int i = 0; i < specialBuildings.Count; i++)
                {
                    SpecialBuildingInfo specialBuilding = specialBuildings[i];
                    if (!specialBuilding.hasSpawned && cameraY >= specialBuilding.triggerWorldY)
                    {
                        if (specialBuilding.triggerWorldY <= nextElementStartY + spawnLookaheadBuffer)
                        {
                            if (specialBuilding.prefab == null) {
                                Debug.LogError($"SkyscraperSpawner: Special building '{specialBuilding.buildingName}' has no prefab assigned!");
                                specialBuilding.hasSpawned = true; continue;
                            }
                            if (specialBuilding.buildingHeight <= 0) {
                                Debug.LogError($"SkyscraperSpawner: Special building '{specialBuilding.buildingName}' has invalid height {specialBuilding.buildingHeight}! Skipping.");
                                specialBuilding.hasSpawned = true; continue;
                            }

                            Debug.Log($"SkyscraperSpawner: Spawning SPECIAL Building '{specialBuilding.buildingName}' (Height: {specialBuilding.buildingHeight}) at Y: {nextElementStartY}");
                            Vector3 spawnPos = new Vector3(transform.position.x, nextElementStartY, transform.position.z);
                            GameObject newSpecialChunk = Instantiate(specialBuilding.prefab, spawnPos, Quaternion.identity, transform);
                            activeSkyscraperChunks.Add(newSpecialChunk); // <<< ADD to tracking list
                            // Store height on the GameObject if needed for despawn, or assume it's known
                            // For simplicity, DespawnOldSkyscrapers will need a way to get each chunk's height.
                            // A better way is to attach a simple script to each chunk prefab holding its height.
                            // For now, we'll try to infer or use an average for despawning.

                            specialBuildings[i].hasSpawned = true;
                            nextElementStartY += specialBuilding.buildingHeight + distanceBetweenElements;
                            Debug.Log($"SkyscraperSpawner: After SPECIAL '{specialBuilding.buildingName}', new nextElementStartY = {nextElementStartY:F2}");
                            specialBuildingSpawnedThisCycle = true;
                            break;
                        }
                    }
                }
            }

            if (!specialBuildingSpawnedThisCycle)
            {
                if (skyscraperSequencePrefabs != null && skyscraperSequencePrefabs.Length > 0 && regularSequenceTotalHeight > 0)
                {
                    Debug.Log($"SkyscraperSpawner: Spawning REGULAR sequence (Total Height: {regularSequenceTotalHeight}) at Y: {nextElementStartY}");
                    SpawnRegularSkyscraperSequence(nextElementStartY); // This method will now add to activeSkyscraperChunks
                    nextElementStartY += regularSequenceTotalHeight + distanceBetweenElements;
                    Debug.Log($"SkyscraperSpawner: After REGULAR sequence, new nextElementStartY = {nextElementStartY:F2}");
                }
                else if (specialBuildings == null || specialBuildings.All(sb => sb.hasSpawned))
                {
                     if ((specialBuildings == null || specialBuildings.Count == 0) && (skyscraperSequencePrefabs == null || skyscraperSequencePrefabs.Length == 0) )
                     {
                         Debug.LogWarning("SkyscraperSpawner: No regular sequences AND no special buildings defined to spawn. Consider disabling spawner.");
                         nextElementStartY = cameraTriggerY + spawnLookaheadBuffer * 2;
                     }
                }
            }
        }
    }

    void SpawnRegularSkyscraperSequence(float startY)
    {
        Debug.Log($"  SpawnRegularSkyscraperSequence: Starting regular sequence at Y={startY:F2}");
        for (int i = 0; i < skyscraperSequencePrefabs.Length; i++)
        {
            GameObject prefabToSpawn = skyscraperSequencePrefabs[i];
            if (prefabToSpawn == null) {
                Debug.LogWarning($"SkyscraperSpawner: Regular sequence prefab at index {i} is null. Skipping.");
                continue;
            }
            float spawnY = startY + (i * regularChunkHeight);
            Vector3 spawnPos = new Vector3(transform.position.x, spawnY, transform.position.z);
            Debug.Log($"    Spawning regular chunk {i+1}/{skyscraperSequencePrefabs.Length}: {prefabToSpawn.name} at Y: {spawnY:F2}");
            GameObject newRegularChunk = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity, transform);
            activeSkyscraperChunks.Add(newRegularChunk); // <<< ADD to tracking list
        }
    }

    // <<< NEW METHOD for Despawning >>>
    void DespawnOldSkyscrapers()
    {
        if (mainCamera == null || activeSkyscraperChunks.Count == 0) return;

        float cameraBottomEdgeY = mainCamera.transform.position.y - cameraHalfHeight;
        float despawnTriggerY = cameraBottomEdgeY - despawnDistanceBelowCamera;

        List<GameObject> chunksToRemove = new List<GameObject>();

        foreach (GameObject chunk in activeSkyscraperChunks)
        {
            if (chunk == null) continue; // Skip if already destroyed or null

            // Estimate chunk's top edge. This is tricky without knowing each chunk's specific height & pivot.
            // For simplicity, we'll assume pivot is roughly center and use an average/known height.
            // A more robust solution: attach a script to each chunk prefab that stores its height.
            float estimatedChunkHeight = regularChunkHeight; // Default to regular chunk height
            // Try to find if it's a known special building to get its specific height
            // This part is complex if not storing height on the GO itself.
            // For now, let's assume all chunks are roughly 'regularChunkHeight' for despawn check,
            // or that special buildings are tall enough that this check is still okay.
            // A better way is to add a simple component to each prefab like "ChunkInfo" with a public height.

            SpriteRenderer sr = chunk.GetComponentInChildren<SpriteRenderer>(); // Get a renderer to estimate bounds
            float chunkTopEdgeY;
            if (sr != null) {
                chunkTopEdgeY = sr.bounds.max.y; // More accurate if sprite renderer bounds are good
            } else {
                 // Fallback: estimate based on position and assumed height (pivot at center)
                chunkTopEdgeY = chunk.transform.position.y + (estimatedChunkHeight / 2f);
            }


            if (chunkTopEdgeY < despawnTriggerY)
            {
                chunksToRemove.Add(chunk);
            }
        }

        foreach (GameObject chunkToRemove in chunksToRemove)
        {
            activeSkyscraperChunks.Remove(chunkToRemove);
            Destroy(chunkToRemove);
            Debug.Log($"SkyscraperSpawner: Despawned skyscraper chunk: {chunkToRemove.name}");
        }
    }
    // <<< END NEW METHOD >>>
}
