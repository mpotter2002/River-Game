using UnityEngine;

// This makes this class show up nicely in the Inspector when used in a list
[System.Serializable]
public class SpecialBuildingInfo
{
    public string buildingName; // For easy identification in the Inspector
    public GameObject prefab;   // The unique building prefab
    [Tooltip("The exact vertical height of THIS specific building prefab in world units.")]
    public float buildingHeight; // Important if special buildings have different heights
    [Tooltip("The world Y-coordinate the camera must reach or pass for this building to be considered for spawning.")]
    public float triggerWorldY;

    // This flag will be managed at runtime to ensure it only spawns once.
    // [System.NonSerialized] will prevent it from being saved in the editor,
    // ensuring it's always false when the game starts.
    [System.NonSerialized]
    public bool hasSpawned = false;
}
