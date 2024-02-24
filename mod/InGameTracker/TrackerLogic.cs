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
            string path = APRandomizer.Instance.ModHelper.Manifest.ModFolderPath + "/locations.jsonc";
            if (File.Exists(path))
            {
                List<TrackerLocationData> locations = JsonConvert.DeserializeObject<List<TrackerLocationData>>(File.ReadAllText(path));
                // index the locations for faster searching
                foreach (TrackerLocationData location in locations)
                {
                    TrackerLocations.Add(location.name, location);
                }
                APRandomizer.OWMLModConsole.WriteLine($"{TrackerLocations.Count} locations parsed!", OWML.Common.MessageType.Success);
            }
            else
            {
                APRandomizer.OWMLModConsole.WriteLine($"Could not find the file at {path}!", OWML.Common.MessageType.Error);
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
                TrackerChecklistData data = new(false, false, "", "");
                if (HTPrefixes.Contains(prefix)) tracker.HTLocations.Add(name, data);
                else if (THPrefixes.Contains(prefix)) tracker.THLocations.Add(name, data);
                else if (BHPrefixes.Contains(prefix)) tracker.BHLocations.Add(name, data);
                else if (GDPrefixes.Contains(prefix)) tracker.GDLocations.Add(name, data);
                else if (BHPrefixes.Contains(prefix)) tracker.DBLocations.Add(name, data);
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
            foreach (var data in datas)
            {
                TrackerChecklistData checklistEntry = data.Value;
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

        public static void CheckItems(ReadOnlyCollection<long> checkedLocations)
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
        /// Returns if a location should be accessible according to logic
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool IsAccessible(TrackerLocationData data, bool useSaveFileItems)
        {
            bool accessible = true;
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
            APRandomizer.OWMLModConsole.WriteLine($"Determining requirements for {data.name}");
            foreach (TrackerLocationData.Requirement req in data.requires)
            {
                // If we don't have at least one of the quantity of required items, the location is inaccessible
                if (req.item != null && !ia.ContainsKey(ItemNames.itemNamesReversed[req.item])) accessible = false;
            }
            return accessible;
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
            return GetLocationChecklist(category).Count(x => x.Value.isAccessible);
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
