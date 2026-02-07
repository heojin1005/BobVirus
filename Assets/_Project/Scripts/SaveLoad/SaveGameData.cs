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
    public HashSet<string> rescuedNpcs = new();        // 구조된 NPC ID
    public int storyProgress = 1;
    public int test = 0;

    public static SaveGameData CreateDefault(int slotIndex)
    {
        return new SaveGameData
        {
            version = 1,
            saveId = Guid.NewGuid().ToString("N"),
            savedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            displayName = $"슬롯 {slotIndex + 1}",
            clearedMaps = new List<string>(),
            currentMapId = "",
            discoveredItems = new HashSet<string>(),
            inventoryItems = new List<string>(),
            rescuedNpcs = new HashSet<string>(),
            storyProgress = 0,
            test = 0
        };
    }
}
