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

        [Header("Equip Slots (4)")]
        [SerializeField] private EquipSlotUI helmetSlot;
        [SerializeField] private EquipSlotUI topSlot;
        [SerializeField] private EquipSlotUI bottomSlot;
        [SerializeField] private EquipSlotUI shoesSlot;

        [Header("Drag Layer")]
        [SerializeField] private RectTransform dragLayer;
        [SerializeField] private Vector2 dragIconSize = new Vector2(90, 90);

        [Header("Saving")]
        [Tooltip("아이템 이동/장착/스왑이 성공할 때마다 즉시 SaveNow()를 호출합니다.")]
        [SerializeField] private bool saveAfterEachMove = true;

        private readonly List<InventorySlotUI> slots = new();
        private SaveGameData data;

        // Drag state
        private enum DragSourceType { None, Inventory, Equip }
        private DragSourceType dragSource = DragSourceType.None;

        private int fromInv = -1;
        private EquipSlotType fromEquip = EquipSlotType.None;

        private bool dragging = false;
        private bool dropConsumed = false;

        private RectTransform ghostRt;
        private Image ghostImage;

        private Coroutine endDragCo;

        private void Awake()
        {
            if (saveDataProvider == null)
                saveDataProvider = FindFirstObjectByType<SaveDataProvider>();

            helmetSlot?.Bind(this);
            topSlot?.Bind(this);
            bottomSlot?.Bind(this);
            shoesSlot?.Bind(this);

            Open();
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
            EnsureSlots(data.inventoryCapacity);
            RefreshAll();
        }

        public void Close()
        {
            panelRoot?.SetActive(false);
        }

        private void EnsureSlots(int capacity)
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

        private void RefreshAll()
        {
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
        }

        // =========================
        // Saving helper
        // =========================
        private void SaveNowIfEnabled()
        {
            if (!saveAfterEachMove) return;

            // SaveDataProvider에는 SaveNow가 없고, 실제 저장은 GameManager가 담당
            if (GameManager.Instance != null)
                GameManager.Instance.SaveNow();
        }

        /// <summary>
        /// 데이터 변경이 실제로 일어난 "성공 케이스"에서만 호출할 것.
        /// </summary>
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

            StartDragging(DragSourceType.Inventory, index, EquipSlotType.None, itemDatabase.GetIconOrDefault(id), eventData);
            slots[index].SetHighlight(true);
        }

        public void BeginDragFromEquip(EquipSlotType slot, PointerEventData eventData)
        {
            if (data == null) return;

            string id = GetEquip(slot);
            if (string.IsNullOrEmpty(id)) return;

            StartDragging(DragSourceType.Equip, -1, slot, itemDatabase.GetIconOrDefault(id), eventData);
            GetEquipUI(slot)?.SetHighlight(true);
        }

        private void StartDragging(DragSourceType src, int inv, EquipSlotType equip, Sprite sprite, PointerEventData eventData)
        {
            if (endDragCo != null) StopCoroutine(endDragCo);

            dragging = true;
            dropConsumed = false;

            dragSource = src;
            fromInv = inv;
            fromEquip = equip;

            CreateGhost(sprite, eventData);
        }

        public void DragMove(PointerEventData eventData)
        {
            if (!dragging || ghostRt == null || dragLayer == null) return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragLayer, eventData.position, eventData.pressEventCamera, out var local))
            {
                ghostRt.anchoredPosition = local;
            }
        }

        // ✅ EndDrag는 즉시 Cleanup하지 않고 1프레임 늦게 정리(드롭 이벤트 보장)
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

            // Inventory -> Inventory : Swap
            if (dragSource == DragSourceType.Inventory)
            {
                if (fromInv != toIndex)
                {
                    SwapInv(fromInv, toIndex);
                    CommitChange(); // ✅ 저장 포함
                }
                CleanupDrag();
                return;
            }

            // Equip -> Inventory : unequip or swap(if possible)
            if (dragSource == DragSourceType.Equip)
            {
                string equipId = GetEquip(fromEquip);
                if (string.IsNullOrEmpty(equipId))
                {
                    CleanupDrag();
                    return;
                }

                string invId = GetInv(toIndex);

                // 빈 칸이면 그냥 해제
                if (string.IsNullOrEmpty(invId))
                {
                    SetInv(toIndex, equipId);
                    SetEquip(fromEquip, "");
                    CommitChange(); // ✅ 저장 포함
                    CleanupDrag();
                    return;
                }

                // 스왑: invId가 원래 equip 슬롯에 들어갈 수 있어야 함
                if (CanEquip(invId, fromEquip))
                {
                    SetInv(toIndex, equipId);
                    SetEquip(fromEquip, invId);
                    CommitChange(); // ✅ 저장 포함
                }

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

            // Inventory -> Equip : equip or swap with currently equipped
            if (dragSource == DragSourceType.Inventory)
            {
                string fromId = GetInv(fromInv);
                if (string.IsNullOrEmpty(fromId))
                {
                    CleanupDrag();
                    return;
                }

                if (!CanEquip(fromId, toSlot))
                {
                    CleanupDrag();
                    return;
                }

                string equippedId = GetEquip(toSlot);

                SetEquip(toSlot, fromId);
                SetInv(fromInv, equippedId ?? "");
                CommitChange(); // ✅ 저장 포함

                CleanupDrag();
                return;
            }

            // Equip -> Equip : move/swap (only if compatible)
            if (dragSource == DragSourceType.Equip)
            {
                if (fromEquip == toSlot)
                {
                    CleanupDrag();
                    return;
                }

                string fromId = GetEquip(fromEquip);
                if (string.IsNullOrEmpty(fromId))
                {
                    CleanupDrag();
                    return;
                }

                if (!CanEquip(fromId, toSlot))
                {
                    CleanupDrag();
                    return;
                }

                string toId = GetEquip(toSlot);

                // 대상 비었으면 이동
                if (string.IsNullOrEmpty(toId))
                {
                    SetEquip(toSlot, fromId);
                    SetEquip(fromEquip, "");
                    CommitChange(); // ✅ 저장 포함
                    CleanupDrag();
                    return;
                }

                // 서로 호환될 때만 스왑
                if (CanEquip(toId, fromEquip))
                {
                    SetEquip(toSlot, fromId);
                    SetEquip(fromEquip, toId);
                    CommitChange(); // ✅ 저장 포함
                }

                CleanupDrag();
                return;
            }

            CleanupDrag();
        }

        // =========================
        // Data helpers
        // =========================
        private string GetInv(int index)
        {
            if (index < 0 || index >= data.inventoryCapacity) return "";
            return data.inventoryItems[index] ?? "";
        }

        private void SetInv(int index, string id)
        {
            if (index < 0 || index >= data.inventoryCapacity) return;
            data.inventoryItems[index] = id ?? "";
        }

        private void SwapInv(int a, int b)
        {
            if (a < 0 || b < 0) return;
            if (a >= data.inventoryCapacity || b >= data.inventoryCapacity) return;
            (data.inventoryItems[a], data.inventoryItems[b]) = (data.inventoryItems[b], data.inventoryItems[a]);
        }

        private string GetEquip(EquipSlotType slot)
        {
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
            cg.blocksRaycasts = false; // 드롭 타겟이 레이캐스트 받게
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
            // 하이라이트 원복
            if (fromInv >= 0 && fromInv < slots.Count)
                slots[fromInv].SetHighlight(false);

            if (fromEquip != EquipSlotType.None)
                GetEquipUI(fromEquip)?.SetHighlight(false);

            dragging = false;
            dropConsumed = false;
            dragSource = DragSourceType.None;
            fromInv = -1;
            fromEquip = EquipSlotType.None;

            CleanupGhost();
        }
    }
}
