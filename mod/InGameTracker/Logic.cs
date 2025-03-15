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

    public Dictionary<string, TrackerChecklistData> LocationChecklistData;

    // Dynamic logic information copy-pasted from the .apworld code
    public enum SlotDataSpawn: long
    {
        Vanilla = 0,
        HourglassTwins = 1,
        TimberHearth = 2,
        BrittleHollow = 3,
        GiantsDeep = 4,
        Stranger = 5,
    }
    public Dictionary<SlotDataSpawn, string> SlotDataSpawnToRegionName = new Dictionary<SlotDataSpawn, string>
    {
        { SlotDataSpawn.Vanilla, "Timber Hearth Village" },
        { SlotDataSpawn.HourglassTwins, "Hourglass Twins" },
        { SlotDataSpawn.TimberHearth, "Timber Hearth" },
        { SlotDataSpawn.BrittleHollow, "Brittle Hollow" },
        { SlotDataSpawn.GiantsDeep, "Giant's Deep" },
        { SlotDataSpawn.Stranger, "Stranger Sunside Hangar" },
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

    // subsets of LocationChecklistData
    private Dictionary<string, TrackerChecklistData> HGTLocationChecklistData;
    private Dictionary<string, TrackerChecklistData> THLocationChecklistData;
    private Dictionary<string, TrackerChecklistData> BHLocationChecklistData;
    private Dictionary<string, TrackerChecklistData> GDLocationChecklistData;
    private Dictionary<string, TrackerChecklistData> DBLocationChecklistData;
    private Dictionary<string, TrackerChecklistData> OWLocationChecklistData;
    private Dictionary<string, TrackerChecklistData> GoalLocationChecklistData;
    private Dictionary<string, TrackerChecklistData> StrangerLocationChecklistData;
    private Dictionary<string, TrackerChecklistData> DWLocationChecklistData;
    private Dictionary<string, Dictionary<string, TrackerChecklistData>> StoryModLocationChecklistData;

    // Ember Twin, Ash Twin, Cave Twin, Tower Twin
    private readonly static string[] HGTPrefixes = ["ET", "AT", "CT", "TT"];
    // Timber Hearth, Attlerock, Timber Moon
    private readonly static string[] THPrefixes = ["TH", "AR", "TM"];
    // Brittle Hollow, Hollow's Lantern, Volcano Moon
    private readonly static string[] BHPrefixes = ["BH", "HL", "VM"];
    // Giant's Deep, Orbital Probe Cannon x2
    private readonly static string[] GDPrefixes = ["GD", "OP", "OR"];
    // Dark Bramble
    private readonly static string[] DBPrefixes = ["DB"];
    private readonly static string GoalPrefix = "Victory - ";
    private readonly static string[] StrangerPrefixes = ["EotE"];
    private readonly static string[] DWPrefixes = ["DW"];

    /// <summary>
    /// Populates all the (prefix)Locations dictionaries in Tracker Manager
    /// </summary>
    public void InitializeAccessibility()
    {
        CanAccessRegion = new();

        LocationChecklistData = new();

        HGTLocationChecklistData = new();
        THLocationChecklistData = new();
        BHLocationChecklistData = new();
        GDLocationChecklistData = new();
        DBLocationChecklistData = new();
        OWLocationChecklistData = new();
        GoalLocationChecklistData = new();
        StrangerLocationChecklistData = new();
        DWLocationChecklistData = new();
        StoryModLocationChecklistData = new();

        bool logsanity = APRandomizer.SlotEnabledLogsanity();
        bool enable_eote_dlc = APRandomizer.SlotEnabledEotEDLC();
        bool dlc_only = APRandomizer.SlotEnabledDLCOnly();

        var DLCPrefixes = StrangerPrefixes.Concat(DWPrefixes);
        foreach (TrackerLocationData loc in TrackerLocations.Values)
        {
            string name = loc.name;

            // Here we only care about `category` values used in locations.jsonc, so e.g. the "dlc|hn1" in items.jsonc can be ignored
            if (!logsanity && (loc.logsanity ?? false)) continue;
            if (!enable_eote_dlc && (loc.category == "dlc")) continue;
            if (dlc_only && (loc.category == "base")) continue;
            if ((!enable_eote_dlc || dlc_only) && (loc.category == "base+dlc")) continue; // only used on some victory events, but may as well

            StoryModMetadata.ModMetadata mod = null;
            if (loc.category != null && StoryModMetadata.LogicCategoryToModMetadata.ContainsKey(loc.category))
            {
                mod = StoryModMetadata.LogicCategoryToModMetadata[loc.category];
                var option = mod.slotDataOption;
                bool modEnabled = (APRandomizer.SlotData.ContainsKey(option) && (long)APRandomizer.SlotData[option] > 0);
                if (!modEnabled) continue; // skip processing this location at all, since it wasn't generated
            }

            TrackerChecklistData data = new(false, false, "");
            LocationChecklistData.Add(name, data);

            if (mod != null)
            {
                if (!StoryModLocationChecklistData.ContainsKey(mod.logicCategory))
                    StoryModLocationChecklistData[mod.logicCategory] = new();
                StoryModLocationChecklistData[mod.logicCategory].Add(name, data);
            }
            else
            {
                if (HGTPrefixes.Any(p => name.StartsWith(p))) HGTLocationChecklistData.Add(name, data);
                else if (THPrefixes.Any(p => name.StartsWith(p))) THLocationChecklistData.Add(name, data);
                else if (BHPrefixes.Any(p => name.StartsWith(p))) BHLocationChecklistData.Add(name, data);
                else if (GDPrefixes.Any(p => name.StartsWith(p))) GDLocationChecklistData.Add(name, data);
                else if (DBPrefixes.Any(p => name.StartsWith(p))) DBLocationChecklistData.Add(name, data);
                else if (StrangerPrefixes.Any(p => name.StartsWith(p))) StrangerLocationChecklistData.Add(name, data);
                else if (DWPrefixes.Any(p => name.StartsWith(p))) DWLocationChecklistData.Add(name, data);
                else if (name.StartsWith(GoalPrefix)) GoalLocationChecklistData.Add(name, data);
                // "The Outer Wilds" is the catch-all category for base game locations lacking a special prefix
                else OWLocationChecklistData.Add(name, data);
            }
        }
        DetermineAllAccessibility();
    }

    public Dictionary<string, TrackerChecklistData> GetLocationChecklist(TrackerCategory category)
    {
        switch (category)
        {
            case TrackerCategory.Goal: return GoalLocationChecklistData;
            case TrackerCategory.HourglassTwins: return HGTLocationChecklistData;
            case TrackerCategory.TimberHearth: return THLocationChecklistData;
            case TrackerCategory.BrittleHollow: return BHLocationChecklistData;
            case TrackerCategory.GiantsDeep: return GDLocationChecklistData;
            case TrackerCategory.DarkBramble: return DBLocationChecklistData;
            case TrackerCategory.OuterWilds: return OWLocationChecklistData;
            case TrackerCategory.Stranger: return StrangerLocationChecklistData;
            case TrackerCategory.Dreamworld: return DWLocationChecklistData;
            case TrackerCategory.All: return LocationChecklistData;
            default:
                var cat = StoryModMetadata.TrackerCategoryToModMetadata[category].logicCategory;
                if (StoryModLocationChecklistData.ContainsKey(cat)) // was this AP slot generated with this story mod's locations?
                    return StoryModLocationChecklistData[cat];
                else
                    return new();
        }
    }

    /// <summary>
    /// Runs when the player receives a new item
    /// </summary>
    /// <param name="itemsHelper"></param>
    public void RecheckAccessibility(IReceivedItemsHelper itemsHelper)
    {
        // only gets new items
        var newItems = itemsHelper.AllItemsReceived.Except(previouslyObtainedItems);

        // Only bother recalculating logic if the item actually unlocks checks
        if (newItems.Any(item => item.Flags == Archipelago.MultiClient.Net.Enums.ItemFlags.Advancement))
            DetermineAllAccessibility();
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
        PopulateCanAccessRegion("Menu");
        IterateOnCanAccessRegion();

        // Determine location logic
        Dictionary<string, TrackerChecklistData> datas = GetLocationChecklist(TrackerCategory.All);
        foreach (var data in datas)
        {
            TrackerChecklistData checklistEntry = data.Value;
            // we can skip accessibility calculation if the location has been checked or ever been accessible
            if (!checklistEntry.hasBeenChecked && !checklistEntry.isAccessible)
            {
                if (LocationChecklistData.ContainsKey(data.Key))
                    LocationChecklistData[data.Key].SetAccessible(IsAccessible(TrackerLocations[data.Key]));
                else
                    APRandomizer.OWMLModConsole.WriteLine($"DetermineAllAccessibility was unable to find checklist data object for {data.Key}!", OWML.Common.MessageType.Error);
            }
        }
    }

    private bool IsConnectionActive(TrackerConnectionData connection)
    {
        if (connection.category == null)
            return true;

        // we have one 'x&y' category in connections.jsonc, but no 'x|y's, so here we only worry about & for now
        HashSet<string> categories = new();
        if (connection.category.Contains("&"))
            foreach (var category in connection.category.Split('&'))
                categories.Add(category);
        else
            categories.Add(connection.category);

        // only use this connection (and its regions) if all the relevant dlcs/story mods were enabled when generating this slot
        bool allContentEnabled = true;
        foreach (var category in categories)
        {
            if (category == "dlc")
                allContentEnabled &= APRandomizer.SlotEnabledEotEDLC();
            else if (connection.category == "base")
                allContentEnabled &= !APRandomizer.SlotEnabledDLCOnly();
            else {
                var found = StoryModMetadata.LogicCategoryToModMetadata.TryGetValue(category, out var mod);
                if (!found)
                {
                    APRandomizer.OWMLModConsole.WriteLine($"IsConnectionActive early returning false for {connection.from}->{connection.to} because {category} (in {connection.category}) was not recognized", OWML.Common.MessageType.Error);
                    return false; // this is for a story mod we haven't integrated yet, so ignore it
                }
                var option = mod.slotDataOption;
                allContentEnabled &= (APRandomizer.SlotData.ContainsKey(option) && (long)APRandomizer.SlotData[option] > 0);
            }
        }
        return allContentEnabled;
    }

    /// <summary>
    /// Determines which regions you can access
    /// </summary>
    /// <param name="regionName"></param>
    public void PopulateCanAccessRegion(string regionName)
    {
        if (!CanAccessRegion.ContainsKey(regionName)) CanAccessRegion.Add(regionName, true);
        TrackerRegionData region = TrackerRegions[regionName];
        foreach (TrackerConnectionData connection in region.toConnections)
        {
            if (!IsConnectionActive(connection))
                continue;

            string to = connection.to;
            if (!CanAccessRegion.ContainsKey(to)) CanAccessRegion.Add(to, false);
            // We don't need to calculate this connection if the target region is already accessible
            if (CanAccessRegion[to]) continue;
            if (CanAccessAll(connection.requires))
            {
                CanAccessRegion[to] = true;
                PopulateCanAccessRegion(to);
            }
        }
    }

    // This fixed point iteration is necessary because of "indirect connections",
    // i.e. connection.requires containing a .region requirement
    private void IterateOnCanAccessRegion()
    {
        HashSet<string> outdatedRegions = new HashSet<string>();
        foreach (var (regionName, canAccess) in CanAccessRegion)
        {
            if (canAccess) continue;
            if (!TrackerRegions.ContainsKey(regionName)) continue;

            TrackerRegionData region = TrackerRegions[regionName];
            foreach (TrackerConnectionData connection in region.fromConnections)
            {
                if (!IsConnectionActive(connection))
                    continue;

                string from = connection.from;
                if (CanAccessRegion.GetValueOrDefault(from, false) && CanAccessAll(connection.requires))
                {
                    outdatedRegions.Add(regionName);
                    APRandomizer.OWMLModConsole.WriteLine($"IterateOnCanAccessRegion found {regionName} was reachable yet false, will iterate again");
                }
            }
        }

        if (outdatedRegions.Any())
        {
            foreach (var regionName in outdatedRegions)
                CanAccessRegion[regionName] = true;

            APRandomizer.OWMLModConsole.WriteLine($"IterateOnCanAccessRegion calling IterateOnCanAccessRegion()");
            IterateOnCanAccessRegion();
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
            if (LocationChecklistData.ContainsKey(loc.name))
            {
                LocationChecklistData[loc.name].hasBeenChecked = true;
                LocationChecklistData[loc.name].hintText = "";
            }
            else APRandomizer.OWMLModConsole.WriteLine($"CheckLocations was unable to find a checklist data object for {loc.name}!", OWML.Common.MessageType.Error);
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
        return CanAccessRegion[data.region];
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
        if (!string.IsNullOrEmpty(requirement.item))
        {
            if (requirement.item.StartsWith("Translator (") && !APRandomizer.SlotEnabledSplitTranslator())
                return ItemsCollected.ContainsKey(Item.Translator);
            return ItemsCollected.ContainsKey(ItemNames.itemNamesReversed[requirement.item]);
        }
        if (!string.IsNullOrEmpty(requirement.location))
            return IsAccessible(TrackerLocations[requirement.location]);
        if (!string.IsNullOrEmpty(requirement.region))
        {
            CanAccessRegion.TryGetValue(requirement.region, out bool canAccessRegion);
            return canAccessRegion;
        }
        if (requirement.anyOf != null)
            return AnyOfAccess(requirement.anyOf);
        throw new ArgumentException($"CanAccess called with invalid TrackerRequirement: {requirement}");
    }

    public IEnumerable<string> GetAllRegionsInRequirements(IEnumerable<TrackerRequirement> requires)
    {
        return requires.Where(req => req.region != null).Select(req => req.region)
            .Concat(requires.Where(req => req.anyOf != null).SelectMany(req => GetAllRegionsInRequirements(req.anyOf)));
    }

    public List<string> GetLogicDisplayStrings(TrackerLocationData data, bool includeLocationName = false)
    {
        // When recursing "up" through the regions needed to reach a certain location, these are
        // the base cases where we want to stop and not explain any further, because it's
        // just too verbose to explain things that indirectly affect nearly every location
        // like "Launch Codes get you to Space" or "Warp Codes get you to any other planet".
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
        unexplainedRegions.AddRange(GetAllRegionsInRequirements(data.requires));

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
                if (!IsConnectionActive(connection))
                    continue;

                var otherRegionName = connection.from;
                if (!regionNameToLogicDisplayString.ContainsKey(otherRegionName))
                    unexplainedRegions.Add(otherRegionName);
                foreach (string region in GetAllRegionsInRequirements(connection.requires))
                    if (!regionNameToLogicDisplayString.ContainsKey(region))
                        unexplainedRegions.Add(region);
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
            "Location Logic:\n" +
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
                if (!IsConnectionActive(connection))
                    continue;

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

        string logicString = $"<color=grey>\"{data.name}\" Region Logic:\n";
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

                string itemName = req.item;
                if (itemName.StartsWith("Translator (") && !APRandomizer.SlotEnabledSplitTranslator())
                    itemName = "Translator";

                string reqStr = (canAccess ? "<color=green>" : "<color=maroon>") +
                    $"(Item: {itemName})</color>";

                reqStrings.Add(reqStr);
            }
            else if (!string.IsNullOrEmpty(req.location))
            {
                bool canAccess = CanAccess(req);

                string reqStr = (canAccess ? "<color=green>" : "<color=maroon>") +
                    $"(Location: {req.location})</color>";

                reqStrings.Add(reqStr);
            }
            else if (!string.IsNullOrEmpty(req.region))
            {
                CanAccessRegion.TryGetValue(req.region, out bool canAccessRegion);

                string reqStr = (canAccessRegion ? "<color=green>" : "<color=maroon>") +
                    $"(Can Access: {req.region})</color>";

                reqStrings.Add(reqStr);
            }
            else if (req.anyOf != null && req.anyOf.Count > 0)
            {
                string reqStr = string.Join(
                    " <color=orange>OR</color> ",
                    GetLogicRequirementsStrings(req.anyOf)
                );
                if (req.anyOf.Count > 1)
                {
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
    public TrackerLocationData GetLocationDataByInfo(TrackerInfo info)
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
