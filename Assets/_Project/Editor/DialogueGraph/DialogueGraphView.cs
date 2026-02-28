using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueGraphView : GraphView
{
    private readonly DialogueGraphEditorWindow ownerWindow;
    private DialogueGraphSO currentGraph;

    // 뷰 유지용
    private Vector3 savedPosition;
    private Vector3 savedScale;

    public DialogueGraphView(DialogueGraphEditorWindow owner)
    {
        ownerWindow = owner;

        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        style.flexGrow = 1;

        graphViewChanged = OnGraphViewChanged;
    }

    public void LoadGraph(DialogueGraphSO graph)
    {
        currentGraph = graph;

        DeleteElements(graphElements.ToList());
        if (currentGraph == null || currentGraph.nodes == null) return;

        var nodeViews = new Dictionary<string, DialogueNodeView>();

        // 노드 생성
        for (int i = 0; i < currentGraph.nodes.Count; i++)
        {
            var node = currentGraph.nodes[i];
            if (node == null || string.IsNullOrEmpty(node.id)) continue;

            var view = new DialogueNodeView(node, currentGraph, i, ownerWindow);
            view.SetPosition(new Rect(node.editorPos, new Vector2(260, 160)));

            AddElement(view);
            nodeViews[node.id] = view;
        }

        // 엣지 생성 (Next + Choices)
        foreach (var node in currentGraph.nodes)
        {
            if (node == null || string.IsNullOrEmpty(node.id)) continue;
            if (!nodeViews.TryGetValue(node.id, out var fromView)) continue;

            if (!string.IsNullOrEmpty(node.nextId) && nodeViews.TryGetValue(node.nextId, out var toView))
            {
                AddElement(fromView.NextOutput.ConnectTo(toView.InputPort));
            }

            if (node.choices != null)
            {
                for (int ci = 0; ci < node.choices.Count; ci++)
                {
                    var ch = node.choices[ci];
                    if (ch == null) continue;

                    if (!string.IsNullOrEmpty(ch.nextId) && nodeViews.TryGetValue(ch.nextId, out var target))
                    {
                        if (fromView.TryGetChoiceOutput(ci, out var choicePort))
                        {
                            AddElement(choicePort.ConnectTo(target.InputPort));
                        }
                    }
                }
            }
        }
    }

    public void ReloadPreserveView()
    {
        SaveView();
        LoadGraph(currentGraph);
        RestoreView();
    }

    private void SaveView()
    {
        savedPosition = viewTransform.position;
        savedScale = viewTransform.scale;
    }

    private void RestoreView()
    {
        viewTransform.position = savedPosition;
        viewTransform.scale = savedScale;
    }

    // =========================
    // Edge 연결/해제 -> 데이터 반영
    // =========================
    private GraphViewChange OnGraphViewChanged(GraphViewChange change)
    {
        if (currentGraph == null) return change;

        if (change.edgesToCreate != null)
        {
            foreach (var edge in change.edgesToCreate)
                ApplyConnect(edge);
        }

        if (change.elementsToRemove != null)
        {
            foreach (var elem in change.elementsToRemove)
            {
                if (elem is Edge edge)
                    ApplyDisconnect(edge);
            }
        }

        return change;
    }

    private void ApplyConnect(Edge edge)
    {
        var outNode = edge.output?.node as DialogueNodeView;
        var inNode = edge.input?.node as DialogueNodeView;
        if (outNode == null || inNode == null) return;

        string key = edge.output.name; // "NEXT" or "CHOICE_0"

        Undo.RecordObject(currentGraph, "Connect Dialogue Edge");

        if (key == "NEXT")
        {
            outNode.Data.nextId = inNode.Data.id;
        }
        else if (key.StartsWith("CHOICE_"))
        {
            int idx = ParseChoiceIndex(key);
            if (idx >= 0 && outNode.Data.choices != null && idx < outNode.Data.choices.Count)
            {
                outNode.Data.choices[idx].nextId = inNode.Data.id;
            }
        }

        EditorUtility.SetDirty(currentGraph);
        AssetDatabase.SaveAssets();
    }

    private void ApplyDisconnect(Edge edge)
    {
        var outNode = edge.output?.node as DialogueNodeView;
        var inNode = edge.input?.node as DialogueNodeView;
        if (outNode == null || inNode == null) return;

        string key = edge.output.name;

        Undo.RecordObject(currentGraph, "Disconnect Dialogue Edge");

        if (key == "NEXT")
        {
            if (outNode.Data.nextId == inNode.Data.id)
                outNode.Data.nextId = "";
        }
        else if (key.StartsWith("CHOICE_"))
        {
            int idx = ParseChoiceIndex(key);
            if (idx >= 0 && outNode.Data.choices != null && idx < outNode.Data.choices.Count)
            {
                if (outNode.Data.choices[idx].nextId == inNode.Data.id)
                    outNode.Data.choices[idx].nextId = "";
            }
        }

        EditorUtility.SetDirty(currentGraph);
        AssetDatabase.SaveAssets();
    }

    private int ParseChoiceIndex(string key)
    {
        var parts = key.Split('_');
        if (parts.Length != 2) return -1;
        return int.TryParse(parts[1], out int idx) ? idx : -1;
    }
}