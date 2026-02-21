using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcUIManager : MonoBehaviour
{
    public static NpcUIManager Instance { get; private set; }

    [Header("Root")]
    [SerializeField] private GameObject npcInteractionRoot;

    [Header("Panels")]
    [SerializeField] private GameObject topicPanelObject;
    [SerializeField] private NpcTopicPanel topicPanel;
    [SerializeField] private DialoguePanelUI dialoguePanel;

    [Header("NPC Database (Stage C Part 1)")]
    [SerializeField] private NpcDatabaseSO npcDatabase;

    [Header("Optional Test Graphs")]
    [SerializeField] private DialogueGraphSO talkTestGraph;
    [SerializeField] private DialogueGraphSO questTestGraph;

    [Header("Dimmer")]
    [SerializeField] private CanvasGroup dimmerCanvasGroup;
    [Tooltip("UI를 연 직후 같은 클릭/터치로 Dimmer Close가 바로 먹는 것 방지(초 단위, unscaled 기준)")]
    [SerializeField] private float ignoreDimmerClickSeconds = 0.12f;

    private const string PauseReason = "NPC_UI";
    private Coroutine dimmerRoutine;
    private float openedAtUnscaled;

    private string currentNpcId;
    private string currentNpcDisplayName;

    public bool IsOpen => npcInteractionRoot != null && npcInteractionRoot.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (npcInteractionRoot != null)
            npcInteractionRoot.SetActive(false);

        ResetPanels();
    }

    public void OpenTopic(
        string npcId,
        string npcDisplayName,
        System.Action onTalk,
        System.Action onTrade,
        System.Action onQuest)
    {
        currentNpcId = npcId;
        currentNpcDisplayName = npcDisplayName;

        PauseService.Instance?.Push(PauseReason);

        ResetPanels();

        if (npcInteractionRoot != null)
            npcInteractionRoot.SetActive(true);

        openedAtUnscaled = Time.unscaledTime;
        ArmDimmerClickNextFrame();

        if (topicPanel == null)
        {
            Debug.LogWarning("NpcUIManager: topicPanel not assigned.");
            return;
        }

        topicPanel.Open(
            npcId,
            npcDisplayName,
            onTalk: onTalk,
            onTrade: onTrade,
            onQuest: onQuest,
            onClose: () => CloseAll()
        );
    }

    public void CloseAllFromDimmer()
    {
        if (Time.unscaledTime - openedAtUnscaled < ignoreDimmerClickSeconds)
            return;

        CloseAll();
    }

    // ✅ 공용: effect 리스트를 실제 SaveGameData에 적용
    private void ApplyEffects(List<DialogueEffect> effects)
    {
        var data = GameManager.Instance != null ? GameManager.Instance.CurrentData : null;
        if (data == null || effects == null) return;

        foreach (var e in effects)
            e?.Apply(data);
    }

    // =========================
    // ✅ Stage C Part 1: npcId → npcDef → graph 오픈 + 헤더 세팅
    // =========================
    public void StartTalk(string npcId)
    {
        if (topicPanelObject != null)
            topicPanelObject.SetActive(false);

        if (dialoguePanel == null)
        {
            Debug.LogWarning("NpcUIManager: dialoguePanel not assigned.");
            return;
        }

        var npcDef = npcDatabase != null ? npcDatabase.Get(npcId) : null;
        if (npcDef == null || npcDef.talkGraph == null)
        {
            Debug.LogWarning($"NpcUIManager: talkGraph not found for npcId='{npcId}'.");
            CloseAll();
            return;
        }

        // ✅ 좌상단 초상/이름 바인딩
        dialoguePanel.SetNpcHeader(npcDef.portrait, npcDef.displayName);

        OpenGraph(npcDef.talkGraph);
    }

    public void StartQuest(string npcId)
    {
        if (topicPanelObject != null)
            topicPanelObject.SetActive(false);

        if (dialoguePanel == null)
        {
            Debug.LogWarning("NpcUIManager: dialoguePanel not assigned.");
            return;
        }

        var npcDef = npcDatabase != null ? npcDatabase.Get(npcId) : null;
        if (npcDef == null || npcDef.questGraph == null)
        {
            Debug.LogWarning($"NpcUIManager: questGraph not found for npcId='{npcId}'.");
            CloseAll();
            return;
        }

        // ✅ 좌상단 초상/이름 바인딩
        dialoguePanel.SetNpcHeader(npcDef.portrait, npcDef.displayName);

        OpenGraph(npcDef.questGraph);
    }

    private void OpenGraph(DialogueGraphSO graph)
    {
        if (graph == null)
        {
            CloseAll();
            return;
        }

        dialoguePanel.OpenByNodeId(
            graph.nodes,
            graph.startNodeId,
            ApplyEffects,
            onEndCallback: () =>
            {
                dialoguePanel.Close();
                CloseAll();
            });
    }

    // =========================
    // (선택) 기존 테스트 유지
    // =========================
    public void StartTalkTest(string npcDisplayName)
    {
        if (talkTestGraph == null)
        {
            Debug.LogWarning("NpcUIManager: talkTestGraph not assigned.");
            return;
        }

        if (topicPanelObject != null)
            topicPanelObject.SetActive(false);

        // 테스트는 헤더를 그냥 displayName만 세팅(원하면 제거)
        dialoguePanel.SetNpcHeader(null, npcDisplayName);

        OpenGraph(talkTestGraph);
    }

    public void StartQuestTest(string npcDisplayName)
    {
        if (questTestGraph == null)
        {
            Debug.LogWarning("NpcUIManager: questTestGraph not assigned.");
            return;
        }

        if (topicPanelObject != null)
            topicPanelObject.SetActive(false);

        dialoguePanel.SetNpcHeader(null, npcDisplayName);

        OpenGraph(questTestGraph);
    }

    // =========================
    // Dimmer 안전장치
    // =========================
    private void ArmDimmerClickNextFrame()
    {
        if (dimmerCanvasGroup == null) return;

        if (dimmerRoutine != null)
            StopCoroutine(dimmerRoutine);

        dimmerCanvasGroup.blocksRaycasts = false;
        dimmerCanvasGroup.interactable = false;

        dimmerRoutine = StartCoroutine(EnableDimmerNextFrame());
    }

    private IEnumerator EnableDimmerNextFrame()
    {
        yield return null;

        dimmerCanvasGroup.blocksRaycasts = true;
        dimmerCanvasGroup.interactable = true;

        dimmerRoutine = null;
    }

    // =========================
    // UI 초기화 / 닫기
    // =========================
    private void ResetPanels()
    {
        if (topicPanelObject != null)
            topicPanelObject.SetActive(true);

        if (dialoguePanel != null)
            dialoguePanel.Close(); // Close 내부에서 헤더/선택지까지 정리
    }

    public void CloseAll()
    {
        if (dimmerRoutine != null)
        {
            StopCoroutine(dimmerRoutine);
            dimmerRoutine = null;
        }

        ResetPanels();

        if (npcInteractionRoot != null)
            npcInteractionRoot.SetActive(false);

        PauseService.Instance?.Pop(PauseReason);
    }
}