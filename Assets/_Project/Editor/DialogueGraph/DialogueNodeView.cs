using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueNodeView : Node
{
    public DialoguePanelUI.DialogueNode Data { get; private set; }
    private readonly DialogueGraphSO ownerGraph;
    private readonly int nodeIndex;
    private readonly DialogueGraphEditorWindow ownerWindow;

    public Port InputPort { get; private set; }
    public Port NextOutput { get; private set; }

    private readonly List<Port> choiceOutputs = new();
    private Label previewLabel;

    public DialogueNodeView(
        DialoguePanelUI.DialogueNode data,
        DialogueGraphSO graph,
        int index,
        DialogueGraphEditorWindow window)
    {
        Data = data;
        ownerGraph = graph;
        nodeIndex = index;
        ownerWindow = window;

        title = string.IsNullOrEmpty(Data.id) ? "(no id)" : Data.id;

        // 클릭 시 우측 패널 선택 반영
        this.RegisterCallback<MouseDownEvent>(evt =>
        {
            if (evt.button == 0) // left click
                ownerWindow.SelectNodeByIndex(nodeIndex);
        });

        // Input
        InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
        InputPort.name = "IN";
        InputPort.portName = "IN";
        inputContainer.Add(InputPort);

        // Next Output
        NextOutput = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
        NextOutput.name = "NEXT";       // ✅ 식별키
        NextOutput.portName = "NEXT";   // ✅ 표시
        outputContainer.Add(NextOutput);

        RebuildChoicePorts();

        previewLabel = new Label(PreviewText(Data.text));
        previewLabel.style.whiteSpace = WhiteSpace.Normal;
        previewLabel.style.unityTextAlign = TextAnchor.UpperLeft;
        mainContainer.Add(previewLabel);

        RefreshExpandedState();
        RefreshPorts();
    }

    private string PreviewText(string text)
    {
        if (string.IsNullOrEmpty(text)) return "(empty)";
        text = text.Replace("\n", " ");
        return text.Length <= 60 ? text : text.Substring(0, 60) + "...";
    }

    private void RebuildChoicePorts()
    {
        foreach (var p in choiceOutputs)
            outputContainer.Remove(p);
        choiceOutputs.Clear();

        if (Data.choices == null) return;

        for (int i = 0; i < Data.choices.Count; i++)
        {
            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            port.name = $"CHOICE_{i}"; // ✅ 식별키

            string label = Data.choices[i] != null ? Data.choices[i].text : "(null)";
            port.portName = $"CHOICE_{i}: {label}"; // ✅ 표시 라벨

            choiceOutputs.Add(port);
            outputContainer.Add(port);
        }
    }

    public bool TryGetChoiceOutput(int index, out Port port)
    {
        port = null;
        if (index < 0 || index >= choiceOutputs.Count) return false;
        port = choiceOutputs[index];
        return true;
    }

    public override void SetPosition(Rect newPos)
    {
        base.SetPosition(newPos);

        if (ownerGraph == null || Data == null) return;

        Undo.RecordObject(ownerGraph, "Move Dialogue Node");
        Data.editorPos = newPos.position;
        EditorUtility.SetDirty(ownerGraph);
    }
}