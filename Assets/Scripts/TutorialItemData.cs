using UnityEngine;

// This makes it so you can create and edit instances of this class
// directly in the Unity Inspector if you make it a public field in another script.
// For more advanced use, you could turn this into a ScriptableObject.
[System.Serializable]
public class TutorialItemData
{
    public string itemName = "New Item";
    [TextArea(3, 5)] // Makes the description field larger in the Inspector
    public string itemDescription = "Some interesting facts about this item...";
    public Sprite shadowSprite; // Assign the shadow version of the item
    public Sprite revealedSprite; // Assign the actual item sprite
    public bool isWildlife = false; // True if this item should be "kept" not "trashed"
    // You could add more fields here, e.g., points for trashing/keeping in the real game
}
