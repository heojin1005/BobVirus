using System;
using System.Collections.Generic;
using UnityEngine;

public class DialogueGraphRunner : MonoBehaviour, IDialogueGraphRunner
{
    [SerializeField] private DialoguePanelUI dialoguePanel;

    public void Play(DialogueGraphSO graph, Action<List<DialogueEffect>> onEffect, Action onEnd)
    {
        if (graph == null)
        {
            Debug.LogError("[DialogueGraphRunner] graph is null.");
            onEnd?.Invoke();
            return;
        }

        if (dialoguePanel == null)
        {
            Debug.LogError("[DialogueGraphRunner] dialoguePanel is not assigned.");
            onEnd?.Invoke();
            return;
        }

        if (graph.nodes == null || graph.nodes.Count == 0)
        {
            Debug.LogWarning($"[DialogueGraphRunner] graph '{graph.name}' has no nodes.");
            onEnd?.Invoke();
            return;
        }

        // If startNodeId is empty, try a safe fallback:
        string startId = graph.startNodeId;
        if (string.IsNullOrEmpty(startId))
        {
            // pick first valid node id
            for (int i = 0; i < graph.nodes.Count; i++)
            {
                if (graph.nodes[i] != null && !string.IsNullOrEmpty(graph.nodes[i].id))
                {
                    startId = graph.nodes[i].id;
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(startId))
        {
            Debug.LogError($"[DialogueGraphRunner] graph '{graph.name}' has no valid start node id.");
            onEnd?.Invoke();
            return;
        }

        // ✅ IMPORTANT: Use nodeId-based mode (this is what supports choices & nextId correctly)
        dialoguePanel.OpenByNodeId(
            nodes: graph.nodes,
            startNodeId: startId,
            applyEffectsCallback: onEffect,
            onEndCallback: onEnd
        );
    }
}