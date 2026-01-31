using UnityEngine;
using UnityEngine.InputSystem;

public class WorldTapInteractor : MonoBehaviour
{
    [SerializeField] private Camera cam;

    // ✅ Hub/Point 액션을 인스펙터에서 연결할 것
    [SerializeField] private InputActionReference pointAction;

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

        // ✅ Point 액션에서 스크린 좌표를 읽는다 (구식 Pointer.current 제거)
        Vector2 screenPos = pointAction.action.ReadValue<Vector2>();

        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        Vector2 wp2 = new Vector2(worldPos.x, worldPos.y);

        Collider2D hit = Physics2D.OverlapPoint(wp2);
        if (hit == null) { Debug.Log("HIT: null"); return; }

        Debug.Log($"HIT: {hit.name} / root={hit.transform.root.name}");

        var interactable = hit.GetComponentInParent<IInteractable>();
        Debug.Log($"IInteractable: {(interactable == null ? "null" : interactable.GetType().Name)}");

        interactable?.Interact();

    }
}
