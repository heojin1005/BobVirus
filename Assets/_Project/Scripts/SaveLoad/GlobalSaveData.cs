using System;
using UnityEngine;

[Serializable]
public class GlobalSaveData
{
    public int version = 1;

    [Header("Tutorial")]
    public bool tutorialCompleted = false;

    [Header("Audio")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float bgmVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Header("Display")]
    [Range(0f, 1f)] public float brightness = 0.5f;

    public long savedAtUnix;

    public void Normalize()
    {
        masterVolume = Mathf.Clamp01(masterVolume);
        bgmVolume = Mathf.Clamp01(bgmVolume);
        sfxVolume = Mathf.Clamp01(sfxVolume);
        brightness = Mathf.Clamp01(brightness);
    }

    public static GlobalSaveData CreateDefault()
    {
        var data = new GlobalSaveData();
        data.Normalize();
        data.savedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return data;
    }
}