using System;
using System.Collections.Generic;
using UnityEngine;
using UI;

// 대화 이후 실행할 함수 내역이다.
public enum DialogueEffectType
{
    StoryProgressAdd,
    StoryProgressSet,
    TestAdd,
    TestSet,
    TalkChange,
    QuestChange,
    StoreAdd,
    ShowDebug,
    Give,
}

[Serializable]
public class DialogueEffect
{
    public DialogueEffectType type;

    [Header("Int Param (Story/Test 등)")] //storyprogressadd, storyprogressset에서 사용
    public int intValue;

    [Header("Debug Message")] //showdebug에서 사용
    [TextArea]
    public string stringValue;

    [Header("Target Object Name (WorldDebugLabel이 붙어있는 오브젝트 이름)")] //showdebug에서 사용
    public string debugObject;

    [Header("노드를 바꿔 줄 대상 이름")] //talkchange, questchange에서 사용
    public string objectName;

    [Header("바꿀 대화 노드 이름")] //talkchange에서 사용
    public string NodeName;

    [Header("바꿀 퀘스트 노드 이름")] //questchange에서 사용
    public string QuestNode;

    [Header("Store Add Params")] //storeadd에서 사용
    public string storeTakeItemId;
    public int storeTakeCount = 1;

    public string storeGiveItemId;
    public int storeGiveCount = 1;
    [Header("아이템 교환(대화 혹은 퀘스트에서)")] //give에서 사용
    public InventoryUIController inventoryUI;
    public string takeItemId;
    public int takeCount = 1;
    public string giveItemId;
    public int giveCount = 1;

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

            case DialogueEffectType.StoreAdd:
            {
                if (string.IsNullOrEmpty(objectName))
                {
                    Debug.LogWarning("StoreAdd: objectName is empty.");
                    return;
                }

                if (string.IsNullOrEmpty(storeTakeItemId))
                {
                    Debug.LogWarning("StoreAdd: storeTakeItemId is empty.");
                    return;
                }

                if (string.IsNullOrEmpty(storeGiveItemId))
                {
                    Debug.LogWarning("StoreAdd: storeGiveItemId is empty.");
                    return;
                }

                var o = data.EnsureNpcOverride(objectName);
                if (o == null)
                {
                    Debug.LogWarning($"StoreAdd: failed to get override for npc '{objectName}'.");
                    return;
                }

                List<SaveGameData.NpcStoreEntryData> merged = null;

                // 이미 override가 있으면 그걸 기준으로 추가
                if (o.storeList != null && o.storeList.Count > 0)
                {
                    merged = new List<SaveGameData.NpcStoreEntryData>(o.storeList);
                }
                else
                {
                    // override가 없으면 기본 storeList를 복사해서 시작
                    merged = new List<SaveGameData.NpcStoreEntryData>();

                    var npcDb = Resources.FindObjectsOfTypeAll<NpcDatabaseSO>();
                    if (npcDb != null)
                    {
                        foreach (var db in npcDb)
                        {
                            if (db == null || db.npcs == null) continue;

                            foreach (var npc in db.npcs)
                            {
                                if (npc == null) continue;
                                if (npc.npcId != objectName) continue;

                                if (npc.storeList != null)
                                {
                                    foreach (var row in npc.storeList)
                                    {
                                        if (row == null) continue;

                                        merged.Add(new SaveGameData.NpcStoreEntryData
                                        {
                                            takeItemId = row.takeItemId,
                                            takeCount = row.takeCount,
                                            giveItemId = row.giveItemId,
                                            giveCount = row.giveCount,
                                            buttonLabel = row.buttonLabel
                                        });
                                    }
                                }
                                goto STORE_FOUND;
                            }
                        }
                    }

                STORE_FOUND:
                    ;
                }

                merged.Add(new SaveGameData.NpcStoreEntryData
                {
                    takeItemId = storeTakeItemId,
                    takeCount = Mathf.Max(1, storeTakeCount),
                    giveItemId = storeGiveItemId,
                    giveCount = Mathf.Max(1, storeGiveCount),
                    buttonLabel = "change"
                });

                o.storeList = merged;
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
            case DialogueEffectType.Give:
                {
                    if (inventoryUI == null)
                    inventoryUI = UnityEngine.Object.FindFirstObjectByType<InventoryUIController>(FindObjectsInactive.Include);
                    if (inventoryUI == null)
                    {
                        
                        Debug.LogError("[InventoryTradeButton] inventoryUI is null.");
                        return;
                    }

                    bool success = inventoryUI.TryTradeInventoryItems(
                        takeItemId,
                        takeCount,
                        giveItemId,
                        giveCount
                    );

                    if (!success)
                    {
                        Debug.LogWarning($"[InventoryTradeButton] trade failed: {takeItemId} x{takeCount} -> {giveItemId} x{giveCount}");
                        return;
                    }

                    Debug.Log($"[InventoryTradeButton] trade success: {takeItemId} x{takeCount} -> {giveItemId} x{giveCount}");
                
                    break;
                }
        }
    }
}