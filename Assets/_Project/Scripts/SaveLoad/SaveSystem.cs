using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public static class SaveSystem
{
    private const int SlotCount = 4;
    private const string FolderName = "saves";

    private static string SaveDir => Path.Combine(Application.persistentDataPath, FolderName);

    public static int GetSlotCount() => SlotCount;

    private static string GetSlotPath(int slotIndex) => Path.Combine(SaveDir, $"slot_{slotIndex}.json");

    private static void EnsureDir()
    {
        if (!Directory.Exists(SaveDir))
            Directory.CreateDirectory(SaveDir);
    }

    public static SaveSlotMeta GetMeta(int slotIndex)
    {
        EnsureDir();
        var path = GetSlotPath(slotIndex);

        if (!File.Exists(path))
            return new SaveSlotMeta { slotIndex = slotIndex, exists = false, displayName = $"슬롯 {slotIndex + 1}", savedAtUnix = 0 };

        // 파일이 있으면 데이터 일부만 읽어서 표시
        try
        {
            var json = File.ReadAllText(path);
            var data = JsonConvert.DeserializeObject<SaveGameData>(json);
            return new SaveSlotMeta
            {
                slotIndex = slotIndex,
                exists = true,
                displayName = string.IsNullOrEmpty(data.displayName) ? $"슬롯 {slotIndex + 1}" : data.displayName,
                savedAtUnix = data.savedAtUnix
            };
        }
        catch
        {
            return new SaveSlotMeta { slotIndex = slotIndex, exists = true, displayName = $"슬롯 {slotIndex + 1} (손상됨)", savedAtUnix = 0 };
        }
    }

    public static SaveSlotMeta[] GetAllMetas()
    {
        var arr = new SaveSlotMeta[SlotCount];
        for (int i = 0; i < SlotCount; i++) arr[i] = GetMeta(i);
        return arr;
    }

    public static void Save(int slotIndex, SaveGameData data)
    {
        EnsureDir();
        data.savedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(GetSlotPath(slotIndex), json);
    }

    public static bool TryLoad(int slotIndex, out SaveGameData data)
    {
        EnsureDir();
        var path = GetSlotPath(slotIndex);
        if (!File.Exists(path)) { data = null; return false; }

        try
        {
            var json = File.ReadAllText(path);
            data = JsonConvert.DeserializeObject<SaveGameData>(json);
            return data != null;
        }
        catch
        {
            data = null;
            return false;
        }
    }
}
