using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Collections.ObjectModel;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;

namespace ArchipelagoRandomizer.InGameTracker
{
    /// <summary>
    /// Helper class for managing tracker accessibility displays
    /// </summary>
    public class TrackerLogic
    {
        public static ReadOnlyCollection<NetworkItem> previouslyObtainedItems;

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
            tracker.TrackerLocations = new();
            string path = APRandomizer.Instance.ModHelper.Manifest.ModFolderPath + "/locations.jsonc";
            if (File.Exists(path))
            {
                List<TrackerLocationData> locations = JsonConvert.DeserializeObject<List<TrackerLocationData>>(File.ReadAllText(path));
                // index the locations for faster searching
                foreach (TrackerLocationData location in locations)
                {
                    tracker.TrackerLocations.Add(location.name, location);
                }
                APRandomizer.OWMLModConsole.WriteLine($"{tracker.TrackerLocations.Count} locations parsed!", OWML.Common.MessageType.Success);
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
            foreach (TrackerInfo info in tracker.Infos.Values)
            {
                string prefix;
                string name = info.locationModID;
                if (name.StartsWith("SLF")) name = name.Replace("SLF__", "");
                prefix = name.Substring(0, 2);
                string nameKey = tracker.GetLocationByName(info).name;
                TrackerChecklistData data = new(false, false, "", "");
                if (HTPrefixes.Contains(prefix)) tracker.HTLocations.Add(info.locationModID, data);
                else if (THPrefixes.Contains(prefix)) tracker.THLocations.Add(info.locationModID, data);
                else if (BHPrefixes.Contains(prefix)) tracker.BHLocations.Add(info.locationModID, data);
                else if (GDPrefixes.Contains(prefix)) tracker.GDLocations.Add(info.locationModID, data);
                else if (BHPrefixes.Contains(prefix)) tracker.DBLocations.Add(info.locationModID, data);
                else tracker.OWLocations.Add(info.locationModID, data);
            }
        }

        /// <summary>
        /// Runs when the player receives a new item
        /// </summary>
        /// <param name="itemsHelper"></param>
        public static void RecheckLogic(ReceivedItemsHelper itemsHelper)
        {
            // only gets new items
            var xorCollection = itemsHelper.AllItemsReceived.Except(previouslyObtainedItems);

            foreach (var item in xorCollection)
            {
                // Only bother recalculating logic if the item actually unlocks checks
                if (item.Flags == Archipelago.MultiClient.Net.Enums.ItemFlags.Advancement)
                {
                    DetermineAllLogic();
                    // We only need recalculate logic once
                    return;
                }
            }
        }

        /// <summary>
        /// Determines all the accessibility for all locations
        /// </summary>
        /// <param name="category"></param>
        public static void DetermineAllLogic()
        {
            Dictionary<string, TrackerChecklistData> datas = GetLocationChecklist(TrackerCategory.All);
            foreach (var data in datas)
            {
                TrackerChecklistData checklistEntry = data.Value;
                // we can skip accessibility calculation if the location has been checked or ever been accessible
                if (!checklistEntry.hasBeenChecked || !checklistEntry.isAccessible)
                {
                    checklistEntry.SetAccessible(IsAccessible(tracker.TrackerLocations[data.Key]));
                }
            }
        }

        /// <summary>
        /// Returns if a location should be accessible according to logic
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool IsAccessible(TrackerLocationData data)
        {
            bool inLogic = true;
            var ia = APRandomizer.SaveData.itemsAcquired;
            foreach (TrackerLocationData.Requirement req in data.requires)
            {
                if (ia[ItemNames.itemNamesReversed[req.item]] <= 0) inLogic = false;
            }
            return inLogic;
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
    }
}
