using System;
using System.Collections.Generic;
using System.Linq;

namespace ArchipelagoRandomizer.InGameTracker
{
    public class InventoryItemHint
    {
        /// <summary>
        /// The location where it can be found
        /// </summary>
        public string Location;
        /// <summary>
        /// If entrance randomizer is enabled in the source world, this is the entrance that you need to use to find this item
        /// </summary>
        public string Entrance;
        /// <summary>
        /// The player/slot whose world the item is in
        /// </summary>
        public string World;
    }

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
        /// If the item has been hinted, any hints for not-yet-found instances will be added here
        /// </summary>
        public List<InventoryItemHint> Hints = new();

        public InventoryItemEntry(string id, string name, Item? apItem, bool itemIsNew = false)
        {
            ID = id;
            Name = name;
            ApItem = apItem;
            ItemIsNew = itemIsNew;
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
            APRandomizer.OWMLWriteLine($"Could not find item with ID {ID} for determining quantity, returning false.", OWML.Common.MessageType.Error);
            return false;
        }

        public void SetNew(bool isNew)
        {
            ItemIsNew = isNew;
        }

        public void AddHint(string location, string world, string entrance = "")
        {
            if (Hints.Any(h => h.Location == location && h.World == world))
                return; // we've received this hint before, don't duplicate it

            Hints.Add(new InventoryItemHint { Location = location, World = world, Entrance = entrance });
        }
    }
}
