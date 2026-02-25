using System.Collections.Generic;
using UnityEngine;

public class InteractionController : MonoBehaviour
{
    public static InteractionController Instance { get; private set; }

    [Header("Save Source")]
    [SerializeField] private SaveDataProvider saveDataProvider;

    private void Awake()
    {
        Instance = this;

        // 씬에 SaveDataProvider가 있으면 자동 연결(수동 할당도 가능)
        if (saveDataProvider == null)
            saveDataProvider = FindFirstObjectByType<SaveDataProvider>();
    }

    /// <summary>
    /// 현재 슬롯/세션의 SaveGameData를 가져온다.
    /// - SaveDataProvider가 디버그/실세이브 공급원을 결정
    /// </summary>
    private SaveGameData GetSaveData()
    {
        return saveDataProvider != null ? saveDataProvider.GetCurrentData() : null;
    }

    public void TryInteract(InteractionTarget target)
    {
        if (target == null) return;

        // 1) SaveData 읽기
        SaveGameData data = GetSaveData();
        data.NormalizeInventory();
        int storyProgress = (data != null) ? data.storyProgress : 0;

        // 2) 조건 검사 + 옵션 만들기
        List<InteractionOption> options = target.kind switch
        {
            InteractionKind.Npc => NPCInteraction(target.targetId, data),
            InteractionKind.Encyclopedia => EncyclopediaOpen(data),
            _ => BuildGenericOptions(target)
        };

        int firstEnabled = -1;
        int enabledCount = 0;

        for (int i = 0; i < options.Count; i++)
        {
            if (options[i].enabled)
            {
                enabledCount++;
                if (firstEnabled < 0) firstEnabled = i;
            }
        }

        if (enabledCount == 0)
        {
            // 옵션이 아예 없거나 전부 disabled면 거부
            // (reason이 있으면 더 친절하게 출력 가능)
            ShowToast("상호작용할 수 없음");
            return;
        }

        // 지금은 선택 UI가 없으니 실행
        options[firstEnabled].execute?.Invoke();
    }

    // private List<InteractionOption> 함수이름(변수...)
    // var list = new List<InteractionOption>();
    // list.Add(new InteractionOption("이름", () =>
    //{
    //실행할 코드
    //}
    //));
    //return list;
    private List<InteractionOption> NPCInteraction(string npcId, SaveGameData data)
    {
        var list = new List<InteractionOption>();
        list.Add(new InteractionOption("NPC 클릭 확인", () =>
        {
            string displayName = npcId;
            NpcUIManager.Instance.OpenTopic(
                npcId,
                displayName,
                onTalk: () => NpcUIManager.Instance.StartTalk(npcId),
                onTrade: () => NpcUIManager.Instance.StartTrade(npcId),
                onQuest: () => NpcUIManager.Instance.StartQuest(npcId)
            );
        }));
        return list;
    }
    private List<InteractionOption> NpcClickCount(string npcId, SaveGameData data)
    {
        var list = new List<InteractionOption>();
        list.Add(new InteractionOption("클릭 카운트", () =>
        {
            if(data == null)
            {
                Debug.Log("[NPC] SaveData is null");
                return;
            }
            data.test += 1;
            Debug.Log($"[NPC:{npcId}] clickCount(test) = {data.test}");
        }));
        return list;
    }
    private List<InteractionOption> EncyclopediaOpen(SaveGameData data)
    {
        var list = new List<InteractionOption>();
        list.Add(new InteractionOption("도감 열기", () =>
        {
            if(data == null)
            {
                Debug.Log("[NPC] SaveData is null");
                return;
            }
            if(data.test >= 5)
            {
                Debug.Log($"test가 {data.test}이므로 사전이 사용 가능합니다.");
            }
            else if(data.test < 5)
            {
                Debug.Log($"test가 {data.test}이므로 사전이 사용 불가능합니다.");
            }
            GameManager.Instance.SaveNow();
        }));
        return list;
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
