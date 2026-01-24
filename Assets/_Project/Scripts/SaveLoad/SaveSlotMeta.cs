using System;

[Serializable]
public class SaveSlotMeta
{
    public int slotIndex;
    public bool exists;
    public string displayName;
    public long savedAtUnix;
}
