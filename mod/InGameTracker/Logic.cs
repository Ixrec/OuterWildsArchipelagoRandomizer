using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Collections.ObjectModel;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Newtonsoft.Json.Linq;

namespace ArchipelagoRandomizer.InGameTracker;

public class TrackerRegionData(string name)
{
    public List<TrackerConnectionData> fromConnections = new();
    public List<TrackerConnectionData> toConnections = new();

    public List<List<TrackerRequirement>> requirements = new();
    public string name = name;
}

/// <summary>
/// Helper class for managing tracker accessibility displays
/// </summary>
public class Logic
{
    /// <summary>
    /// List of items the player has obtained prior to receiving a new item
    /// </summary>
    public ReadOnlyCollection<ItemInfo> previouslyObtainedItems;

    /// <summary>
    /// Parsed version of locations.jsonc
    /// </summary>
    public Dictionary<string, TrackerLocationData> TrackerLocations;

    public Dictionary<string, TrackerRegionData> StaticTrackerRegions;
    /// <summary>
    /// List of all locations with their connections
    /// </summary>
    public Dictionary<string, TrackerRegionData> TrackerRegions;

    private Dictionary<Item, uint> ItemsCollected;
    private Dictionary<string, bool> CanAccessRegion; // not guaranteed to contain region: false entries, always use TryGetValue()

    private TrackerManager tracker;

    // Ember Twin, Ash Twin, Cave Twin, Tower Twin
    private readonly string[] HGTPrefixes = ["ET", "AT", "CT", "TT"];
    // Timber Hearth, Attlerock, Timber Moon
    private readonly string[] THPrefixes = ["TH", "AR", "TM"];
    // Brittle Hollow, Hollow's Lantern, Volcano Moon
    private readonly string[] BHPrefixes = ["BH", "HL", "VM"];
    // Giant's Deep, Orbital Probe Cannon x2
    private readonly string[] GDPrefixes = ["GD", "OP", "OR"];
    // Dark Bramble
    private readonly string[] DBPrefixes = ["DB"];

    // Dynamic logic information copy-pasted from the .apworld code
    public enum SlotDataSpawn: long
    {
        Vanilla = 0,
        HourglassTwins = 1,
        TimberHearth = 2,
        BrittleHollow = 3,
        GiantsDeep = 4,
    }
    public Dictionary<SlotDataSpawn, string> SlotDataSpawnToRegionName = new Dictionary<SlotDataSpawn, string>
    {
        { SlotDataSpawn.Vanilla, "Timber Hearth Village" },
        { SlotDataSpawn.HourglassTwins, "Hourglass Twins" },
        { SlotDataSpawn.TimberHearth, "Timber Hearth" },
        { SlotDataSpawn.BrittleHollow, "Brittle Hollow" },
        { SlotDataSpawn.GiantsDeep, "Giant's Deep" },
    };
    public Dictionary<string, string> SlotDataWarpPlatformIdToRegionName = new Dictionary<string, string>
    {
        { "SS", "Sun Station" },
        { "ST", "Hourglass Twins" },
        { "ET", "Hourglass Twins" },
        { "ETT", "Hourglass Twins" },
        { "ATP", "Ash Twin Interior" },
        { "ATT", "Hourglass Twins" },
        { "TH", "Timber Hearth" },
        { "THT", "Hourglass Twins" },
        { "BHNG", "Brittle Hollow" },
        { "WHS", "White Hole Station" },
        { "BHF", "Hanging City Ceiling" },
        { "BHT", "Hourglass Twins" },
        { "GD", "Giant's Deep" },
        { "GDT", "Hourglass Twins" },
    };
    public Dictionary<string, HashSet<string>> SlotDataWarpPlatformIdToRequiredItems = new Dictionary<string, HashSet<string>>
    {
        { "SS", [ "Spacesuit" ] },
    };
    // end stuff copy-pasted from .apworld

    public void AddConnection(Dictionary<string, TrackerRegionData> regions, TrackerConnectionData connection)
    {
        if (!regions.ContainsKey(connection.from)) regions.Add(connection.from, new(connection.from));
        if (!regions.ContainsKey(connection.to)) regions.Add(connection.to, new(connection.to));
        regions[connection.from].toConnections.Add(connection);
        regions[connection.to].fromConnections.Add(connection);
    }

    /// <summary>
    /// Parse locations.jsonc and connections.json into data that we can use
    /// </summary>
    public void ParseLocations()
    {
        tracker = APRandomizer.Tracker;
        TrackerLocations = [];
        StaticTrackerRegions = [];

        string path = APRandomizer.Instance.ModHelper.Manifest.ModFolderPath + "/locations.jsonc";
        if (File.Exists(path))
        {
            List<TrackerLocationData> locations = JsonConvert.DeserializeObject<List<TrackerLocationData>>(File.ReadAllText(path));
            // index the locations for faster searching
            TrackerLocations = locations.ToDictionary(x => x.name, x => x);
        }
        else
        {
            APRandomizer.OWMLModConsole.WriteLine($"Could not find the file at {path}!", OWML.Common.MessageType.Error);
        }

        path = APRandomizer.Instance.ModHelper.Manifest.ModFolderPath + "/connections.jsonc";
        if (File.Exists(path))
        {
            List<TrackerConnectionData> connections = JsonConvert.DeserializeObject<List<TrackerConnectionData>>(File.ReadAllText(path));
            foreach (TrackerConnectionData connection in connections)
                AddConnection(StaticTrackerRegions, connection);
        }
    }

    /// <summary>
    /// Populates all the (prefix)Locations dictionaries in Tracker Manager
    /// </summary>
    public void InitializeAccessibility()
    {
        bool logsanity = false;
        if (APRandomizer.SlotData.ContainsKey("logsanity")) logsanity = (long)APRandomizer.SlotData["logsanity"] > 0;
        CanAccessRegion = new();
        tracker.HGTLocations = new();
        tracker.THLocations = new();
        tracker.BHLocations = new();
        tracker.GDLocations = new();
        tracker.DBLocations = new();
        tracker.OWLocations = new();
        tracker.GoalLocations = new();
        foreach (TrackerLocationData loc in TrackerLocations.Values)
        {
            string prefix;
            string name = loc.name;
            // skip logsanity locations if logsanity is off
            if (!logsanity && name.Contains("Ship Log")) continue;
            prefix = name.Substring(0, 2);
            TrackerChecklistData data = new(false, false, "");
            if (HGTPrefixes.Contains(prefix)) tracker.HGTLocations.Add(name, data);
            else if (THPrefixes.Contains(prefix)) tracker.THLocations.Add(name, data);
            else if (BHPrefixes.Contains(prefix)) tracker.BHLocations.Add(name, data);
            else if (GDPrefixes.Contains(prefix)) tracker.GDLocations.Add(name, data);
            else if (DBPrefixes.Contains(prefix)) tracker.DBLocations.Add(name, data);
            else if (name.StartsWith("Victory - ") && loc.address == null) tracker.GoalLocations.Add(name, data);
            else tracker.OWLocations.Add(name, data);
        }
        DetermineAllAccessibility();
    }

    /// <summary>
    /// Runs when the player receives a new item
    /// </summary>
    /// <param name="itemsHelper"></param>
    public void RecheckAccessibility(IReceivedItemsHelper itemsHelper)
    {
        // only gets new items
        var xorCollection = itemsHelper.AllItemsReceived.Except(previouslyObtainedItems);

        foreach (var item in xorCollection)
        {
            // Only bother recalculating logic if the item actually unlocks checks
            if (item.Flags == Archipelago.MultiClient.Net.Enums.ItemFlags.Advancement)
            {
                DetermineAllAccessibility();
                // We only need recalculate logic once
                return;
            }
        }
    }

    /// <summary>
    /// Determines all the accessibility for all locations
    /// </summary>
    /// <param name="category"></param>
    public void DetermineAllAccessibility()
    {
        // First rebuild ItemsCollected, since region and location logic both need items to be up to date
        ItemsCollected = new Dictionary<Item, uint>();
        foreach (var itemInfo in APRandomizer.APSession.Items.AllItemsReceived)
        {
            ItemNames.archipelagoIdToItem.TryGetValue(itemInfo.ItemId, out Item item);
            if (ItemsCollected.ContainsKey(item)) ItemsCollected[item] += 1;
            else ItemsCollected.Add(item, 1);
        }

        // Reset TrackerRegions to a fresh, semi-deep copy to clear any dynamic region connections from the previous slot
        TrackerRegions = StaticTrackerRegions.ToDictionary(e => e.Key, e => {
            var copy = new TrackerRegionData(e.Value.name);
            copy.fromConnections = new(e.Value.fromConnections);
            copy.toConnections = new(e.Value.toConnections);
            copy.requirements = new(e.Value.requirements);
            return copy;
        });

        // Add dynamic region connections due to non-vanilla spawns and warp platform randomization
        SlotDataSpawn spawn = SlotDataSpawn.Vanilla;
        if (!APRandomizer.SlotData.TryGetValue("spawn", out var rawSpawn))
        {
            APRandomizer.OWMLModConsole.WriteLine($"slot_data['spawn'] missing, defaulting to vanilla spawn", OWML.Common.MessageType.Error);
        }
        else
        {
            if (Enum.IsDefined(typeof(SlotDataSpawn), rawSpawn))
                spawn = (SlotDataSpawn)rawSpawn;
            else
                APRandomizer.OWMLModConsole.WriteLine($"{rawSpawn} is not a valid spawn setting, defaulting to vanilla", OWML.Common.MessageType.Error);
        }

        var spawnConnection = new TrackerConnectionData();
        spawnConnection.from = "Menu";
        spawnConnection.to = SlotDataSpawnToRegionName[spawn];
        spawnConnection.requires = new();
        AddConnection(TrackerRegions, spawnConnection);

        // just hardcode the vanilla warps again, it's easier than deriving these strings from the maps in WarpPlatforms.cs
        List<List<string>> warps = [["SS", "ST"], ["ET", "ETT"], ["ATP", "ATT"], ["TH", "THT"], ["BHNG", "WHS"], ["BHF", "BHT"], ["GD", "GDT"]];
        if (!APRandomizer.SlotData.TryGetValue("warps", out var warpSlotData))
        {
            APRandomizer.OWMLModConsole.WriteLine($"slot_data['warps'] missing, defaulting to vanilla warps", OWML.Common.MessageType.Error);
        }
        else
        {
            if (warpSlotData is string warpString && warpString == "vanilla")
            {
                // do nothing
            }
            else if (warpSlotData is not JArray warpsArray)
            {
                APRandomizer.OWMLModConsole.WriteLine($"Leaving vanilla warps unchanged because slot_data['warps'] was invalid: {warpSlotData}", OWML.Common.MessageType.Error);
            }
            else
            {
                var warpsFromSlotData = new List<List<string>>();
                foreach (JToken warpTokenPair in warpsArray)
                {
                    if (warpTokenPair is not JArray warpPairArray)
                    {
                        APRandomizer.OWMLModConsole.WriteLine($"Leaving vanilla warps unchanged because slot_data['warps'] was invalid: {warpSlotData}", OWML.Common.MessageType.Error);
                        break;
                    }

                    List<string> warpStringPair = [warpPairArray[0].ToString(), warpPairArray[1].ToString()];
                    warpsFromSlotData.Add(warpStringPair);
                }
                warps = warpsFromSlotData;
            }
        }

        foreach (var warpPair in warps)
        {
            var w1 = warpPair[0];
            var w2 = warpPair[1];

            if (!SlotDataWarpPlatformIdToRegionName.TryGetValue(w1, out var r1))
            {
                APRandomizer.OWMLModConsole.WriteLine($"slot_data['warps'] was invalid: {warpSlotData}", OWML.Common.MessageType.Error);
                break;
            }
            if (!SlotDataWarpPlatformIdToRegionName.TryGetValue(w2, out var r2))
            {
                APRandomizer.OWMLModConsole.WriteLine($"slot_data['warps'] was invalid: {warpSlotData}", OWML.Common.MessageType.Error);
                break;
            }

            var requirements = new List<TrackerRequirement>();

            // every warp connection requires warp codes to use
            var nwctr = new TrackerRequirement();
            nwctr.item = "Nomai Warp Codes";
            requirements.Add(nwctr);

            // these maps are for corner cases where one or more additional items are required
            if (SlotDataWarpPlatformIdToRequiredItems.TryGetValue(r1, out var items1))
            {
                foreach (var item in items1) {
                    var tr = new TrackerRequirement();
                    tr.item = item;
                    requirements.Add(tr);
                }
            }
            if (SlotDataWarpPlatformIdToRequiredItems.TryGetValue(r2, out var items2))
            {
                foreach (var item in items2)
                {
                    var tr = new TrackerRequirement();
                    tr.item = item;
                    requirements.Add(tr);
                }
            }

            var warpConnection = new TrackerConnectionData();
            warpConnection.from = r1;
            warpConnection.to = r2;
            warpConnection.requires = requirements;
            AddConnection(TrackerRegions, warpConnection);

            var reverseWarpConnection = new TrackerConnectionData();
            reverseWarpConnection.from = r2;
            reverseWarpConnection.to = r1;
            reverseWarpConnection.requires = requirements;
            AddConnection(TrackerRegions, reverseWarpConnection);
        }

        // Build region logic recursively from Menu region
        CanAccessRegion = new();
        BuildRegionLogic("Menu");

        // Determine location logic
        Dictionary<string, TrackerChecklistData> datas = GetLocationChecklist(TrackerCategory.All);
        foreach (var data in datas)
        {
            TrackerChecklistData checklistEntry = data.Value;
            // we can skip accessibility calculation if the location has been checked or ever been accessible
            if (!checklistEntry.hasBeenChecked && !checklistEntry.isAccessible)
            {
                if (tracker.HGTLocations.ContainsKey(data.Key)) tracker.HGTLocations[data.Key].SetAccessible(IsAccessible(TrackerLocations[data.Key]));
                else if (tracker.THLocations.ContainsKey(data.Key)) tracker.THLocations[data.Key].SetAccessible(IsAccessible(TrackerLocations[data.Key]));
                else if (tracker.BHLocations.ContainsKey(data.Key)) tracker.BHLocations[data.Key].SetAccessible(IsAccessible(TrackerLocations[data.Key]));
                else if (tracker.GDLocations.ContainsKey(data.Key)) tracker.GDLocations[data.Key].SetAccessible(IsAccessible(TrackerLocations[data.Key]));
                else if (tracker.DBLocations.ContainsKey(data.Key)) tracker.DBLocations[data.Key].SetAccessible(IsAccessible(TrackerLocations[data.Key]));
                else if (tracker.OWLocations.ContainsKey(data.Key)) tracker.OWLocations[data.Key].SetAccessible(IsAccessible(TrackerLocations[data.Key]));
                else if (tracker.GoalLocations.ContainsKey(data.Key)) tracker.GoalLocations[data.Key].SetAccessible(IsAccessible(TrackerLocations[data.Key]));
                else APRandomizer.OWMLModConsole.WriteLine($"DetermineAllAccessibility was unable to find a Locations dictionary for {data.Key}!", OWML.Common.MessageType.Error);
            }
        }
    }

    /// <summary>
    /// Determines which regions you can access
    /// </summary>
    /// <param name="regionName"></param>
    public void BuildRegionLogic(string regionName)
    {
        if (!CanAccessRegion.ContainsKey(regionName)) CanAccessRegion.Add(regionName, true);
        TrackerRegionData region = TrackerRegions[regionName];
        foreach (TrackerConnectionData connection in region.toConnections)
        {
            string to = connection.to;
            if (!CanAccessRegion.ContainsKey(to)) CanAccessRegion.Add(to, false);
            // We don't need to calculate this connection if the target region is already accessible
            if (CanAccessRegion[to]) continue;
            if (CanAccessAll(connection.requires))
            {
                CanAccessRegion[to] = true;
                BuildRegionLogic(to);
            }
        }
    }

    /// <summary>
    /// Checks which locations have been checked
    /// </summary>
    /// <param name="checkedLocations"></param>
    public void CheckLocations(ReadOnlyCollection<long> checkedLocations)
    {
        foreach (long location in checkedLocations)
        {
            TrackerLocationData loc = GetLocationByID(location);
            if (tracker.HGTLocations.ContainsKey(loc.name))
            {
                tracker.HGTLocations[loc.name].hasBeenChecked = true;
                tracker.HGTLocations[loc.name].hintText = "";
            }
            else if (tracker.THLocations.ContainsKey(loc.name))
            { 
                tracker.THLocations[loc.name].hasBeenChecked = true;
                tracker.THLocations[loc.name].hintText = "";
            }
            else if (tracker.BHLocations.ContainsKey(loc.name))
            {
                tracker.BHLocations[loc.name].hasBeenChecked = true;
                tracker.BHLocations[loc.name].hintText = "";
            }
            else if (tracker.GDLocations.ContainsKey(loc.name))
            {
                tracker.GDLocations[loc.name].hasBeenChecked = true;
                tracker.GDLocations[loc.name].hintText = "";
            }
            else if (tracker.DBLocations.ContainsKey(loc.name))
            { 
                tracker.DBLocations[loc.name].hasBeenChecked = true;
                tracker.DBLocations[loc.name].hintText = "";
            }
            else if (tracker.OWLocations.ContainsKey(loc.name))
            {
                tracker.OWLocations[loc.name].hasBeenChecked = true;
                tracker.OWLocations[loc.name].hintText = "";
            }
            else APRandomizer.OWMLModConsole.WriteLine($"CheckLocations was unable to find a Locations dictionary for {loc.name}!", OWML.Common.MessageType.Error);
        }
    }

    /// <summary>
    /// Returns if a location should be accessible according to logic
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public bool IsAccessible(TrackerLocationData data)
    {
        // Location logic
        if (!CanAccessAll(data.requires)) return false;

        // Region logic
        if (!CanAccessRegion.ContainsKey(data.region)) CanAccessRegion.Add(data.region, false);
        if (!CanAccessRegion[data.region]) return false;
        return true;
    }

    // AND condition
    private bool CanAccessAll(List<TrackerRequirement> requirementsList)
    {
        return requirementsList.All(requirement => CanAccess(requirement));
    }
    // OR condition
    private bool AnyOfAccess(List<TrackerRequirement> requirementsList)
    {
        return requirementsList.Any(requirement => CanAccess(requirement));
    }

    private bool CanAccess(TrackerRequirement requirement)
    {
        // we don't have the item
        if (!string.IsNullOrEmpty(requirement.item) && !ItemsCollected.ContainsKey(ItemNames.itemNamesReversed[requirement.item])) return false;
        // we can't reach the location
        if (!string.IsNullOrEmpty(requirement.location) && !IsAccessible(TrackerLocations[requirement.location])) return false;
        // we don't fulfill any of the AnyOf requirements
        if (requirement.anyOf != null) if (!AnyOfAccess(requirement.anyOf)) return false;
        return true;
    }

    public List<string> GetLogicDisplayStrings(TrackerLocationData data, bool includeLocationName = false)
    {
        // When recursing "up" through the regions needed to reach a certain location, these are
        // the base cases where we want to stop and not explain any further, because:
        // 1) most (all?) cycles involve these regions, and it's nice to not have to deal with cycles
        // 2) it's just too verbose to explain things that indirectly affect nearly every location
        // like "Launch Codes get you to Space" or "Warp Codes get you to any other planet"
        string[] regionLogicDenylist = [
            "Menu",
            "Space",
            "Hourglass Twins",
            "Timber Hearth Village", // leaving out "Timber Hearth" for now
            "Brittle Hollow",
            "Giant's Deep",
            // Dark Bramble and Quantum Moon are not in this list, because:
            // - their connections from Space have item requirements
            // - they don't have warp platforms
        ];

        List<string> unexplainedRegions = new List<string> { data.region };
        Dictionary<string, string> regionNameToLogicDisplayString = new();

        while (unexplainedRegions.Count > 0)
        {
            var regionName = unexplainedRegions.First();
            unexplainedRegions.Remove(regionName);
            if (regionLogicDenylist.Contains(regionName)) continue;
            TrackerRegionData regionData = TrackerRegions[regionName];

            regionNameToLogicDisplayString[regionName] = GetRegionLogicString(regionData);

            foreach (TrackerConnectionData connection in regionData.fromConnections)
            {
                var otherRegionName = connection.from;
                if (!regionNameToLogicDisplayString.ContainsKey(otherRegionName))
                    unexplainedRegions.Add(otherRegionName);
            }
        }

        var logicDisplayStrings = new List<string> { GetLocationLogicString(data, includeLocationName) };

        logicDisplayStrings.AddRange(regionNameToLogicDisplayString.Values);

        foreach (var req in data.requires.Where(req => req.location != null))
        {
            logicDisplayStrings.AddRange(GetLogicDisplayStrings(TrackerLocations[req.location], true));
        }

        return logicDisplayStrings;
    }

    private string GetLocationLogicString(TrackerLocationData data, bool includeLocationName = false)
    {
        CanAccessRegion.TryGetValue(data.region, out bool canAccessRegion);

        var locationLogic = new List<string> {
            (canAccessRegion ? "<color=green>" : "<color=maroon>") + $"(Can Access: {data.region})</color>"
        };
        locationLogic.AddRange(
            GetLogicRequirementsStrings(data.requires)
        );

        return "<color=grey>" +
            (includeLocationName ? $"\"{data.name}\" " : "") +
            "Location Logic: " +
            string.Join(" <color=lime>AND</color> ", locationLogic) +
            "</color>";
    }

    private string GetRegionLogicString(TrackerRegionData data)
    {
        List<string> connectionLogicStrings = new();
        if (data.fromConnections != null && data.fromConnections.Count > 0)
        {
            foreach (TrackerConnectionData connection in data.fromConnections)
            {
                CanAccessRegion.TryGetValue(connection.from, out bool canAccessRegion);

                string connectionLogicString = (canAccessRegion ? "<color=green>" : "<color=maroon>") +
                    $"(Can Access: {connection.from})</color>";

                if (connection.requires != null && connection.requires.Count > 0)
                {
                    connectionLogicString += " <color=lime>AND</color> ";
                    connectionLogicString += string.Join(
                        " <color=lime>AND</color> ",
                        GetLogicRequirementsStrings(connection.requires)
                    );

                    // if we have multiple connections to OR together *and* this connection has multiple parts being ANDed,
                    // then we need an extra set of parentheses around this connection.
                    if (data.fromConnections.Count > 1) {
                        connectionLogicString = $"({connectionLogicString})";
                    }
                }

                connectionLogicStrings.Add(connectionLogicString);
            }
        }

        string logicString = $"<color=grey>\"{data.name}\" Region Logic: ";
        logicString += string.Join(
            " <color=orange>OR</color> ",
            connectionLogicStrings
        );
        logicString += "</color>";
        return logicString;
    }

    private List<string> GetLogicRequirementsStrings(List<TrackerRequirement> requirements)
    {
        List<string> reqStrings = new();
        foreach (var req in requirements)
        {
            if (!string.IsNullOrEmpty(req.item))
            {
                bool canAccess = CanAccess(req);

                string reqStr = (canAccess ? "<color=green>" : "<color=maroon>") +
                    $"(Item: {req.item})</color>";

                reqStrings.Add(reqStr);
            }
            else if (!string.IsNullOrEmpty(req.location))
            {
                bool canAccess = CanAccess(req);

                string reqStr = (canAccess ? "<color=green>" : "<color=maroon>") +
                    $"(Location: {req.location})</color>";

                reqStrings.Add(reqStr);
            }
            else if (req.anyOf != null && req.anyOf.Count > 0)
            {
                string reqStr = string.Join(
                    " <color=orange>OR</color> ",
                    GetLogicRequirementsStrings(req.anyOf)
                );
                if (req.anyOf.Count > 1) {
                    reqStr = "(" + reqStr + ")";
                }
                reqStrings.Add(reqStr);
            }
        }
        return reqStrings;
    }

    /// <summary>
    /// Determines the number of locations in an area that have been checked
    /// </summary>
    /// <param name="category"></param>
    /// <returns></returns>
    public int GetCheckedCount(TrackerCategory category)
    {
        return GetLocationChecklist(category).Count(x => x.Value.hasBeenChecked);
    }

    /// <summary>
    /// Determines the number of locations in an area that are accessible
    /// </summary>
    /// <param name="category"></param>
    /// <returns></returns>
    public int GetAccessibleCount(TrackerCategory category)
    {
        return GetLocationChecklist(category).Count(x => (x.Value.isAccessible || x.Value.hasBeenChecked));
    }

    /// <summary>
    /// Determines the total number of randomized locations in an area
    /// </summary>
    /// <param name="category"></param>
    /// <returns></returns>
    public int GetTotalCount(TrackerCategory category)
    {
        return GetLocationChecklist(category).Count();
    }

    /// <summary>
    /// Returns if there's a location with a hint in the area
    /// </summary>
    /// <param name="category"></param>
    /// <returns></returns>
    public bool GetHasHint(TrackerCategory category)
    {
        return GetLocationChecklist(category).Count(x => x.Value.hintText != "" && !x.Value.hasBeenChecked) > 0;
    }

    /// <summary>
    /// Returns the dictionary for the requested category.
    /// </summary>
    /// <param name="category"></param>
    /// <returns></returns>
    public Dictionary<string, TrackerChecklistData> GetLocationChecklist(TrackerCategory category)
    {
        switch (category)
        {
            case TrackerCategory.Goal:
                {
                    return tracker.GoalLocations;
                }
            case TrackerCategory.HourglassTwins:
                {
                    return tracker.HGTLocations;
                }
            case TrackerCategory.TimberHearth:
                {
                    return tracker.THLocations;
                }
            case TrackerCategory.BrittleHollow:
                {
                    return tracker.BHLocations;
                }
            case TrackerCategory.GiantsDeep:
                {
                    return tracker.GDLocations;
                }
            case TrackerCategory.DarkBramble:
                {
                    return tracker.DBLocations;
                }
            case TrackerCategory.OuterWilds:
                {
                    return tracker.OWLocations;
                }
            case TrackerCategory.All:
                {
                    // returns all of them
                    return tracker.HGTLocations.Concat(tracker.THLocations).Concat(tracker.BHLocations).Concat(tracker.GDLocations).Concat(tracker.DBLocations).Concat(tracker.OWLocations).Concat(tracker.GoalLocations).ToDictionary(x => x.Key, x => x.Value);
                }
        }
        return null;
    }

    /// <summary>
    /// Returns a location in the tracker data by its Archipelago numeric ID
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public TrackerLocationData GetLocationByID(long id)
    {
        TrackerLocationData loc;
        try
        {
            loc = TrackerLocations.Values.FirstOrDefault((x) => x.address == id);
        }
        catch (Exception e)
        {
            APRandomizer.OWMLModConsole.WriteLine(e.Message, OWML.Common.MessageType.Error);
            loc = new();
        }
        return loc;
    }

    /// <summary>
    /// Returns a location in the tracker data by its OWAP string ID (which can be found in the Item enum)
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    public TrackerLocationData GetLocationByName(TrackerInfo info)
    {
        if (Enum.TryParse<Location>(info.locationModID, out Location loc))
        {
            return TrackerLocations[LocationNames.locationNames[loc]];
        }
        else
        {
            APRandomizer.OWMLModConsole.WriteLine($"Unable to find location {info} by name!", OWML.Common.MessageType.Error);
            return null;
        }
    }
}
