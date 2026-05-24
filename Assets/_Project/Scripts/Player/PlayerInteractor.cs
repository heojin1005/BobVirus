using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float interactRange = 1.5f;
    [SerializeField] private LayerMask interactLayer;

    public void OnInteract(InputValue value)
    {
        if (SettingsOverlayController.BlocksInput)
        return; //입력 막기
        if (!value.isPressed)
            return;
        PerformInteraction();
    }

    private void PerformInteraction()
    {
        Vector2 playerPos = transform.position;

        if (DoorTileManager.Instance != null)
        {
            if (DoorTileManager.Instance.TryToggleNearbyDoor(playerPos, interactRange))
                return;
        }

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