using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic; // [추가] List를 사용하기 위해 필요

public class WorldTapInteractor : MonoBehaviour
{
    [SerializeField] private Camera cam;

    [Header("Safety")]
    [SerializeField] private bool ignoreWhenPointerOverUI = true;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    public void OnTap(InputValue value)
    {
        if (!value.isPressed) return;
        if (cam == null) return;
        if (Mouse.current == null) return; 

        // [수정] 경고가 뜨는 IsPointerOverGameObject 대신, 안전한 커스텀 UI 레이캐스트 사용
        if (ignoreWhenPointerOverUI && IsPointerOverUI())
        {
            return; // 마우스가 UI 위에 있으므로 월드 클릭 무시
        }

        Vector2 screenPos = Mouse.current.position.ReadValue();

        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        Vector2 wp2 = new Vector2(worldPos.x, worldPos.y);

        Collider2D hit = Physics2D.OverlapPoint(wp2);
        if (hit == null) return; 

        var interactable = hit.GetComponentInParent<IInteractable>();
        if (interactable != null)
        {
            interactable.Interact();
        }
    }

    // [핵심 추가] Input System 콜백 내에서도 안전하게 UI 클릭 여부를 판별하는 함수
    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        // 가상의 마우스 포인터 이벤트를 생성
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = Mouse.current.position.ReadValue();

        // 마우스 위치에 있는 모든 UI 그래픽 요소들을 찔러서 결과를 리스트에 담음
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

        // 결과가 1개라도 있다면 UI를 클릭한 것
        return results.Count > 0;
    }
}