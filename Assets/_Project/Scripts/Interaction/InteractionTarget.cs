using UnityEngine;

public enum InteractionKind
{
    Npc,
    Encyclopedia,
    DeployTerminal,
    Chest,
    Generic,
    Tutorial
}

public enum NpcDirectAction
{
    None,
    Talk,
    Trade,
    Quest
}

public class InteractionTarget : MonoBehaviour
{
    public InteractionKind kind = InteractionKind.Generic;
    public string targetId = "id_undefined";

    [Header("NPC Only")]
    public NpcDirectAction npcDirectAction = NpcDirectAction.None;
}