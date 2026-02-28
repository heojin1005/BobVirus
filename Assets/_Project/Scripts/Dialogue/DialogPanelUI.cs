using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialoguePanelUI : MonoBehaviour
{
    // =========================
    // (구버전 호환용) index 기반
    // =========================
    [System.Serializable]
    public class Choice
    {
        public string text;
        public int nextIndex; // -1이면 종료
        public List<DialogueEffect> effects; // 선택 시 실행
    }

    [System.Serializable]
    public class Step
    {
        [TextArea] public string text;
        public List<Choice> choices;
        public List<DialogueEffect> effects; // Step 진입 시 실행
    }

    // =========================
    // ✅ nodeId 기반(최종)
    // =========================
    [System.Serializable]
    public class DialogueChoice
    {
        public string text;
        public string nextId; // 비어있으면 종료
        public List<DialogueEffect> effects; // 선택 시 실행
    }

    [System.Serializable]
    public class DialogueNode
    {
        public string id;

        [TextArea] public string text;

        // Next 상태일 때 이동
        public string nextId;

        // Choices 상태일 때
        public List<DialogueChoice> choices;

        // ✅ 노드 진입 시 실행
        public List<DialogueEffect> enterEffects;
        public Vector2 editorPos = new Vector2(100, 100);
    }

    [Header("NPC Header (TopLeftNPC)")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TextMeshProUGUI npcNameText;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI contentText;

    [Header("Next (State 1)")]
    [SerializeField] private Button nextButton;

    [Header("Choices (State 2)")]
    [SerializeField] private Transform choicesRoot;
    [SerializeField] private Button choiceButtonPrefab;

    private System.Action<List<DialogueEffect>> applyEffects;
    private System.Action onEnd;

    // 구버전 상태
    private List<Step> steps;
    private int currentIndex;

    // 신버전 상태
    private Dictionary<string, DialogueNode> nodeMap;
    private string currentNodeId;

    // =========================
    // ✅ Header API
    // =========================
    public void SetNpcHeader(Sprite portrait, string displayName)
    {
        if (portraitImage != null)
        {
            portraitImage.sprite = portrait;
            portraitImage.enabled = (portrait != null);
        }

        if (npcNameText != null)
        {
            npcNameText.text = displayName ?? "";
        }
    }

    public void ClearNpcHeader()
    {
        if (portraitImage != null)
        {
            portraitImage.sprite = null;
            portraitImage.enabled = false;
        }

        if (npcNameText != null)
        {
            npcNameText.text = "";
        }
    }

    // -------------------------
    // (구버전) index 기반 Open
    // -------------------------
    public void Open(List<Step> dialogueSteps, System.Action<List<DialogueEffect>> applyEffectsCallback, System.Action onEndCallback)
    {
        // nodeId 모드 해제
        nodeMap = null;
        currentNodeId = null;

        steps = dialogueSteps;
        currentIndex = 0;

        applyEffects = applyEffectsCallback;
        onEnd = onEndCallback;

        gameObject.SetActive(true);

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextClicked_Index);
        }

        if (steps == null || steps.Count == 0)
        {
            EndDialogue();
            return;
        }

        RenderIndex();
    }

    // -------------------------
    // ✅ nodeId 기반 Open
    // -------------------------
    public void OpenByNodeId(List<DialogueNode> nodes, string startNodeId, System.Action<List<DialogueEffect>> applyEffectsCallback, System.Action onEndCallback)
    {
        // index 모드 해제
        steps = null;
        currentIndex = 0;

        applyEffects = applyEffectsCallback;
        onEnd = onEndCallback;

        nodeMap = BuildNodeMap(nodes);
        currentNodeId = startNodeId;

        gameObject.SetActive(true);

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextClicked_Node);
        }

        if (nodeMap == null || nodeMap.Count == 0)
        {
            EndDialogue();
            return;
        }

        GoTo(currentNodeId);
    }

    public void Close()
    {
        ClearChoices();
        ClearNpcHeader(); // ✅ 잔상 방지
        gameObject.SetActive(false);
    }

    // =========================
    // index 기반 로직
    // =========================
    private void OnNextClicked_Index()
    {
        GoTo(currentIndex + 1);
    }

    private void OnChoiceClicked_Index(Choice choice)
    {
        if (choice != null && choice.effects != null && choice.effects.Count > 0)
            applyEffects?.Invoke(choice.effects);

        if (choice == null || choice.nextIndex < 0)
        {
            EndDialogue();
            return;
        }

        GoTo(choice.nextIndex);
    }

    private void GoTo(int index)
    {
        currentIndex = index;

        if (steps == null || currentIndex < 0 || currentIndex >= steps.Count)
        {
            EndDialogue();
            return;
        }

        RenderIndex();
    }

    private void RenderIndex()
    {
        ClearChoices();

        Step step = steps[currentIndex];

        if (step.effects != null && step.effects.Count > 0)
            applyEffects?.Invoke(step.effects);

        if (contentText != null)
            contentText.text = step.text ?? "";

        bool hasChoices = step.choices != null && step.choices.Count > 0 && choicesRoot != null && choiceButtonPrefab != null;

        if (nextButton != null)
            nextButton.gameObject.SetActive(!hasChoices);

        if (choicesRoot != null)
            choicesRoot.gameObject.SetActive(hasChoices);

        if (hasChoices)
        {
            foreach (var c in step.choices)
            {
                var btn = Instantiate(choiceButtonPrefab, choicesRoot);

                var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = c.text;

                btn.onClick.RemoveAllListeners();
                Choice captured = c;
                btn.onClick.AddListener(() => OnChoiceClicked_Index(captured));
            }
        }
    }

    // =========================
    // nodeId 기반 로직
    // =========================
    private void OnNextClicked_Node()
    {
        if (nodeMap == null) { EndDialogue(); return; }

        if (string.IsNullOrEmpty(currentNodeId) || !nodeMap.TryGetValue(currentNodeId, out var node) || node == null)
        {
            EndDialogue();
            return;
        }

        if (string.IsNullOrEmpty(node.nextId))
        {
            EndDialogue();
            return;
        }

        GoTo(node.nextId);
    }

    private void OnChoiceClicked_Node(DialogueChoice choice)
    {
        if (choice != null && choice.effects != null && choice.effects.Count > 0)
            applyEffects?.Invoke(choice.effects);

        if (choice == null || string.IsNullOrEmpty(choice.nextId))
        {
            EndDialogue();
            return;
        }

        GoTo(choice.nextId);
    }

    private void GoTo(string nodeId)
    {
        currentNodeId = nodeId;

        if (nodeMap == null || string.IsNullOrEmpty(currentNodeId) || !nodeMap.TryGetValue(currentNodeId, out var node) || node == null)
        {
            EndDialogue();
            return;
        }

        RenderNode(node);
    }

    private void RenderNode(DialogueNode node)
    {
        ClearChoices();

        if (node.enterEffects != null && node.enterEffects.Count > 0)
            applyEffects?.Invoke(node.enterEffects);

        if (contentText != null)
            contentText.text = node.text ?? "";

        bool hasChoices = node.choices != null && node.choices.Count > 0 && choicesRoot != null && choiceButtonPrefab != null;

        if (nextButton != null)
            nextButton.gameObject.SetActive(!hasChoices);

        if (choicesRoot != null)
            choicesRoot.gameObject.SetActive(hasChoices);

        if (hasChoices)
        {
            foreach (var c in node.choices)
            {
                var btn = Instantiate(choiceButtonPrefab, choicesRoot);

                var txt = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.text = c.text;

                btn.onClick.RemoveAllListeners();
                DialogueChoice captured = c;
                btn.onClick.AddListener(() => OnChoiceClicked_Node(captured));
            }
        }
    }

    private Dictionary<string, DialogueNode> BuildNodeMap(List<DialogueNode> nodes)
    {
        if (nodes == null) return null;

        var map = new Dictionary<string, DialogueNode>();
        foreach (var n in nodes)
        {
            if (n == null) continue;

            if (string.IsNullOrEmpty(n.id))
            {
                Debug.LogWarning("DialoguePanelUI: Node has empty id. Skipping.");
                continue;
            }

            if (map.ContainsKey(n.id))
            {
                Debug.LogWarning($"DialoguePanelUI: Duplicate node id '{n.id}'. Keeping first.");
                continue;
            }

            map.Add(n.id, n);
        }
        return map;
    }

    private void ClearChoices()
    {
        if (choicesRoot == null) return;
        for (int i = choicesRoot.childCount - 1; i >= 0; i--)
            Destroy(choicesRoot.GetChild(i).gameObject);
    }

    private void EndDialogue()
    {
        onEnd?.Invoke();
    }
}