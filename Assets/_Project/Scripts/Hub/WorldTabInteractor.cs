using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class WorldTapInteractor : MonoBehaviour
{
    [SerializeField] private Camera cam;

    // ✅ Hub/Point 액션을 인스펙터에서 연결할 것
    [SerializeField] private InputActionReference pointAction;

    [Header("Safety")]
    [SerializeField] private bool ignoreWhenPointerOverUI = true;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    private void OnEnable()
    {
        if (pointAction != null) pointAction.action.Enable();
    }

    private void OnDisable()
    {
        if (pointAction != null) pointAction.action.Disable();
    }

    // ✅ Send Messages: Tap 액션이 발생하면 호출됨
    public void OnTap(InputValue value)
    {
        if (!value.isPressed) return;
        if (cam == null) return;
        if (pointAction == null) return;

        // ✅ UI 위 클릭/터치면 월드 상호작용 무시(안전망)
        if (ignoreWhenPointerOverUI && EventSystem.current != null)
        {
            // 파라미터 없는 버전은 마우스/에디터에서 확실하게 먹음.
            if (EventSystem.current.IsPointerOverGameObject())
                return;
        }

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
