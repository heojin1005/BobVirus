using UnityEngine;
using UnityEngine.InputSystem;

public class WorldTapInteractor : MonoBehaviour
{
    [SerializeField] private Camera cam;

    private void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    // ✅ Send Messages는 보통 InputValue를 넘겨줌
    public void OnTap(InputValue value)
    {
        // Button 액션이면 isPressed로 체크 가능
        if (!value.isPressed) return;
        if (cam == null) return;
        if (Pointer.current == null) return;

        Vector2 screenPos = Pointer.current.position.ReadValue();
        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));
        Vector2 wp2 = new Vector2(worldPos.x, worldPos.y);

        Collider2D hit = Physics2D.OverlapPoint(wp2);
        if (hit == null) return;

        var interactable = hit.GetComponentInParent<IInteractable>();
        interactable?.Interact();
    }
}
