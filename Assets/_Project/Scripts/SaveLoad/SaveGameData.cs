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
            npcOverrides = new Dictionary<string, NpcOverrideData>()
        };
        data.NormalizeInventory();
        return data;
    }
}