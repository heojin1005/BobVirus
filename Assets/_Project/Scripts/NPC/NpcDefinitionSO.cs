// NpcDefinitionSO.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Game/NPC/Npc Definition")]
public class NpcDefinitionSO : ScriptableObject
{
    public string npcId;
    public string displayName;
    public Sprite portrait;

    [Header("Stage C Part 1: Simple Graph Links (no resolver yet)")]
    public DialogueGraphSO talkGraph;
    public DialogueGraphSO questGraph;
}