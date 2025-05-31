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

    [Header("Special One-Off Buildings")]
    [Tooltip("List of unique buildings to spawn at specific Y-coordinates.")]
    public List<SpecialBuildingInfo> specialBuildings = new List<SpecialBuildingInfo>();

    [Header("Nature Phase Prefabs")]
    // [Tooltip("Assign nature-themed prefabs to spawn when the timer is low.")]
    // [SerializeField] private GameObject[] naturePrefabs; // REMOVED
    // [Tooltip("The vertical height of ONE nature prefab. Assumes ALL nature prefabs have this SAME height.")]
    // [SerializeField] private float natureChunkHeight = 15f; // REMOVED

    [Header("General Spawning Settings")]
    [Tooltip("The vertical distance (in world units) AFTER one element ends BEFORE the next element starts.")]
    [SerializeField] private float distanceBetweenElements = 100f;
    [Tooltip("The world Y-coordinate where the very FIRST element should start spawning.")]
    [SerializeField] private float firstElementStartY = 50f;

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
    private List<GameObject> activeSpawnedChunks = new List<GameObject>();
    private bool isSpawningHalted = false;
    // private bool isNatureModeActive = false; // REMOVED

    void Start()
    {
        Debug.Log($"SkyscraperSpawner '{gameObject.name}': Start() initiated.");
        
        bool canSpawnRegular = skyscraperSequencePrefabs != null && skyscraperSequencePrefabs.Length > 0;
        bool canSpawnSpecial = specialBuildings != null && specialBuildings.Count > 0;
        // bool canSpawnNature = naturePrefabs != null && naturePrefabs.Length > 0; // REMOVED

        if (!canSpawnRegular && !canSpawnSpecial /*&& !canSpawnNature*/) {
            Debug.LogWarning($"SkyscraperSpawner '{gameObject.name}': No Regular, Special, OR Nature Prefabs assigned. Spawner will be disabled if it remains enabled.");
            return;
        }
        if (canSpawnRegular && regularChunkHeight <= 0) {
            Debug.LogError($"SkyscraperSpawner '{gameObject.name}': Regular Chunk Height must be a positive value if regular sequences are used! Disabling spawner."); enabled = false; return;
        }
        // if (canSpawnNature && natureChunkHeight <= 0) {
        //     Debug.LogError($"SkyscraperSpawner '{gameObject.name}': Nature Chunk Height must be a positive value if nature prefabs are used! Disabling spawner."); enabled = false; return;
        // }

        if (mainCamera == null) {
            mainCamera = Camera.main;
            if (mainCamera == null) {
                 Debug.LogError($"SkyscraperSpawner '{gameObject.name}': Main Camera reference not set/found! Disabling spawner."); enabled = false; return;
            }
        }
        Debug.Log($"SkyscraperSpawner '{gameObject.name}': Main Camera assigned: {mainCamera.name}");

        if (canSpawnRegular) {
            regularSequenceTotalHeight = skyscraperSequencePrefabs.Length * regularChunkHeight;
            Debug.Log($"SkyscraperSpawner '{gameObject.name}': Regular Sequence Total Height calculated: {regularSequenceTotalHeight}");
        } else {
            regularSequenceTotalHeight = 0;
        }

        nextElementStartY = firstElementStartY; // Initialize here
        if (mainCamera != null) {
             cameraHalfHeight = mainCamera.orthographicSize;
        } else {
            enabled = false; return;
        }
        ResumeSpawning(); // Call this to set initial flags, reset special buildings, and set nextElementStartY
        Debug.Log($"SkyscraperSpawner '{gameObject.name}': Start() completed. Initial nextElementStartY = {nextElementStartY:F2}, CameraHalfHeight = {cameraHalfHeight:F2}, Halted: {isSpawningHalted}");
    }
    
    void OnEnable()
    {
        // When re-enabled (e.g. by TutorialManager), ensure a clean state.
        // Start() will have done initial setup if it's the first enable.
        // If re-enabled after being disabled mid-game, ResumeSpawning() is more appropriate
        // to reset gameplay-specific states like 'hasSpawned' flags and nextElementStartY.
        // TutorialManager should call ResumeSpawning explicitly when it wants this spawner to start fresh.
        // For now, we'll rely on Start() for initial setup and TutorialManager calls for resets.
    }


    void Update()
    {
        if (!mainCamera || !enabled) return;

        if (!isSpawningHalted)
        {
            CheckAndSpawnNextElement();
        }
        DespawnOldSkyscrapers();
    }

    public void HaltSpawning()
    {
        Debug.Log($"SkyscraperSpawner '{gameObject.name}': Halting ALL future spawning.");
        isSpawningHalted = true;
    }

    public void ResumeSpawning() // Called by TutorialManager to start/restart normal skyscraper operation
    {
        Debug.Log($"SkyscraperSpawner '{gameObject.name}': Resuming/Resetting spawning state. Skyscraper mode active.");
        isSpawningHalted = false;
        // isNatureModeActive = false; // REMOVED
        // --- MODIFICATION: Clear previously spawned chunks ---
        Debug.Log($"SkyscraperSpawner '{gameObject.name}': Clearing {activeSpawnedChunks.Count} previously spawned chunks.");
        foreach (GameObject chunk in activeSpawnedChunks)
        {
            if (chunk != null) // Check if it hasn't been destroyed already
            {
                Destroy(chunk);
            }
        }
        activeSpawnedChunks.Clear();
        // --- END MODIFICATION ---

        // Reset 'hasSpawned' for special buildings
        if (specialBuildings != null)
        {
            foreach (var sb in specialBuildings)
            {
                sb.hasSpawned = false;
            }
            Debug.Log($"SkyscraperSpawner '{gameObject.name}': Special buildings 'hasSpawned' flags reset.");
        }

        nextElementStartY = firstElementStartY;
        Debug.Log($"SkyscraperSpawner '{gameObject.name}': nextElementStartY reset to firstElementStartY: {firstElementStartY:F2}");
    }

    void CheckAndSpawnNextElement()
    {
        if (isSpawningHalted) return;

        float cameraY = mainCamera.transform.position.y;
        float cameraVisibleTopY = cameraY + cameraHalfHeight;

        float currentChunkHeightForLookahead = regularChunkHeight > 0 ? regularChunkHeight : 20f;
        if (regularChunkHeight <= 0 && specialBuildings != null && specialBuildings.Count > 0) {
            SpecialBuildingInfo nextSpecial = specialBuildings.FirstOrDefault(sb => !sb.hasSpawned && sb.buildingHeight > 0);
            if (nextSpecial != null) currentChunkHeightForLookahead = nextSpecial.buildingHeight;
        }
        float cameraTriggerY = cameraVisibleTopY + currentChunkHeightForLookahead;

        if (cameraTriggerY >= nextElementStartY)
        {
            bool elementSpawnedThisCycle = false;
            GameObject spawnedElementRoot = null; // Used to add to activeSpawnedChunks if it's a single object

            // Nature mode spawning REMOVED
            // if (isNatureModeActive)
            // {
            //     if (naturePrefabs != null && naturePrefabs.Length > 0)
            //     {
            //         int randomIndex = Random.Range(0, naturePrefabs.Length);
            //         GameObject prefabToSpawn = naturePrefabs[randomIndex];
            //         if (prefabToSpawn != null)
            //         {
            //             if (natureChunkHeight <= 0) { Debug.LogError($"SkyscraperSpawner '{gameObject.name}': Nature Chunk Height is invalid for {prefabToSpawn.name}. Cannot spawn precisely."); return; }
            //             Vector3 spawnPos = new Vector3(transform.position.x, nextElementStartY, transform.position.z);
            //             spawnedElementRoot = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity, transform);
            //             activeSpawnedChunks.Add(spawnedElementRoot); // Add nature chunk
            //             nextElementStartY += natureChunkHeight + distanceBetweenElements;
            //             elementSpawnedThisCycle = true;
            //         } else Debug.LogWarning($"SkyscraperSpawner '{gameObject.name}': Nature prefab at index {randomIndex} is null.");
            //     } else Debug.LogWarning($"SkyscraperSpawner '{gameObject.name}': In Nature Mode, but no naturePrefabs assigned or list is empty.");
            // }
            // else 
            {
                if (specialBuildings != null && !elementSpawnedThisCycle)
                {
                    for (int i = 0; i < specialBuildings.Count; i++)
                    {
                        SpecialBuildingInfo specialBuilding = specialBuildings[i];
                        if (!specialBuilding.hasSpawned && cameraY >= specialBuilding.triggerWorldY)
                        {
                            if (specialBuilding.triggerWorldY <= nextElementStartY + currentChunkHeightForLookahead)
                            {
                                if (specialBuilding.prefab == null || specialBuilding.buildingHeight <= 0) {
                                     Debug.LogError($"SkyscraperSpawner '{gameObject.name}': Special building '{specialBuilding.buildingName}' misconfigured. Marking as spawned.");
                                     specialBuildings[i].hasSpawned = true; continue;
                                }
                                Vector3 spawnPos = new Vector3(transform.position.x, nextElementStartY, transform.position.z);
                                spawnedElementRoot = Instantiate(specialBuilding.prefab, spawnPos, Quaternion.identity, transform);
                                activeSpawnedChunks.Add(spawnedElementRoot); // Add special building
                                specialBuildings[i].hasSpawned = true;
                                nextElementStartY += specialBuilding.buildingHeight + distanceBetweenElements;
                                elementSpawnedThisCycle = true;
                                break;
                            }
                        }
                    }
                }

                if (!elementSpawnedThisCycle && skyscraperSequencePrefabs != null && skyscraperSequencePrefabs.Length > 0 && regularSequenceTotalHeight > 0)
                {
                    SpawnRegularSkyscraperSequence(nextElementStartY); // This method adds to activeSpawnedChunks
                    nextElementStartY += regularSequenceTotalHeight + distanceBetweenElements;
                    elementSpawnedThisCycle = true; // Mark that a regular sequence was initiated
                }
            }

            if (elementSpawnedThisCycle) { // Check if anything was actually spawned or sequence initiated
                Debug.Log($"SkyscraperSpawner '{gameObject.name}': After spawn attempt, new nextElementStartY = {nextElementStartY:F2}");
            } else if (!elementSpawnedThisCycle) {
                 bool noRegular = skyscraperSequencePrefabs == null || skyscraperSequencePrefabs.Length == 0 || regularSequenceTotalHeight <=0;
                 bool allSpecialDone = specialBuildings == null || specialBuildings.All(sb => sb.hasSpawned);
                 // bool noNature = naturePrefabs == null || naturePrefabs.Length == 0; // REMOVED
                 // if (isNatureModeActive && noNature) {
                 //     Debug.LogWarning($"SkyscraperSpawner '{gameObject.name}': In Nature Mode, but no Nature Prefabs to spawn. Advancing nextElementStartY.");
                 // } else 
                 if (noRegular && allSpecialDone) {
                     Debug.LogWarning($"SkyscraperSpawner '{gameObject.name}': No regular sequences and all special buildings done. Spawning will effectively stop. Advancing nextElementStartY.");
                 }
                 if (!elementSpawnedThisCycle) { // Still nothing spawned
                    nextElementStartY = cameraTriggerY + currentChunkHeightForLookahead / 2f; // Push it a bit ahead to avoid tight loop
                 }
            }
        }
    }

    List<GameObject> SpawnRegularSkyscraperSequence(float startY)
    {
        List<GameObject> spawnedSequenceChunks = new List<GameObject>();
        for (int i = 0; i < skyscraperSequencePrefabs.Length; i++)
        {
            GameObject prefabToSpawn = skyscraperSequencePrefabs[i];
            if (prefabToSpawn == null) continue;
            float spawnY = startY + (i * regularChunkHeight);
            Vector3 spawnPos = new Vector3(transform.position.x, spawnY, transform.position.z);
            GameObject newRegularChunk = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity, transform);
            activeSpawnedChunks.Add(newRegularChunk); // Ensure individual sequence chunks are added
            spawnedSequenceChunks.Add(newRegularChunk);
        }
        return spawnedSequenceChunks;
    }

    void DespawnOldSkyscrapers()
    {
        if (mainCamera == null || activeSpawnedChunks.Count == 0) return;
        float cameraBottomEdgeY = mainCamera.transform.position.y - cameraHalfHeight;
        float despawnTriggerY = cameraBottomEdgeY - despawnDistanceBelowCamera;
        
        // Iterate backwards when removing from a list during iteration
        for (int i = activeSpawnedChunks.Count - 1; i >= 0; i--)
        {
            GameObject chunk = activeSpawnedChunks[i];
            if (chunk == null) { // If already destroyed by other means
                activeSpawnedChunks.RemoveAt(i);
                continue;
            }

            Renderer rend = chunk.GetComponentInChildren<Renderer>();
            float chunkTopEdgeY;
            if (rend != null) {
                chunkTopEdgeY = rend.bounds.max.y;
            } else {
                float estimatedHeight = regularChunkHeight > 0 ? regularChunkHeight : 20f; // natureChunkHeight removed
                chunkTopEdgeY = chunk.transform.position.y + (estimatedHeight / 2f);
            }

            if (chunkTopEdgeY < despawnTriggerY) {
                // Debug.Log($"SkyscraperSpawner '{gameObject.name}': Despawning chunk: {chunk.name}");
                Destroy(chunk);
                activeSpawnedChunks.RemoveAt(i);
            }
        }
    }

    public void ForceSpawnNow()
    {
        CheckAndSpawnNextElement();
    }
}
