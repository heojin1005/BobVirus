using System;
using UnityEngine;

// 대화 이후 실행할 함수 내역이다.
public enum DialogueEffectType
{
    StoryProgressAdd,
    StoryProgressSet,
    TestAdd,
    TestSet,
    TalkChange,
    QuestChange,
    ShowDebug,
}

[Serializable]
public class DialogueEffect
{
    public DialogueEffectType type;

    [Header("Int Param (Story/Test 등)")]
    public int intValue;

    [Header("Debug Message")]
    [TextArea]
    public string stringValue;

    [Header("Target Object Name (WorldDebugLabel이 붙어있는 오브젝트 이름)")]
    public string debugObject;

    [Header("노드를 바꿔 줄 대상 이름")]
    public string objectName;

    [Header("바꿀 대화 노드 이름")]
    public string NodeName;

    [Header("바꿀 퀘스트 노드 이름")]
    public string QuestNode;
    public void Apply(SaveGameData data)
    {
        if (data == null) return;

        switch (type)
        {
            case DialogueEffectType.StoryProgressAdd:
                data.storyProgress += intValue;
                break;

            case DialogueEffectType.StoryProgressSet:
                data.storyProgress = intValue;
                break;

            case DialogueEffectType.TestAdd:
                data.test += intValue;
                break;

            case DialogueEffectType.TestSet:
                data.test = intValue;
                break;

            case DialogueEffectType.TalkChange:
            {
                var o = data.EnsureNpcOverride(objectName);
                o.talkGraphId = NodeName;
                GameManager.Instance.SaveNow();
                break;
            }

            case DialogueEffectType.QuestChange:
            {
                var o = data.EnsureNpcOverride(objectName);
                o.questGraphId = QuestNode;
                GameManager.Instance.SaveNow();       
                break; 
            }
            case DialogueEffectType.ShowDebug:
            {
                if (string.IsNullOrEmpty(debugObject))
                {
                    Debug.LogWarning("ShowDebug: debugObject is empty.");
                    return;
                }
                GameObject target = GameObject.Find(debugObject);
                if (target == null)
                {
                    Debug.LogWarning($"ShowDebug: GameObject '{debugObject}' not found.");
                    return;
                }
                var label = target.GetComponent<WorldDebugLabel>();
                if (label == null)
                {
                    Debug.LogWarning($"ShowDebug: WorldDebugLabel not found on '{debugObject}'.");
                    return;
                }
                label.Show(
                    string.IsNullOrEmpty(stringValue) ? "(Debug)" : stringValue,
                    2f
                );
                break;
            }
        }
    }
}