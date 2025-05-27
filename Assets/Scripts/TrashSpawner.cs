using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Required for List

public class TrashSpawner : MonoBehaviour
{
    [Header("Real Game Trash Prefabs")]
    [Tooltip("Assign your ACTUAL REGULAR trash item PREFABS here for the main game.")]
    [SerializeField] private GameObject[] realTrashPrefabs;
    [Tooltip("Assign the Divvy Bike PREFAB here. It should have TrashItem.cs with 'isDivvyBike' checked.")]
    [SerializeField] private GameObject divvyBikePrefab;
    [Tooltip("How often (in seconds) the Divvy Bike should attempt to spawn.")]
    [SerializeField] private float divvyBikeSpawnInterval = 10f;

    [Header("Spawning Area (Local Offsets from Camera)")]
    [SerializeField] private float spawnAreaMinX = -5f;
    [SerializeField] private float spawnAreaMaxX = 5f;
    [SerializeField] private float spawnAreaMinY = 11f;
    [SerializeField] private float spawnAreaMaxY = 15f;

    [Header("Spawning Timing (for regular trash)")]
    [SerializeField] private float minSpawnDelay = 0.5f;
    [SerializeField] private float maxSpawnDelay = 2.0f;
    [SerializeField] private float initialDelay = 1.0f;

    // --- Tutorial Specific Variables ---
    private List<TutorialItemData> currentTutorialItems;
    private int nextTutorialItemIndex = 0;
    private bool isTutorialMode = false;

    private Coroutine spawningCoroutine;
    private float timeSinceLastDivvyBikeSpawn = 0f; // Renamed for clarity, tracks time since last successful Divvy spawn
    private bool shouldAttemptDivvySpawnNext = false; // Flag to signal SpawnNextItem

    void OnDisable()
    {
        StopSpawning();
    }

    // This Update method will handle the Divvy Bike timer
    void Update()
    {
        // Only update Divvy Bike timer if in real game mode and the spawner is actively running
        if (!isTutorialMode && spawningCoroutine != null && divvyBikePrefab != null)
        {
            timeSinceLastDivvyBikeSpawn += Time.deltaTime;
            if (timeSinceLastDivvyBikeSpawn >= divvyBikeSpawnInterval)
            {
                shouldAttemptDivvySpawnNext = true;
                // Timer will be reset in SpawnNextItem if Divvy bike is successfully chosen
            }
        }
    }


    public void StartTutorialSpawning(List<TutorialItemData> tutorialItemsToSpawn)
    {
        Debug.Log("TrashSpawner: Starting TUTORIAL spawning.");
        isTutorialMode = true;
        currentTutorialItems = tutorialItemsToSpawn;
        nextTutorialItemIndex = 0;
        timeSinceLastDivvyBikeSpawn = 0f; // Reset Divvy bike timer
        shouldAttemptDivvySpawnNext = false;

        if (currentTutorialItems == null || currentTutorialItems.Count == 0)
        {
            Debug.LogWarning("TrashSpawner: Tutorial items list is empty or null. No tutorial items will spawn.");
            isTutorialMode = false;
            return;
        }
        StopSpawningInternal();
        spawningCoroutine = StartCoroutine(SpawnLoop());
    }

    public void StartRealGameSpawning()
    {
        Debug.Log("TrashSpawner: Starting REAL GAME spawning.");
        isTutorialMode = false;
        currentTutorialItems = null;
        timeSinceLastDivvyBikeSpawn = 0f; // Reset Divvy bike timer for new game
        shouldAttemptDivvySpawnNext = false;


        if ((realTrashPrefabs == null || realTrashPrefabs.Length == 0) && divvyBikePrefab == null)
        {
            Debug.LogError("TrashSpawner: No RealTrashPrefabs or DivvyBikePrefab assigned. Cannot start real game spawning.");
            return;
        }
        StopSpawningInternal();
        spawningCoroutine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        StopSpawningInternal();
    }

    private void StopSpawningInternal()
    {
        if (spawningCoroutine != null)
        {
            StopCoroutine(spawningCoroutine);
            spawningCoroutine = null;
            Debug.Log("TrashSpawner: Spawning coroutine stopped.");
        }
    }

    IEnumerator SpawnLoop()
    {
        Debug.Log($"TrashSpawner.SpawnLoop: Starting with initialDelay: {initialDelay}s.");
        yield return new WaitForSeconds(initialDelay);
        Debug.Log("TrashSpawner.SpawnLoop: Initial delay complete. Entering spawn cycle.");

        while (true)
        {
            // The general delay between spawn *opportunities*
            float currentSpawnDelay = Random.Range(minSpawnDelay, maxSpawnDelay);
            // Debug.Log($"TrashSpawner.SpawnLoop: Waiting for currentSpawnDelay: {currentSpawnDelay:F2}s.");
            yield return new WaitForSeconds(currentSpawnDelay);
            // Debug.Log("TrashSpawner.SpawnLoop: Current delay complete. Calling SpawnNextItem().");
            SpawnNextItem();
        }
    }

    void SpawnNextItem()
    {
        GameObject objectToSpawn = null;
        TutorialItemData tutorialDataForThisItem = null;

        if (isTutorialMode)
        {
            // --- TUTORIAL MODE SPAWNING ---
            // (Tutorial mode logic remains the same as before)
            string ctiStatus = (currentTutorialItems == null) ? "NULL" : currentTutorialItems.Count.ToString();
            if (currentTutorialItems == null || currentTutorialItems.Count == 0 || nextTutorialItemIndex >= currentTutorialItems.Count) {
                Debug.LogWarning($"TrashSpawner.SpawnNextItem: Condition MET for 'Reached end of tutorial items or list empty'. currentTutorialItems.Count='{ctiStatus}', nextTutorialItemIndex={nextTutorialItemIndex}. Tutorial spawning cycle complete for now (or list was invalid).");
                return;
            }
            tutorialDataForThisItem = currentTutorialItems[nextTutorialItemIndex];
            if (tutorialDataForThisItem.shadowSprite != null) {
                GameObject shadowGO = new GameObject("TutorialShadow_" + tutorialDataForThisItem.itemName);
                SpriteRenderer sr = shadowGO.AddComponent<SpriteRenderer>();
                sr.sprite = tutorialDataForThisItem.shadowSprite;
                sr.sortingLayerName = "Default"; sr.sortingOrder = 2;
                shadowGO.AddComponent<BoxCollider2D>();
                TutorialShadowItem shadowItemScript = shadowGO.AddComponent<TutorialShadowItem>();
                shadowItemScript.Initialize(tutorialDataForThisItem);
                objectToSpawn = shadowGO;
            } else {
                Debug.LogWarning($"TrashSpawner.SpawnNextItem: Shadow sprite for tutorial item '{tutorialDataForThisItem.itemName}' IS NULL.");
            }
            nextTutorialItemIndex++;
        }
        else // --- REAL GAME MODE SPAWNING ---
        {
            // Debug.Log($"TrashSpawner.SpawnNextItem: Real Game. TimeSinceDivvy: {timeSinceLastDivvyBikeSpawn:F2}, Interval: {divvyBikeSpawnInterval:F2}, ShouldSpawnDivvy: {shouldAttemptDivvySpawnNext}");

            // Prioritize Divvy Bike if its flag is set and prefab exists
            if (shouldAttemptDivvySpawnNext && divvyBikePrefab != null)
            {
                objectToSpawn = divvyBikePrefab;
                timeSinceLastDivvyBikeSpawn = 0f; // Reset timer explicitly after choosing to spawn it
                shouldAttemptDivvySpawnNext = false; // Clear the flag
                Debug.Log("TrashSpawner: Spawning Divvy Bike!");
            }
            else // Spawn regular trash (if any exist)
            {
                if (realTrashPrefabs != null && realTrashPrefabs.Length > 0)
                {
                    int randomIndex = Random.Range(0, realTrashPrefabs.Length);
                    objectToSpawn = realTrashPrefabs[randomIndex];
                    // Debug.Log($"TrashSpawner: Spawning regular trash: {objectToSpawn.name}");
                }
                else if (divvyBikePrefab == null) // Only log error if no regular trash AND no divvy bike at all
                {
                     Debug.LogError("TrashSpawner: realTrashPrefabs is not set or empty, and no DivvyBikePrefab. Nothing to spawn in real game mode.");
                     return;
                }
                // If only divvy bike exists and it's not its time (flag is false), then objectToSpawn might remain null for this cycle, which is fine.
            }
        }

        if (objectToSpawn == null) {
            // Debug.LogWarning("TrashSpawner.SpawnNextItem: objectToSpawn is NULL. Nothing will be spawned this cycle.");
            return;
        }

        float randomXOffset = Random.Range(spawnAreaMinX, spawnAreaMaxX);
        float randomYOffset = Random.Range(spawnAreaMinY, spawnAreaMaxY);
        Vector3 localSpawnOffset = new Vector3(randomXOffset, randomYOffset, 0f);
        Vector3 spawnPosition = transform.position + localSpawnOffset;

        if (isTutorialMode)
        {
            if (objectToSpawn.GetComponent<TutorialShadowItem>() != null) {
                 objectToSpawn.transform.position = spawnPosition;
            }
        }
        else {
            Instantiate(objectToSpawn, spawnPosition, Quaternion.identity);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = transform.position;
        Vector3 topLeft = center + new Vector3(spawnAreaMinX, spawnAreaMaxY, 0);
        Vector3 topRight = center + new Vector3(spawnAreaMaxX, spawnAreaMaxY, 0);
        Vector3 bottomLeft = center + new Vector3(spawnAreaMinX, spawnAreaMinY, 0);
        Vector3 bottomRight = center + new Vector3(spawnAreaMaxX, spawnAreaMinY, 0);
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
        Gizmos.DrawLine(bottomLeft, topLeft);
    }
}
