using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    public class InventorySlotUI : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler,
        IInitializePotentialDragHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler,
        IDropHandler
    {
        [Header("UI")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI countText; // 수량 표시(선택)
        [SerializeField] private GameObject highlight; // 선택(없어도 됨)

        [Header("Long Press Drag")]
        [Tooltip("이 시간(초) 이상 누르고 있으면 아이템 드래그가 시작됩니다. (TimeScale=0 환경이므로 Realtime 기준)")]
        [SerializeField] private float longPressSeconds = 0.18f;
        [Tooltip("이 시간(초) 이상 누르고 있으면 '분해(분할) UI'를 엽니다. (Realtime 기준)")]
        [SerializeField] private float splitPressSeconds = 1.0f;

        [Tooltip("롱프레스 전에 이 픽셀 이상 움직이면 스크롤로 간주(드래그 안 함)")]
        [SerializeField] private float moveCancelThreshold = 10f;

        public int Index { get; private set; }
        private InventoryUIController owner;

        private ScrollRect parentScrollRect;

        // pointer state
        private bool pointerDown;
        private Vector2 pointerDownPos;
        private Vector2 lastPointerPos;

        // long press state
        private Coroutine longPressCo;
        private bool longPressReady;
        private bool splitOpened;

        // forwarding state (scroll)
        private bool forwardingToScroll;

        public void Bind(int index, InventoryUIController owner)
        {
            Index = index;
            this.owner = owner;

            if (parentScrollRect == null)
                parentScrollRect = GetComponentInParent<ScrollRect>();
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

            // 0~1은 표시 안 함
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

        // =========================
        // Pointer / Long press
        // =========================
        public void OnPointerDown(PointerEventData eventData)
        {
            // ✅ Split-confirm 드래그 상태면: 탭(다운) = 드롭 시도
            if (owner != null && owner.IsHoldingPayloadDrag)
            {
                owner.DropToInventory(Index);
                eventData.Use();
                return;
            }
            //if (owner != null && !owner.IsSlotInputReady)
            //{
            //    eventData.Use();
            //    return;
            //}
            pointerDown = true;
            forwardingToScroll = false;
            longPressReady = false;
            splitOpened = false;   // ✅ 추가
            pointerDownPos = eventData.position;
            lastPointerPos = eventData.position; // ✅ 추가

            // 아이템이 없는 슬롯이면 롱프레스 드래그를 시작하지 않음
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
            // 손가락/마우스가 슬롯 밖으로 나가면 롱프레스 취소
            //pointerDown = false;
            //StopLongPress();
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
            owner?.TryOpenSplitFromInventory(Index);

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

        // =========================
        // Drag gate + Scroll forwarding
        // =========================
        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            // ScrollRect가 "드래그 가능"하다고 인지하도록 설정 (모바일에서 특히 도움됨)
            eventData.useDragThreshold = true;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // ✅ 이미 split UI를 열었으면, 이 드래그는 무시 (스크롤로 넘기는 게 안전)
            if (splitOpened)
            {
                ForwardBeginDragToScroll(eventData);
                return;
            }
            // 롱프레스 전에 일정 이상 움직였으면 스크롤로 넘김
            if (!longPressReady)
            {
                // 이동량이 매우 작으면(눌렀다가 살짝 흔들린 수준) 아직 판단하지 않고, 스크롤로 넘기지 않음
                // 하지만 Unity는 BeginDrag를 호출했으니, 여기서는 스크롤로 넘기는 게 UX가 더 안정적임.
                ForwardBeginDragToScroll(eventData);
                return;
            }

            // 롱프레스 성공했으면 아이템 드래그 시작
            forwardingToScroll = false;
            owner?.BeginDragFromInventory(Index, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            lastPointerPos = eventData.position; // ✅ 추가
            // 롱프레스 중에도 사용자가 많이 움직이면(스크롤 의도) 롱프레스 취소
            if (!longPressReady && pointerDown)
            {
                if (Vector2.Distance(pointerDownPos, eventData.position) >= moveCancelThreshold)
                {
                    StopLongPress();
                }
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

            owner?.EndDrag(eventData); // ✅ 지연 정리 버전
        }

        public void OnDrop(PointerEventData eventData)
        {
            owner?.DropToInventory(Index);
        }

        // =========================
        // ScrollRect forwarding helpers
        // =========================
        private void ForwardBeginDragToScroll(PointerEventData eventData)
        {
            StopLongPress();
            longPressReady = false;

            if (parentScrollRect == null || !parentScrollRect.enabled)
            {
                // 스크롤이 없으면 그냥 아무것도 안 함(드래그도 안 함)
                forwardingToScroll = false;
                return;
            }

            forwardingToScroll = true;

            // ScrollRect가 받도록 pointerDrag를 바꿔주고 이벤트 직접 전달
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
