using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class WorldTapInteractor : MonoBehaviour
{
    [SerializeField] private Camera cam;

    [Header("Safety")]
    [SerializeField] private bool ignoreWhenPointerOverUI = true;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    // [핵심 변경] OnTap(InputValue value) 콜백 함수를 삭제하고,
    // Update문에서 마우스의 좌클릭을 물리적으로 직접 감지합니다.
    private void Update()
    {
        if (Mouse.current == null) return;

        // 마우스 왼쪽 버튼이 '이번 프레임에 눌렸을 때'만 실행 (가만히 있어도 100% 작동)
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleWorldClick();
        }
    }

    private void HandleWorldClick()
    {
        if (cam == null) return;

        // UI 위를 클릭했다면 월드 상호작용 무시
        if (ignoreWhenPointerOverUI && IsPointerOverUI())
        {
            return; 
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

    // UI 레이캐스트 안전망
    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = Mouse.current.position.ReadValue();

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

        return results.Count > 0;
    }
}