using System;
using UnityEngine;

public interface IDialogueGraphRunner
{
    /// <summary>
    /// graph를 실제로 실행해서 UI(DialoguePanelUI)에 띄운다.
    /// onEffect: 노드/선택지에서 발생한 효과를 SaveGameData에 반영할 때 호출
    /// onEnd: 대화 종료 시 호출
    /// </summary>
    void Play(DialogueGraphSO graph, Action<System.Collections.Generic.List<DialogueEffect>> onEffect, Action onEnd);
}