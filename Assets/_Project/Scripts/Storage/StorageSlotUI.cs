using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    public class StorageSlotUI : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler,
        IInitializePotentialDragHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler,
        IDropHandler
    {
        [Header("UI")]
        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject highlight;

        [Header("Long Press Drag")]
        [Tooltip("TimeScale=0 환경이므로 Realtime 기준")]
        [SerializeField] private float longPressSeconds = 0.18f;

        [Tooltip("롱프레스 전에 이 픽셀 이상 움직이면 스크롤로 간주")]
        [SerializeField] private float moveCancelThreshold = 10f;

        public int Index { get; private set; }
        private InventoryUIController owner;

        private ScrollRect parentScrollRect;

        private bool pointerDown;
        private Vector2 pointerDownPos;

        private Coroutine longPressCo;
        private bool longPressReady;

        private bool forwardingToScroll;

private void Awake()
{
    parentScrollRect = GetComponentInParent<ScrollRect>(true);
}
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

        public void OnPointerDown(PointerEventData eventData)
        {
            pointerDown = true;
            forwardingToScroll = false;
            longPressReady = false;
            pointerDownPos = eventData.position;

            if (iconImage == null || !iconImage.enabled || iconImage.sprite == null)
                return;

            StopLongPress();
            longPressCo = StartCoroutine(LongPressRoutine());
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pointerDown = false;
            StopLongPress();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerDown = false;
            StopLongPress();
        }

        private IEnumerator LongPressRoutine()
        {
            yield return new WaitForSecondsRealtime(longPressSeconds);
            if (!pointerDown) yield break;
            longPressReady = true;
        }

        private void StopLongPress()
        {
            if (longPressCo != null)
            {
                StopCoroutine(longPressCo);
                longPressCo = null;
            }
            longPressReady = false;
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = true;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!longPressReady)
            {
                ForwardBeginDragToScroll(eventData);
                return;
            }

            forwardingToScroll = false;
            owner?.BeginDragFromStorage(Index, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!longPressReady && pointerDown)
            {
                if (Vector2.Distance(pointerDownPos, eventData.position) >= moveCancelThreshold)
                    StopLongPress();
            }

            if (forwardingToScroll)
            {
                ForwardDragToScroll(eventData);
                return;
            }

            owner?.DragMove(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            StopLongPress();

            if (forwardingToScroll)
            {
                ForwardEndDragToScroll(eventData);
                forwardingToScroll = false;
                return;
            }

            owner?.EndDrag(eventData);
        }

        public void OnDrop(PointerEventData eventData)
        {
            owner?.DropToStorage(Index);
        }

        private void ForwardBeginDragToScroll(PointerEventData eventData)
        {
            StopLongPress();
            longPressReady = false;

            if (parentScrollRect == null || !parentScrollRect.enabled)
            {
                forwardingToScroll = false;
                return;
            }

            forwardingToScroll = true;
            eventData.pointerDrag = parentScrollRect.gameObject;
            ExecuteEvents.Execute(parentScrollRect.gameObject, eventData, ExecuteEvents.beginDragHandler);
        }

        private void ForwardDragToScroll(PointerEventData eventData)
        {
            if (parentScrollRect == null) return;
            ExecuteEvents.Execute(parentScrollRect.gameObject, eventData, ExecuteEvents.dragHandler);
        }

        private void ForwardEndDragToScroll(PointerEventData eventData)
        {
            if (parentScrollRect == null) return;
            ExecuteEvents.Execute(parentScrollRect.gameObject, eventData, ExecuteEvents.endDragHandler);
        }
    }
}