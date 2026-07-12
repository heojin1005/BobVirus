using UnityEngine;

public class GameManager : MonoBehaviour
{
    public const int DefaultProfileSlot = 0;

    public static GameManager Instance { get; private set; }

    public int CurrentSlot { get; private set; } = -1;
    public SaveGameData CurrentData { get; private set; }

    // ✅ 전역 세이브
    public GlobalSaveData GlobalData { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // ✅ 게임 실행 시 전역 세이브 1회 로드
        GlobalData = GlobalSaveSystem.LoadOrCreate();
    }

    public void ContinueDefaultProfile()
    {
        if (!LoadGame(DefaultProfileSlot))
            StartNewGame(DefaultProfileSlot);
    }

    public void RestartDefaultProfile()
    {
        StartNewGame(DefaultProfileSlot);
    }

    public void StartNewGame(int slotIndex)
    {
        CurrentSlot = slotIndex;
        CurrentData = SaveGameData.CreateDefault(slotIndex);
        SaveSystem.Save(slotIndex, CurrentData);
    }

    public bool LoadGame(int slotIndex)
    {
        if (!SaveSystem.TryLoad(slotIndex, out var data))
            return false;

        CurrentSlot = slotIndex;
        CurrentData = data;
        return true;
    }

    public void SaveNow()
    {
        if (CurrentSlot < 0 || CurrentData == null) return;
        SaveSystem.Save(CurrentSlot, CurrentData);
    }

    // =========================
    // Global Save API
    // =========================
    public void SaveGlobalNow()
    {
        if (GlobalData == null)
            GlobalData = GlobalSaveSystem.LoadOrCreate();

        GlobalSaveSystem.Save(GlobalData);
    }

    public void ReloadGlobal()
    {
        GlobalData = GlobalSaveSystem.LoadOrCreate();
    }

    public void ResetTutorialFlagForDev()
    {
        if (GlobalData == null)
            GlobalData = GlobalSaveSystem.LoadOrCreate();

        GlobalData.tutorialCompleted = false;
        SaveGlobalNow();
    }

    public void MarkTutorialCompleted()
    {
        if (GlobalData == null)
            GlobalData = GlobalSaveSystem.LoadOrCreate();

        if (!GlobalData.tutorialCompleted)
        {
            GlobalData.tutorialCompleted = true;
            SaveGlobalNow();
        }
    }
}
