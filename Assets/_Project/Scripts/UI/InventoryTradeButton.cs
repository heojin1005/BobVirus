using UnityEngine;
using UI;

public class InventoryTradeButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InventoryUIController inventoryUI;

    [Header("Take Item")]
    [SerializeField] private string takeItemId;
    [SerializeField] private int takeCount = 1;

    [Header("Give Item")]
    [SerializeField] private string giveItemId;
    [SerializeField] private int giveCount = 1;

    public void ExecuteTrade()
    {
        if (inventoryUI == null)
        {
            Debug.LogError("[InventoryTradeButton] inventoryUI is null.");
            return;
        }

        bool success = inventoryUI.TryTradeInventoryItems(
            takeItemId,
            takeCount,
            giveItemId,
            giveCount
        );

        if (!success)
        {
            Debug.LogWarning($"[InventoryTradeButton] trade failed: {takeItemId} x{takeCount} -> {giveItemId} x{giveCount}");
            return;
        }

        Debug.Log($"[InventoryTradeButton] trade success: {takeItemId} x{takeCount} -> {giveItemId} x{giveCount}");
    }
}