using UnityEngine;

public class NpcInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string npcId = "NPC_test";

    public void Interact()
    {
        Debug.Log($"[Hub] NPC Interact: {npcId}");
        //UIOverlayManager.Instance.OpenNpcPanel(npcId);
    }
}
