using Archipelago.MultiClient.Net.Enums;

namespace ArchipelagoRandomizer
{
    /// <summary>
    /// Defines the information related to an item
    /// </summary>
    public struct ArchipelagoItem
    {
        public long ItemId;
        public string ItemName;
        public int PlayerSlot;
        public ItemFlags Flags;

        public ArchipelagoItem(long itemId, string itemName, int playerSlot, ItemFlags flags)
        {
            ItemId = itemId;
            ItemName = itemName;
            PlayerSlot = playerSlot;
            Flags = flags;
        }
    }
}
