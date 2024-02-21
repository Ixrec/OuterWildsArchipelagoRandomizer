using System;

namespace ArchipelagoRandomizer.InGameTracker
{
    public class InventoryItemEntry(string ID, string Name, bool IsAPItem = true, bool ItemIsNew = false, string HintedLocation = "", string HintedEntrance = "", string HintedWorld = "")
    {
        /// <summary>
        /// Internal name of the item, should usually match Item.Itemtype.ToString() unless it doesn't exist as an item
        /// </summary>
        public string ID = ID;
        /// <summary>
        /// Display name of the item as it shows in the inventory
        /// </summary>
        public string Name = Name;
        /// <summary>
        /// If true, the item is an item in the Archipelago item pool. Should be true for nearly everything.
        /// </summary>
        public bool IsAPItem = IsAPItem;
        /// <summary>
        /// If true, the item has been obtained since the last time the player has opened the console
        /// </summary>
        public bool ItemIsNew = ItemIsNew;
        /// <summary>
        /// If the item has been hinted, the location that it can be found
        /// </summary>
        public string HintedLocation = HintedLocation;
        /// <summary>
        /// If the item has been hinted and entrance randomizer is enabled in the source world, this is the entrance that you need to use to find this item
        /// </summary>
        public string HintedEntrance = HintedEntrance;
        /// <summary>
        /// If the item has been hinted, the player that the item is in
        /// </summary>
        public string HintedWorld = HintedWorld;

        public bool HasOneOrMore()
        {
            // Fake items like the Outer Wilds Ventures frequency, which aren't randomized, should at the moment always return true here
            if (!IsAPItem) return true;

            if (Enum.TryParse(ID, out Item result))
            {
                var ia = APRandomizer.SaveData.itemsAcquired;
                return ia.ContainsKey(result) ? ia[result] > 0 : false;
            }
            APRandomizer.OWMLModConsole.WriteLine($"Could not find item with ID {ID} for determining quantity, returning false.", OWML.Common.MessageType.Error);
            return false;
        }

        public void SetNew(bool isNew)
        {
            ItemIsNew = isNew;
        }

        public void SetHints(string location, string world, string entrance = "")
        {
            HintedLocation = location;
            HintedWorld = world;
            HintedEntrance = entrance;
        }
    }
}
