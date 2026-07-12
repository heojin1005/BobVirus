using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UI;

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
    [SerializeField] private MonoBehaviour graphRunnerBehaviour;
    private IDialogueGraphRunner graphRunner;

    [Header("Store UI")]
    [SerializeField] private NpcStorePanelUI storePanel;

    [Header("Trade Runtime")]
    [SerializeField] private InventoryUIController inventoryUI;

    [Header("Dimmer")]
    [SerializeField] private CanvasGroup dimmerCanvasGroup;
    [SerializeField] private float ignoreDimmerClickSeconds = 0.12f;

    private const string PauseReason = "NPC_UI";
    private Coroutine dimmerRoutine;
    private float openedAtUnscaled;

    private class TradeEntryRuntime
    {
        public string takeItemId;
        public int takeCount;
        public string giveItemId;
        public int giveCount;
        public string buttonLabel;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (npcInteractionRoot != null)
            npcInteractionRoot.SetActive(false);

        graphRunner = graphRunnerBehaviour as IDialogueGraphRunner;
        if (graphRunner == null && graphRunnerBehaviour != null)
            Debug.LogError("[NpcUIManager] graphRunnerBehaviour는 IDialogueGraphRunner를 구현해야 합니다.");

        if (inventoryUI == null)
            inventoryUI = FindFirstObjectByType<InventoryUIController>(FindObjectsInactive.Include);

        ResetPanels();
    }

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

    private List<TradeEntryRuntime> ResolveStoreRows(NpcDefinitionSO def, SaveGameData data, string npcId)
    {
        var rows = new List<TradeEntryRuntime>();

        if (def != null && def.storeList != null)
        {
            foreach (var e in def.storeList)
            {
                if (e == null) continue;

                rows.Add(new TradeEntryRuntime
                {
                    takeItemId = e.takeItemId,
                    takeCount = Mathf.Max(1, e.takeCount),
                    giveItemId = e.giveItemId,
                    giveCount = Mathf.Max(1, e.giveCount),
                    buttonLabel = string.IsNullOrEmpty(e.buttonLabel) ? "교환" : e.buttonLabel
                });
            }
        }

        var o = data != null ? data.GetNpcOverride(npcId) : null;
        if (o != null && o.storeList != null)
        {
            rows.Clear();

            foreach (var e in o.storeList)
            {
                if (e == null) continue;

                rows.Add(new TradeEntryRuntime
                {
                    takeItemId = e.takeItemId,
                    takeCount = Mathf.Max(1, e.takeCount),
                    giveItemId = e.giveItemId,
                    giveCount = Mathf.Max(1, e.giveCount),
                    buttonLabel = string.IsNullOrEmpty(e.buttonLabel) ? "교환" : e.buttonLabel
                });
            }
        }

        return rows;
    }

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
            Debug.LogError("[Talk] graphRunner is not assigned/invalid.");
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
            Debug.LogError("[Quest] graphRunner is not assigned/invalid.");
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

        if (storePanel == null)
        {
            storePanel = FindFirstObjectByType<NpcStorePanelUI>(FindObjectsInactive.Include);
            Debug.LogWarning($"[Trade] storePanel was null -> auto find result: {(storePanel != null ? storePanel.name : "null")}");
        }

        if (inventoryUI == null)
            inventoryUI = FindFirstObjectByType<InventoryUIController>(FindObjectsInactive.Include);

        if (storePanel == null)
        {
            Debug.LogError("[Trade] storePanel is STILL null. Assign NpcStorePanelUI in inspector.");
            return;
        }

        if (inventoryUI == null)
        {
            Debug.LogError("[Trade] inventoryUI is null. Assign InventoryUIController in inspector.");
            return;
        }

        storePanel.transform.SetAsLastSibling();

        var npcName = def != null ? def.displayName : npcId;
        var finalStore = ResolveStoreRows(def, data, npcId);

        var rows = new List<NpcStorePanelUI.RowData>();

        foreach (var r in finalStore)
        {
            string takeItemId = r.takeItemId;
            int takeCount = r.takeCount;
            string giveItemId = r.giveItemId;
            int giveCount = r.giveCount;
            string buttonLabel = r.buttonLabel;

            rows.Add(new NpcStorePanelUI.RowData
            {
                takeItemId = takeItemId,
                takeCount = takeCount,
                giveItemId = giveItemId,
                giveCount = giveCount,
                buttonLabel = buttonLabel,
                onClick = () =>
                {
                    bool success = inventoryUI.TryTradeInventoryItems(
                        takeItemId,
                        takeCount,
                        giveItemId,
                        giveCount
                    );

                    if (success)
                    {
                        Debug.Log($"[Trade] success: {takeItemId} x{takeCount} -> {giveItemId} x{giveCount}");
                        if (GameManager.Instance != null)
                            GameManager.Instance.SaveNow();
                    }
                    else
                    {
                        Debug.LogWarning($"[Trade] failed: {takeItemId} x{takeCount} -> {giveItemId} x{giveCount}");
                    }
                }
            });
        }

        Debug.Log($"[Trade] Opening store panel. rows={rows.Count}, npcName={npcName}");

        storePanel.Open(npcName, rows, () =>
        {
            Debug.Log("[Trade] Store panel closed.");
            CloseAll();
        });
    }

    private void ApplyEffects(List<DialogueEffect> effects)
    {
        var data = GetData();
        if (data == null || effects == null) return;

        foreach (var e in effects)
            e?.Apply(data);

        Debug.Log($"[DialogueEffect] storyProgress={data.storyProgress}, test={data.test}");
    }

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