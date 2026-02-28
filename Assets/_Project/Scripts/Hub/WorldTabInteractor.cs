using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class WorldTapInteractor : MonoBehaviour
{
    [SerializeField] private Camera cam;

    // (더 이상 Point 액션을 통해 좌표를 읽지 않음)
    // 남겨두고 싶으면 SerializeField 유지해도 되지만, 실제로는 사용하지 않음.
    // [SerializeField] private InputActionReference pointAction;

    // UI Raycast 재사용 버퍼(가비지 줄이기)
    private static readonly List<RaycastResult> uiHits = new();

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    // ✅ Send Messages: Tap 액션이 발생하면 호출됨
    public void OnTap(InputValue value)
    {
        // Tap 액션이 Press 계열이면 isPressed로 걸러도 되지만,
        // 프로젝트마다 설정이 달라 간헐 이슈를 만들 수 있어서 "안전하게" 처리.
        // (원하면 다시 if (!value.isPressed) return; 넣어도 됨)
        if (cam == null) return;

        // ✅ UI(예: NPC 패널/인벤 등)가 열려 있으면 월드 탭 차단
        if (PauseService.Instance != null && PauseService.Instance.IsPaused)
            return;

        // ✅ 현재 포인터(마우스/터치)의 스크린 좌표를 직접 읽는다
        var pointer = Pointer.current;
        if (pointer == null)
        {
            Debug.LogWarning("Pointer.current is null (no active pointer device).");
            return;
        }

        Vector2 screenPos = pointer.position.ReadValue();

        // ✅ UI 위를 눌렀다면 월드 탭 무시
        if (IsPointerOverUI(screenPos))
            return;

        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        Vector2 wp2 = new Vector2(worldPos.x, worldPos.y);

        Collider2D hit = Physics2D.OverlapPoint(wp2);
        if (hit == null)
        {
            Debug.Log("HIT: null");
            return;
        }

        Debug.Log($"HIT: {hit.name} / root={hit.transform.root.name}");

        var interactable = hit.GetComponentInParent<IInteractable>();
        Debug.Log($"IInteractable: {(interactable == null ? "null" : interactable.GetType().Name)}");

        interactable?.Interact();
    }

    private bool IsPointerOverUI(Vector2 screenPos)
    {
        // EventSystem이 없으면 UI 판정 불가 → 월드 탭 허용
        if (EventSystem.current == null) return false;

        var ped = new PointerEventData(EventSystem.current)
        {
            position = screenPos
        };

        uiHits.Clear();
        EventSystem.current.RaycastAll(ped, uiHits);
        return uiHits.Count > 0;
    }
}