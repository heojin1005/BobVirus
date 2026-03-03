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
        [SerializeField] private Text countText; // 수량 표시(선택)
        [SerializeField] private GameObject highlight;

        [Header("Long Press Drag")]
        [Tooltip("TimeScale=0 환경이므로 Realtime 기준")]
        [SerializeField] private float longPressSeconds = 0.18f;
        [SerializeField] private float splitPressSeconds = 1.0f;
        private bool splitOpened;
        private Vector2 lastPointerPos;

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

        public void SetCount(int count)
        {
            if (countText == null) return;

            if (count <= 1)
            {
                countText.gameObject.SetActive(false);
                countText.text = "";
                return;
            }

            countText.gameObject.SetActive(true);
            countText.text = count.ToString();
        }
        public void SetHighlight(bool on)
        {
            if (highlight != null) highlight.SetActive(on);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (owner != null && owner.IsHoldingPayloadDrag)
            {
                owner.DropToStorage(Index);
                eventData.Use();
                return;
            }
            pointerDown = true;
            forwardingToScroll = false;
            longPressReady = false;
            pointerDownPos = eventData.position;
            splitOpened = false;
            lastPointerPos = eventData.position;
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
            // 1) 짧은 롱프레스(기존): 드래그 가능 상태 준비
            yield return new WaitForSecondsRealtime(longPressSeconds);

            if (!pointerDown) { longPressCo = null; yield break; }

            longPressReady = true;

            // 2) 긴 롱프레스(추가): Split UI 자동 오픈 (움직이지 않아도 뜸)
            float extra = splitPressSeconds - longPressSeconds;
            if (extra > 0f)
                yield return new WaitForSecondsRealtime(extra);

            if (!pointerDown) { longPressCo = null; yield break; }

            // 스크롤 의도(많이 이동)였으면 Split 열지 않음
            // (pointerDownPos 기준으로 충분히 이동했다면 취소)
            // ※ moveCancelThreshold는 기존과 동일하게 사용
            if (Vector2.Distance(pointerDownPos, lastPointerPos) >= moveCancelThreshold)
            {
                longPressCo = null;
                yield break;
            }

            // 아이템이 있는 슬롯에서만 Split
            if (iconImage == null || !iconImage.enabled || iconImage.sprite == null)
            {
                longPressCo = null;
                yield break;
            }

            // ✅ Split UI 오픈
            splitOpened = true;
            pointerDown = false;   // 이후 드래그/스크롤 트리거 방지
            longPressReady = false;

            owner?.TryOpenSplitFromStorage(Index);

            longPressCo = null;
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
            if (splitOpened)
            {
                ForwardBeginDragToScroll(eventData);
                return;
            }
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
            lastPointerPos = eventData.position; // ✅ 추가
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