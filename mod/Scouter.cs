﻿using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipelagoRandomizer
{
    /// <summary>
    /// Class responsible for scouting locations and returning their values and IDs, for hinting and changing the appearance of checks
    /// </summary>
    public class Scouter
    {
        public static Dictionary<Location, ArchipelagoItem> ScoutedLocations;

        public Scouter() 
        {
            APRandomizer.OnSessionOpened += ScoutAllLocations;
        }

        public void ScoutAllLocations(ArchipelagoSession session)
        {
            ScoutedLocations = new();
            List<long> locations = new List<long>();
            foreach (Location loc in LocationNames.locationNames.Keys)
            {
                // annoying exception
                if (loc == Location.SLF__TH_VILLAGE_X3) continue;

                // we don't need to scout logsanity locations if logsanity is off
                bool logsanity = APRandomizer.SlotData.ContainsKey("logsanity") && (long)APRandomizer.SlotData["logsanity"] != 0;
                APRandomizer.OWMLModConsole.WriteLine($"Got location {loc} and logsanity is {logsanity}");
                if (!logsanity && LocationNames.IsLogsanityLocation(loc)) continue;


                locations.Add(LocationNames.locationToArchipelagoId[loc]);
            }
            // Now we actually scout, code taken and modified from the Tunic randomizer (thanks Silent and Scipio!)
            session.Locations.ScoutLocationsAsync(locations.ToArray()).ContinueWith(locationInfoPacket =>
            {
                foreach (NetworkItem location in locationInfoPacket.Result.Locations)
                {
                    Location name = LocationNames.archipelagoIdToLocation[location.Location];
                    string item = session.Items.GetItemName(location.Item) == null ? "UNKNOWN ITEM" : session.Items.GetItemName(location.Item);
                    ScoutedLocations.Add(name, new(item, location.Player, location.Flags));
                }

                APRandomizer.OWMLModConsole.WriteLine("All locations scouted.", OWML.Common.MessageType.Success);
            }).Wait(TimeSpan.FromSeconds(5));

        }
    }
}