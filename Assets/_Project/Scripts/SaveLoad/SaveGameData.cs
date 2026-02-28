using System;
using System.Collections.Generic;

[Serializable]
public class SaveGameData
{
    public int version = 1;

    // 슬롯 정보/표시에 쓸 것
    public string saveId;                 // GUID 등
    public long savedAtUnix;              // 마지막 저장 시각(Unix seconds)
    public string displayName;            // 슬롯 표시용(예: "슬롯 1")

    // 너 요구사항(전투 시스템 제외)
    public List<string> clearedMaps = new();
    public string currentMapId = "";      // 도전중 맵
    public HashSet<string> discoveredItems = new();   // 도감: 발견한 아이템
    public List<string> inventoryItems = new();       // 소지 아이템 ID 리스트(간단 버전)
    public int inventoryCapacity = 20;
    public string helmetId = ""; // 뚝배기
    public string topId = ""; // 상의
    public string bottomId = ""; // 하의
    public string shoesId = ""; // 신발
    public string weaponId = ""; // 무기
    public HashSet<string> rescuedNpcs = new();        // 구조된 NPC ID
    public int storyProgress = 1;
    public int test = 0;
    public void NormalizeInventory()
    {
        if (inventoryItems == null)
            inventoryItems = new List<string>();

        // 부족하면 빈칸 추가
        while (inventoryItems.Count < inventoryCapacity)
            inventoryItems.Add("");

        // 넘치면 잘라냄 (정책에 따라 다르게 가능)
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
            inventoryCapacity = 45
        };
        data.NormalizeInventory();
        return data;
    }
}
