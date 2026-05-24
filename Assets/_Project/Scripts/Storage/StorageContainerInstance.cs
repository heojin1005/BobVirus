using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class StorageContainerInstance : MonoBehaviour
{
    [Header("Template (ItemSet DB)")]
    [SerializeField] private StorageContainerDefinitionSO definition;

    [Header("Persistence")]
    [Tooltip("ON이면 SaveGameData.containers에 저장/누적됨 (허브 저장창고/영구 상자)")]
    [SerializeField] private bool persistToSave = true;

    [Tooltip("Save에 저장될 키. 허브 고정 창고면 고정 키 사용 추천. (비워두면 자동 생성 가능)")]
    [SerializeField] private string containerKey = "";

    [Tooltip("persistToSave가 ON이고 containerKey가 비어있을 때, 씬 인스턴스에서만 자동으로 키를 생성합니다.")]
    [SerializeField] private bool autoGenerateKeyInEditor = true;

    // 런타임(비저장) 컨테이너 데이터
    private SaveGameData.ContainerSaveData runtimeData;

    public StorageContainerDefinitionSO Definition => definition;
    public bool PersistToSave => persistToSave;
    public string ContainerKey => containerKey;

    private void OnValidate()
    {
        if (Application.isPlaying) return;
        if (!autoGenerateKeyInEditor) return;
        if (!persistToSave) return;

#if UNITY_EDITOR
        // ✅ 프리팹 "에셋" 편집 중이면 자동 생성 금지 (키가 프리팹에 박히는 걸 방지)
        if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
            return;

        // ✅ 씬에 배치된 인스턴스(또는 프리팹 인스턴스)에서만 키 생성
        if (string.IsNullOrWhiteSpace(containerKey))
        {
            containerKey = Guid.NewGuid().ToString("N");
            EditorUtility.SetDirty(this);
        }
#else
        if (string.IsNullOrWhiteSpace(containerKey))
            containerKey = Guid.NewGuid().ToString("N");
#endif
    }

    public SaveGameData.ContainerSaveData GetOrCreateContainerData(SaveGameData saveData)
    {
        if (definition == null)
        {
            Debug.LogError($"[StorageContainerInstance] Definition is null on {name}");
            return null;
        }

        // 1) 저장형
        if (persistToSave)
        {
            if (saveData == null)
            {
                Debug.LogError("[StorageContainerInstance] SaveGameData is null (persistToSave=true).");
                return null;
            }

            if (string.IsNullOrWhiteSpace(containerKey))
                containerKey = Guid.NewGuid().ToString("N");

            List<string> initial = definition.BuildNormalizedInitialItems();
            return saveData.EnsureContainer(containerKey, definition.initialCapacity, initial);
        }

        // 2) 비저장형(세션 한정)
        if (runtimeData == null)
        {
            runtimeData = new SaveGameData.ContainerSaveData
            {
                containerKey = $"runtime_{GetInstanceID()}",
                capacity = Mathf.Max(1, definition.initialCapacity),
                slots = BuildInitialSlotsFromTemplate(definition.BuildNormalizedInitialItems())
            };
            runtimeData.Normalize();
        }
        else
        {
            runtimeData.Normalize();
        }

        return runtimeData;
    }

    private static List<SaveGameData.ItemSlotData> BuildInitialSlotsFromTemplate(List<string> template)
    {
        var slots = new List<SaveGameData.ItemSlotData>();
        if (template == null) return slots;

        for (int i = 0; i < template.Count; i++)
        {
            var id = template[i] ?? "";
            slots.Add(string.IsNullOrEmpty(id)
                ? new SaveGameData.ItemSlotData("", 0)
                : new SaveGameData.ItemSlotData(id, 1));
        }
        return slots;
    }

    [ContextMenu("Generate Container Key")]
    private void GenerateKey()
    {
        containerKey = Guid.NewGuid().ToString("N");
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    [ContextMenu("Clear Container Key")]
    private void ClearKey()
    {
        containerKey = "";
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }
}