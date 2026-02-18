using UnityEngine;

public enum InteractionKind
{
    Npc,
    Encyclopedia,
    DeployTerminal,
    Chest,
    Generic
}

public class InteractionTarget : MonoBehaviour
{
    public InteractionKind kind = InteractionKind.Generic;
    public string targetId = "id_undefined";
}
