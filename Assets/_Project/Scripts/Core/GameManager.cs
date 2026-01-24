using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // 나중에 세이브 슬롯, 옵션, 상태값 전부 여기서 관리
    public int CurrentSlot { get; private set; } = -1;
    public SaveGameData CurrentData {get; private set;}

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
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
}
