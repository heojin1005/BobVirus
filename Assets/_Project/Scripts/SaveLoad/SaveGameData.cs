using System;
using System.Collections.Generic;

[Serializable]
public class SaveGameData
{
    public int version = 1;

    public string saveId;
    public long savedAtUnix;
    public string displayName;

    public List<string> clearedMaps = new();
    public string currentMapId = "";
    public HashSet<string> discoveredItems = new();
    public List<string> inventoryItems = new();
    public int inventoryCapacity = 20;

    public string helmetId = "";
    public string topId = "";
    public string bottomId = "";
    public string shoesId = "";
    public string weaponId = "";

    public HashSet<string> rescuedNpcs = new();
    public int storyProgress = 1;
    public int test = 0;

    // =========================
    // ✅ NPC Override Data
    // =========================
    [Serializable]
    public class NpcStoreEntryData
    {
        public string itemId;
        public int price;
    }

    [Serializable]
    public class NpcOverrideData
    {
        // null/empty면 "오버라이드 안 함(기본 사용)"
        public string talkGraphId;
        public string questGraphId;

        // null이면 "오버라이드 안 함(기본 사용)"
        public List<NpcStoreEntryData> storeList;
    }

    // npcId -> override
    public Dictionary<string, NpcOverrideData> npcOverrides = new();

    public NpcOverrideData GetNpcOverride(string npcId)
    {
        if (string.IsNullOrEmpty(npcId)) return null;
        if (npcOverrides == null) npcOverrides = new Dictionary<string, NpcOverrideData>();
        npcOverrides.TryGetValue(npcId, out var o);
        return o;
    }

    public NpcOverrideData EnsureNpcOverride(string npcId)
    {
        if (string.IsNullOrEmpty(npcId)) return null;
        if (npcOverrides == null) npcOverrides = new Dictionary<string, NpcOverrideData>();

        if (!npcOverrides.TryGetValue(npcId, out var o) || o == null)
        {
            o = new NpcOverrideData();
            npcOverrides[npcId] = o;
        }
        return o;
    }

    public void ClearNpcOverride(string npcId)
    {
        if (npcOverrides == null) return;
        if (string.IsNullOrEmpty(npcId)) return;
        npcOverrides.Remove(npcId);
    }

    // =========================
    // ✅ Storage / Chest Data (NEW)
    // =========================
    [Serializable]
    public class ContainerSaveData
    {
        public string containerKey;          // Dictionary key와 동일하게 넣어두면 디버깅에 편함
        public int capacity = 20;
        public List<string> items = new();

        public void Normalize()
        {
            if (items == null) items = new List<string>();

            for (int i = 0; i < items.Count; i++)
                if (items[i] == null) items[i] = "";

            while (items.Count < capacity)
                items.Add("");

            if (items.Count > capacity)
                items.RemoveRange(capacity, items.Count - capacity);
        }
    }

    /// <summary>
    /// containerKey -> 저장된 창고 데이터
    /// - 허브 저장용 창고: 여기(Dictionary)에 누적 저장
    /// - 맵 파밍 상자도 "persistToSave = true"면 여기로 저장 가능
    /// </summary>
    public Dictionary<string, ContainerSaveData> containers = new();

    /// <summary>
    /// 저장된 컨테이너 가져오기(없으면 null)
    /// </summary>
    public ContainerSaveData GetContainer(string containerKey)
    {
        if (string.IsNullOrEmpty(containerKey)) return null;
        if (containers == null) containers = new Dictionary<string, ContainerSaveData>();
        containers.TryGetValue(containerKey, out var c);
        return c;
    }

    /// <summary>
    /// 컨테이너가 없으면 생성해서 반환.
    /// '템플릿(초기 capacity/items)'을 넣어주면 최초 생성 때만 적용됨.
    /// </summary>
    public ContainerSaveData EnsureContainer(string containerKey, int defaultCapacity, List<string> defaultItemsOrNull)
    {
        if (string.IsNullOrEmpty(containerKey)) return null;
        if (containers == null) containers = new Dictionary<string, ContainerSaveData>();

        if (!containers.TryGetValue(containerKey, out var c) || c == null)
        {
            c = new ContainerSaveData();
            c.containerKey = containerKey;
            c.capacity = Math.Max(1, defaultCapacity);

            c.items = defaultItemsOrNull != null ? new List<string>(defaultItemsOrNull) : new List<string>();
            c.Normalize();

            containers[containerKey] = c;
        }
        else
        {
            // 기존 데이터가 있더라도 안전하게 정규화
            if (c.capacity <= 0) c.capacity = Math.Max(1, defaultCapacity);
            c.Normalize();
        }

        return c;
    }

    public void RemoveContainer(string containerKey)
    {
        if (containers == null) return;
        if (string.IsNullOrEmpty(containerKey)) return;
        containers.Remove(containerKey);
    }

    // =========================
    // Existing
    // =========================
    public void NormalizeInventory()
    {
        if (inventoryItems == null)
            inventoryItems = new List<string>();

        while (inventoryItems.Count < inventoryCapacity)
            inventoryItems.Add("");

        if (inventoryItems.Count > inventoryCapacity)
            inventoryItems.RemoveRange(inventoryCapacity,
                inventoryItems.Count - inventoryCapacity);
    }

    public static SaveGameData CreateDefault(int slotIndex)
    {
        var data = new SaveGameData
        {
            version = 1,
            saveId = Guid.NewGuid().ToString("N"),
            savedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            displayName = $"슬롯 {slotIndex + 1}",
            clearedMaps = new List<string>(),
            currentMapId = "",
            discoveredItems = new HashSet<string>(),
            inventoryItems = new List<string> { "Rifle", "Grenade" },
            rescuedNpcs = new HashSet<string>(),
            storyProgress = 0,
            test = 0,
            helmetId = "",
            topId = "",
            bottomId = "",
            shoesId = "",
            weaponId = "",
            inventoryCapacity = 45,
            npcOverrides = new Dictionary<string, NpcOverrideData>(),

            // ✅ NEW
            containers = new Dictionary<string, ContainerSaveData>()
        };

        data.NormalizeInventory();
        return data;
    }
}