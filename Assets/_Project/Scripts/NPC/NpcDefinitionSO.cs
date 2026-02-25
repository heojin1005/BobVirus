// NpcDefinitionSO.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NpcStoreEntry
{
    public string itemId;   // ItemDatabase에서 찾는 ID
    public int price;       // 나중에 구매 로직 붙일 때 사용 (일단 UI 표시용)
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