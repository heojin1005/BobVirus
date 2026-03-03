using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    public class InventoryUIController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private SaveDataProvider saveDataProvider;
        [SerializeField] private ItemDatabase itemDatabase;

        [Header("UI Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("Inventory Grid")]
        [SerializeField] private Transform inventoryGridRoot;   // ScrollView/Viewport/Content
        [SerializeField] private InventorySlotUI slotPrefab;

        [Header("Inventory Scroll (AutoScroll)")]
        [Tooltip("ScrollRect가 비어있으면 inventoryGridRoot에서 자동 탐색합니다.")]
        [SerializeField] private ScrollRect inventoryScrollRect;
        [Tooltip("Viewport가 비어있으면 ScrollRect.viewport를 사용합니다.")]
        [SerializeField] private RectTransform inventoryViewport;
        [Tooltip("드래그 중 포인터가 뷰포트 상/하단 이 픽셀 안으로 들어오면 오토 스크롤")]
        [SerializeField] private float edgeThresholdPx = 70f;
        [Tooltip("오토 스크롤 속도(정규화 기준). 0.4~1.2 추천")]
        [SerializeField] private float autoScrollSpeed = 0.85f;
        [SerializeField] private bool enableAutoScroll = true;

        [Header("Storage UI (Left)")]
        [SerializeField] private GameObject storagePanelRoot;     // 왼쪽 패널 루트
        [SerializeField] private Transform storageGridRoot;        // StorageScrollView/Viewport/Content
        [SerializeField] private StorageSlotUI storageSlotPrefab;  // StorageSlotUI 프리팹

        [Header("Storage Scroll (AutoScroll)")]
        [SerializeField] private ScrollRect storageScrollRect;
        [SerializeField] private RectTransform storageViewport;

        [Header("Equip Slots (4)")]
        [SerializeField] private EquipSlotUI helmetSlot;
        [SerializeField] private EquipSlotUI topSlot;
        [SerializeField] private EquipSlotUI bottomSlot;
        [SerializeField] private EquipSlotUI shoesSlot;

        [Header("Drag Layer")]
        [SerializeField] private RectTransform dragLayer;
        [SerializeField] private Vector2 dragIconSize = new Vector2(90, 90);

        [Header("Saving")]
        [Tooltip("ON이면 아이템 이동/장착/스왑이 성공할 때마다 즉시 SaveNow()를 호출합니다. (권장: OFF, 닫을 때 1회 저장)")]
        [SerializeField] private bool saveAfterEachMove = false;

        private readonly List<InventorySlotUI> slots = new();
        private readonly List<StorageSlotUI> storageSlots = new();

        private SaveGameData data;                           // 플레이어 세이브
        private SaveGameData.ContainerSaveData storageData;   // 현재 열려있는 창고

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
        public bool IsStorageOpen => storagePanelRoot != null && storagePanelRoot.activeSelf && storageData != null;

        // Drag state
        private enum DragSourceType { None, Inventory, Storage, Equip }
        private DragSourceType dragSource = DragSourceType.None;

        private int fromInv = -1;
        private int fromStorage = -1;
        private EquipSlotType fromEquip = EquipSlotType.None;

        private bool dragging = false;
        private bool dropConsumed = false;

        private RectTransform ghostRt;
        private Image ghostImage;

        private Coroutine endDragCo;

        // AutoScroll runtime state
        private Vector2 lastPointerScreenPos;
        private Camera lastPressEventCamera;

        private void Awake()
        {
            if (saveDataProvider == null)
                saveDataProvider = FindFirstObjectByType<SaveDataProvider>();

            helmetSlot?.Bind(this);
            topSlot?.Bind(this);
            bottomSlot?.Bind(this);
            shoesSlot?.Bind(this);

            if (inventoryScrollRect == null && inventoryGridRoot != null)
                inventoryScrollRect = inventoryGridRoot.GetComponentInParent<ScrollRect>();
            if (inventoryViewport == null && inventoryScrollRect != null)
                inventoryViewport = inventoryScrollRect.viewport;

            if (storageScrollRect == null && storageGridRoot != null)
                storageScrollRect = storageGridRoot.GetComponentInParent<ScrollRect>();
            if (storageViewport == null && storageScrollRect != null)
                storageViewport = storageScrollRect.viewport;

            if (storagePanelRoot != null)
                storagePanelRoot.SetActive(false);
        }

        private void Update()
        {
            if (enableAutoScroll && dragging)
            {
                TryAutoScroll(inventoryScrollRect, inventoryViewport, lastPointerScreenPos, lastPressEventCamera);

                if (IsStorageOpen)
                    TryAutoScroll(storageScrollRect, storageViewport, lastPointerScreenPos, lastPressEventCamera);
            }
        }

        public void Open()
        {
            panelRoot?.SetActive(true);

            data = saveDataProvider != null ? saveDataProvider.GetCurrentData() : null;
            if (data == null)
            {
                Debug.LogError("[InventoryUI] GetCurrentData() is null");
                return;
            }

            data.NormalizeInventory();
            EnsureInventorySlots(data.inventoryCapacity);
            RefreshAll();
        }

        public void Close()
        {
            // 인벤 닫히면 창고도 닫힘 처리
            CloseStorage();
            panelRoot?.SetActive(false);
        }

        public void SaveNow()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SaveNow();
        }

        // =========================
        // Storage API (StoragePanelController가 호출)
        // =========================
        public void OpenStorage(SaveGameData.ContainerSaveData containerData)
        {
            if (containerData == null)
            {
                Debug.LogError("[InventoryUI] OpenStorage called with null data");
                return;
            }

            storageData = containerData;
            storageData.Normalize();

            if (storagePanelRoot != null)
                storagePanelRoot.SetActive(true);

            EnsureStorageSlots(storageData.capacity);
            RefreshStorage();
        }

        public void CloseStorage()
        {
            storageData = null;
            if (storagePanelRoot != null)
                storagePanelRoot.SetActive(false);
        }

        private void EnsureInventorySlots(int capacity)
        {
            while (slots.Count < capacity)
            {
                var slot = Instantiate(slotPrefab, inventoryGridRoot);
                slots.Add(slot);
            }

            for (int i = 0; i < slots.Count; i++)
                slots[i].gameObject.SetActive(i < capacity);

            for (int i = 0; i < capacity; i++)
                slots[i].Bind(i, this);
        }

        private void EnsureStorageSlots(int capacity)
        {
            if (storageGridRoot == null || storageSlotPrefab == null)
            {
                Debug.LogError("[InventoryUI] Storage grid/prefab is not assigned.");
                return;
            }

            while (storageSlots.Count < capacity)
            {
                var slot = Instantiate(storageSlotPrefab, storageGridRoot);
                storageSlots.Add(slot);
            }

            for (int i = 0; i < storageSlots.Count; i++)
                storageSlots[i].gameObject.SetActive(i < capacity);

            for (int i = 0; i < capacity; i++)
                storageSlots[i].Bind(i, this);
        }

        private void RefreshAll()
        {
            if (data == null) return;

            // Inventory
            for (int i = 0; i < data.inventoryCapacity; i++)
            {
                string id = data.inventoryItems[i];
                slots[i].SetIcon(string.IsNullOrEmpty(id) ? null : itemDatabase.GetIconOrDefault(id));
                slots[i].SetHighlight(false);
            }

            // Equipped
            helmetSlot?.SetIcon(string.IsNullOrEmpty(data.helmetId) ? null : itemDatabase.GetIconOrDefault(data.helmetId));
            topSlot?.SetIcon(string.IsNullOrEmpty(data.topId) ? null : itemDatabase.GetIconOrDefault(data.topId));
            bottomSlot?.SetIcon(string.IsNullOrEmpty(data.bottomId) ? null : itemDatabase.GetIconOrDefault(data.bottomId));
            shoesSlot?.SetIcon(string.IsNullOrEmpty(data.shoesId) ? null : itemDatabase.GetIconOrDefault(data.shoesId));

            helmetSlot?.SetHighlight(false);
            topSlot?.SetHighlight(false);
            bottomSlot?.SetHighlight(false);
            shoesSlot?.SetHighlight(false);

            if (IsStorageOpen)
                RefreshStorage();
        }

        private void RefreshStorage()
        {
            if (!IsStorageOpen || storageData == null) return;

            storageData.Normalize();

            for (int i = 0; i < storageData.capacity; i++)
            {
                string id = storageData.items[i];
                storageSlots[i].SetIcon(string.IsNullOrEmpty(id) ? null : itemDatabase.GetIconOrDefault(id));
                storageSlots[i].SetHighlight(false);
            }
        }

        // =========================
        // Saving helper
        // =========================
        private void SaveNowIfEnabled()
        {
            if (!saveAfterEachMove) return;
            if (GameManager.Instance != null)
                GameManager.Instance.SaveNow();
        }

        private void CommitChange()
        {
            RefreshAll();
            SaveNowIfEnabled();
        }

        // =========================
        // Drag entry points
        // =========================
        public void BeginDragFromInventory(int index, PointerEventData eventData)
        {
            if (data == null) return;

            string id = GetInv(index);
            if (string.IsNullOrEmpty(id)) return;

            StartDragging(DragSourceType.Inventory, index, -1, EquipSlotType.None, itemDatabase.GetIconOrDefault(id), eventData);
            slots[index].SetHighlight(true);
        }

        public void BeginDragFromStorage(int index, PointerEventData eventData)
        {
            if (!IsStorageOpen || storageData == null) return;

            string id = GetStorage(index);
            if (string.IsNullOrEmpty(id)) return;

            StartDragging(DragSourceType.Storage, -1, index, EquipSlotType.None, itemDatabase.GetIconOrDefault(id), eventData);
            storageSlots[index].SetHighlight(true);
        }

        public void BeginDragFromEquip(EquipSlotType slot, PointerEventData eventData)
        {
            if (data == null) return;

            string id = GetEquip(slot);
            if (string.IsNullOrEmpty(id)) return;

            StartDragging(DragSourceType.Equip, -1, -1, slot, itemDatabase.GetIconOrDefault(id), eventData);
            GetEquipUI(slot)?.SetHighlight(true);
        }

        private void StartDragging(DragSourceType src, int inv, int stor, EquipSlotType equip, Sprite sprite, PointerEventData eventData)
        {
            if (endDragCo != null) StopCoroutine(endDragCo);

            dragging = true;
            dropConsumed = false;

            dragSource = src;
            fromInv = inv;
            fromStorage = stor;
            fromEquip = equip;

            lastPointerScreenPos = eventData.position;
            lastPressEventCamera = eventData.pressEventCamera;

            CreateGhost(sprite, eventData);
        }

        public void DragMove(PointerEventData eventData)
        {
            if (!dragging || ghostRt == null || dragLayer == null) return;

            lastPointerScreenPos = eventData.position;
            lastPressEventCamera = eventData.pressEventCamera;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragLayer, eventData.position, eventData.pressEventCamera, out var local))
            {
                ghostRt.anchoredPosition = local;
            }
        }

        public void EndDrag(PointerEventData eventData)
        {
            if (!dragging) return;

            if (endDragCo != null) StopCoroutine(endDragCo);
            endDragCo = StartCoroutine(EndDragNextFrame());
        }

        private IEnumerator EndDragNextFrame()
        {
            yield return null;
            if (dragging) CleanupDrag();
        }

        // =========================
        // Drop Targets
        // =========================
        public void DropToInventory(int toIndex)
        {
            if (!dragging || data == null) return;
            if (dropConsumed) return;
            dropConsumed = true;

            // Inventory -> Inventory swap
            if (dragSource == DragSourceType.Inventory)
            {
                if (fromInv != toIndex)
                {
                    SwapInv(fromInv, toIndex);
                    CommitChange();
                }
                CleanupDrag();
                return;
            }

            // Storage -> Inventory move/swap
            if (dragSource == DragSourceType.Storage && IsStorageOpen && storageData != null)
            {
                string fromId = GetStorage(fromStorage);
                if (string.IsNullOrEmpty(fromId)) { CleanupDrag(); return; }

                string toId = GetInv(toIndex);

                if (string.IsNullOrEmpty(toId))
                {
                    SetInv(toIndex, fromId);
                    SetStorage(fromStorage, "");
                    CommitChange();
                    CleanupDrag();
                    return;
                }

                SetInv(toIndex, fromId);
                SetStorage(fromStorage, toId);
                CommitChange();
                CleanupDrag();
                return;
            }

            // Equip -> Inventory (기존 정책 유지)
            if (dragSource == DragSourceType.Equip)
            {
                string equipId = GetEquip(fromEquip);
                if (string.IsNullOrEmpty(equipId)) { CleanupDrag(); return; }

                string invId = GetInv(toIndex);

                if (string.IsNullOrEmpty(invId))
                {
                    SetInv(toIndex, equipId);
                    SetEquip(fromEquip, "");
                    CommitChange();
                    CleanupDrag();
                    return;
                }

                if (CanEquip(invId, fromEquip))
                {
                    SetInv(toIndex, equipId);
                    SetEquip(fromEquip, invId);
                    CommitChange();
                }

                CleanupDrag();
                return;
            }

            CleanupDrag();
        }

        public void DropToStorage(int toIndex)
        {
            if (!dragging) return;
            if (dropConsumed) return;
            if (!IsStorageOpen || storageData == null) { CleanupDrag(); return; }
            dropConsumed = true;

            // Storage -> Storage swap
            if (dragSource == DragSourceType.Storage)
            {
                if (fromStorage != toIndex)
                {
                    SwapStorage(fromStorage, toIndex);
                    CommitChange();
                }
                CleanupDrag();
                return;
            }

            // Inventory -> Storage move/swap
            if (dragSource == DragSourceType.Inventory)
            {
                string fromId = GetInv(fromInv);
                if (string.IsNullOrEmpty(fromId)) { CleanupDrag(); return; }

                string toId = GetStorage(toIndex);

                if (string.IsNullOrEmpty(toId))
                {
                    SetStorage(toIndex, fromId);
                    SetInv(fromInv, "");
                    CommitChange();
                    CleanupDrag();
                    return;
                }

                SetStorage(toIndex, fromId);
                SetInv(fromInv, toId);
                CommitChange();
                CleanupDrag();
                return;
            }

            // Equip -> Storage (원하면 다음 단계에서 허용 가능)
            CleanupDrag();
        }

        public void DropToEquip(EquipSlotType toSlot)
        {
            if (!dragging || data == null) return;
            if (dropConsumed) return;
            dropConsumed = true;

            // Inventory -> Equip
            if (dragSource == DragSourceType.Inventory)
            {
                string fromId = GetInv(fromInv);
                if (string.IsNullOrEmpty(fromId)) { CleanupDrag(); return; }
                if (!CanEquip(fromId, toSlot)) { CleanupDrag(); return; }

                string equippedId = GetEquip(toSlot);

                SetEquip(toSlot, fromId);
                SetInv(fromInv, equippedId ?? "");
                CommitChange();

                CleanupDrag();
                return;
            }

            // Equip -> Equip
            if (dragSource == DragSourceType.Equip)
            {
                if (fromEquip == toSlot) { CleanupDrag(); return; }

                string fromId = GetEquip(fromEquip);
                if (string.IsNullOrEmpty(fromId)) { CleanupDrag(); return; }
                if (!CanEquip(fromId, toSlot)) { CleanupDrag(); return; }

                string toId = GetEquip(toSlot);

                if (string.IsNullOrEmpty(toId))
                {
                    SetEquip(toSlot, fromId);
                    SetEquip(fromEquip, "");
                    CommitChange();
                    CleanupDrag();
                    return;
                }

                if (CanEquip(toId, fromEquip))
                {
                    SetEquip(toSlot, fromId);
                    SetEquip(fromEquip, toId);
                    CommitChange();
                }

                CleanupDrag();
                return;
            }

            CleanupDrag();
        }

        // =========================
        // AutoScroll
        // =========================
        private void TryAutoScroll(ScrollRect sr, RectTransform viewport, Vector2 pointerScreenPos, Camera eventCamera)
{
    if (sr == null || viewport == null) return;
    if (!sr.vertical) return;
    if (sr.content == null) return;

    var content = sr.content;
    if (content.rect.height <= viewport.rect.height + 0.01f) return;

    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
        viewport, pointerScreenPos, eventCamera, out var local))
        return;

    // ✅ 핵심: 포인터가 이 viewport 안에 있을 때만 오토스크롤
    var rect = viewport.rect;
    bool inside =
        local.x >= rect.xMin && local.x <= rect.xMax &&
        local.y >= rect.yMin && local.y <= rect.yMax;

    if (!inside) return;

    float dt = Time.unscaledDeltaTime;
    if (dt <= 0f) return;

    float topEdge = rect.yMax - edgeThresholdPx;
    float bottomEdge = rect.yMin + edgeThresholdPx;

    float dir = 0f;
    if (local.y >= topEdge) dir = +1f;
    else if (local.y <= bottomEdge) dir = -1f;

    if (dir == 0f) return;

    float delta = autoScrollSpeed * dt * dir;
    sr.verticalNormalizedPosition = Mathf.Clamp01(sr.verticalNormalizedPosition + delta);
}

        // =========================
        // Data helpers
        // =========================
        private string GetInv(int index)
        {
            if (data == null) return "";
            if (index < 0 || index >= data.inventoryCapacity) return "";
            return data.inventoryItems[index] ?? "";
        }

        private void SetInv(int index, string id)
        {
            if (data == null) return;
            if (index < 0 || index >= data.inventoryCapacity) return;
            data.inventoryItems[index] = id ?? "";
        }

        private void SwapInv(int a, int b)
        {
            if (data == null) return;
            if (a < 0 || b < 0) return;
            if (a >= data.inventoryCapacity || b >= data.inventoryCapacity) return;
            (data.inventoryItems[a], data.inventoryItems[b]) = (data.inventoryItems[b], data.inventoryItems[a]);
        }

        private string GetStorage(int index)
        {
            if (storageData == null) return "";
            if (index < 0 || index >= storageData.capacity) return "";
            return storageData.items[index] ?? "";
        }

        private void SetStorage(int index, string id)
        {
            if (storageData == null) return;
            if (index < 0 || index >= storageData.capacity) return;
            storageData.items[index] = id ?? "";
        }

        private void SwapStorage(int a, int b)
        {
            if (storageData == null) return;
            if (a < 0 || b < 0) return;
            if (a >= storageData.capacity || b >= storageData.capacity) return;
            (storageData.items[a], storageData.items[b]) = (storageData.items[b], storageData.items[a]);
        }

        private string GetEquip(EquipSlotType slot)
        {
            if (data == null) return "";
            return slot switch
            {
                EquipSlotType.Helmet => data.helmetId ?? "",
                EquipSlotType.Top => data.topId ?? "",
                EquipSlotType.Bottom => data.bottomId ?? "",
                EquipSlotType.Shoes => data.shoesId ?? "",
                _ => ""
            };
        }

        private void SetEquip(EquipSlotType slot, string id)
        {
            if (data == null) return;

            id ??= "";
            switch (slot)
            {
                case EquipSlotType.Helmet: data.helmetId = id; break;
                case EquipSlotType.Top: data.topId = id; break;
                case EquipSlotType.Bottom: data.bottomId = id; break;
                case EquipSlotType.Shoes: data.shoesId = id; break;
            }
        }

        private bool CanEquip(string itemId, EquipSlotType slot)
        {
            if (string.IsNullOrEmpty(itemId)) return false;
            return itemDatabase.GetEquipSlotOrNone(itemId) == slot;
        }

        private EquipSlotUI GetEquipUI(EquipSlotType slot)
        {
            return slot switch
            {
                EquipSlotType.Helmet => helmetSlot,
                EquipSlotType.Top => topSlot,
                EquipSlotType.Bottom => bottomSlot,
                EquipSlotType.Shoes => shoesSlot,
                _ => null
            };
        }

        // =========================
        // Ghost UI
        // =========================
        private void CreateGhost(Sprite sprite, PointerEventData eventData)
        {
            CleanupGhost();

            if (dragLayer == null)
            {
                Debug.LogError("[InventoryUI] DragLayer is null. Assign it in inspector.");
                return;
            }

            var go = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            go.transform.SetParent(dragLayer, false);

            ghostRt = go.GetComponent<RectTransform>();
            ghostRt.sizeDelta = dragIconSize;

            var cg = go.GetComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.alpha = 0.9f;

            ghostImage = go.GetComponent<Image>();
            ghostImage.raycastTarget = false;
            ghostImage.sprite = sprite;
            ghostImage.preserveAspect = true;

            DragMove(eventData);
        }

        private void CleanupGhost()
        {
            if (ghostRt != null) Destroy(ghostRt.gameObject);
            ghostRt = null;
            ghostImage = null;
        }

        private void CleanupDrag()
        {
            if (fromInv >= 0 && fromInv < slots.Count)
                slots[fromInv].SetHighlight(false);

            if (fromStorage >= 0 && fromStorage < storageSlots.Count)
                storageSlots[fromStorage].SetHighlight(false);

            if (fromEquip != EquipSlotType.None)
                GetEquipUI(fromEquip)?.SetHighlight(false);

            dragging = false;
            dropConsumed = false;

            dragSource = DragSourceType.None;
            fromInv = -1;
            fromStorage = -1;
            fromEquip = EquipSlotType.None;

            CleanupGhost();
        }
    }
}