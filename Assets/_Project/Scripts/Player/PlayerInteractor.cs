using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float interactRange = 1.5f; // 상호작용 탐색 반경
    [SerializeField] private LayerMask interactLayer;    // NPC, Item 등

    public void OnInteract(InputValue value)
    {
        if (value.isPressed)
        {
            PerformInteraction();
        }
    }

    private void PerformInteraction()
    {
        Vector2 playerPos = transform.position;

        // [우선순위 1] 타일맵 '문' 근처 탐색
        if (DoorTileManager.Instance != null)
        {
            // 마우스 방향 무시! 플레이어 위치에서 interactRange 반경 내 가장 가까운 문을 찾음
            if (DoorTileManager.Instance.TryToggleNearbyDoor(playerPos, interactRange))
            {
                //Debug.Log("[PlayerInteractor] 근처 문 상호작용 성공!");
                return;
            }
        }

        // [우선순위 2] NPC 및 파밍 아이템 탐색
        Collider2D[] hits = Physics2D.OverlapCircleAll(playerPos, interactRange, interactLayer);

        Collider2D closestCollider = null;
        float minDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            if (hit.gameObject == this.gameObject) continue;

            float dist = Vector2.Distance(playerPos, hit.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestCollider = hit;
            }
        }

        if (closestCollider != null)
        {
            IInteractable interactable = closestCollider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact();
                return;
            }

            InteractionTarget target = closestCollider.GetComponentInParent<InteractionTarget>();
            if (target != null && InteractionController.Instance != null)
            {
                InteractionController.Instance.TryInteract(target);
                return;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}