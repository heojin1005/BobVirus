using UnityEngine;

/// <summary>
/// InteractionController 같은 시스템들이 "현재 세이브 데이터"를 얻어오기 위한 공급원.
/// 디버그/실세이브를 여기서 통제한다.
/// </summary>
public class SaveDataProvider : MonoBehaviour
{
    [Header("Debug (개발용)")]
    [SerializeField] private bool useDebugSaveData = false;
    [SerializeField] private int debugSlotIndex = 0;
    [SerializeField] private SaveGameData debugSaveData;

    private void Awake()
    {
        // 디버그 모드면 데이터가 없을 때 기본 생성
        if (useDebugSaveData && debugSaveData == null)
            debugSaveData = SaveGameData.CreateDefault(debugSlotIndex);
    }

    /// <summary>
    /// 현재 게임에서 사용해야 하는 SaveGameData 반환
    /// </summary>
    public SaveGameData GetCurrentData()
    {
        if (useDebugSaveData) return debugSaveData;

        // ✅ 실제 세이브 시스템 공급원: GameManager
        if (GameManager.Instance == null) return null;
        return GameManager.Instance.CurrentData;
    }

    /// <summary>
    /// 현재 슬롯 반환 (UI/로그/디버깅용)
    /// </summary>
    public int GetCurrentSlot()
    {
        if (useDebugSaveData) return debugSlotIndex;

        if (GameManager.Instance == null) return -1;
        return GameManager.Instance.CurrentSlot;
    }
}
