using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Project/Storage/Container Definition", fileName = "ContainerDefinition_")]
public class StorageContainerDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("세이브에 저장될 때 사용하는 기본 키. (허브 고정 창고면 이 값을 그대로 쓰는 걸 추천)")]
    public string containerId = "hub_storage_01";

    [Header("Initial Layout (Template)")]
    [Min(1)]
    public int initialCapacity = 20;

    [Tooltip("초기 아이템. 길이가 capacity보다 짧으면 나머지는 빈칸으로 채웁니다. 빈칸은 \"\" 로 처리됩니다.")]
    public List<string> initialItems = new();

    /// <summary>
    /// 템플릿 기반으로 '초기 아이템 리스트(정규화 완료)'를 만들어줌.
    /// </summary>
    public List<string> BuildNormalizedInitialItems()
    {
        var list = initialItems != null ? new List<string>(initialItems) : new List<string>();

        // 빈칸 규칙 통일
        for (int i = 0; i < list.Count; i++)
            if (list[i] == null) list[i] = "";

        while (list.Count < initialCapacity)
            list.Add("");

        if (list.Count > initialCapacity)
            list.RemoveRange(initialCapacity, list.Count - initialCapacity);

        return list;
    }
}