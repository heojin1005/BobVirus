using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public static class GlobalSaveSystem
{
    private const string FolderName = "saves";
    private const string FileName = "global_save.json";

    private static string SaveDir => Path.Combine(Application.persistentDataPath, FolderName);
    private static string SavePath => Path.Combine(SaveDir, FileName);

    private static void EnsureDir()
    {
        if (!Directory.Exists(SaveDir))
            Directory.CreateDirectory(SaveDir);
    }

    public static GlobalSaveData LoadOrCreate()
    {
        EnsureDir();

        if (!File.Exists(SavePath))
        {
            var created = GlobalSaveData.CreateDefault();
            Save(created);
            return created;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            var data = JsonConvert.DeserializeObject<GlobalSaveData>(json);

            if (data == null)
                data = GlobalSaveData.CreateDefault();

            data.Normalize();
            return data;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GlobalSaveSystem] Load failed. Recreating default. {e.Message}");

            var fallback = GlobalSaveData.CreateDefault();
            Save(fallback);
            return fallback;
        }
    }

    public static void Save(GlobalSaveData data)
    {
        if (data == null)
            return;

        EnsureDir();
        data.Normalize();
        data.savedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(SavePath, json);
    }

    public static bool Exists()
    {
        EnsureDir();
        return File.Exists(SavePath);
    }

    public static void Delete()
    {
        EnsureDir();

        if (File.Exists(SavePath))
            File.Delete(SavePath);
    }

    public static void ResetToDefault()
    {
        Save(GlobalSaveData.CreateDefault());
    }

    public static void ResetTutorialFlagOnly()
    {
        var data = LoadOrCreate();
        data.tutorialCompleted = false;
        Save(data);
    }
}