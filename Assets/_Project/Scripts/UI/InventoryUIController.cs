using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
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

        [Header("Split UI")]
        [SerializeField] private SplitDragPanel splitDragPanel;

        // ✅ payload 드래그 핵심 상태
        private string dragItemId = "";
        private int dragCount = 0;
        private bool holdDragAfterSplit = false;

        [Header("Saving")]
        [Tooltip("ON이면 아이템 이동/장착/스왑이 성공할 때마다 즉시 SaveNow()를 호출합니다. (권장: OFF, 닫을 때 1회 저장)")]
        [SerializeField] private bool saveAfterEachMove = false;

        private readonly List<InventorySlotUI> slots = new();
        private readonly List<StorageSlotUI> storageSlots = new();

        private SaveGameData data;                           // 플레이어 세이브
        private SaveGameData.ContainerSaveData storageData;   // 현재 열려있는 창고

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
        public bool IsStorageOpen => storagePanelRoot != null && storagePanelRoot.activeSelf && storageData != null;

        // ✅ SlotUI에서 "클릭으로 드롭" 판단에 사용
        public bool IsHoldingPayloadDrag => dragging && holdDragAfterSplit;

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

            for (int i = 0; i < data.inventoryCapacity; i++)
            {
                var s = data.inventorySlots[i];
                string id = (s == null) ? "" : s.id;
                int count = (s == null) ? 0 : s.count;

                slots[i].SetIcon(string.IsNullOrEmpty(id) ? null : itemDatabase.GetIconOrDefault(id));
                slots[i].SetCount(count);
                slots[i].SetHighlight(false);
            }

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
                var s = storageData.slots[i];
                string id = (s == null) ? "" : s.id;
                int count = (s == null) ? 0 : s.count;

                storageSlots[i].SetIcon(string.IsNullOrEmpty(id) ? null : itemDatabase.GetIconOrDefault(id));
                storageSlots[i].SetCount(count);
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
        // Split entry points (SlotUI가 호출)
        // =========================
        public void TryOpenSplitFromInventory(int index)
        {
            if (data == null) return;
            if (splitDragPanel == null) return;
            if (dragging) return;

            var s = GetInvSlot(index);
            if (s == null || string.IsNullOrEmpty(s.id) || s.count <= 1) return;

            string id = s.id;
            int currentCount = s.count;

            splitDragPanel.Show(
                currentCount,
                onConfirm: (splitCount) =>
                {
                    splitCount = Mathf.Clamp(splitCount, 1, currentCount - 1);

                    // ✅ 원본 슬롯 차감
                    s.count -= splitCount;
                    if (s.count <= 0)
                    {
                        s.id = "";
                        s.count = 0;
                    }

                    // ✅ payload 설정
                    dragItemId = id;
                    dragCount = splitCount;
                    holdDragAfterSplit = true;

                    // ✅ 이벤트 없이 드래그 진입
                    StartDragging(DragSourceType.Inventory, index, -1, EquipSlotType.None, itemDatabase.GetIconOrDefault(id));

                    slots[index].SetHighlight(true);
                    CommitChange();
                },
                onCancel: () => { }
            );
        }

        public void TryOpenSplitFromStorage(int index)
        {
            if (!IsStorageOpen || storageData == null) return;
            if (splitDragPanel == null) return;
            if (dragging) return;

            var s = GetStorageSlot(index);
            if (s == null || string.IsNullOrEmpty(s.id) || s.count <= 1) return;

            string id = s.id;
            int currentCount = s.count;

            splitDragPanel.Show(
                currentCount,
                onConfirm: (splitCount) =>
                {
                    splitCount = Mathf.Clamp(splitCount, 1, currentCount - 1);

                    s.count -= splitCount;
                    if (s.count <= 0)
                    {
                        s.id = "";
                        s.count = 0;
                    }

                    dragItemId = id;
                    dragCount = splitCount;
                    holdDragAfterSplit = true;

                    StartDragging(DragSourceType.Storage, -1, index, EquipSlotType.None, itemDatabase.GetIconOrDefault(id));

                    storageSlots[index].SetHighlight(true);
                    CommitChange();
                },
                onCancel: () => { }
            );
        }

        // =========================
        // Drag entry points
        // =========================
        public void BeginDragFromInventory(int index, PointerEventData eventData)
        {
            if (data == null) return;

            var s = GetInvSlot(index);
            if (s == null || string.IsNullOrEmpty(s.id) || s.count <= 0) return;

            dragItemId = s.id;
            dragCount = s.count;
            holdDragAfterSplit = false;

            StartDragging(DragSourceType.Inventory, index, -1, EquipSlotType.None, itemDatabase.GetIconOrDefault(s.id), eventData);
            slots[index].SetHighlight(true);
        }

        public void BeginDragFromStorage(int index, PointerEventData eventData)
        {
            if (!IsStorageOpen || storageData == null) return;

            var s = GetStorageSlot(index);
            if (s == null || string.IsNullOrEmpty(s.id) || s.count <= 0) return;

            dragItemId = s.id;
            dragCount = s.count;
            holdDragAfterSplit = false;

            StartDragging(DragSourceType.Storage, -1, index, EquipSlotType.None, itemDatabase.GetIconOrDefault(s.id), eventData);
            storageSlots[index].SetHighlight(true);
        }

        public void BeginDragFromEquip(EquipSlotType slot, PointerEventData eventData)
        {
            if (data == null) return;

            string id = GetEquip(slot);
            if (string.IsNullOrEmpty(id)) return;

            dragItemId = id;
            dragCount = 1;
            holdDragAfterSplit = false;

            StartDragging(DragSourceType.Equip, -1, -1, slot, itemDatabase.GetIconOrDefault(id), eventData);
            GetEquipUI(slot)?.SetHighlight(true);
        }

        // ✅ 기존 StartDragging 유지 + eventData 없는 오버로드 추가
        private void StartDragging(DragSourceType src, int inv, int stor, EquipSlotType equip, Sprite sprite)
        {
            // ✅ Input System 기준 포인터 위치
            Vector2 pos;

            if (Pointer.current != null)
                pos = Pointer.current.position.ReadValue();
            else
                pos = lastPointerScreenPos; // 혹시 모를 fallback (마지막 포인터 위치)

            Camera cam = lastPressEventCamera; // 보통 null이어도 됨 (Overlay Canvas면 특히)
            StartDraggingInternal(src, inv, stor, equip, sprite, pos, cam);
        }

        private void StartDragging(DragSourceType src, int inv, int stor, EquipSlotType equip, Sprite sprite, PointerEventData eventData)
        {
            if (eventData == null)
            {
                StartDragging(src, inv, stor, equip, sprite);
                return;
            }

            StartDraggingInternal(src, inv, stor, equip, sprite, eventData.position, eventData.pressEventCamera);
        }

        private void StartDraggingInternal(DragSourceType src, int inv, int stor, EquipSlotType equip, Sprite sprite, Vector2 screenPos, Camera eventCamera)
        {
            if (endDragCo != null) StopCoroutine(endDragCo);

            dragging = true;
            dropConsumed = false;

            dragSource = src;
            fromInv = inv;
            fromStorage = stor;
            fromEquip = equip;

            lastPointerScreenPos = screenPos;
            lastPressEventCamera = eventCamera;

            CreateGhost(sprite, screenPos, eventCamera);
        }

        public void DragMove(PointerEventData eventData)
        {
            if (!dragging || ghostRt == null || dragLayer == null) return;
            if (eventData == null) return;

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

            // ✅ split-confirm 드래그는 포인터를 떼도 유지되어야 함
            if (holdDragAfterSplit) return;

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

            // ✅ payload 드래그면 별도 처리(클릭 드롭)
            if (holdDragAfterSplit)
            {
                HandlePayloadDropToInventory(toIndex);
                return;
            }

            if (dragSource == DragSourceType.Inventory && fromInv == toIndex)
            {
                CleanupDrag();
                return;
            }

            if (dragSource == DragSourceType.Inventory)
            {
                var from = GetInvSlot(fromInv);
                var to = GetInvSlot(toIndex);
                if (from == null || to == null) { CleanupDrag(); return; }

                if (IsEmpty(to))
                {
                    to.id = from.id;
                    to.count = from.count;
                    ClearSlot(from);

                    CommitChange();
                    CleanupDrag();
                    return;
                }

                if (!IsEmpty(from) && from.id == to.id)
                {
                    if (TryMerge(from, to))
                        CommitChange();

                    CleanupDrag();
                    return;
                }

                (data.inventorySlots[fromInv], data.inventorySlots[toIndex]) =
                    (data.inventorySlots[toIndex], data.inventorySlots[fromInv]);

                CommitChange();
                CleanupDrag();
                return;
            }

            if (dragSource == DragSourceType.Storage && IsStorageOpen && storageData != null)
            {
                var from = GetStorageSlot(fromStorage);
                var to = GetInvSlot(toIndex);
                if (from == null || to == null) { CleanupDrag(); return; }
                if (IsEmpty(from)) { CleanupDrag(); return; }

                if (IsEmpty(to))
                {
                    to.id = from.id;
                    to.count = from.count;
                    ClearSlot(from);

                    CommitChange();
                    CleanupDrag();
                    return;
                }

                if (from.id == to.id)
                {
                    if (TryMerge(from, to))
                        CommitChange();

                    CleanupDrag();
                    return;
                }

                (storageData.slots[fromStorage], data.inventorySlots[toIndex]) =
                    (data.inventorySlots[toIndex], storageData.slots[fromStorage]);

                CommitChange();
                CleanupDrag();
                return;
            }

            if (dragSource == DragSourceType.Equip)
            {
                string equipId = GetEquip(fromEquip);
                if (string.IsNullOrEmpty(equipId)) { CleanupDrag(); return; }

                var to = GetInvSlot(toIndex);
                if (to == null) { CleanupDrag(); return; }

                if (IsEmpty(to))
                {
                    to.id = equipId;
                    to.count = 1;
                    SetEquip(fromEquip, "");

                    CommitChange();
                    CleanupDrag();
                    return;
                }

                if (to.id == equipId)
                {
                    var tmpFrom = new SaveGameData.ItemSlotData(equipId, 1);
                    if (TryMerge(tmpFrom, to))
                    {
                        if (IsEmpty(tmpFrom))
                            SetEquip(fromEquip, "");

                        CommitChange();
                    }

                    CleanupDrag();
                    return;
                }

                if (CanEquip(to.id, fromEquip))
                {
                    string invId = to.id;

                    to.id = equipId;
                    to.count = 1;

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

            // ✅ payload 드래그면 별도 처리(클릭 드롭)
            if (holdDragAfterSplit)
            {
                HandlePayloadDropToStorage(toIndex);
                return;
            }

            if (dragSource == DragSourceType.Storage && fromStorage == toIndex)
            {
                CleanupDrag();
                return;
            }

            if (dragSource == DragSourceType.Storage)
            {
                var from = GetStorageSlot(fromStorage);
                var to = GetStorageSlot(toIndex);
                if (from == null || to == null) { CleanupDrag(); return; }

                if (IsEmpty(to))
                {
                    to.id = from.id;
                    to.count = from.count;
                    ClearSlot(from);

                    CommitChange();
                    CleanupDrag();
                    return;
                }

                if (!IsEmpty(from) && from.id == to.id)
                {
                    if (TryMerge(from, to))
                        CommitChange();

                    CleanupDrag();
                    return;
                }

                (storageData.slots[fromStorage], storageData.slots[toIndex]) =
                    (storageData.slots[toIndex], storageData.slots[fromStorage]);

                CommitChange();
                CleanupDrag();
                return;
            }

            if (dragSource == DragSourceType.Inventory)
            {
                var from = GetInvSlot(fromInv);
                var to = GetStorageSlot(toIndex);
                if (from == null || to == null) { CleanupDrag(); return; }
                if (IsEmpty(from)) { CleanupDrag(); return; }

                if (IsEmpty(to))
                {
                    to.id = from.id;
                    to.count = from.count;
                    ClearSlot(from);

                    CommitChange();
                    CleanupDrag();
                    return;
                }

                if (from.id == to.id)
                {
                    if (TryMerge(from, to))
                        CommitChange();

                    CleanupDrag();
                    return;
                }

                (data.inventorySlots[fromInv], storageData.slots[toIndex]) =
                    (storageData.slots[toIndex], data.inventorySlots[fromInv]);

                CommitChange();
                CleanupDrag();
                return;
            }

            CleanupDrag();
        }

        public void DropToEquip(EquipSlotType toSlot)
        {
            if (!dragging || data == null) return;
            if (dropConsumed) return;
            dropConsumed = true;

            // ✅ payload 드래그(분해) 상태에서는 장착은 아직 금지(정책)
            if (holdDragAfterSplit)
            {
                ReturnPayloadRemainToSource(dragCount);
                CommitChange();
                CleanupDrag();
                return;
            }

            if (dragSource == DragSourceType.Inventory)
            {
                string fromId = GetInv(fromInv);
                var fromSlot = GetInvSlot(fromInv);
                if (fromSlot != null && fromSlot.count > 1)
                {
                    CleanupDrag();
                    return;
                }
                if (string.IsNullOrEmpty(fromId)) { CleanupDrag(); return; }
                if (!CanEquip(fromId, toSlot)) { CleanupDrag(); return; }

                string equippedId = GetEquip(toSlot);

                SetEquip(toSlot, fromId);
                SetInv(fromInv, equippedId ?? "");
                CommitChange();

                CleanupDrag();
                return;
            }

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
        // Payload Drop (split-confirm)
        // =========================
        private void HandlePayloadDropToInventory(int toIndex)
        {
            var to = GetInvSlot(toIndex);
            if (to == null)
            {
                CancelPayloadDragReturnToSource();
                CleanupDrag();
                return;
            }

            if (IsEmpty(to))
            {
                to.id = dragItemId;
                to.count = dragCount;

                CommitChange();
                CleanupDrag();
                return;
            }

            if (to.id == dragItemId)
            {
                var tmpFrom = new SaveGameData.ItemSlotData(dragItemId, dragCount);
                if (TryMerge(tmpFrom, to))
                {
                    int remain = tmpFrom.count;
                    if (remain > 0) ReturnPayloadRemainToSource(remain);
                    CommitChange();
                }
                else
                {
                    ReturnPayloadRemainToSource(dragCount);
                    CommitChange();
                }

                CleanupDrag();
                return;
            }

            // payload는 swap 금지 -> 복귀
            ReturnPayloadRemainToSource(dragCount);
            CommitChange();
            CleanupDrag();
        }

        private void HandlePayloadDropToStorage(int toIndex)
        {
            var to = GetStorageSlot(toIndex);
            if (to == null)
            {
                CancelPayloadDragReturnToSource();
                CleanupDrag();
                return;
            }

            if (IsEmpty(to))
            {
                to.id = dragItemId;
                to.count = dragCount;

                CommitChange();
                CleanupDrag();
                return;
            }

            if (to.id == dragItemId)
            {
                var tmpFrom = new SaveGameData.ItemSlotData(dragItemId, dragCount);
                if (TryMerge(tmpFrom, to))
                {
                    int remain = tmpFrom.count;
                    if (remain > 0) ReturnPayloadRemainToSource(remain);
                    CommitChange();
                }
                else
                {
                    ReturnPayloadRemainToSource(dragCount);
                    CommitChange();
                }

                CleanupDrag();
                return;
            }

            ReturnPayloadRemainToSource(dragCount);
            CommitChange();
            CleanupDrag();
        }

        private void ReturnPayloadRemainToSource(int count)
        {
            if (count <= 0) return;

            if (dragSource == DragSourceType.Inventory)
            {
                var s = GetInvSlot(fromInv);
                if (s == null) return;

                if (IsEmpty(s))
                {
                    s.id = dragItemId;
                    s.count = count;
                }
                else if (s.id == dragItemId)
                {
                    s.count += count;
                }
                else
                {
                    Debug.LogWarning("[SplitDrag] Inventory source slot changed; cannot return cleanly.");
                }
            }
            else if (dragSource == DragSourceType.Storage)
            {
                var s = GetStorageSlot(fromStorage);
                if (s == null) return;

                if (IsEmpty(s))
                {
                    s.id = dragItemId;
                    s.count = count;
                }
                else if (s.id == dragItemId)
                {
                    s.count += count;
                }
                else
                {
                    Debug.LogWarning("[SplitDrag] Storage source slot changed; cannot return cleanly.");
                }
            }
        }

        private void CancelPayloadDragReturnToSource()
        {
            ReturnPayloadRemainToSource(dragCount);
            CommitChange();
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

            var s = data.inventorySlots[index];
            if (s == null) return "";
            return (string.IsNullOrEmpty(s.id) || s.count <= 0) ? "" : (s.id ?? "");
        }

        private void SetInv(int index, string id)
        {
            if (data == null) return;
            if (index < 0 || index >= data.inventoryCapacity) return;

            id ??= "";
            var s = data.inventorySlots[index] ?? new SaveGameData.ItemSlotData("", 0);

            if (string.IsNullOrEmpty(id))
            {
                s.id = "";
                s.count = 0;
            }
            else
            {
                s.id = id;
                s.count = 1; // (기존 구조 호환)
            }

            data.inventorySlots[index] = s;
        }

        private string GetStorage(int index)
        {
            if (storageData == null) return "";
            if (index < 0 || index >= storageData.capacity) return "";

            var s = storageData.slots[index];
            if (s == null) return "";
            return (string.IsNullOrEmpty(s.id) || s.count <= 0) ? "" : (s.id ?? "");
        }

        private void SetStorage(int index, string id)
        {
            if (storageData == null) return;
            if (index < 0 || index >= storageData.capacity) return;

            id ??= "";
            var s = storageData.slots[index] ?? new SaveGameData.ItemSlotData("", 0);

            if (string.IsNullOrEmpty(id))
            {
                s.id = "";
                s.count = 0;
            }
            else
            {
                s.id = id;
                s.count = 1; // (기존 구조 호환)
            }

            storageData.slots[index] = s;
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

        private SaveGameData.ItemSlotData GetInvSlot(int index)
        {
            if (data == null) return null;
            if (index < 0 || index >= data.inventoryCapacity) return null;
            return data.inventorySlots[index];
        }

        private SaveGameData.ItemSlotData GetStorageSlot(int index)
        {
            if (storageData == null) return null;
            if (index < 0 || index >= storageData.capacity) return null;
            return storageData.slots[index];
        }

        private static bool IsEmpty(SaveGameData.ItemSlotData s)
        {
            return s == null || string.IsNullOrEmpty(s.id) || s.count <= 0;
        }

        private static void ClearSlot(SaveGameData.ItemSlotData s)
        {
            if (s == null) return;
            s.id = "";
            s.count = 0;
        }

        private bool TryMerge(SaveGameData.ItemSlotData from, SaveGameData.ItemSlotData to)
        {
            if (from == null || to == null) return false;
            if (IsEmpty(from)) return false;
            if (IsEmpty(to)) return false;
            if (from.id != to.id) return false;

            int maxStack = itemDatabase != null
                ? itemDatabase.GetMaxStackOrDefault(from.id, 1)
                : 1;

            maxStack = Mathf.Max(1, maxStack);

            int space = maxStack - to.count;
            if (space <= 0) return false;

            int move = Mathf.Min(space, from.count);
            if (move <= 0) return false;

            to.count += move;
            from.count -= move;

            if (from.count <= 0)
                ClearSlot(from);

            return true;
        }

        // =========================
        // Ghost UI
        // =========================
        private void CreateGhost(Sprite sprite, Vector2 screenPos, Camera eventCamera)
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

            // ✅ eventData 없이도 위치 세팅
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragLayer, screenPos, eventCamera, out var local))
            {
                ghostRt.anchoredPosition = local;
            }
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

            // ✅ payload 리셋
            dragItemId = "";
            dragCount = 0;
            holdDragAfterSplit = false;

            CleanupGhost();
        }
    }
}