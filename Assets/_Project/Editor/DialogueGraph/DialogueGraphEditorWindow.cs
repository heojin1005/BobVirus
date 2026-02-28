using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueGraphEditorWindow : EditorWindow
{
    private DialogueGraphView graphView;
    private IMGUIContainer inspectorIMGUI;

    private DialogueGraphSO currentGraph;
    private int selectedNodeIndex = -1;

    [MenuItem("Tools/Dialogue Graph Editor")]
    public static void Open()
    {
        var window = GetWindow<DialogueGraphEditorWindow>();
        window.titleContent = new GUIContent("Dialogue Graph");
        window.Show();
    }

    private void OnEnable()
    {
        rootVisualElement.style.flexGrow = 1;

        // =========================
        // ✅ Toolbar 대체 UI
        // =========================
        var topBar = new VisualElement();
        topBar.style.flexDirection = FlexDirection.Row;
        topBar.style.height = 28;
        topBar.style.paddingLeft = 6;
        topBar.style.paddingRight = 6;
        topBar.style.alignItems = Align.Center;
        topBar.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);

        var addBtn = new Button(() => CreateNode())
        {
            text = "+ Node"
        };

        var refreshBtn = new Button(() => ReloadGraphPreserveView())
        {
            text = "Refresh"
        };

        topBar.Add(addBtn);
        topBar.Add(refreshBtn);

        rootVisualElement.Add(topBar);

        // =========================
        // Split View
        // =========================
        var split = new TwoPaneSplitView(0, 700, TwoPaneSplitViewOrientation.Horizontal);
        rootVisualElement.Add(split);

        graphView = new DialogueGraphView(this);
        graphView.style.flexGrow = 1;
        split.Add(graphView);

        inspectorIMGUI = new IMGUIContainer(DrawInspector);
        inspectorIMGUI.style.flexGrow = 1;
        inspectorIMGUI.style.paddingLeft = 8;
        inspectorIMGUI.style.paddingRight = 8;
        inspectorIMGUI.style.paddingTop = 6;
        split.Add(inspectorIMGUI);

        rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown);

        TryLoadFromSelection();
    }

    private void OnDisable()
    {
        rootVisualElement.UnregisterCallback<KeyDownEvent>(OnKeyDown);
    }

    private void OnSelectionChange()
    {
        TryLoadFromSelection();
    }

    private void TryLoadFromSelection()
    {
        var selected = Selection.activeObject as DialogueGraphSO;
        if (selected != null)
        {
            LoadGraph(selected);
        }
    }

    public void LoadGraph(DialogueGraphSO graph)
    {
        currentGraph = graph;
        selectedNodeIndex = -1;
        graphView.LoadGraph(graph);
        inspectorIMGUI?.MarkDirtyRepaint();
    }

    public void SelectNodeByIndex(int index)
    {
        selectedNodeIndex = index;
        inspectorIMGUI?.MarkDirtyRepaint();
    }

    private void CreateNode()
    {
        if (currentGraph == null)
        {
            EditorUtility.DisplayDialog("Dialogue Graph", "DialogueGraphSO를 먼저 선택해줘.", "OK");
            return;
        }

        Undo.RecordObject(currentGraph, "Create Dialogue Node");

        if (currentGraph.nodes == null)
            currentGraph.nodes = new System.Collections.Generic.List<DialoguePanelUI.DialogueNode>();

        var node = new DialoguePanelUI.DialogueNode
        {
            id = $"node_{currentGraph.nodes.Count + 1:000}",
            text = "New line...",
            nextId = "",
            editorPos = new Vector2(120, 120),
            choices = new System.Collections.Generic.List<DialoguePanelUI.DialogueChoice>(),
            enterEffects = new System.Collections.Generic.List<DialogueEffect>()
        };

        currentGraph.nodes.Add(node);

        EditorUtility.SetDirty(currentGraph);
        AssetDatabase.SaveAssets();

        ReloadGraphPreserveView();
        SelectNodeByIndex(currentGraph.nodes.Count - 1);
    }

    private void OnKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace)
        {
            DeleteSelectedNode();
            evt.StopPropagation();
        }
    }

    private void DeleteSelectedNode()
    {
        if (currentGraph == null) return;
        if (selectedNodeIndex < 0 || selectedNodeIndex >= currentGraph.nodes.Count) return;

        Undo.RecordObject(currentGraph, "Delete Dialogue Node");

        var removed = currentGraph.nodes[selectedNodeIndex];
        string removedId = removed.id;

        currentGraph.nodes.RemoveAt(selectedNodeIndex);

        foreach (var n in currentGraph.nodes)
        {
            if (n.nextId == removedId) n.nextId = "";
            if (n.choices != null)
            {
                foreach (var c in n.choices)
                    if (c.nextId == removedId) c.nextId = "";
            }
        }

        selectedNodeIndex = -1;

        EditorUtility.SetDirty(currentGraph);
        AssetDatabase.SaveAssets();

        ReloadGraphPreserveView();
    }

    private void ReloadGraphPreserveView()
    {
        if (graphView != null && currentGraph != null)
            graphView.ReloadPreserveView();
    }

    private void DrawInspector()
    {
        if (currentGraph == null)
        {
            EditorGUILayout.HelpBox("DialogueGraphSO를 선택하면 편집 가능", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("Graph Asset", EditorStyles.boldLabel);
        EditorGUILayout.ObjectField(currentGraph, typeof(DialogueGraphSO), false);

        if (selectedNodeIndex < 0 || selectedNodeIndex >= currentGraph.nodes.Count)
        {
            EditorGUILayout.HelpBox("왼쪽에서 노드를 선택해.", MessageType.None);
            return;
        }

        var so = new SerializedObject(currentGraph);
        so.Update();

        var nodesProp = so.FindProperty("nodes");
        var nodeProp = nodesProp.GetArrayElementAtIndex(selectedNodeIndex);

        EditorGUILayout.PropertyField(nodeProp, true);

        if (so.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(currentGraph);
            AssetDatabase.SaveAssets();
            ReloadGraphPreserveView();
        }
    }
}