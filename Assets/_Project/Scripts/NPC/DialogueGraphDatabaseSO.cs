// DialogueGraphDatabaseSO.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Dialogue/Dialogue Graph Database")]
public class DialogueGraphDatabaseSO : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public string graphId;          // 저장/오버라이드에 쓰는 키
        public DialogueGraphSO graph;   // 실제 에셋
    }

    public List<Entry> graphs = new List<Entry>();

    private Dictionary<string, DialogueGraphSO> map;

    private void OnEnable() => Build();

    public void Build()
    {
        map = new Dictionary<string, DialogueGraphSO>();
        foreach (var e in graphs)
        {
            if (e == null) continue;
            if (string.IsNullOrEmpty(e.graphId)) continue;
            if (e.graph == null) continue;
            if (map.ContainsKey(e.graphId)) continue;
            map.Add(e.graphId, e.graph);
        }
    }

    public DialogueGraphSO Get(string graphId)
    {
        if (string.IsNullOrEmpty(graphId)) return null;
        if (map == null || map.Count == 0) Build();
        map.TryGetValue(graphId, out var g);
        return g;
    }
}