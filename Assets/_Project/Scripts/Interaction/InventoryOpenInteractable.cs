using UnityEngine;

public class InventoryOpenInteractable : MonoBehaviour, IInteractable
{
    [Header("Ref")]
    [SerializeField] private InventoryToggleController inventoryToggle;

    [Header("Behavior")]
    [SerializeField] private bool toggle = true;   // true면 토글, false면 무조건 Open

    private void Awake()
    {
        if (inventoryToggle == null)
            inventoryToggle = FindFirstObjectByType<InventoryToggleController>();
    }

    public void Interact()
    {
        if (inventoryToggle == null)
        {
            Debug.LogError("[InventoryOpenInteractable] InventoryToggleController not found.");
            return;
        }

        if (toggle) inventoryToggle.Toggle();
        else inventoryToggle.OpenInventory();
    }
}
