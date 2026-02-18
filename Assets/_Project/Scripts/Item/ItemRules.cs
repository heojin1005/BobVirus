public static class ItemRules
{
    public static bool IsEmpty(string id) => string.IsNullOrEmpty(id);

    /// <summary>
    /// itemId가 특정 장착 슬롯(slot)에 들어갈 수 있는지 검사
    /// </summary>
    public static bool CanEquip(ItemDatabase db, string itemId, EquipSlotType slot)
    {
        if (db == null) return false;
        if (slot == EquipSlotType.None) return false;
        if (IsEmpty(itemId)) return false;

        return db.GetEquipSlotOrNone(itemId) == slot;
    }
}
