using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Items/Item Database", fileName = "ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    [Header("Definitions")]
    [Tooltip("모든 ItemDefinition을 여기에 등록")]
    public List<ItemDefinition> items = new();

    [Header("Fallbacks (optional)")]
    [Tooltip("id가 비어있거나 조회 실패시 사용할 기본 아이콘")]
    public Sprite defaultItemIcon;

    private Dictionary<string, ItemDefinition> _map;

    /// <summary>
    /// 런타임 조회를 위한 캐시 구축
    /// </summary>
    public void BuildCacheIfNeeded()
    {
        if (_map != null) return;

        _map = new Dictionary<string, ItemDefinition>();

        foreach (var def in items)
        {
            if (def == null) continue;
            if (string.IsNullOrEmpty(def.id)) continue;

            // 중복 ID는 마지막 등록된 것으로 덮어쓰기
            _map[def.id] = def;
        }
    }

    public bool TryGet(string id, out ItemDefinition def)
    {
        def = null;
        if (string.IsNullOrEmpty(id)) return false;

        BuildCacheIfNeeded();
        return _map.TryGetValue(id, out def);
    }

    public ItemDefinition GetOrNull(string id)
    {
        return TryGet(id, out var def) ? def : null;
    }

    public Sprite GetIconOrDefault(string id)
    {
        if (TryGet(id, out var def) && def.icon != null)
            return def.icon;

        return defaultItemIcon;
    }

    public EquipSlotType GetEquipSlotOrNone(string id)
    {
        if (TryGet(id, out var def))
            return def.equipSlot;

        return EquipSlotType.None;
    }
    public int GetMaxStackOrDefault(string id, int fallback = 1)
    {
        if (fallback < 1) fallback = 1;

        if (TryGet(id, out var def))
            return Mathf.Max(1, def.maxStack);

        return fallback;
    }

    public bool IsStackable(string id)
    {
        return GetMaxStackOrDefault(id, 1) > 1;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 에디터에서 리스트 변경 시 캐시 갱신
        _map = null;
    }
#endif
}
