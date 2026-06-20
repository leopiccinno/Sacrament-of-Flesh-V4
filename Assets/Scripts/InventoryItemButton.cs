using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryItemButton : MonoBehaviour
{
    public Image iconImage;

    private InventoryItem item;
    private InventoryManager inventoryManager;

    public void Setup(InventoryItem newItem, InventoryManager manager)
    {
        item = newItem;
        inventoryManager = manager;

        if (iconImage != null)
        {
            iconImage.sprite = item.iconSprite;
            iconImage.preserveAspect = true;
        }
    }

    public void OnClicked()
    {
        if (inventoryManager != null && item != null)
        {
            inventoryManager.ShowItemDescription(item);
        }
    }
}