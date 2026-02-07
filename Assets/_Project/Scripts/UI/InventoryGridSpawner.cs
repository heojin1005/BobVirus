using System.Collections.Generic;
using UnityEngine;

public class InventoryGridSpawner : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform inventoryGridRoot;
    [SerializeField] private GameObject slotPrefab;

    [Header("Test")]
    [SerializeField] private int capacity = 20;

    private readonly List<GameObject> spawned = new();

    private void Start()
    {
        Build();
    }

    [ContextMenu("Build")]
    public void Build()
    {
        // 기존 생성물 정리
        for (int i = spawned.Count - 1; i >= 0; i--)
        {
            if (spawned[i] != null) Destroy(spawned[i]);
        }
        spawned.Clear();

        // capacity만큼 생성
        for (int i = 0; i < capacity; i++)
        {
            var go = Instantiate(slotPrefab, inventoryGridRoot);
            go.name = $"Slot_{i}";
            spawned.Add(go);
        }
    }
}
