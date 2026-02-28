// NpcDatabaseSO.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/NPC/Npc Database")]
public class NpcDatabaseSO : ScriptableObject
{
    public List<NpcDefinitionSO> npcs = new List<NpcDefinitionSO>();

    private Dictionary<string, NpcDefinitionSO> map;

    private void OnEnable()
    {
        BuildMap();
    }

    public void BuildMap()
    {
        map = new Dictionary<string, NpcDefinitionSO>();
        foreach (var npc in npcs)
        {
            if (npc == null) continue;
            if (string.IsNullOrEmpty(npc.npcId)) continue;
            if (map.ContainsKey(npc.npcId)) continue;
            map.Add(npc.npcId, npc);
        }
    }

    public NpcDefinitionSO Get(string npcId)
    {
        if (string.IsNullOrEmpty(npcId)) return null;
        if (map == null || map.Count == 0) BuildMap();
        map.TryGetValue(npcId, out var npc);
        return npc;
    }
}