using UnityEngine;

[RequireComponent(typeof(StorageContainerInstance))]
public class StorageChestInteractable : MonoBehaviour, IInteractable
{
    [Header("Refs")]
    [SerializeField] private StorageContainerInstance containerInstance;
    [SerializeField] private InventoryToggleController inventoryToggle;
    [SerializeField] private SaveDataProvider saveDataProvider;
    [SerializeField] private StoragePanelController storagePanelController;

    private void Awake()
    {
        if (containerInstance == null)
            containerInstance = GetComponent<StorageContainerInstance>();

        if (inventoryToggle == null)
            inventoryToggle = FindFirstObjectByType<InventoryToggleController>();

        if (saveDataProvider == null)
            saveDataProvider = FindFirstObjectByType<SaveDataProvider>();

        // Inventory Canvas가 비활성 상태로 시작할 수도 있어서 inactive 포함
        if (storagePanelController == null)
            storagePanelController = FindFirstObjectByType<StoragePanelController>(FindObjectsInactive.Include);
    }

    public void Interact()
    {
        if (containerInstance == null)
        {
            Debug.LogError("[StorageChestInteractable] StorageContainerInstance not found.");
            return;
        }

        if (inventoryToggle == null)
        {
            Debug.LogError("[StorageChestInteractable] InventoryToggleController not found.");
            return;
        }

        if (storagePanelController == null)
        {
            Debug.LogError("[StorageChestInteractable] StoragePanelController not found. (Inventory Canvas에 붙여줘야 함)");
            return;
        }

        // 1) 인벤을 연다 (시간정지/입력맵 전환/월드탭 비활성 등 기존 로직 그대로)
        inventoryToggle.OpenInventory();

        // 2) 열 컨테이너 데이터 준비
        SaveGameData save = saveDataProvider != null ? saveDataProvider.GetCurrentData() : null;
        var data = containerInstance.GetOrCreateContainerData(save);
        if (data == null)
        {
            Debug.LogError("[StorageChestInteractable] Container data is null.");
            return;
        }

        // 3) UI쪽 컨트롤러에 전달 (3단계에서 실제 좌측 UI 바인딩)
        storagePanelController.OpenStorage(containerInstance, data);
    }
}