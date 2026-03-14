using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NpcStoreEntry
{
    public string takeItemId;
    public int takeCount = 1;

    public string giveItemId;
    public int giveCount = 1;

    public string buttonLabel = "교환";
}

[CreateAssetMenu(menuName = "Game/NPC/Npc Definition")]
public class NpcDefinitionSO : ScriptableObject
{
    public string npcId;
    public string displayName;
    public Sprite portrait;

    [Header("Dialogue (Default)")]
    public DialogueGraphSO talkGraph;
    public DialogueGraphSO questGraph;

    [Header("Store (Default)")]
    public List<NpcStoreEntry> storeList = new List<NpcStoreEntry>();
}