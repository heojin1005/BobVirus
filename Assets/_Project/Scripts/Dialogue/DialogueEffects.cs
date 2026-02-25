using System;
using UnityEngine;
//대화 이후 실행할 함수 내역이다.
public enum DialogueEffectType
{
    StoryProgressAdd,
    StoryProgressSet,
    TestAdd,
    TestSet,
    TalkChange,
}

[Serializable]
public class DialogueEffect
{
    public DialogueEffectType type;
    public int intValue;

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
                var o = data.EnsureNpcOverride("detective");
                o.talkGraphId = "detectiveTalk2";
                GameManager.Instance.SaveNow();
                break;
        }
    }
}