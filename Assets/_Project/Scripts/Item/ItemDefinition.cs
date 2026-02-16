using UnityEngine;

[CreateAssetMenu(menuName = "Game/Items/Item Definition", fileName = "Item_")]
public class ItemDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("SaveGameData에 저장되는 고유 ID (문자열). 예: helmet_iron")]
    public string id;

    [Header("Visual")]
    public Sprite icon;

    [Header("Equip Rules")]
    [Tooltip("장착 불가 아이템은 None")]
    public EquipSlotType equipSlot = EquipSlotType.None;

    [Header("Optional Display Info")]
    public string displayName;
    [TextArea] public string description;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 실수 방지: 공백/앞뒤 스페이스 제거
        if (!string.IsNullOrEmpty(id))
            id = id.Trim();
    }
#endif
}
