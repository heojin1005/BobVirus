using UnityEngine;
public class RemoteInteractionInvoker : MonoBehaviour
{
    [SerializeField] private InteractionTarget target;

    public void InvokeRemote()
    {
        if (target == null) return;
        if (InteractionController.Instance == null) return;

        InteractionController.Instance.TryInteract(target);
    }
}