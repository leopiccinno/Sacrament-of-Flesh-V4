using UnityEngine;

[CreateAssetMenu(fileName = "NewInventoryItem", menuName = "Inventory/Inventory Item")]
public class InventoryItem : ScriptableObject
{
    public string itemName;

    [TextArea(2, 5)]
    public string description;

    public Sprite iconSprite;
    public Sprite bigInspectSprite;
}