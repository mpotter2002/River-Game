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
    [SerializeField] private float despawnDistanceBelowCamera = 20f;

    [Header("References")]
    [Tooltip("Assign the main camera here (or leave empty if tagged 'MainCamera').")]
    [SerializeField] private Camera mainCamera;

    // Internal variables
    private float nextElementStartY;
    private float regularSequenceTotalHeight;
    private float cameraHalfHeight;
    private List<GameObject> activeSkyscraperChunks = new List<GameObject>();

    void Start()
    {
        Debug.Log($"SkyscraperSpawner '{gameObject.name}': Start() initiated.");

        if ((skyscraperSequencePrefabs == null || skyscraperSequencePrefabs.Length == 0) && (specialBuildings == null || specialBuildings.Count == 0)) {
            Debug.LogWarning($"SkyscraperSpawner '{gameObject.name}': No Regular OR Special Building Prefabs assigned. Spawner will be disabled.");
            enabled = false; return;
        }
        if (regularChunkHeight <= 0 && (skyscraperSequencePrefabs != null && skyscraperSequencePrefabs.Length > 0) ) {
            Debug.LogError($"SkyscraperSpawner '{gameObject.name}': Regular Chunk Height must be a positive value if regular sequences are used! Disabling spawner."); enabled = false; return;
        }
        if (mainCamera == null) {
            mainCamera = Camera.main;
            if (mainCamera == null) {
                 Debug.LogError($"SkyscraperSpawner '{gameObject.name}': Main Camera reference not set/found! Disabling spawner."); enabled = false; return;
            }
        }
        Debug.Log($"SkyscraperSpawner '{gameObject.name}': Main Camera assigned: {mainCamera.name}");

        if (skyscraperSequencePrefabs != null && skyscraperSequencePrefabs.Length > 0) {
            regularSequenceTotalHeight = skyscraperSequencePrefabs.Length * regularChunkHeight;
            Debug.Log($"SkyscraperSpawner '{gameObject.name}': Regular Sequence Total Height calculated: {regularSequenceTotalHeight} (Count: {skyscraperSequencePrefabs.Length}, ChunkHeight: {regularChunkHeight})");
            if (regularSequenceTotalHeight <= 0) {
                 Debug.LogWarning($"SkyscraperSpawner '{gameObject.name}': Regular sequence total height is zero or negative. Regular sequences may not spawn correctly.");
            }
        } else {
            regularSequenceTotalHeight = 0;
            Debug.Log($"SkyscraperSpawner '{gameObject.name}': No regular skyscraper sequence prefabs assigned.");
        }

        if (specialBuildings != null && specialBuildings.Count > 0) {
            specialBuildings = specialBuildings.OrderBy(sb => sb.triggerWorldY).ToList();
            Debug.Log($"SkyscraperSpawner '{gameObject.name}': Sorted {specialBuildings.Count} special buildings by trigger Y.");
            foreach (var sb in specialBuildings) {
                sb.hasSpawned = false; // Ensure reset at start
                // <<< MODIFIED LOG to explicitly show hasSpawned after reset >>>
                Debug.Log($"  - Special Building Entry: '{sb.buildingName}', TriggerY: {sb.triggerWorldY}, Height: {sb.buildingHeight}, Prefab Assigned: {sb.prefab != null}, Initial hasSpawned state: {sb.hasSpawned}");
            }
        } else {
            Debug.Log($"SkyscraperSpawner '{gameObject.name}': No special buildings assigned (list might be null or empty).");
        }

        nextElementStartY = firstElementStartY;
        cameraHalfHeight = mainCamera.orthographicSize;
        Debug.Log($"SkyscraperSpawner '{gameObject.name}': Initial nextElementStartY = {nextElementStartY:F2}, CameraHalfHeight = {cameraHalfHeight:F2}");
        Debug.Log($"SkyscraperSpawner '{gameObject.name}': Start() completed successfully.");
    }

    void Update()
    {
        if (!mainCamera) return; // Stop if no camera
        // Debug.Log($"SkyscraperSpawner '{gameObject.name}': Update. CameraY: {mainCamera.transform.position.y:F2}");
        CheckAndSpawnNextElement();
        DespawnOldSkyscrapers();
    }

    void CheckAndSpawnNextElement()
    {
        float cameraY = mainCamera.transform.position.y;
        float cameraVisibleTopY = cameraY + cameraHalfHeight;
        float spawnLookaheadBuffer = regularChunkHeight > 0 ? regularChunkHeight : 20f;
        if (regularChunkHeight <= 0 && specialBuildings != null && specialBuildings.Count > 0) {
            SpecialBuildingInfo nextSpecial = specialBuildings.FirstOrDefault(sb => !sb.hasSpawned && sb.buildingHeight > 0);
            if (nextSpecial != null) spawnLookaheadBuffer = nextSpecial.buildingHeight;
        }
        float cameraTriggerY = cameraVisibleTopY + spawnLookaheadBuffer;

        if (cameraTriggerY >= nextElementStartY)
        {
            Debug.Log($"SkyscraperSpawner '{gameObject.name}': === SPAWN OPPORTUNITY! CameraTriggerY ({cameraTriggerY:F2}) >= nextElementStartY ({nextElementStartY:F2}) ===");
            bool elementSpawnedThisCycle = false;

            // --- 1. Check for Special Buildings first ---
            if (specialBuildings != null)
            {
                Debug.Log($"SkyscraperSpawner '{gameObject.name}': Checking special buildings. Count: {specialBuildings.Count}");

                for (int i = 0; i < specialBuildings.Count; i++)
                {
                    SpecialBuildingInfo specialBuilding = specialBuildings[i];
                    Debug.Log($"  SkyscraperSpawner '{gameObject.name}': Considering Special: '{specialBuilding.buildingName}', Spawned: {specialBuilding.hasSpawned}, CamY: {cameraY:F2}, TriggerY: {specialBuilding.triggerWorldY:F2}");

                    if (!specialBuilding.hasSpawned && cameraY >= specialBuilding.triggerWorldY)
                    {
                        if (specialBuilding.triggerWorldY <= nextElementStartY + spawnLookaheadBuffer)
                        {
                            if (specialBuilding.prefab == null) {
                                Debug.LogError($"SkyscraperSpawner '{gameObject.name}': Special building '{specialBuilding.buildingName}' has NO PREFAB assigned! Marking as spawned to avoid repeat errors.");
                                specialBuildings[i].hasSpawned = true;
                                continue;
                            }
                            if (specialBuilding.buildingHeight <= 0) {
                                Debug.LogError($"SkyscraperSpawner '{gameObject.name}': Special building '{specialBuilding.buildingName}' has invalid height {specialBuilding.buildingHeight}! Skipping and marking as spawned.");
                                specialBuildings[i].hasSpawned = true;
                                continue;
                            }

                            Debug.Log($"SkyscraperSpawner '{gameObject.name}': Spawning SPECIAL Building '{specialBuilding.buildingName}' (Height: {specialBuilding.buildingHeight}) at Y: {nextElementStartY}");
                            Vector3 spawnPos = new Vector3(transform.position.x, nextElementStartY, transform.position.z);
                            GameObject newSpecialChunk = Instantiate(specialBuilding.prefab, spawnPos, Quaternion.identity, transform);
                            Debug.Log("newSpecialChunk: " + newSpecialChunk.name);
                            activeSkyscraperChunks.Add(newSpecialChunk);

                            specialBuildings[i].hasSpawned = true;
                            nextElementStartY += specialBuilding.buildingHeight + distanceBetweenElements;
                            Debug.Log($"SkyscraperSpawner '{gameObject.name}': After SPECIAL '{specialBuilding.buildingName}', new nextElementStartY = {nextElementStartY:F2}");
                            elementSpawnedThisCycle = true;
                            break;
                        }
                        else
                        {
                            Debug.Log($"  SkyscraperSpawner '{gameObject.name}': Special Building '{specialBuilding.buildingName}' triggerY ({specialBuilding.triggerWorldY:F2}) is too far ahead of current nextElementStartY ({nextElementStartY:F2} + buffer). Waiting for nextElementStartY to catch up.");
                        }
                    }
                    else if (specialBuilding.hasSpawned)
                    {
                        // This log is fine for already spawned items
                        // Debug.Log($"  SkyscraperSpawner '{gameObject.name}': Special Building '{specialBuilding.buildingName}' has already spawned.");
                    }
                    else if (cameraY < specialBuilding.triggerWorldY)
                    {
                        Debug.Log($"  SkyscraperSpawner '{gameObject.name}': Special Building '{specialBuilding.buildingName}' not triggered yet. CamY ({cameraY:F2}) < TriggerY ({specialBuilding.triggerWorldY:F2}).");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"SkyscraperSpawner '{gameObject.name}': specialBuildings list is NULL. Cannot check for special buildings.");
            }


            // --- 2. If no special building was spawned this cycle, try spawning a regular sequence ---
            if (!elementSpawnedThisCycle)
            {
                Debug.Log($"SkyscraperSpawner '{gameObject.name}': No special building spawned this cycle. Checking for regular sequence.");
                if (skyscraperSequencePrefabs != null && skyscraperSequencePrefabs.Length > 0 && regularSequenceTotalHeight > 0)
                {
                    Debug.Log($"SkyscraperSpawner '{gameObject.name}': Spawning REGULAR sequence (Total Height: {regularSequenceTotalHeight}) at Y: {nextElementStartY}");
                    SpawnRegularSkyscraperSequence(nextElementStartY);
                    nextElementStartY += regularSequenceTotalHeight + distanceBetweenElements;
                    Debug.Log($"SkyscraperSpawner '{gameObject.name}': After REGULAR sequence, new nextElementStartY = {nextElementStartY:F2}");
                }
                else if (specialBuildings == null || specialBuildings.All(sb => sb.hasSpawned))
                {
                     if ((specialBuildings == null || specialBuildings.Count == 0) && (skyscraperSequencePrefabs == null || skyscraperSequencePrefabs.Length == 0) )
                     {
                         Debug.LogWarning($"SkyscraperSpawner '{gameObject.name}': No regular sequences AND no special buildings defined. Spawner might be finished or misconfigured.");
                         nextElementStartY = cameraTriggerY + spawnLookaheadBuffer;
                     }
                     else if (skyscraperSequencePrefabs == null || skyscraperSequencePrefabs.Length == 0) {
                        Debug.Log($"SkyscraperSpawner '{gameObject.name}': No regular sequences defined, and all special buildings spawned (or none to begin with). Waiting for nextElementStartY to advance if camera keeps moving.");
                     }
                }
            }
        }
    }

    void SpawnRegularSkyscraperSequence(float startY)
    {
        // Debug.Log($"  SpawnRegularSkyscraperSequence: Starting regular sequence at Y={startY:F2} for '{gameObject.name}'");
        for (int i = 0; i < skyscraperSequencePrefabs.Length; i++)
        {
            GameObject prefabToSpawn = skyscraperSequencePrefabs[i];
            if (prefabToSpawn == null) {
                Debug.LogWarning($"SkyscraperSpawner '{gameObject.name}': Regular sequence prefab at index {i} is null. Skipping.");
                continue;
            }
            float spawnY = startY + (i * regularChunkHeight);
            Vector3 spawnPos = new Vector3(transform.position.x, spawnY, transform.position.z);
            // Debug.Log($"    Spawning regular chunk {i+1}/{skyscraperSequencePrefabs.Length}: {prefabToSpawn.name} at Y: {spawnY:F2}");
            GameObject newRegularChunk = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity, transform);
            activeSkyscraperChunks.Add(newRegularChunk);
        }
    }

    void DespawnOldSkyscrapers()
    {
        if (mainCamera == null || activeSkyscraperChunks.Count == 0) return;

        float cameraBottomEdgeY = mainCamera.transform.position.y - cameraHalfHeight;
        float despawnTriggerY = cameraBottomEdgeY - despawnDistanceBelowCamera;
        List<GameObject> chunksToRemove = new List<GameObject>();

        foreach (GameObject chunk in activeSkyscraperChunks)
        {
            if (chunk == null) continue;
            SpriteRenderer sr = chunk.GetComponentInChildren<SpriteRenderer>();
            float chunkTopEdgeY;
            if (sr != null) {
                chunkTopEdgeY = sr.bounds.max.y;
            } else {
                float heightToUse = regularChunkHeight;
                chunkTopEdgeY = chunk.transform.position.y + (heightToUse / 2f);
            }

            if (chunkTopEdgeY < despawnTriggerY) {
                chunksToRemove.Add(chunk);
            }
        }
        foreach (GameObject chunkToRemove in chunksToRemove) {
            activeSkyscraperChunks.Remove(chunkToRemove);
            Destroy(chunkToRemove);
            // Debug.Log($"SkyscraperSpawner '{gameObject.name}': Despawned skyscraper chunk: {chunkToRemove.name}");
        }
    }
}
