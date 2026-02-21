// DialogueGraphSO.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Dialogue/Dialogue Graph")]
public class DialogueGraphSO : ScriptableObject
{
    public string graphId;
    public string startNodeId;

    public List<DialoguePanelUI.DialogueNode> nodes;
}