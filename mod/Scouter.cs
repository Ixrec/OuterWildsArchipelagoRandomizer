using Archipelago.MultiClient.Net;
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
            APRandomizer.OWMLModConsole.WriteLine("Starting scout process...", OWML.Common.MessageType.Message);
            ScoutedLocations = new();
            List<long> locationIDs = new List<long>();
            foreach (Location loc in LocationNames.locationNames.Keys)
            {
                // annoying exception
                if (loc == Location.SLF__TH_VILLAGE_X3) continue;

                // we don't need to scout logsanity locations if logsanity is off
                bool logsanity = APRandomizer.SlotData.ContainsKey("logsanity") && (long)APRandomizer.SlotData["logsanity"] != 0;
                if (!logsanity && LocationNames.IsLogsanityLocation(loc)) continue;


                locationIDs.Add(LocationNames.locationToArchipelagoId[loc]);
            }
            APRandomizer.OWMLModConsole.WriteLine($"Compiled list of locations to scout, with {locationIDs.Count} entries...", OWML.Common.MessageType.Message);

            // Now we actually scout, code taken and modified from the Tunic randomizer (thanks Silent and Scipio!)
            session.Locations.ScoutLocationsAsync(locationIDs.ToArray()).ContinueWith(locationInfoPacket =>
            {
                APRandomizer.OWMLModConsole.WriteLine("Scouting items...", OWML.Common.MessageType.Message);
                foreach (NetworkItem location in locationInfoPacket.Result.Locations)
                {
                    Location name = LocationNames.archipelagoIdToLocation[location.Location];
                    APRandomizer.OWMLModConsole.WriteLine($"Adding location {name}");
                    string item = session.Items.GetItemName(location.Item) == null ? "UNKNOWN ITEM" : session.Items.GetItemName(location.Item);
                    ScoutedLocations.Add(name, new(item, location.Player, location.Flags));
                }

                APRandomizer.OWMLModConsole.WriteLine("All locations scouted.", OWML.Common.MessageType.Success);
            }).Wait(TimeSpan.FromSeconds(5));

        }
    }
}
