using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    [Header("Inventory UI")]
    public Transform itemGrid;
    public GameObject inventoryItemButtonPrefab;
    public TextMeshProUGUI descriptionText;

    private List<InventoryItem> collectedItems = new List<InventoryItem>();

    public void AddItem(InventoryItem item)
    {
        if (item == null)
            return;

        if (collectedItems.Contains(item))
            return;

        collectedItems.Add(item);
        CreateInventoryButton(item);
    }

    private void CreateInventoryButton(InventoryItem item)
    {
        GameObject newButton = Instantiate(inventoryItemButtonPrefab, itemGrid);

        InventoryItemButton itemButton = newButton.GetComponent<InventoryItemButton>();

        if (itemButton != null)
        {
            itemButton.Setup(item, this);
        }
    }

    public void ShowItemDescription(InventoryItem item)
    {
        if (descriptionText != null && item != null)
        {
            descriptionText.text = item.description;
        }
    }
}