using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Required for List

public class TrashSpawner : MonoBehaviour
{
    [Header("Real Game Trash Prefabs")]
    [Tooltip("Assign your ACTUAL trash item PREFABS here for the main game.")]
    [SerializeField] private GameObject[] realTrashPrefabs;

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

    private List<TutorialItemData> currentTutorialItems;
    private int nextTutorialItemIndex = 0;
    private bool isTutorialMode = false;
    private Coroutine spawningCoroutine;

    void OnDisable()
    {
        StopSpawning();
    }

    public void StartTutorialSpawning(List<TutorialItemData> tutorialItemsToSpawn)
    {
        Debug.Log("TrashSpawner: Attempting to Start TUTORIAL spawning.");
        isTutorialMode = true;
        currentTutorialItems = tutorialItemsToSpawn;
        nextTutorialItemIndex = 0; // Reset index when starting tutorial

        if (currentTutorialItems != null)
        {
            Debug.Log($"TrashSpawner: Received tutorialItemsToSpawn list with {currentTutorialItems.Count} items.");
        }
        else
        {
            Debug.LogWarning("TrashSpawner: Received tutorialItemsToSpawn list IS NULL.");
        }

        if (currentTutorialItems == null || currentTutorialItems.Count == 0)
        {
            Debug.LogWarning("TrashSpawner: Tutorial items list is empty or null AFTER assignment. No tutorial items will spawn.");
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
        if (realTrashPrefabs == null || realTrashPrefabs.Length == 0)
        {
            Debug.LogError("TrashSpawner: RealTrashPrefabs array is empty or null. Cannot start real game spawning.");
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
        while (true) // This loop continues as long as the coroutine is active
        {
            float randomDelay = Random.Range(minSpawnDelay, maxSpawnDelay);
            Debug.Log($"TrashSpawner.SpawnLoop: Waiting for randomDelay: {randomDelay:F2}s.");
            yield return new WaitForSeconds(randomDelay);
            Debug.Log("TrashSpawner.SpawnLoop: Random delay complete. Calling SpawnNextItem().");
            SpawnNextItem();
        }
    }

    void SpawnNextItem()
    {
        Debug.Log($"TrashSpawner.SpawnNextItem: Method called. isTutorialMode = {isTutorialMode}"); // Log entry to method

        GameObject objectToSpawn = null;
        TutorialItemData tutorialDataForThisItem = null;

        if (isTutorialMode)
        {
            Debug.Log("TrashSpawner.SpawnNextItem: ---- In Tutorial Mode ----");

            string ctiStatus = (currentTutorialItems == null) ? "NULL" : currentTutorialItems.Count.ToString();
            Debug.Log($"TrashSpawner.SpawnNextItem - PRE-CHECK: currentTutorialItems.Count='{ctiStatus}', nextTutorialItemIndex={nextTutorialItemIndex}");

            if (currentTutorialItems == null || currentTutorialItems.Count == 0 || nextTutorialItemIndex >= currentTutorialItems.Count)
            {
                Debug.LogWarning($"TrashSpawner.SpawnNextItem: Condition MET for 'Reached end of tutorial items or list empty'. currentTutorialItems.Count='{ctiStatus}', nextTutorialItemIndex={nextTutorialItemIndex}. Tutorial spawning cycle complete for now (or list was invalid).");
                // If we've reached the end, we should probably stop the spawner or this specific loop for tutorial items.
                // For now, just returning will stop this particular spawn attempt.
                // The TutorialManager should ideally call StopSpawning() when the tutorial phase is over.
                return;
            }

            tutorialDataForThisItem = currentTutorialItems[nextTutorialItemIndex];
            Debug.Log($"TrashSpawner.SpawnNextItem: Processing tutorial item index {nextTutorialItemIndex}, Item Name: '{tutorialDataForThisItem.itemName}'");

            if (tutorialDataForThisItem.shadowSprite != null)
            {
                Debug.Log($"TrashSpawner.SpawnNextItem: Shadow sprite for '{tutorialDataForThisItem.itemName}' IS ASSIGNED ('{tutorialDataForThisItem.shadowSprite.name}'). Creating shadow GameObject.");

                GameObject shadowGO = new GameObject("TutorialShadow_" + tutorialDataForThisItem.itemName);
                SpriteRenderer sr = shadowGO.AddComponent<SpriteRenderer>();
                sr.sprite = tutorialDataForThisItem.shadowSprite;

                sr.sortingLayerName = "Default";
                sr.sortingOrder = 2;
                Debug.Log($"TrashSpawner.SpawnNextItem: Added SpriteRenderer to '{shadowGO.name}', Sprite: {sr.sprite.name}, SortingLayer: {sr.sortingLayerName}, Order: {sr.sortingOrder}");

                BoxCollider2D collider = shadowGO.AddComponent<BoxCollider2D>();
                Debug.Log($"TrashSpawner.SpawnNextItem: Added BoxCollider2D to '{shadowGO.name}'.");

                TutorialShadowItem shadowItemScript = shadowGO.AddComponent<TutorialShadowItem>();
                Debug.Log($"TrashSpawner.SpawnNextItem: Added TutorialShadowItem script to '{shadowGO.name}'.");
                shadowItemScript.Initialize(tutorialDataForThisItem);
                Debug.Log($"TrashSpawner.SpawnNextItem: Initialized TutorialShadowItem script for '{shadowGO.name}'.");

                objectToSpawn = shadowGO;
            }
            else
            {
                Debug.LogWarning($"TrashSpawner.SpawnNextItem: Shadow sprite for tutorial item '{tutorialDataForThisItem.itemName}' IS NULL. Cannot create shadow object for this item.");
            }
            nextTutorialItemIndex++; // Increment after processing the current item
            Debug.Log($"TrashSpawner.SpawnNextItem: Incremented nextTutorialItemIndex to {nextTutorialItemIndex}");
        }
        else // Real game mode
        {
            Debug.Log("TrashSpawner.SpawnNextItem: ---- In Real Game Mode ----");
            if (realTrashPrefabs == null || realTrashPrefabs.Length == 0)
            {
                Debug.LogError("TrashSpawner: realTrashPrefabs is not set or empty in real game mode.");
                return;
            }
            int randomIndex = Random.Range(0, realTrashPrefabs.Length);
            objectToSpawn = realTrashPrefabs[randomIndex];
            Debug.Log($"TrashSpawner.SpawnNextItem: Selected real trash prefab '{objectToSpawn.name}' for real game mode.");
        }

        if (objectToSpawn == null)
        {
            Debug.LogWarning("TrashSpawner.SpawnNextItem: objectToSpawn is NULL after selection logic. Nothing will be spawned this cycle.");
            return;
        }

        float randomXOffset = Random.Range(spawnAreaMinX, spawnAreaMaxX);
        float randomYOffset = Random.Range(spawnAreaMinY, spawnAreaMaxY);
        Vector3 localSpawnOffset = new Vector3(randomXOffset, randomYOffset, 0f);
        Vector3 spawnPosition = transform.position + localSpawnOffset;
        Debug.Log($"TrashSpawner.SpawnNextItem: Calculated spawnPosition: {spawnPosition}");

        if (isTutorialMode)
        {
            // In tutorial mode, objectToSpawn is the already created shadowGO
            if (objectToSpawn.GetComponent<TutorialShadowItem>() != null) // A good check to ensure it's the shadow
            {
                 objectToSpawn.transform.position = spawnPosition;
                 Debug.Log($"TrashSpawner.SpawnNextItem: Positioned tutorial shadow '{objectToSpawn.name}' at {spawnPosition}.");
            }
            else
            {
                 Debug.LogWarning($"TrashSpawner.SpawnNextItem: In tutorial mode, but objectToSpawn ('{objectToSpawn.name}') does not have TutorialShadowItem script. This is unexpected.");
            }
        }
        else // Real game mode, objectToSpawn is a prefab
        {
            GameObject newInstance = Instantiate(objectToSpawn, spawnPosition, Quaternion.identity);
            Debug.Log($"TrashSpawner.SpawnNextItem: Instantiated real trash '{newInstance.name}' at {spawnPosition}.");
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
