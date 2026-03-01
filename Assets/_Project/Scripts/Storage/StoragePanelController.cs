using UnityEngine;

public class StoragePanelController : MonoBehaviour
{
    public bool IsStorageOpen { get; private set; }

    public StorageContainerInstance CurrentInstance { get; private set; }
    public SaveGameData.ContainerSaveData CurrentData { get; private set; }

    [Header("Optional: Auto close when Inventory closes")]
    [SerializeField] private UI.InventoryUIController inventoryUI;

    private void Awake()
    {
        if (inventoryUI == null)
            inventoryUI = FindFirstObjectByType<UI.InventoryUIController>(FindObjectsInactive.Include);
    }

    private void Update()
    {
        // 인벤이 닫히면 창고도 같이 닫힘 처리
        if (IsStorageOpen && inventoryUI != null && !inventoryUI.IsOpen)
        {
            CloseStorage();
        }
    }

    public void OpenStorage(StorageContainerInstance instance, SaveGameData.ContainerSaveData data)
    {
        CurrentInstance = instance;
        CurrentData = data;
        IsStorageOpen = true;

        Debug.Log($"[StoragePanel] OpenStorage - key={data.containerKey}, cap={data.capacity}, persist={instance.PersistToSave}, def={instance.Definition?.name}");
    }

    public void CloseStorage()
    {
        Debug.Log("[StoragePanel] CloseStorage");
        CurrentInstance = null;
        CurrentData = null;
        IsStorageOpen = false;
    }
}