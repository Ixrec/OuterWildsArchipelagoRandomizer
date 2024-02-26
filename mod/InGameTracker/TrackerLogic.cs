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
        public static ReadOnlyCollection<NetworkItem> previouslyObtainedItems;

        /// <summary>
        /// Parsed version of locations.jsonc
        /// </summary>
        public static Dictionary<string, TrackerLocationData> TrackerLocations;
        /// <summary>
        /// List of all locations with their connections
        /// </summary>
        public static Dictionary<string, TrackerRegionData> TrackerRegions;

        private static TrackerManager tracker;

        // Ember Twin, Ash Twin, Cave Twin, Tower Twin
        private static readonly string[] HTPrefixes = ["ET", "AT", "CT", "TT"];
        // Timber Hearth, Attlerock, Timber Moon
        private static readonly string[] THPrefixes = ["TH", "AR", "TM"];
        // Brittle Hollow, Hollow's Lantern, Volcano Moon
        private static readonly string[] BHPrefixes = ["BH", "HL", "VM"];
        // Giant's Deep, Orbital Probe Cannon x2
        private static readonly string[] GDPrefixes = ["GD", "OP", "OR"];
        // Dark Bramble
        private static readonly string[] DBPrefixes = ["DB"];

        public static void ParseLocations()
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
                    if (!TrackerRegions.ContainsKey(connection.from)) TrackerRegions.Add(connection.from, new());
                    if (!TrackerRegions.ContainsKey(connection.to)) TrackerRegions.Add(connection.to, new());
                    TrackerRegions[connection.from].toConnections.Add(connection);
                    TrackerRegions[connection.to].fromConnections.Add(connection);
                    APRandomizer.OWMLModConsole.WriteLine($"From: {connection.from} with {TrackerRegions[connection.from].fromConnections.Count}, To: {connection.to} with {TrackerRegions[connection.to].toConnections.Count}");
                }
            }
        }

        /// <summary>
        /// Returns the logic of a location as a string
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string GetLogicString(TrackerLocationData data)
        {
            string logicString = "Logic: ";
            foreach (TrackerLocationData.Requirement req in data.requires)
            {
                if (!string.IsNullOrEmpty(req.item))
                {
                    logicString += $"(item: {req.item}) ";
                }
                if (!string.IsNullOrEmpty(req.location))
                {
                    logicString += $"(location: {req.location}) ";
                }
                if (!string.IsNullOrEmpty(req.region))
                {
                    logicString += $"(region: {req.region}) ";
                }
            }

            return logicString;
        }

        /// <summary>
        /// Populates all the (prefix)Locations dictionaries in Tracker Manager
        /// </summary>
        public static void InitializeAccessibility()
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
            DetermineAllAccessibility(false);
        }

        /// <summary>
        /// Runs when the player receives a new item
        /// </summary>
        /// <param name="itemsHelper"></param>
        public static void RecheckAccessibility(ReceivedItemsHelper itemsHelper)
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
        public static void DetermineAllAccessibility(bool useSaveFileLocations = true)
        {
            Dictionary<string, TrackerChecklistData> datas = GetLocationChecklist(TrackerCategory.All);
            BuildLocationLogic(TrackerRegions["Menu"]);
            foreach (var data in datas)
            {
                TrackerChecklistData checklistEntry = data.Value;
                // Build region logic, we always start from the Menu region
                // we can skip accessibility calculation if the location has been checked or ever been accessible
                if (!checklistEntry.hasBeenChecked || !checklistEntry.isAccessible)
                {
                    if (tracker.HTLocations.ContainsKey(data.Key)) tracker.HTLocations[data.Key].SetAccessible(IsAccessible(TrackerLocations[data.Key], useSaveFileLocations));
                    else if (tracker.THLocations.ContainsKey(data.Key)) tracker.THLocations[data.Key].SetAccessible(IsAccessible(TrackerLocations[data.Key], useSaveFileLocations));
                    else if (tracker.BHLocations.ContainsKey(data.Key)) tracker.BHLocations[data.Key].SetAccessible(IsAccessible(TrackerLocations[data.Key], useSaveFileLocations));
                    else if (tracker.GDLocations.ContainsKey(data.Key)) tracker.GDLocations[data.Key].SetAccessible(IsAccessible(TrackerLocations[data.Key], useSaveFileLocations));
                    else if (tracker.DBLocations.ContainsKey(data.Key)) tracker.DBLocations[data.Key].SetAccessible(IsAccessible(TrackerLocations[data.Key], useSaveFileLocations));
                    else if (tracker.OWLocations.ContainsKey(data.Key)) tracker.OWLocations[data.Key].SetAccessible(IsAccessible(TrackerLocations[data.Key], useSaveFileLocations));
                    else APRandomizer.OWMLModConsole.WriteLine($"Unable to find a Locations dictionary for {data.Key}!", OWML.Common.MessageType.Error);
                }
            }
        }

        /// <summary>
        /// Assigns location logic to a region
        /// </summary>
        /// <param name="region"></param>
        /// <param name="previousRegions"></param>
        public static void BuildLocationLogic(TrackerRegionData region, List<TrackerRegionData> previousRegions = null)
        {
            previousRegions ??= [region];
            APRandomizer.OWMLModConsole.WriteLine($"Connections: From: {region.fromConnections.Count}, To: {region.toConnections.Count}");
            // If we're out of children, we've reached a dead end and don't need to calculate any more logic
            if (region.toConnections.Count <= 0) return;
            APRandomizer.OWMLModConsole.WriteLine($"Building logic for {region.toConnections[0].from}'s children...", OWML.Common.MessageType.Info);
            foreach (TrackerConnectionData connection in region.toConnections)
            {
                APRandomizer.OWMLModConsole.WriteLine($"Building logic for {region.toConnections[0].to}", OWML.Common.MessageType.Message);
                // Prevents looping if we've hit a region we've already been to
                if (previousRegions.Contains(TrackerRegions[connection.to])) continue;

                List<List<TrackerConnectionData.Requirement>> requirements = new();
                if (region.reqs != null && region.reqs.Count > 0)
                {
                    // inherit all parent conditions
                    requirements.AddRange(region.reqs);
                }
                if (connection.requires != null && connection.requires.Count > 0)
                {
                    // Add condition from the connection
                    requirements.Add(connection.requires);
                }
                TrackerRegions[connection.to].reqs.AddRange(requirements);
                TrackerRegions[connection.to].reqs = RemoveDuplicates(TrackerRegions[connection.to].reqs);
                APRandomizer.OWMLModConsole.WriteLine($"Built logic for {connection.to} with {requirements.Count} extra conditions.", OWML.Common.MessageType.Success);
                BuildLocationLogic(TrackerRegions[connection.to], previousRegions);
            }
        }

        private static List<List<TrackerConnectionData.Requirement>> RemoveDuplicates(List<List<TrackerConnectionData.Requirement>> requirements)
        {
            foreach (var requirement in requirements)
            {
                // Sorts all item requirements alphabetically
                requirement.OrderBy(x => x.item).ToList();
            }
            List<List<TrackerConnectionData.Requirement>> newRequirements = new();
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
        public static void CheckLocations(ReadOnlyCollection<long> checkedLocations)
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
        public static void ApplyHint(Hint hint, ArchipelagoSession session)
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
        public static bool IsAccessible(TrackerLocationData data, bool useSaveFileItems)
        {
            Dictionary<Item, uint> ia;
            if (useSaveFileItems)
            {
                ia = APRandomizer.SaveData.itemsAcquired;
            }
            else
            {
                // If we're not reading from the save file, then we have nothing
                ia = APRandomizer.APSession.Items.AllItemsReceived.ToDictionary(x => ItemNames.archipelagoIdToItem[x.Item], x => (uint)0);
            }
            // Region logic
            foreach (List<TrackerConnectionData.Requirement> requirementsList in TrackerRegions[data.region].reqs)
            {
                foreach (TrackerConnectionData.Requirement req in requirementsList)
                {
                    // we don't have the item
                    if (req.item != null && !ia.ContainsKey(ItemNames.itemNamesReversed[req.item])) return false;
                    // we don't fulfill any of the AnyOf requirements
                    if (req.anyOf != null) if (!AnyOfAccess(req.anyOf, ia)) return false;
                }
            }
            // Location logic
            foreach (TrackerLocationData.Requirement req in data.requires)
            {
                // If we don't have at least one of the quantity of required items, the location is inaccessible
                if (req.item != null && !ia.ContainsKey(ItemNames.itemNamesReversed[req.item])) return false;
            }
            
            return true;
        }

        private static bool AnyOfAccess(List<TrackerConnectionData.Requirement> reqs, Dictionary<Item, uint> itemsAcquired)
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
        public static int GetCheckedCount(TrackerCategory category)
        {
            return GetLocationChecklist(category).Count(x => x.Value.hasBeenChecked);
        }

        /// <summary>
        /// Determines the number of locations in an area that are accessible
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public static int GetAccessibleCount(TrackerCategory category)
        {
            return GetLocationChecklist(category).Count(x => (x.Value.isAccessible || x.Value.hasBeenChecked));
        }

        /// <summary>
        /// Determines the total number of randomized locations in an area
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public static int GetTotalCount(TrackerCategory category)
        {
            return GetLocationChecklist(category).Count();
        }

        public static bool GetHasHint(TrackerCategory category)
        {
            return GetLocationChecklist(category).Count(x => x.Value.hintText != "") > 0;
        }

        /// <summary>
        /// Returns the dictionary for the requested category.
        /// </summary>
        /// <param name="category"></param>
        /// <returns></returns>
        public static Dictionary<string, TrackerChecklistData> GetLocationChecklist(TrackerCategory category)
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
        public static TrackerLocationData GetLocationByID(long id)
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
        public static TrackerLocationData GetLocationByName(TrackerInfo info)
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
