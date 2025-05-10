using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Required for List

public class TrashSpawner : MonoBehaviour
{
    [Header("Real Game Trash Prefabs")]
    [Tooltip("Assign your ACTUAL trash item PREFABS here for the main game.")]
    [SerializeField] private GameObject[] realTrashPrefabs;

    // --- Values below are LOCAL OFFSETS from this object's position (which should be parented to the camera) ---
    [Header("Spawning Area (Local Offsets from Camera)")]
    [Tooltip("Minimum X offset from the camera's center.")]
    [SerializeField] private float spawnAreaMinX = -5f;
    [Tooltip("Maximum X offset from the camera's center.")]
    [SerializeField] private float spawnAreaMaxX = 5f;
    [Tooltip("Minimum Y offset from the camera's center.")]
    [SerializeField] private float spawnAreaMinY = 11f;
    [Tooltip("Maximum Y offset from the camera's center.")]
    [SerializeField] private float spawnAreaMaxY = 15f;

    [Header("Spawning Timing")]
    [Tooltip("Minimum time delay (seconds) between spawns.")]
    [SerializeField] private float minSpawnDelay = 0.5f;
    [Tooltip("Maximum time delay (seconds) between spawns.")]
    [SerializeField] private float maxSpawnDelay = 2.0f;
    [Tooltip("Initial delay (seconds) before the first spawn when this component becomes enabled.")]
    [SerializeField] private float initialDelay = 1.0f;

    // --- Tutorial Specific Variables ---
    private List<TutorialItemData> currentTutorialItems;
    private int nextTutorialItemIndex = 0;
    private bool isTutorialMode = false;

    private Coroutine spawningCoroutine; // To store and manage the spawning loop

    // void Awake()
    // {
    //     // Consider disabling by default if always controlled externally
    //     // this.enabled = false;
    // }

    void OnDisable()
    {
        // When this component is disabled, stop all spawning
        StopSpawning();
    }

    // --- Public methods to be called by TutorialManager ---

   public void StartTutorialSpawning(List<TutorialItemData> tutorialItemsToSpawn)
{
    Debug.Log("TrashSpawner: Attempting to Start TUTORIAL spawning."); // Modified log

    isTutorialMode = true;
    currentTutorialItems = tutorialItemsToSpawn;
    nextTutorialItemIndex = 0;

    // --- ADD THIS DEBUG LOG ---
    if (currentTutorialItems != null)
    {
        Debug.Log($"TrashSpawner: Received tutorialItemsToSpawn list with {currentTutorialItems.Count} items.");
    }
    else
    {
        Debug.LogWarning("TrashSpawner: Received tutorialItemsToSpawn list IS NULL.");
    }
    // --------------------------

    if (currentTutorialItems == null || currentTutorialItems.Count == 0)
    {
        Debug.LogWarning("TrashSpawner: Tutorial items list is empty or null AFTER assignment. No tutorial items will spawn."); // Modified log
        isTutorialMode = false; // Revert if no items
        return;
    }

    StopSpawningInternal(); // Stop any existing coroutine
    spawningCoroutine = StartCoroutine(SpawnLoop());
}
    public void StartRealGameSpawning()
    {
        Debug.Log("TrashSpawner: Starting REAL GAME spawning.");
        isTutorialMode = false;
        currentTutorialItems = null; // Clear tutorial items

        if (realTrashPrefabs == null || realTrashPrefabs.Length == 0)
        {
            Debug.LogError("TrashSpawner: RealTrashPrefabs array is empty or null. Cannot start real game spawning.");
            return;
        }

        StopSpawningInternal(); // Stop any existing coroutine
        spawningCoroutine = StartCoroutine(SpawnLoop());
    }

    // Public method to stop spawning, can be called externally
    public void StopSpawning()
    {
        StopSpawningInternal();
    }

    // Internal method to handle stopping the coroutine
    private void StopSpawningInternal()
    {
        if (spawningCoroutine != null)
        {
            StopCoroutine(spawningCoroutine);
            spawningCoroutine = null;
            Debug.Log("TrashSpawner: Spawning coroutine stopped.");
        }
    }

    // --- End Public methods ---

    IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(initialDelay);
        while (true) // Loop will continue as long as the coroutine is running
        {
            float randomDelay = Random.Range(minSpawnDelay, maxSpawnDelay);
            yield return new WaitForSeconds(randomDelay);
            SpawnNextItem();
        }
    }

    void SpawnNextItem()
    {
        GameObject objectToSpawn = null; // This will be the fully configured GO for tutorial, or prefab for real game
        TutorialItemData tutorialDataForThisItem = null;

        if (isTutorialMode)
        {
            if (currentTutorialItems == null || currentTutorialItems.Count == 0 || nextTutorialItemIndex >= currentTutorialItems.Count)
            {
                Debug.Log("TrashSpawner: Reached end of tutorial items or list empty. Tutorial spawning cycle complete for now.");
                // The TutorialManager should call StopSpawning() when the tutorial phase is truly over.
                // This coroutine might stop if this spawner component gets disabled.
                return; // Don't spawn anything further in this mode if out of items
            }

            tutorialDataForThisItem = currentTutorialItems[nextTutorialItemIndex];
            if (tutorialDataForThisItem.shadowSprite != null)
            {
                // Create and configure the shadow GameObject
                GameObject shadowGO = new GameObject("TutorialShadow_" + tutorialDataForThisItem.itemName);
                SpriteRenderer sr = shadowGO.AddComponent<SpriteRenderer>();
                sr.sprite = tutorialDataForThisItem.shadowSprite;
                // TODO: Set appropriate sorting layer and order for shadow sprites
                // Example: sr.sortingLayerName = "Gameplay"; sr.sortingOrder = 0;

                BoxCollider2D collider = shadowGO.AddComponent<BoxCollider2D>();
                // Decide if trigger or not: collider.isTrigger = true;

                TutorialShadowItem shadowItemScript = shadowGO.AddComponent<TutorialShadowItem>();
                shadowItemScript.Initialize(tutorialDataForThisItem);

                objectToSpawn = shadowGO; // This is now a fully formed GameObject
            }
            else
            {
                Debug.LogWarning($"TrashSpawner: Shadow sprite for tutorial item '{tutorialDataForThisItem.itemName}' is null.");
            }
            nextTutorialItemIndex++;
        }
        else // Real game mode
        {
            if (realTrashPrefabs == null || realTrashPrefabs.Length == 0)
            {
                Debug.LogError("TrashSpawner: realTrashPrefabs is not set or empty in real game mode.");
                return;
            }
            int randomIndex = Random.Range(0, realTrashPrefabs.Length);
            // In real game mode, objectToSpawn is a PREFAB
            objectToSpawn = realTrashPrefabs[randomIndex];
        }

        if (objectToSpawn == null)
        {
            return; // Nothing to spawn
        }

        // Calculate spawn position
        float randomXOffset = Random.Range(spawnAreaMinX, spawnAreaMaxX);
        float randomYOffset = Random.Range(spawnAreaMinY, spawnAreaMaxY);
        Vector3 localSpawnOffset = new Vector3(randomXOffset, randomYOffset, 0f);
        Vector3 spawnPosition = transform.position + localSpawnOffset;

        // Instantiate or Position
        if (isTutorialMode)
        {
            // objectToSpawn is already an instantiated GameObject, just set its position
            if (objectToSpawn.GetComponent<TutorialShadowItem>() != null) // Check it's the shadow GO
            {
                 objectToSpawn.transform.position = spawnPosition;
                 // Consider parenting if needed: objectToSpawn.transform.SetParent(this.transform);
            }
        }
        else // Real game mode: objectToSpawn is a prefab
        {
            Instantiate(objectToSpawn, spawnPosition, Quaternion.identity);
            // Consider parenting if needed: newRealTrash.transform.SetParent(this.transform);
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
