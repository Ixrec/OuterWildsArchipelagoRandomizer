using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Collections.ObjectModel;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net;

namespace ArchipelagoRandomizer.InGameTracker
{
    /// <summary>
    /// Helper class for managing tracker accessibility displays
    /// </summary>
    public class TrackerLogic
    {
        public ReadOnlyCollection<NetworkItem> previouslyObtainedItems;

        /// <summary>
        /// Parsed version of locations.jsonc
        /// </summary>
        public Dictionary<string, TrackerLocationData> TrackerLocations;
        /// <summary>
        /// List of all locations with their connections
        /// </summary>
        public Dictionary<string, TrackerRegionData> TrackerRegions;

        public Dictionary<string, bool> CanAccessRegion;

        private TrackerManager tracker;

        // Ember Twin, Ash Twin, Cave Twin, Tower Twin
        private readonly string[] HTPrefixes = ["ET", "AT", "CT", "TT"];
        // Timber Hearth, Attlerock, Timber Moon
        private readonly string[] THPrefixes = ["TH", "AR", "TM"];
        // Brittle Hollow, Hollow's Lantern, Volcano Moon
        private readonly string[] BHPrefixes = ["BH", "HL", "VM"];
        // Giant's Deep, Orbital Probe Cannon x2
        private readonly string[] GDPrefixes = ["GD", "OP", "OR"];
        // Dark Bramble
        private readonly string[] DBPrefixes = ["DB"];

        public void ParseLocations()
        {
            tracker = APRandomizer.Tracker;
            TrackerLocations = new();
            TrackerRegions = new();
            string path = APRandomizer.Instance.ModHelper.Manifest.ModFolderPath + "/locations.jsonc";
            if (File.Exists(path))
            {
                List<TrackerLocationData> locations = JsonConvert.DeserializeObject<List<TrackerLocationData>>(File.ReadAllText(path));
                // index the locations for faster searching
                TrackerLocations = locations.ToDictionary(x => x.name, x => x);
                APRandomizer.OWMLModConsole.WriteLine($"{TrackerLocations.Count} locations parsed!", OWML.Common.MessageType.Success);
            }
            else
            {
                APRandomizer.OWMLModConsole.WriteLine($"Could not find the file at {path}!", OWML.Common.MessageType.Error);
            }
            path = APRandomizer.Instance.ModHelper.Manifest.ModFolderPath + "/connections.jsonc";
            if ( File.Exists(path))
            {
                List<TrackerConnectionData> connections = JsonConvert.DeserializeObject<List<TrackerConnectionData>>(File.ReadAllText(path));
                APRandomizer.OWMLModConsole.WriteLine($"{connections.Count} connections parsed!", OWML.Common.MessageType.Success);
                foreach (TrackerConnectionData connection in connections)
                {
                    if (!TrackerRegions.ContainsKey(connection.from)) TrackerRegions.Add(connection.from, new(connection.from));
                    if (!TrackerRegions.ContainsKey(connection.to)) TrackerRegions.Add(connection.to, new(connection.to));
                    TrackerRegions[connection.from].toConnections.Add(connection);
                    TrackerRegions[connection.to].fromConnections.Add(connection);
                }
            }
        }

        /// <summary>
        /// Returns the logic of a location as a string
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public string GetLocationLogicString(TrackerLocationData data)
        {
            string logicString = "Location Logic: ";
            foreach (TrackerRequirement req in data.requires)
            {
                if (!string.IsNullOrEmpty(req.item))
                {
                    if (!logicString.EndsWith(": ")) logicString += " AND ";
                    logicString += $"(item: {req.item})";
                }
            }

            return logicString;
        }

        /// <summary>
        /// Returns the logic of a region as a string
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public string GetRegionLogicString(string region)
        {
            TrackerRegionData data = TrackerRegions[region];
            string logicString = "Regional Logic: ";
            if (data.fromConnections != null && data.fromConnections.Count > 0)
            {
                foreach (TrackerConnectionData connection in data.fromConnections)
                {
                    if (!logicString.EndsWith("(") && !logicString.EndsWith(": ")) logicString += " OR ";
                    logicString += $"(Can Access: {connection.from} ";
                    if (connection.requires != null && connection.requires.Count > 0)
                    {
                        logicString += "AND (";
                        foreach (TrackerRequirement req in connection.requires)
                        {
                            if (!string.IsNullOrEmpty(req.item))
                            {
                                if (!logicString.EndsWith("(")) logicString += " AND ";
                                logicString += $"Item: {req.item}";
                            }
                            if (req.anyOf != null &&  req.anyOf.Count > 0)
                            {
                                logicString = "(";
                                foreach (var con in req.anyOf)
                                {
                                    if (!logicString.EndsWith("(")) logicString += " OR ";
                                    logicString += $"Item: {con.item}";
                                    // not going to bother with nested any ofs for now, not sure if those will ever have a reason to be used.
                                }
                                logicString += ")";
                            }
                        }
                        logicString += ")";
                    }
                    logicString += ")";
                }
            }
            return logicString;
        }

        /// <summary>
        /// Populates all the (prefix)Locations dictionaries in Tracker Manager
        /// </summary>
        public void InitializeAccessibility()
        {
            CanAccessRegion = new();
            tracker.HTLocations = new();
            tracker.THLocations = new();
            tracker.BHLocations = new();
            tracker.GDLocations = new();
            tracker.DBLocations = new();
            tracker.OWLocations = new();
            foreach (TrackerLocationData loc in TrackerLocations.Values)
            {
                string prefix;
                string name = loc.name;
                prefix = name.Substring(0, 2);
                TrackerChecklistData data = new(false, false, "");
                if (HTPrefixes.Contains(prefix)) tracker.HTLocations.Add(name, data);
                else if (THPrefixes.Contains(prefix)) tracker.THLocations.Add(name, data);
                else if (BHPrefixes.Contains(prefix)) tracker.BHLocations.Add(name, data);
                else if (GDPrefixes.Contains(prefix)) tracker.GDLocations.Add(name, data);
                else if (DBPrefixes.Contains(prefix)) tracker.DBLocations.Add(name, data);
                else tracker.OWLocations.Add(name, data);
            }
            DetermineAllAccessibility();
        }

        /// <summary>
        /// Runs when the player receives a new item
        /// </summary>
        /// <param name="itemsHelper"></param>
        public void RecheckAccessibility(ReceivedItemsHelper itemsHelper)
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
            // Build region logic, we always start from the Menu region
            Dictionary<Item, uint> ia = new();
            foreach (var itemID in APRandomizer.APSession.Items.AllItemsReceived)
            {
                Item item = ItemNames.archipelagoIdToItem[itemID.Item];
                if (ia.ContainsKey(item)) ia[item] += 1;
                else ia.Add(item, 1);
            }
            BuildRegionLogic("Menu", ia);

            // Determine locaiton logic
            Dictionary<string, TrackerChecklistData> datas = GetLocationChecklist(TrackerCategory.All);
            foreach (var data in datas)
            {
                TrackerChecklistData checklistEntry = data.Value;
                // we can skip accessibility calculation if the location has been checked or ever been accessible
                if (!checklistEntry.hasBeenChecked || !checklistEntry.isAccessible)
                {
                    if (tracker.HTLocations.ContainsKey(data.Key)) tracker.HTLocations[data.Key].SetAccessible(IsAccessible(TrackerLocations[data.Key], ia));
                    else if (tracker.THLocations.ContainsKey(data.Key)) tracker.THLocations[data.Key].SetAccessible(IsAccessible(TrackerLocations[data.Key], ia));
                    else if (tracker.BHLocations.ContainsKey(data.Key)) tracker.BHLocations[data.Key].SetAccessible(IsAccessible(TrackerLocations[data.Key], ia));
                    else if (tracker.GDLocations.ContainsKey(data.Key)) tracker.GDLocations[data.Key].SetAccessible(IsAccessible(TrackerLocations[data.Key], ia));
                    else if (tracker.DBLocations.ContainsKey(data.Key)) tracker.DBLocations[data.Key].SetAccessible(IsAccessible(TrackerLocations[data.Key], ia));
                    else if (tracker.OWLocations.ContainsKey(data.Key)) tracker.OWLocations[data.Key].SetAccessible(IsAccessible(TrackerLocations[data.Key], ia));
                    else APRandomizer.OWMLModConsole.WriteLine($"Unable to find a Locations dictionary for {data.Key}!", OWML.Common.MessageType.Error);
                }
            }

        }

        /// <summary>
        /// Determines which regions you can access
        /// </summary>
        /// <param name="regionName"></param>
        /// <param name="itemsAccessible"></param>
        public void BuildRegionLogic(string regionName, Dictionary<Item, uint> itemsAccessible)
        {
            if (!CanAccessRegion.ContainsKey(regionName)) CanAccessRegion.Add(regionName, true);
            TrackerRegionData region = TrackerRegions[regionName];
            foreach (TrackerConnectionData connection in region.toConnections)
            {
                string to = connection.to;
                if (!CanAccessRegion.ContainsKey(to)) CanAccessRegion.Add(to, false);
                // We don't need to calculate this connection if the target region is already accessible
                if (CanAccessRegion[to]) continue;
                if (CanAccess(connection.requires, itemsAccessible))
                {
                    CanAccessRegion[to] = true;
                    BuildRegionLogic(to, itemsAccessible);
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
                if (tracker.HTLocations.ContainsKey(loc.name)) tracker.HTLocations[loc.name].hasBeenChecked = true;
                else if (tracker.THLocations.ContainsKey(loc.name)) tracker.THLocations[loc.name].hasBeenChecked = true;
                else if (tracker.BHLocations.ContainsKey(loc.name)) tracker.BHLocations[loc.name].hasBeenChecked = true;
                else if (tracker.GDLocations.ContainsKey(loc.name)) tracker.GDLocations[loc.name].hasBeenChecked = true;
                else if (tracker.DBLocations.ContainsKey(loc.name)) tracker.DBLocations[loc.name].hasBeenChecked = true;
                else if (tracker.OWLocations.ContainsKey(loc.name)) tracker.OWLocations[loc.name].hasBeenChecked = true;
                else APRandomizer.OWMLModConsole.WriteLine($"Unable to find a Locations dictionary for {loc.name}!", OWML.Common.MessageType.Error);
            }
        }

        /// <summary>
        /// Reads a hint and applies it to the checklist
        /// </summary>
        /// <param name="hint"></param>
        public void ApplyHint(Hint hint, ArchipelagoSession session)
        {
            string playerName;
            if (hint.ReceivingPlayer == session.ConnectionInfo.Slot)
            {
                playerName = "your";
            }
            else
            {
                playerName = session.Players.GetPlayerName(hint.ReceivingPlayer) + "'s";
            }
            string itemColor;
            switch (hint.ItemFlags)
            {
                case Archipelago.MultiClient.Net.Enums.ItemFlags.Advancement: itemColor = "#B883B4"; break;
                case Archipelago.MultiClient.Net.Enums.ItemFlags.NeverExclude: itemColor = "#524798"; break;
                case Archipelago.MultiClient.Net.Enums.ItemFlags.Trap: itemColor = "#DA6F62"; break;
                default: itemColor = "#01CACA"; break;
            }
            string itemTitle = $"<color={itemColor}>{session.Items.GetItemName(hint.ItemId)}</color>";
            string hintDescription = $"It looks like {playerName} <color={itemColor}>{itemTitle}</color> can be found here";
            TrackerLocationData loc = GetLocationByID(hint.LocationId);
            if (tracker.HTLocations.ContainsKey(loc.name)) tracker.HTLocations[loc.name].hintText = hintDescription;
            else if (tracker.THLocations.ContainsKey(loc.name)) tracker.THLocations[loc.name].hintText = hintDescription;
            else if (tracker.BHLocations.ContainsKey(loc.name)) tracker.BHLocations[loc.name].hintText = hintDescription;
            else if (tracker.GDLocations.ContainsKey(loc.name)) tracker.GDLocations[loc.name].hintText = hintDescription;
            else if (tracker.DBLocations.ContainsKey(loc.name)) tracker.DBLocations[loc.name].hintText = hintDescription;
            else if (tracker.OWLocations.ContainsKey(loc.name)) tracker.OWLocations[loc.name].hintText = hintDescription;
            else APRandomizer.OWMLModConsole.WriteLine($"Unable to find a Locations dictionary for {loc.name}!", OWML.Common.MessageType.Error);
        }

        /// <summary>
        /// Returns if a location should be accessible according to logic
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool IsAccessible(TrackerLocationData data, Dictionary<Item, uint> itemsAccessible)
        {
            // Location logic
            if (!CanAccess(data.requires, itemsAccessible)) return false;

            // Region logic
            if (!CanAccessRegion.ContainsKey(data.region)) CanAccessRegion.Add(data.region, false);
            if (!CanAccessRegion[data.region]) return false;
            return true;
        }

        private bool CanAccess(List<TrackerRequirement> requirementsList, Dictionary<Item, uint> itemsAcquired)
        {
            foreach (TrackerRequirement regionRequirement in requirementsList)
            {
                // we don't have the item
                if (!string.IsNullOrEmpty(regionRequirement.item) && !itemsAcquired.ContainsKey(ItemNames.itemNamesReversed[regionRequirement.item])) return false;
                // we don't fulfill any of the AnyOf requirements
                if (regionRequirement.anyOf != null) if (!AnyOfAccess(regionRequirement.anyOf, itemsAcquired)) return false;
            }
            return true;
        }

        private bool AnyOfAccess(List<TrackerRequirement> reqs, Dictionary<Item, uint> itemsAcquired)
        {
            foreach (var req in reqs)
            {
                // check sub-anyof and if it succeeds we've got a true condition
                if (req.anyOf != null) if (AnyOfAccess(req.anyOf, itemsAcquired)) return true;
                // if we have the item, return true
                if (req.item != null && itemsAcquired.ContainsKey(ItemNames.itemNamesReversed[req.item])) return true;
            }
            return false;
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

        public bool GetHasHint(TrackerCategory category)
        {
            return GetLocationChecklist(category).Count(x => x.Value.hintText != "") > 0;
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
                case TrackerCategory.HourglassTwins:
                    {
                        return tracker.HTLocations;
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
                        return tracker.HTLocations.Concat(tracker.THLocations).Concat(tracker.BHLocations).Concat(tracker.GDLocations).Concat(tracker.DBLocations).Concat(tracker.OWLocations).ToDictionary(x => x.Key, x => x.Value);
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
}
