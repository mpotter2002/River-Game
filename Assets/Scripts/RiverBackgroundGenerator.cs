using UnityEngine;
using System.Collections.Generic;

public class RiverBackgroundGenerator : MonoBehaviour
{
    [Header("Chunk Prefab")]
    [Tooltip("The standard repeating background/river chunk prefab.")]
    [SerializeField] private GameObject riverChunkPrefab; // Only needs the river prefab

    [Header("Generation Settings")]
    [Tooltip("The exact height of the River Chunk prefab in Unity units.")]
    [SerializeField] private float chunkHeight = 20f; // <<< SET THIS ACCURATELY for the RIVER prefab!
    [Tooltip("How far above the camera's view edge should we spawn the next chunk?")]
    [SerializeField] private float spawnAheadDistance = 5f;
    [Tooltip("How far below the camera's view edge should a chunk be before we destroy it?")]
    [SerializeField] private float despawnDistanceBelowCamera = 10f;

    [Header("References")]
    [Tooltip("Assign the main camera here (or leave empty if tagged 'MainCamera').")]
    [SerializeField] private Camera mainCamera;

    // Internal variables
    private List<GameObject> activeChunks = new List<GameObject>();
    private float nextSpawnY;
    private float cameraHalfHeight;

    void Start()
    {
        if (riverChunkPrefab == null) {
            Debug.LogError("RiverBackgroundGenerator: River Chunk Prefab must be assigned!"); enabled = false; return;
        }
        if (chunkHeight <= 0) {
            Debug.LogError("RiverBackgroundGenerator: Chunk Height must be a positive value!"); enabled = false; return;
        }
        if (mainCamera == null) {
            mainCamera = Camera.main;
            if (mainCamera == null) {
                 Debug.LogError("RiverBackgroundGenerator: Main Camera reference not set/found!"); enabled = false; return;
            }
        }

        cameraHalfHeight = mainCamera.orthographicSize;
        nextSpawnY = mainCamera.transform.position.y - cameraHalfHeight;
        SpawnInitialChunks();
    }

    void SpawnInitialChunks() {
        float initialSpawnCeiling = mainCamera.transform.position.y + cameraHalfHeight + spawnAheadDistance;
        while (nextSpawnY < initialSpawnCeiling) {
            SpawnChunk(nextSpawnY);
        }
    }

    void Update() {
        TrySpawnNextChunk();
        DespawnOldChunks();
    }

    void TrySpawnNextChunk() {
        float spawnTriggerY = mainCamera.transform.position.y + cameraHalfHeight + spawnAheadDistance;
        while (nextSpawnY < spawnTriggerY) {
            SpawnChunk(nextSpawnY);
        }
    }

    void SpawnChunk(float spawnY) {
        Vector3 spawnPos = new Vector3(transform.position.x, spawnY, transform.position.z);
        GameObject newChunk = Instantiate(riverChunkPrefab, spawnPos, Quaternion.identity, transform);
        activeChunks.Add(newChunk);
        nextSpawnY += chunkHeight; // Always use river chunk height
    }

    void DespawnOldChunks() {
        float despawnTriggerY = mainCamera.transform.position.y - cameraHalfHeight - despawnDistanceBelowCamera;
        List<GameObject> chunksToRemove = new List<GameObject>();
        foreach (GameObject chunk in activeChunks) {
            // Assumes river chunk pivot is centered
            float chunkTopEdgeY = chunk.transform.position.y + (chunkHeight / 2f);
            if (chunkTopEdgeY < despawnTriggerY) {
                chunksToRemove.Add(chunk);
            }
        }
        foreach (GameObject chunkToRemove in chunksToRemove) {
            activeChunks.Remove(chunkToRemove);
            Destroy(chunkToRemove);
        }
    }
}