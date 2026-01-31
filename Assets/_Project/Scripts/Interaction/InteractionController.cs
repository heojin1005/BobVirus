using System.Collections.Generic;
using UnityEngine;

public class InteractionController : MonoBehaviour
{
    public static InteractionController Instance { get; private set; }

    [Header("Save")]
    [SerializeField] private int currentSaveSlot = 0;

    [Header("TEST (임시)")]
    [SerializeField] private bool useDebugSaveData = true;
    [SerializeField] private SaveGameData debugSaveData;

    private void Awake()
    {
        Instance = this;

        // 테스트 편의: 디버그 데이터가 없으면 기본 생성
        if (useDebugSaveData && debugSaveData == null)
            debugSaveData = SaveGameData.CreateDefault(currentSaveSlot);
    }

    /// <summary>
    /// 현재 슬롯의 SaveGameData를 가져오는 함수.
    /// 지금은 테스트용(인스펙터) / 나중에 SaveManager 연결로 교체하면 됨.
    /// </summary>
    private SaveGameData GetSaveData()
    {
        if (useDebugSaveData) return debugSaveData;

        // TODO: 네 실제 세이브 매니저로 교체
        // return SaveManager.Instance.GetSlot(currentSaveSlot);

        return null;
    }

    public void TryInteract(InteractionTarget target)
    {
        if (target == null) return;

        // 1) SaveData 읽기
        SaveGameData data = GetSaveData();
        int storyProgress = (data != null) ? data.storyProgress : 0;

        // (선물 아이템 보유 여부도 나중에 data/inventory로 대체 가능)
        bool hasGiftItem = false;

        // 2) 조건 검사 + 옵션 만들기
        List<InteractionOption> options = target.kind switch
        {
            InteractionKind.Npc => BuildNpcOptions(target.targetId, storyProgress, hasGiftItem),
            InteractionKind.Encyclopedia => BuildEncyclopediaOptions(),
            InteractionKind.DeployTerminal => BuildDeployOptions(),
            _ => BuildGenericOptions(target)
        };

        // 3) 거부/허용 처리
        int enabledCount = 0;
        int firstEnabledIndex = -1;
        for (int i = 0; i < options.Count; i++)
        {
            if (options[i].enabled)
            {
                enabledCount++;
                if (firstEnabledIndex < 0) firstEnabledIndex = i;
            }
        }

        if (enabledCount == 0)
        {
            ShowToast("상호작용할 수 없음");
            return;
        }

        // UI 없으니: 옵션 1개면 실행, 여러 개면 목록 출력 후 첫 enabled 실행(테스트용)
        if (enabledCount == 1)
        {
            options[firstEnabledIndex].execute?.Invoke();
            return;
        }

        Debug.Log($"[Interaction] Options for {target.kind}:{target.targetId} (storyProgress={storyProgress})");
        for (int i = 0; i < options.Count; i++)
        {
            var opt = options[i];
            Debug.Log($"  {i}. {opt.title} {(opt.enabled ? "" : $"(disabled: {opt.reason})")}");
        }

        options[firstEnabledIndex].execute?.Invoke();
    }

    /// <summary>
    /// 핵심: storyProgress에 따라 NPC 옵션 분기
    /// storyProgress == 0 -> "대화하기" 중심
    /// storyProgress == 1 -> "선물 주기" 중심 (예시)
    /// </summary>
    private List<InteractionOption> BuildNpcOptions(string npcId, int storyProgress, bool hasGiftItem)
    {
        var list = new List<InteractionOption>();

        if (storyProgress == 0)
        {
            // 분기 A: 스토리 0이면 대화만(혹은 대화 우선)
            list.Add(new InteractionOption("대화하기", () =>
            {
                Debug.Log($"[NPC:{npcId}] Talk (storyProgress={storyProgress})");
            }));

            // 원하면 선물은 아예 안 보여도 되고, disabled로 보여도 됨.
            list.Add(new InteractionOption(
                "선물 주기(잠김)",
                () => Debug.Log($"[NPC:{npcId}] Give gift"),
                enabled: false,
                reason: "스토리 진행도 부족"
            ));

            return list;
        }

        if (storyProgress == 1)
        {
            // 분기 B: 스토리 1이면 "선물 주기"만 허용 (테스트용 확정 분기)
            list.Add(new InteractionOption("선물 주기", () =>
            {
                Debug.Log($"[NPC:{npcId}] Gift (storyProgress={storyProgress})");
            }, enabled: true));

            return list; // ✅ Talk 옵션을 아예 추가하지 않음
        }


        // 나머지 진행도 처리(혹시 2 이상도 생기면)
        list.Add(new InteractionOption("대화하기", () =>
        {
            Debug.Log($"[NPC:{npcId}] Talk (storyProgress={storyProgress})");
        }));

        return list;
    }

    private List<InteractionOption> BuildEncyclopediaOptions()
    {
        return new List<InteractionOption>
        {
            new InteractionOption("도감 열기", () => Debug.Log("[Encyclopedia] Open"))
        };
    }

    private List<InteractionOption> BuildDeployOptions()
    {
        bool canDeploy = true;

        return new List<InteractionOption>
        {
            new InteractionOption("출격 준비", () => Debug.Log("[Deploy] Open UI"), enabled: canDeploy, reason: canDeploy ? null : "조건 부족")
        };
    }

    private List<InteractionOption> BuildGenericOptions(InteractionTarget target)
    {
        return new List<InteractionOption>
        {
            new InteractionOption("상호작용", () => Debug.Log($"[Generic] {target.name}"))
        };
    }

    private void ShowToast(string msg)
    {
        Debug.Log($"[Toast] {msg}");
    }
}
