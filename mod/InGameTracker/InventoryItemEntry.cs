using System;

namespace ArchipelagoRandomizer.InGameTracker
{
    public class InventoryItemEntry {
        /// <summary>
        /// Internal name of the item, should usually match Item.Itemtype.ToString() unless it doesn't exist as an item
        /// </summary>
        public string ID;
        /// <summary>
        /// Display name of the item as it shows in the inventory
        /// </summary>
        public string Name;
        /// <summary>
        /// The item in the Archipelago item pool, if any. Only null for pseudo-item entries like the OWV frequency.
        /// </summary>
        public Item? ApItem;
        /// <summary>
        /// If true, the item has been obtained since the last time the player has opened the console
        /// </summary>
        public bool ItemIsNew = false;
        /// <summary>
        /// If the item has been hinted, the location that it can be found
        /// </summary>
        public string HintedLocation = "";
        /// <summary>
        /// If the item has been hinted and entrance randomizer is enabled in the source world, this is the entrance that you need to use to find this item
        /// </summary>
        public string HintedEntrance = "";
        /// <summary>
        /// If the item has been hinted, the player that the item is in
        /// </summary>
        public string HintedWorld = "";

        public InventoryItemEntry(string id, string name, Item? apItem, bool itemIsNew = false, string hintedLocation = "", string hintedEntrance = "", string hintedWorld = "")
        {
            ID = id;
            Name = name;
            ApItem = apItem;
            ItemIsNew = itemIsNew;
            HintedLocation = hintedLocation;
            HintedEntrance = hintedEntrance;
            HintedWorld = hintedWorld;
        }

        public InventoryItemEntry(string id, string name)
        {
            ID = id;
            Name = name;
            ApItem = null;
        }

        public InventoryItemEntry(Item apItem, string name)
        {
            ID = apItem.ToString();
            Name = name;
            ApItem = apItem;
        }

        public bool HasOneOrMore()
        {
            // Fake items like the Outer Wilds Ventures frequency, which aren't randomized, should at the moment always return true here
            if (ApItem == null) return true;

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
