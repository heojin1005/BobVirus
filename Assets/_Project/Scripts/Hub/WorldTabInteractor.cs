using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class WorldTapInteractor : MonoBehaviour
{
    [SerializeField] private Camera cam;

    // ✅ Hub/Point 액션을 인스펙터에서 연결할 것
    [SerializeField] private InputActionReference pointAction;

    // UI Raycast 재사용 버퍼(가비지 줄이기)
    private static readonly List<RaycastResult> uiHits = new();

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    private void OnEnable()
    {
        // PlayerInput이 Enable을 해주더라도, 안전하게 보장
        if (pointAction != null) pointAction.action.Enable();
    }

    private void OnDisable()
    {
        // 다른 시스템에서 공유한다면 Disable 하지 않는 편이 더 안전할 수도 있음.
        // 지금은 단독 사용 가정으로 비활성화해도 OK.
        if (pointAction != null) pointAction.action.Disable();
    }

    // ✅ Send Messages: Tap 액션이 발생하면 호출됨
    public void OnTap(InputValue value)
    {
        if (!value.isPressed) return;
        if (cam == null) return;
        if (pointAction == null) return;

        // ✅ UI(예: NPC 패널/인벤 등)가 열려 있으면 월드 탭 차단
        // PauseService가 프로젝트에 없거나 씬에 없으면 null일 수 있으니 방어
        if (PauseService.Instance != null && PauseService.Instance.IsPaused)
            return;

        // ✅ Point 액션에서 스크린 좌표를 읽는다
        Vector2 screenPos = pointAction.action.ReadValue<Vector2>();

        // ✅ UI 위를 눌렀다면 월드 탭 무시 (IsPointerOverGameObject 경고/오작동 방지)
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
