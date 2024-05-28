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
    public class LocationScouter
    {
        public static Dictionary<Location, ArchipelagoItem> ScoutedLocations;

        public LocationScouter() 
        {
            APRandomizer.OnSessionOpened += OnSessionOpened;
        }

        public void OnSessionOpened(ArchipelagoSession session)
        {
            // For now, simply assume that if we have any scouts at all, then we've scouted everything we care about.
            if (APRandomizer.SaveData.scoutedLocations != null)
                return;

            APRandomizer.OWMLModConsole.WriteLine($"save data does not contain any location scouts, so calling ScoutAllLocations()");
            ScoutAllLocations(session);
        }

        public void ScoutAllLocations(ArchipelagoSession session)
        {
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
            // Now we actually scout, code taken and modified from the Tunic randomizer (thanks Silent and Scipio!)
            var scoutTask = Task.Run(() => session.Locations.ScoutLocationsAsync(locationIDs.ToArray()).ContinueWith(locationInfoPacket =>
            {
                foreach (NetworkItem apLocationInfo in locationInfoPacket.Result.Locations)
                {
                    Location modLocation = LocationNames.archipelagoIdToLocation[apLocationInfo.Location];
                    string itemName = session.Items.GetItemName(apLocationInfo.Item) == null ? "UNKNOWN ITEM" : session.Items.GetItemName(apLocationInfo.Item);
                    ScoutedLocations.Add(modLocation, new(apLocationInfo.Item, itemName, apLocationInfo.Player, apLocationInfo.Flags));
                }

                APRandomizer.SaveData.scoutedLocations = ScoutedLocations;
                APRandomizer.Instance.WriteToSaveFile();
                APRandomizer.OWMLModConsole.WriteLine($"Cached {ScoutedLocations.Count} location scouts in save data.", OWML.Common.MessageType.Success);
            }));
            if (!scoutTask.Wait(TimeSpan.FromSeconds(5)))
            {
                APRandomizer.OWMLModConsole.WriteLine("Scouting failed! Hints will not be available this session.", OWML.Common.MessageType.Error);
            }
        }
    }
}
