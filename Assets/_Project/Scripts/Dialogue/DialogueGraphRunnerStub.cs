using System;
using System.Collections.Generic;
using UnityEngine;

public class DialogueGraphRunnerStub : MonoBehaviour, IDialogueGraphRunner
{
    [SerializeField] private DialoguePanelUI dialoguePanel;

    public void Play(DialogueGraphSO graph, Action<List<DialogueEffect>> onEffect, Action onEnd)
    {
        Debug.LogWarning($"[DialogueGraphRunnerStub] Runner is not implemented yet. graph={(graph != null ? graph.name : "null")}");

        // 임시로 "Runner 미구현" 한 줄만 띄우고 종료되게 해둠 (테스트 대사처럼 헷갈리게 안 함)
        if (dialoguePanel == null)
        {
            Debug.LogError("[DialogueGraphRunnerStub] dialoguePanel is not assigned.");
            onEnd?.Invoke();
            return;
        }

        var steps = new List<DialoguePanelUI.Step>
        {
            new DialoguePanelUI.Step { text = $"(Runner 미구현) graph: {graph.name}" },
            new DialoguePanelUI.Step { text = "DialogueGraphRunner를 구현하면 여기에 실제 그래프 대사가 나옵니다." },
        };

        dialoguePanel.Open(steps, _ => { }, () => { onEnd?.Invoke(); });
    }
}