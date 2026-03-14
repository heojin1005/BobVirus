using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
[Serializable]
public class SaveGameData
{
    // ✅ 세이브 데이터 스키마 버전
    // v1: inventoryItems(List<string>), ContainerSaveData.items(List<string>)
    // v2: inventorySlots(List<ItemSlotData>), ContainerSaveData.slots(List<ItemSlotData>)
    public int version = 2;

    public string saveId;
    public long savedAtUnix;
    public string displayName;

    public List<string> clearedMaps = new();
    public string currentMapId = "";
    public HashSet<string> discoveredItems = new();

    // =========================
    // ✅ Inventory Slots (v2)
    // =========================
    [Serializable]
    public class ItemSlotData
    {
        public string id = "";
        public int count = 0;

        public ItemSlotData() { }
        public ItemSlotData(string id, int count)
        {
            this.id = id ?? "";
            this.count = count;
        }

        public bool IsEmpty => string.IsNullOrEmpty(id) || count <= 0;
    }

    public List<ItemSlotData> inventorySlots = new();

    // ✅ v1 legacy field (기존 세이브 로드용)
    // - 새 코드에서는 사용하지 말 것.
    [JsonProperty("inventoryItems", NullValueHandling = NullValueHandling.Ignore)]
    public List<string> inventoryItemsLegacy = null;

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
        public string takeItemId;
        public int takeCount = 1;

        public string giveItemId;
        public int giveCount = 1;

        public string buttonLabel = "교환";
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
    // ✅ Storage / Chest Data
    // =========================
    [Serializable]
    public class ContainerSaveData
    {
        public string containerKey;          // Dictionary key와 동일하게 넣어두면 디버깅에 편함
        public int capacity = 20;

        // ✅ v2
        public List<ItemSlotData> slots = new();

        // ✅ v1 legacy field (기존 세이브 로드용)
        [JsonProperty("items", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> itemsLegacy = null;

        public void Normalize()
        {
            if (slots == null) slots = new List<ItemSlotData>();

            // ✅ v1 -> v2 마이그레이션 (컨테이너)
            if (slots.Count == 0 && itemsLegacy != null && itemsLegacy.Count > 0)
            {
                slots.Clear();
                for (int i = 0; i < itemsLegacy.Count; i++)
                {
                    var id = itemsLegacy[i] ?? "";
                    slots.Add(string.IsNullOrEmpty(id) ? new ItemSlotData("", 0) : new ItemSlotData(id, 1));
                }
                itemsLegacy = null;
            }

            // ✅ 불변식 정리: (id=="" -> count=0), (count<=0 -> id="")
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] == null) { slots[i] = new ItemSlotData("", 0); continue; }

                slots[i].id ??= "";
                if (string.IsNullOrEmpty(slots[i].id) || slots[i].count <= 0)
                {
                    slots[i].id = "";
                    slots[i].count = 0;
                }
            }

            while (slots.Count < capacity)
                slots.Add(new ItemSlotData("", 0));

            if (slots.Count > capacity)
                slots.RemoveRange(capacity, slots.Count - capacity);
        }
    }

    /// <summary>
    /// containerKey -> 저장된 창고 데이터
    /// </summary>
    public Dictionary<string, ContainerSaveData> containers = new();

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

            // 템플릿(문자열 리스트)을 v2 슬롯 구조로 변환
            c.slots = new List<ItemSlotData>();
            if (defaultItemsOrNull != null)
            {
                for (int i = 0; i < defaultItemsOrNull.Count; i++)
                {
                    var id = defaultItemsOrNull[i] ?? "";
                    c.slots.Add(string.IsNullOrEmpty(id) ? new ItemSlotData("", 0) : new ItemSlotData(id, 1));
                }
            }
            c.Normalize();

            containers[containerKey] = c;
        }
        else
        {
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
    // Inventory Normalize (v2)
    // =========================
    public void NormalizeInventory()
    {
        if (inventorySlots == null)
            inventorySlots = new List<ItemSlotData>();

        // ✅ v1 -> v2 마이그레이션 (인벤)
        if (inventorySlots.Count == 0 && inventoryItemsLegacy != null && inventoryItemsLegacy.Count > 0)
        {
            inventorySlots.Clear();
            for (int i = 0; i < inventoryItemsLegacy.Count; i++)
            {
                var id = inventoryItemsLegacy[i] ?? "";
                inventorySlots.Add(string.IsNullOrEmpty(id) ? new ItemSlotData("", 0) : new ItemSlotData(id, 1));
            }
            inventoryItemsLegacy = null;
        }

        // ✅ 불변식 정리
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (inventorySlots[i] == null) { inventorySlots[i] = new ItemSlotData("", 0); continue; }

            inventorySlots[i].id ??= "";
            if (string.IsNullOrEmpty(inventorySlots[i].id) || inventorySlots[i].count <= 0)
            {
                inventorySlots[i].id = "";
                inventorySlots[i].count = 0;
            }
        }

        while (inventorySlots.Count < inventoryCapacity)
            inventorySlots.Add(new ItemSlotData("", 0));

        if (inventorySlots.Count > inventoryCapacity)
            inventorySlots.RemoveRange(inventoryCapacity, inventorySlots.Count - inventoryCapacity);
    }

    public static SaveGameData CreateDefault(int slotIndex)
    {
        var data = new SaveGameData
        {
            version = 2,
            saveId = Guid.NewGuid().ToString("N"),
            savedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            displayName = $"슬롯 {slotIndex + 1}",
            clearedMaps = new List<string>(),
            currentMapId = "",
            discoveredItems = new HashSet<string>(),

            inventorySlots = new List<ItemSlotData>
            {
                new ItemSlotData("Rifle", 1),
                new ItemSlotData("Grenade", 1),
                new ItemSlotData("Small_Potion", 3),
                new ItemSlotData("Small_Potion", 3)
            },

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

            containers = new Dictionary<string, ContainerSaveData>()
        };

        data.NormalizeInventory();
        return data;
    }
}