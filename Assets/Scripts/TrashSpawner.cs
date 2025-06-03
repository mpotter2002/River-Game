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

    [Header("Spawning Timing (for regular trash & tutorial items)")]
    [SerializeField] private float minSpawnDelay = 0.5f;
    [SerializeField] private float maxSpawnDelay = 2.0f;
    [SerializeField] private float initialDelay = 1.0f;

    [Header("References")]
    [Tooltip("Assign the Main Camera here. Used for checking if tutorial items are off-screen.")]
    [SerializeField] private Camera mainCamera;

    [Tooltip("Assign GOOD ITEM PREFABS here. These are items that DEDUCT points if clicked.")]
    [SerializeField] private GameObject[] goodItemPrefabs;
    [Tooltip("How often (in seconds) a good item should attempt to spawn.")]
    [SerializeField] private float goodItemSpawnInterval = 12f;
    [Tooltip("How many points to deduct when a good item is clicked.")]
    [SerializeField] private int goodItemDeductionAmount = 3;

    // --- Tutorial Specific Variables ---
    private TutorialItemData currentTutorialItemFocus; // The single item to repeatedly spawn
    private bool isTutorialMode = false;
    private GameObject activeTutorialShadowInstance = null; // Track the active shadow

    private Coroutine spawningCoroutine;
    private float timeSinceLastDivvyBikeSpawn = 0f;
    private bool shouldAttemptDivvySpawnNext = false;
    private float timeSinceLastGoodItemSpawn = 0f;
    private bool shouldAttemptGoodItemSpawnNext = false;

    void Start()
    {
        if (mainCamera == null)
        {
            if (transform.parent != null) mainCamera = transform.parent.GetComponent<Camera>();
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) Debug.LogError($"TrashSpawner '{gameObject.name}': Main Camera not assigned/found! Off-screen checks won't work.");
        }
    }

    void OnDisable()
    {
        StopSpawning();
    }

    void Update()
    {
        if (!isTutorialMode && spawningCoroutine != null) {
            if (divvyBikePrefab != null) {
                timeSinceLastDivvyBikeSpawn += Time.deltaTime;
                if (timeSinceLastDivvyBikeSpawn >= divvyBikeSpawnInterval) {
                    shouldAttemptDivvySpawnNext = true;
                }
            }
            if (goodItemPrefabs != null && goodItemPrefabs.Length > 0) {
                timeSinceLastGoodItemSpawn += Time.deltaTime;
                if (timeSinceLastGoodItemSpawn >= goodItemSpawnInterval) {
                    shouldAttemptGoodItemSpawnNext = true;
                }
            }
        }

        // --- ADDED: Continuously check if active tutorial shadow is off-screen ---
        if (isTutorialMode && activeTutorialShadowInstance != null && mainCamera != null)
        {
            float cameraBottomEdge = mainCamera.transform.position.y - mainCamera.orthographicSize;
            // Estimate shadow's top edge or use its actual bounds if more precise check is needed
            // For simplicity, using its transform.position.y. Adjust buffer based on sprite size/pivot.
            float shadowOffScreenBuffer = activeTutorialShadowInstance.GetComponent<SpriteRenderer>()?.bounds.size.y ?? 2f; // Use sprite height or a buffer

            if (activeTutorialShadowInstance.transform.position.y < cameraBottomEdge - shadowOffScreenBuffer)
            {
                Debug.Log($"TrashSpawner: Active tutorial shadow '{activeTutorialShadowInstance.name}' (Type: {currentTutorialItemFocus?.itemName}) went off-screen. Destroying it to allow respawn of the same item type.");
                Destroy(activeTutorialShadowInstance);
                activeTutorialShadowInstance = null; // Allow a new one of the same type to spawn
            }
        }
        // --- END OFF-SCREEN CHECK ---
    }

    public void StartTutorialSpawning(TutorialItemData itemToFocusOn)
    {
        Debug.Log($"TrashSpawner: Starting TUTORIAL spawning. Focusing on item: {(itemToFocusOn != null ? itemToFocusOn.itemName : "NULL ITEM")}");
        isTutorialMode = true;
        currentTutorialItemFocus = itemToFocusOn;
        if (activeTutorialShadowInstance != null) Destroy(activeTutorialShadowInstance); // Clear any old one
        activeTutorialShadowInstance = null;
        timeSinceLastDivvyBikeSpawn = 0f;
        shouldAttemptDivvySpawnNext = false;

        if (currentTutorialItemFocus == null) {
            Debug.LogWarning("TrashSpawner: StartTutorialSpawning called with null itemToFocusOn. Tutorial mode cannot start effectively.");
            isTutorialMode = false; return;
        }
        if (currentTutorialItemFocus.shadowSprite == null) {
            Debug.LogWarning($"TrashSpawner: itemToFocusOn '{currentTutorialItemFocus.itemName}' has no shadowSprite. Cannot spawn tutorial item.");
            isTutorialMode = false; return;
        }

        StopSpawningInternal();
        spawningCoroutine = StartCoroutine(SpawnLoop());
    }

    public void SetCurrentTutorialItem(TutorialItemData newItemData)
    {
        Debug.Log($"TrashSpawner: Setting new tutorial focus item: {(newItemData != null ? newItemData.itemName : "NULL ITEM")}");
        if (activeTutorialShadowInstance != null) {
            Debug.Log($"TrashSpawner: Destroying previous active tutorial shadow '{activeTutorialShadowInstance.name}' due to item change.");
            Destroy(activeTutorialShadowInstance);
        }
        activeTutorialShadowInstance = null;
        currentTutorialItemFocus = newItemData;

        if (isTutorialMode) {
            if (currentTutorialItemFocus == null || currentTutorialItemFocus.shadowSprite == null) {
                Debug.LogWarning("TrashSpawner: New tutorial item is null or has no shadow. Spawning will pause until valid item set.");
                // If spawner was running, it will now fail to spawn in SpawnNextItem until a valid item is set.
            }
        }
    }

    public void StartRealGameSpawning()
    {
        Debug.Log("TrashSpawner: Starting REAL GAME spawning.");
        isTutorialMode = false;
        currentTutorialItemFocus = null;
        if (activeTutorialShadowInstance != null) { Destroy(activeTutorialShadowInstance); activeTutorialShadowInstance = null; }
        timeSinceLastDivvyBikeSpawn = 0f;
        shouldAttemptDivvySpawnNext = false;
        timeSinceLastGoodItemSpawn = 0f;
        shouldAttemptGoodItemSpawnNext = false;

        if ((realTrashPrefabs == null || realTrashPrefabs.Length == 0) && divvyBikePrefab == null) {
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
        if (spawningCoroutine != null) {
            StopCoroutine(spawningCoroutine);
            spawningCoroutine = null;
            Debug.Log("TrashSpawner: Spawning coroutine stopped.");
        }
        // When stopping, also destroy any active tutorial shadow
        if (isTutorialMode && activeTutorialShadowInstance != null) {
            Debug.Log($"TrashSpawner: Destroying active tutorial shadow '{activeTutorialShadowInstance.name}' on StopSpawning.");
            Destroy(activeTutorialShadowInstance);
            activeTutorialShadowInstance = null;
        }
    }

    IEnumerator SpawnLoop()
    {
        Debug.Log($"TrashSpawner.SpawnLoop: Starting with initialDelay: {initialDelay}s.");
        yield return new WaitForSeconds(initialDelay);
        Debug.Log("TrashSpawner.SpawnLoop: Initial delay complete. Entering spawn cycle.");

        while (true)
        {
            float currentSpawnDelay = Random.Range(minSpawnDelay, maxSpawnDelay);
            yield return new WaitForSeconds(currentSpawnDelay);
            SpawnNextItem();
        }
    }

    void SpawnNextItem()
    {
        GameObject objectToInstantiate = null;
        GameObject dynamicallyCreatedObject = null;
        bool isGoodItemToSpawn = false;

        if (isTutorialMode)
        {
            if (activeTutorialShadowInstance != null) { // Check again, in case Update didn't catch it or it was just destroyed
                // Debug.Log("TrashSpawner.SpawnNextItem: Active tutorial shadow still exists. Skipping spawn.");
                return;
            }

            if (currentTutorialItemFocus == null || currentTutorialItemFocus.shadowSprite == null) {
                Debug.LogWarning("TrashSpawner.SpawnNextItem: currentTutorialItemFocus is null or has no shadowSprite in tutorial mode. Cannot spawn.");
                return;
            }

            Debug.Log($"TrashSpawner.SpawnNextItem: Spawning tutorial shadow for: '{currentTutorialItemFocus.itemName}'");

            GameObject shadowGO = new GameObject("TutorialShadow_" + currentTutorialItemFocus.itemName);
            SpriteRenderer sr = shadowGO.AddComponent<SpriteRenderer>();
            sr.sprite = currentTutorialItemFocus.shadowSprite;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 2;

            shadowGO.AddComponent<BoxCollider2D>();
            TutorialShadowItem shadowItemScript = shadowGO.AddComponent<TutorialShadowItem>();
            shadowItemScript.Initialize(currentTutorialItemFocus);
            
            dynamicallyCreatedObject = shadowGO;
            activeTutorialShadowInstance = dynamicallyCreatedObject;
        }
        else // --- REAL GAME MODE SPAWNING ---
        {
            if (shouldAttemptGoodItemSpawnNext && goodItemPrefabs != null && goodItemPrefabs.Length > 0) {
                int randomGoodIndex = Random.Range(0, goodItemPrefabs.Length);
                objectToInstantiate = goodItemPrefabs[randomGoodIndex];
                timeSinceLastGoodItemSpawn = 0f;
                shouldAttemptGoodItemSpawnNext = false;
                isGoodItemToSpawn = true;
            } else if (shouldAttemptDivvySpawnNext && divvyBikePrefab != null) {
                objectToInstantiate = divvyBikePrefab;
                timeSinceLastDivvyBikeSpawn = 0f;
                shouldAttemptDivvySpawnNext = false;
            } else {
                if (realTrashPrefabs != null && realTrashPrefabs.Length > 0) {
                    int randomIndex = Random.Range(0, realTrashPrefabs.Length);
                    objectToInstantiate = realTrashPrefabs[randomIndex];
                } else if (divvyBikePrefab == null && (goodItemPrefabs == null || goodItemPrefabs.Length == 0)) {
                    Debug.LogError("TrashSpawner: realTrashPrefabs empty and no DivvyBikePrefab or GoodItemPrefabs.");
                    return;
                }
            }
        }

        if (objectToInstantiate == null && dynamicallyCreatedObject == null) {
            return;
        }

        float randomXOffset = Random.Range(spawnAreaMinX, spawnAreaMaxX);
        float randomYOffset = Random.Range(spawnAreaMinY, spawnAreaMaxY);
        Vector3 localSpawnOffset = new Vector3(randomXOffset, randomYOffset, 0f);
        Vector3 spawnPosition = transform.position + localSpawnOffset;

        if (isTutorialMode && dynamicallyCreatedObject != null) {
            dynamicallyCreatedObject.transform.position = spawnPosition;
        }
        else if (!isTutorialMode && objectToInstantiate != null) {
            GameObject spawned = Instantiate(objectToInstantiate, spawnPosition, Quaternion.identity);
            if (isGoodItemToSpawn) {
                TrashItem ti = spawned.GetComponent<TrashItem>();
                if (ti != null) {
                    ti.isGoodItem = true;
                    ti.goodItemDeductionAmount = goodItemDeductionAmount;
                }
            }
        }
    }

    void OnDrawGizmosSelected() { /* ... same ... */ }

    public void OnBackButtonClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("TitleScreen"); // Replace with your actual title scene name
    }
}
