using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcUIManager : MonoBehaviour
{
    public static NpcUIManager Instance { get; private set; }

    [Header("DB")]
    [SerializeField] private NpcDatabaseSO npcDatabase;
    [SerializeField] private DialogueGraphDatabaseSO dialogueGraphDatabase;

    [Header("Panels")]
    [SerializeField] private GameObject npcInteractionRoot;
    [SerializeField] private GameObject topicPanelObject;
    [SerializeField] private NpcTopicPanel topicPanel;
    [SerializeField] private DialoguePanelUI dialoguePanel;

    [Header("Runner")]
    [SerializeField] private MonoBehaviour graphRunnerBehaviour; // IDialogueGraphRunner 구현체
    private IDialogueGraphRunner graphRunner;

    [Header("Store UI")]
    [SerializeField] private NpcStorePanelUI storePanel;

    [Header("Dimmer")]
    [SerializeField] private CanvasGroup dimmerCanvasGroup;
    [SerializeField] private float ignoreDimmerClickSeconds = 0.12f;

    private const string PauseReason = "NPC_UI";
    private Coroutine dimmerRoutine;
    private float openedAtUnscaled;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (npcInteractionRoot != null)
            npcInteractionRoot.SetActive(false);

        // Runner 캐스팅
        graphRunner = graphRunnerBehaviour as IDialogueGraphRunner;
        if (graphRunner == null && graphRunnerBehaviour != null)
            Debug.LogError("[NpcUIManager] graphRunnerBehaviour는 IDialogueGraphRunner를 구현해야 합니다.");

        ResetPanels();
    }

    // =========================
    // Public UI Entry
    // =========================
    public void OpenTopic(string npcId, string npcDisplayName, System.Action onTalk, System.Action onTrade, System.Action onQuest)
    {
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

    // =========================
    // Resolver (Definition + Save Override)
    // =========================
    private SaveGameData GetData()
    {
        return GameManager.Instance != null ? GameManager.Instance.CurrentData : null;
    }

    private NpcDefinitionSO GetNpcDef(string npcId)
    {
        if (npcDatabase == null) return null;
        return npcDatabase.Get(npcId);
    }

    private DialogueGraphSO ResolveGraph(DialogueGraphSO defaultGraph, string overrideGraphId)
    {
        if (!string.IsNullOrEmpty(overrideGraphId) && dialogueGraphDatabase != null)
        {
            var g = dialogueGraphDatabase.Get(overrideGraphId);
            if (g != null) return g;
        }
        return defaultGraph;
    }

    private List<StorePanelUI.Row> ResolveStoreRows(NpcDefinitionSO def, SaveGameData data, string npcId)
    {
        var rows = new List<StorePanelUI.Row>();

        // 기본
        if (def != null && def.storeList != null)
        {
            foreach (var e in def.storeList)
            {
                if (e == null) continue;
                rows.Add(new StorePanelUI.Row { itemId = e.itemId, price = e.price });
            }
        }

        // override 있으면 교체
        var o = data != null ? data.GetNpcOverride(npcId) : null;
        if (o != null && o.storeList != null)
        {
            rows.Clear();
            foreach (var e in o.storeList)
            {
                if (e == null) continue;
                rows.Add(new StorePanelUI.Row { itemId = e.itemId, price = e.price });
            }
        }

        return rows;
    }

    // =========================
    // Interaction Entry Points
    // =========================
    public void StartTalk(string npcId)
    {
        var def = GetNpcDef(npcId);
        var data = GetData();
        var o = data != null ? data.GetNpcOverride(npcId) : null;

        var graph = ResolveGraph(def != null ? def.talkGraph : null, o != null ? o.talkGraphId : null);

        if (graph == null)
        {
            Debug.LogWarning($"[Talk] graph is NULL. npc={npcId}");
            return;
        }

        if (graphRunner == null)
        {
            Debug.LogError("[Talk] graphRunner is not assigned/invalid. (IDialogueGraphRunner 필요)");
            return;
        }

        if (topicPanelObject != null)
            topicPanelObject.SetActive(false);

        Debug.Log($"[Talk] npc={npcId}, graph={graph.name}");
        graphRunner.Play(graph, ApplyEffects, () => { CloseAll(); });
    }

    public void StartQuest(string npcId)
    {
        var def = GetNpcDef(npcId);
        var data = GetData();
        var o = data != null ? data.GetNpcOverride(npcId) : null;

        var graph = ResolveGraph(def != null ? def.questGraph : null, o != null ? o.questGraphId : null);

        if (graph == null)
        {
            Debug.LogWarning($"[Quest] graph is NULL. npc={npcId}");
            return;
        }

        if (graphRunner == null)
        {
            Debug.LogError("[Quest] graphRunner is not assigned/invalid. (IDialogueGraphRunner 필요)");
            return;
        }

        if (topicPanelObject != null)
            topicPanelObject.SetActive(false);

        Debug.Log($"[Quest] npc={npcId}, graph={graph.name}");
        graphRunner.Play(graph, ApplyEffects, () => { CloseAll(); });
    }

    public void StartTrade(string npcId)
    {
        Debug.Log($"[Trade] StartTrade called. npcId={npcId}");

        var def = GetNpcDef(npcId);
        var data = GetData();

        if (topicPanelObject != null)
            topicPanelObject.SetActive(false);

        // ✅ storePanel이 null이면 자동으로 씬에서 찾아보기(안전망)
        if (storePanel == null)
        {
            storePanel = FindObjectOfType<NpcStorePanelUI>(true);
            Debug.LogWarning($"[Trade] storePanel was null -> auto find result: {(storePanel != null ? storePanel.name : "null")}");
        }

        if (storePanel == null)
        {
            Debug.LogError("[Trade] storePanel is STILL null. Assign NpcStorePanelUI in inspector.");
            return;
        }

        // ✅ 패널이 뒤에 깔리는 경우 방지
        storePanel.transform.SetAsLastSibling();

        var npcName = def != null ? def.displayName : npcId;

        // ResolveStoreRows가 기존 StorePanelUI.Row를 반환하는 구조면 변환 필요
        var finalStore = ResolveStoreRows(def, data, npcId);

        var rows = new List<NpcStorePanelUI.RowData>();
        foreach (var r in finalStore)
        {
            rows.Add(new NpcStorePanelUI.RowData { itemId = r.itemId, price = r.price });
        }

        Debug.Log($"[Trade] Opening store panel. rows={rows.Count}, npcName={npcName}");

        storePanel.Open(npcName, rows, () =>
        {
            Debug.Log("[Trade] Store panel closed.");
            storePanel.Close();
            CloseAll();
        });
    }

    // =========================
    // Effects apply
    // =========================
    private void ApplyEffects(List<DialogueEffect> effects)
    {
        var data = GetData();
        if (data == null || effects == null) return;

        foreach (var e in effects)
            e?.Apply(data);

        Debug.Log($"[DialogueEffect] storyProgress={data.storyProgress}, test={data.test}");
    }

    // =========================
    // UI plumbing
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

    private void ResetPanels()
    {
        if (topicPanelObject != null)
            topicPanelObject.SetActive(true);

        if (dialoguePanel != null)
            dialoguePanel.Close();

        if (storePanel != null)
            storePanel.Close();
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