using System;
using System.Collections.Generic;
using System.Linq;

namespace ArchipelagoRandomizer.InGameTracker;

public enum TrackerCategory
{
    All,
    Goal,
    HourglassTwins,
    TimberHearth,
    BrittleHollow,
    GiantsDeep,
    DarkBramble,
    OuterWilds,
    Stranger,
    Dreamworld,
    // story mod categories start here
    HearthsNeighbor,
    TheOutsider,
    AstralCodec,
    HearthsNeighbor2Magistarium,
    FretsQuest,
}

public class TrackerRequirement
{
    public string item;
    public string location;
    public string region;
    public List<TrackerRequirement> anyOf;
}

/// <summary>
/// Class for parsing all information in connections.jsonc
/// Should support all info in it, but may need to be updated as logic becomes more complex
/// </summary>
public class TrackerConnectionData
{
    public string? category;
    public string from;
    public string to;
    public List<TrackerRequirement> requires;
}

/// <summary>
/// Class for parsing information in locations.jsonc.
/// Should support all info currently in it, but this may have to be changed as logic grows more complex.
/// </summary>
public class TrackerLocationData
{
    public string? category;
    public bool? logsanity;
    public int? address;
    public string name;
    public string region;
    public List<TrackerRequirement> requires;
}

public class TrackerChecklistData(bool isAccessible, bool hasBeenChecked, string hintText)
{
    /// <summary>
    /// Whether the item has ever been accessible
    /// </summary>
    public bool isAccessible = isAccessible;
    /// <summary>
    /// Whether the location has been checked already
    /// </summary>
    public bool hasBeenChecked = hasBeenChecked;
    /// <summary>
    /// If a hint says something is here, this is the name of the item
    /// </summary>
    public string hintText = hintText;

    public void SetAccessible(bool access)
    {
        isAccessible = access;
    }
}

/// <summary>
/// Contains info for the tracker including the Ship Log image and description
/// </summary>
public struct TrackerInfo
{
    /// <summary>
    /// Location name as displayed in Location.cs. Will be null if this is the goal instead of an AP location.
    /// </summary>
    public string locationModID;
    /// <summary>
    /// Text shown to the player describing where the item is
    /// </summary>
    public string description;
    /// <summary>
    /// Explore Fact card picture showing the general location of the item
    /// </summary>
    public string thumbnail;
    public bool isDLCOnly;
}

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

public class InventoryItemEntry
{
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
    public bool IsDLCOnly = false;
    public string? StoryModOption = null;

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

    public InventoryItemEntry(Item apItem, string name, bool isDLCOnly = false, string storyModOption = null)
    {
        ID = apItem.ToString();
        Name = name;
        ApItem = apItem;
        IsDLCOnly = isDLCOnly;
        StoryModOption = storyModOption;
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

    public void AddHint(string location, string world, string entrance = "")
    {
        if (Hints.Any(h => h.Location == location && h.World == world))
            return; // we've received this hint before, don't duplicate it

        Hints.Add(new InventoryItemHint { Location = location, World = world, Entrance = entrance });
    }
}
