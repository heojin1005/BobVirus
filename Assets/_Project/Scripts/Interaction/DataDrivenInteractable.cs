using UnityEngine;

[RequireComponent(typeof(InteractionTarget))]
public class DataDrivenInteractable : MonoBehaviour, IInteractable
{
    private InteractionTarget target;

    private void Awake()
    {
        target = GetComponent<InteractionTarget>();
    }

    public void Interact()
    {
        if (InteractionController.Instance == null)
        {
            Debug.LogWarning("InteractionController not found");
            return;
        }
        InteractionController.Instance.TryInteract(target);
    }
}
