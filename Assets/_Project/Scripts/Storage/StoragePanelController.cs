using UnityEngine;
using UI;

public class StoragePanelController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private InventoryUIController inventoryUI;

    private void Awake()
    {
        if (inventoryUI == null)
            inventoryUI = FindFirstObjectByType<InventoryUIController>(FindObjectsInactive.Include);
    }

    public void OpenStorage(StorageContainerInstance instance, SaveGameData.ContainerSaveData data)
    {
        if (inventoryUI == null)
        {
            Debug.LogError("[StoragePanelController] InventoryUIController not found.");
            return;
        }

        if (data == null)
        {
            Debug.LogError("[StoragePanelController] ContainerSaveData is null.");
            return;
        }

        // ✅ 여기서 실제로 왼쪽 StorageWindow를 켜고 슬롯을 그리는 쪽으로 전달
        inventoryUI.OpenStorage(data);
    }

    public void CloseStorage()
    {
        if (inventoryUI == null) return;
        inventoryUI.CloseStorage();
    }
}