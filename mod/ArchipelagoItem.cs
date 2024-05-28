using Archipelago.MultiClient.Net.Enums;

namespace ArchipelagoRandomizer
{
    /// <summary>
    /// Defines the information related to an item
    /// </summary>
    public struct ArchipelagoItem
    {
        // more or less copied from the Tunic AP
        public string ItemName;
        public int PlayerSlot;
        public ItemFlags Flags;

        public ArchipelagoItem(string itemName, int playerSlot, ItemFlags flags)
        {
            ItemName = itemName;
            PlayerSlot = playerSlot;
            Flags = flags;
        }
    }
}
