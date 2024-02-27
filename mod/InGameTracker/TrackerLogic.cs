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
            foreach (List<TrackerRequirement> requirementList in data.requirements)
            {
                if (!logicString.EndsWith(": ")) logicString += " OR ";
                logicString += "(";
                foreach (TrackerRequirement req in requirementList)
                {
                    if (!string.IsNullOrEmpty(req.item)) logicString += $"(item: {req.item})";
                    if (req.anyOf != null) logicString += GetAnyOfString(req);
                }
                logicString += ")";
                // if the condition is empty, remove the " AND ()" at the end
                if (logicString.EndsWith("()")) logicString = logicString.Substring(0, logicString.Length - 6);
            }
            return logicString;
        }

        private string GetAnyOfString(TrackerRequirement requirement)
        {
            string logicString = "";
            foreach (TrackerRequirement req in requirement.anyOf)
            {
                if (!string.IsNullOrEmpty(req.item)) logicString += $"OR (item: {req.item})";
                if (req.anyOf != null) logicString += GetAnyOfString(req);
            }
            if (logicString.StartsWith("OR ")) logicString = logicString.Substring(3);
            return logicString;
        }

        /// <summary>
        /// Populates all the (prefix)Locations dictionaries in Tracker Manager
        /// </summary>
        public void InitializeAccessibility()
        {
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
            BuildLocationLogic(TrackerRegions["Menu"]);
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
            Dictionary<string, TrackerChecklistData> datas = GetLocationChecklist(TrackerCategory.All);
            foreach (var data in datas)
            {
                TrackerChecklistData checklistEntry = data.Value;
                // Build region logic, we always start from the Menu region
                // we can skip accessibility calculation if the location has been checked or ever been accessible
                if (!checklistEntry.hasBeenChecked || !checklistEntry.isAccessible)
                {
                    if (tracker.HTLocations.ContainsKey(data.Key)) tracker.HTLocations[data.Key].SetAccessible(IsAccessible(TrackerLocations[data.Key]));
                    else if (tracker.THLocations.ContainsKey(data.Key)) tracker.THLocations[data.Key].SetAccessible(IsAccessible(TrackerLocations[data.Key]));
                    else if (tracker.BHLocations.ContainsKey(data.Key)) tracker.BHLocations[data.Key].SetAccessible(IsAccessible(TrackerLocations[data.Key]));
                    else if (tracker.GDLocations.ContainsKey(data.Key)) tracker.GDLocations[data.Key].SetAccessible(IsAccessible(TrackerLocations[data.Key]));
                    else if (tracker.DBLocations.ContainsKey(data.Key)) tracker.DBLocations[data.Key].SetAccessible(IsAccessible(TrackerLocations[data.Key]));
                    else if (tracker.OWLocations.ContainsKey(data.Key)) tracker.OWLocations[data.Key].SetAccessible(IsAccessible(TrackerLocations[data.Key]));
                    else APRandomizer.OWMLModConsole.WriteLine($"Unable to find a Locations dictionary for {data.Key}!", OWML.Common.MessageType.Error);
                }
            }
        }

        /// <summary>
        /// Assigns location logic to a region
        /// </summary>
        /// <param name="region"></param>
        /// <param name="previousRegions"></param>
        public void BuildLocationLogic(TrackerRegionData region, List<TrackerRegionData> previousRegions = null)
        {
            previousRegions ??= [];
            previousRegions.Add(region);
            APRandomizer.OWMLModConsole.WriteLine($"We are at {region.name} which has {region.fromConnections.Count} parents and {region.toConnections.Count} children, and we have been to {previousRegions.Count} regions including this one.");
            // If we're out of children, we've reached a dead end and don't need to calculate any more logic
            if (region.toConnections.Count <= 0)
            {
                APRandomizer.OWMLModConsole.WriteLine($"Finished building logic for {region.name}");
                return;
            }
            APRandomizer.OWMLModConsole.WriteLine($"Building logic for {region.name}'s children...", OWML.Common.MessageType.Info);
            foreach (TrackerConnectionData connection in region.toConnections)
            {
                APRandomizer.OWMLModConsole.WriteLine($"Building logic for {connection.to} from {region.name}", OWML.Common.MessageType.Message);
                // Prevents looping if we've hit a region we've already been to
                if (previousRegions.Contains(TrackerRegions[connection.to])) continue;

                APRandomizer.OWMLModConsole.WriteLine($"Source old {region.name} {GetRegionLogicString(region.name)}");
                APRandomizer.OWMLModConsole.WriteLine($"{connection.to} old {GetRegionLogicString(connection.to)}");

                List<List<TrackerRequirement>> sourceRequirements = new();
                if (region.requirements != null && region.requirements.Count > 0)
                {
                    sourceRequirements = new(region.requirements);
                    if (connection.requires != null && connection.requires.Count > 0)
                    {
                        foreach (List<TrackerRequirement> reqs in sourceRequirements)
                        {
                            reqs.AddRange(connection.requires);
                        }
                    }
                }
                else
                {
                    if (connection.requires != null && connection.requires.Count > 0)
                    {
                        sourceRequirements.Add(connection.requires);
                    }
                }
                TrackerRegions[connection.to].requirements.AddRange(sourceRequirements);
                TrackerRegions[connection.to].requirements = RemoveDuplicates(TrackerRegions[connection.to].requirements);
                APRandomizer.OWMLModConsole.WriteLine($"Source new {region.name} {GetRegionLogicString(region.name)}");
                APRandomizer.OWMLModConsole.WriteLine($"{connection.to} new {GetRegionLogicString(connection.to)}");

            }
            APRandomizer.OWMLModConsole.WriteLine($"Finished building logic for {region.name}");
            foreach (TrackerConnectionData connection in region.toConnections)
            {
                // Prevents looping if we've hit a region we've already been to
                if (previousRegions.Contains(TrackerRegions[connection.to])) continue;
                BuildLocationLogic(TrackerRegions[connection.to], new(previousRegions));
            }

                /*
                if (region.requirements != null && region.requirements.Count > 0)
                {
                    // inherit all parent conditions
                    APRandomizer.OWMLModConsole.WriteLine($"Pulling in {region.requirements.Count} conditions from parent");
                    TrackerRegions[connection.to].requirements.AddRange(region.requirements); 
                    APRandomizer.OWMLModConsole.WriteLine($"1 Source old {region.name} {GetRegionLogicString(region.name)}");
                    APRandomizer.OWMLModConsole.WriteLine($"{connection.to} old {GetRegionLogicString(connection.to)}");
                }
                if (connection.requires != null && connection.requires.Count > 0)
                {
                    APRandomizer.OWMLModConsole.WriteLine($"Adding conditions {connection.requires.ElementAt(0).item}{(connection.requires.Count > 1 ? $" plus {connection.requires.Count - 1} more" : "")}");
                    APRandomizer.OWMLModConsole.WriteLine($"2 Source old {region.name} {GetRegionLogicString(region.name)}");
                    APRandomizer.OWMLModConsole.WriteLine($"{connection.to} old {GetRegionLogicString(connection.to)}");
                    // Add condition from the connection
                    List<List<TrackerRequirement>> workingRequirement = new(TrackerRegions[connection.to].requirements);
                    if (workingRequirement.Count > 0)
                    {
                        foreach (List<TrackerRequirement> reqList in workingRequirement)
                        {
                            APRandomizer.OWMLModConsole.WriteLine($"reqList length: {reqList.Count}");
                            APRandomizer.OWMLModConsole.WriteLine($"3 Source old {region.name} {GetRegionLogicString(region.name)}");
                            APRandomizer.OWMLModConsole.WriteLine($"{connection.to} old {GetRegionLogicString(connection.to)}");
                            foreach (TrackerRequirement newReq in connection.requires)
                            {
                                if (reqList.Contains(newReq)) continue;
                                TrackerRequirement req = new()
                                {
                                    item = newReq.item,
                                    anyOf = newReq.anyOf
                                };
                                reqList.Add(req);
                                APRandomizer.OWMLModConsole.WriteLine($"Adding condition {newReq.item}");
                                oldCount++;
                            }
                            APRandomizer.OWMLModConsole.WriteLine($"4 Source old {region.name} {GetRegionLogicString(region.name)}");
                            APRandomizer.OWMLModConsole.WriteLine($"{connection.to} old {GetRegionLogicString(connection.to)}");
                        }
                        TrackerRegions[connection.to].requirements = workingRequirement;
                    }
                    else
                    {
                        APRandomizer.OWMLModConsole.WriteLine($"Requirements are empty, so just adding {connection.requires.Count} existing conditions.");
                        TrackerRegions[connection.to].requirements.Add(connection.requires);
                        oldCount++;
                    }
                }
                TrackerRegions[connection.to].requirements = RemoveDuplicates(TrackerRegions[connection.to].requirements);
                TrackerRegions[region.name].requirements = oldRequirements;
                APRandomizer.OWMLModConsole.WriteLine($"Built logic for {connection.to} with {oldCount} extra conditions.", OWML.Common.MessageType.Success);
                APRandomizer.OWMLModConsole.WriteLine($"Source {region.name} {GetRegionLogicString(region.name)}");
                APRandomizer.OWMLModConsole.WriteLine($"{connection.to} new {GetRegionLogicString(connection.to)}");
                BuildLocationLogic(TrackerRegions[connection.to], new(previousRegions));*/
            
        }

        private List<List<TrackerRequirement>> RemoveDuplicates(List<List<TrackerRequirement>> requirements)
        {
            foreach (var requirement in requirements)
            {
                // Sorts all item requirements alphabetically
                requirement.OrderBy(x => x.item).ToList();
            }
            List<List<TrackerRequirement>> newRequirements = new();
            foreach (var requirement in requirements)
            {
                // Hopefully this actually prevents duplicates from being added
                if (!newRequirements.Contains(requirement)) newRequirements.Add(requirement);
            }
            return newRequirements;
            // AnyOfs should be dealt with later
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
        public bool IsAccessible(TrackerLocationData data)
        {
            Dictionary<Item, uint> ia = new();
            foreach (var itemID in APRandomizer.APSession.Items.AllItemsReceived)
            {
                Item item = ItemNames.archipelagoIdToItem[itemID.Item];
                if (ia.ContainsKey(item)) ia[item] += 1;
                else ia.Add(item, 1);
            }

            // Location logic
            if (!CanAccess(data.requires, ia)) return false;

            // Region logic
            if (TrackerRegions[data.region].requirements == null || TrackerRegions[data.region].requirements.Count == 0) return true;
            foreach (List<TrackerRequirement> requirementsList in TrackerRegions[data.region].requirements)
            {
                if (CanAccess(requirementsList, ia)) return true;
            }
            return false;
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
