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
            {
                ScoutedLocations = APRandomizer.SaveData.scoutedLocations;
                APRandomizer.OWMLModConsole.WriteLine($"Scouted locations loaded from save file.", OWML.Common.MessageType.Success);
                return;
            }

            APRandomizer.OWMLModConsole.WriteLine($"save data does not contain any location scouts, so calling ScoutAllLocations()");
            ScoutAllHintableLocations(session);
        }

        public void ScoutAllHintableLocations(ArchipelagoSession session)
        {
            List<string> hintablePrefixes = new();
            foreach (var (_, prefixes) in Hints.characterToLocationPrefixes)
                hintablePrefixes.AddRange(prefixes);

            List<long> hintableLocationIDs = LocationNames.locationNames.Keys
                .Where(loc => LocationNames.locationToArchipelagoId.ContainsKey(loc))
                .Where(loc => hintablePrefixes.Any(p => LocationNames.locationNames[loc].StartsWith(p)))
                .Select(loc => LocationNames.locationToArchipelagoId[loc])
                .ToList();

            // Now we actually scout, code taken and modified from the Tunic randomizer (thanks Silent and Scipio!)
            ScoutedLocations = new();
            var scoutTask = Task.Run(() => session.Locations.ScoutLocationsAsync(hintableLocationIDs.ToArray()).ContinueWith(locationInfoPacket =>
            {
                foreach (var (locationId, scoutedItemInfo) in locationInfoPacket.Result)
                {
                    Location modLocation = LocationNames.archipelagoIdToLocation[locationId];
                    ScoutedLocations.Add(modLocation, new(scoutedItemInfo.ItemId, scoutedItemInfo.ItemName, scoutedItemInfo.Player, scoutedItemInfo.Flags));
                }

                APRandomizer.SaveData.scoutedLocations = ScoutedLocations;
                APRandomizer.WriteToSaveFile();
                APRandomizer.OWMLModConsole.WriteLine($"Cached {ScoutedLocations.Count} location scouts in save data.", OWML.Common.MessageType.Success);
            }));
            if (!scoutTask.Wait(TimeSpan.FromSeconds(5)))
            {
                APRandomizer.OWMLModConsole.WriteLine("Scouting failed! Hints will not be available this session. There was likely an APWorld mismatch, check the server logs for more info.", OWML.Common.MessageType.Error);
            }
        }
    }
}
