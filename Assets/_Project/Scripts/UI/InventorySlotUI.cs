using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    public class InventorySlotUI : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler,
        IDropHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject highlight; // 선택(없어도 됨)

        public int Index { get; private set; }
        private InventoryUIController owner;

        public void Bind(int index, InventoryUIController owner)
        {
            Index = index;
            this.owner = owner;
        }

        public void SetIcon(Sprite sprite)
        {
            if (iconImage == null) return;

            if (sprite == null)
            {
                iconImage.enabled = false;
                iconImage.sprite = null;
            }
            else
            {
                iconImage.enabled = true;
                iconImage.sprite = sprite;
            }
        }

        public void SetHighlight(bool on)
        {
            if (highlight != null) highlight.SetActive(on);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            owner?.BeginDragFromInventory(Index, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            owner?.DragMove(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            owner?.EndDrag(eventData); // ✅ 지연 정리 버전
        }

        public void OnDrop(PointerEventData eventData)
        {
            owner?.DropToInventory(Index);
        }
    }
}
