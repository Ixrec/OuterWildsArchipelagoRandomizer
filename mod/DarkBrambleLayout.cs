using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class DarkBrambleLayout
{
    // same as OuterFogWarpVolume.Name, but without the None value
    public enum DBRoom
    {
        Hub,
        EscapePod,
        AnglerNest,
        Pioneer,
        ExitOnly,
        Vessel,
        Cluster,
        SmallNest,
    }

    // These are just the warps we want to randomize. They exclude things like the recursive zones, most bramble seeds,
    // and all of the "outer" warps that either go back a zone or leave DB entirely.
    public enum DBWarp
    {
        Hub1,
        Hub2,
        Hub3,
        Hub4,
        Cluster1,
        Cluster2,
        EscapePod1,
        AnglerNest1,
        AnglerNest2,
    }

    public static Dictionary<DBRoom, List<DBWarp>> WarpsInRoom = new() {
        { DBRoom.Hub, new(){ DBWarp.Hub1, DBWarp.Hub2, DBWarp.Hub3, DBWarp.Hub4 } },
        { DBRoom.Cluster, new(){ DBWarp.Cluster1, DBWarp.Cluster2 } },
        { DBRoom.EscapePod, new(){ DBWarp.EscapePod1 } },
        { DBRoom.AnglerNest, new(){ DBWarp.AnglerNest1, DBWarp.AnglerNest2 } },
    };

    private record DBLayout
    {
        public DBRoom entrance;
        public Dictionary<DBWarp, DBRoom> warps;
    }

    private static int Seed = 42;

    private static DBLayout GenerateDBLayout()
    {
        /*
         algorithm:
        - call Pioneer, ExitOnly, Vessel and SmallNest the "dead end rooms"
        - call Hub, Cluster, EscapePod, AnglerNest "transit rooms"/non-dead end rooms
        - select a transit room for the entrance, initialize unmapped warps with that room's warps
        - while there are transit rooms unused:
            randomly select one of the unused warps, map it to a random unused transit room, add that room's warps to unmapped warps
        - while there are unused dead end rooms:
            randomly select one of the unused warps, map it to a random unused dead end room
        - while there are still unmapped warps:
            randomly pick any DB room to map them to
         */

        var unusedTransitRooms = new List<DBRoom> { DBRoom.Hub, DBRoom.Cluster, DBRoom.EscapePod, DBRoom.AnglerNest };
        var unusedDeadEndRooms = new List<DBRoom> { DBRoom.Pioneer, DBRoom.ExitOnly, DBRoom.Vessel, DBRoom.SmallNest };

        var prng = new System.Random(Seed);

        var db = new DBLayout();
        db.warps = new();
        var unmappedWarps = new List<DBWarp>();

        var entranceIndex = prng.Next(unusedTransitRooms.Count);
        db.entrance = unusedTransitRooms[entranceIndex];
        unusedTransitRooms.RemoveAt(entranceIndex);

        unmappedWarps.AddRange(WarpsInRoom[db.entrance]);

        while (unusedTransitRooms.Count > 0)
        {
            var warpIndex = prng.Next(unmappedWarps.Count);
            var warp = unmappedWarps[warpIndex];
            unmappedWarps.RemoveAt(warpIndex);

            var roomIndex = prng.Next(unusedTransitRooms.Count);
            var room = unusedTransitRooms[roomIndex];
            unusedTransitRooms.RemoveAt(roomIndex);

            db.warps[warp] = room;

            unmappedWarps.AddRange(WarpsInRoom[room]);
        }

        while (unusedDeadEndRooms.Count > 0)
        {
            var warpIndex = prng.Next(unmappedWarps.Count);
            var warp = unmappedWarps[warpIndex];
            unmappedWarps.RemoveAt(warpIndex);

            var roomIndex = prng.Next(unusedDeadEndRooms.Count);
            var room = unusedDeadEndRooms[roomIndex];
            unusedDeadEndRooms.RemoveAt(roomIndex);

            db.warps[warp] = room;
        }

        var allRooms = Enum.GetValues(typeof(DBRoom));
        while (unmappedWarps.Count > 0)
        {
            var warpIndex = prng.Next(unmappedWarps.Count);
            var warp = unmappedWarps[warpIndex];
            unmappedWarps.RemoveAt(warpIndex);

            var roomIndex = prng.Next(allRooms.Length);
            var room = (DBRoom)allRooms.GetValue(roomIndex);

            db.warps[warp] = room;
        }

        return db;
    }

    private static DBLayout CurrentDBLayout = GenerateDBLayout();

    // for testing
    [HarmonyPrefix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Update))]
    public static void ToolModeUI_Update_Prefix()
    {
        if (OWInput.SharedInputManager.IsNewlyPressed(InputLibrary.left2))
        {
        }
        if (OWInput.SharedInputManager.IsNewlyPressed(InputLibrary.right2))
        {
        }
        if (OWInput.SharedInputManager.IsNewlyPressed(InputLibrary.down2))
        {
            Seed++;
            CurrentDBLayout = GenerateDBLayout();
            ApplyDBLayout();
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(TravelerAudioManager), nameof(TravelerAudioManager.SyncTravelers))]
    public static void TravelerAudioManager_SyncTravelers_Prefix(TravelerAudioManager __instance)
    {
        try
        {
            var signals = __instance._signals;
            if (signals.Any(s => s == null))
            {
                APRandomizer.OWMLModConsole.WriteLine($"TravelerAudioManager_SyncTravelers_Prefix cleaning up references to vanilla AudioSignals we had to destroy, so they don't NRE later");
                __instance._signals = signals.Where(s => s != null).ToList();
            }
        }
        catch (Exception e) {
            APRandomizer.OWMLModConsole.WriteLine($"TravelerAudioManager_SyncTravelers_Prefix failed: {e.Message}\n{e.StackTrace}");
        }
    }

    private static InnerFogWarpVolume EntranceIFVW;
    private static Dictionary<DBRoom, OuterFogWarpVolume> RoomToOFWV = new();
    private static Dictionary<DBWarp, List<InnerFogWarpVolume>> WarpToIFWVs = new();

    public static void OnCompleteSceneLoad(OWScene _scene, OWScene _loadScene)
    {
        var pioneerInteractables = GameObject.Find("DB_PioneerDimension_Body/Sector_PioneerDimension/Interactables_PioneerDimension");
        var vesselInteractables = GameObject.Find("DB_VesselDimension_Body/Sector_VesselDimension/Interactables_VesselDimension");
        // base game inconsistency: SmallNest's root GO is DB_SmallNest_Body, not DB_SmallNestDimension_Body
        var smallNestInteractables = GameObject.Find("DB_SmallNest_Body/Sector_SmallNestDimension/Interactables_SmallNestDimension");
        var anglerNestInteractables = GameObject.Find("DB_AnglerNestDimension_Body/Sector_AnglerNestDimension/Interactables_AnglerNestDimension");
        var clusterInteractables = GameObject.Find("DB_ClusterDimension_Body/Sector_ClusterDimension/Interactables_ClusterDimension");
        var exitOnlyInteractables = GameObject.Find("DB_ExitOnlyDimension_Body/Sector_ExitOnlyDimension/Interactables_ExitOnlyDimension");
        var hubInteractables = GameObject.Find("DB_HubDimension_Body/Sector_HubDimension/Interactables_HubDimension");
        var escapePodInteractables = GameObject.Find("DB_EscapePodDimension_Body/Sector_EscapePodDimension/Interactables_EscapePodDimension");

        EntranceIFVW = GameObject.Find("DarkBramble_Body/Sector_DB/Interactables_DB/EntranceWarp_ToHub").GetComponent<InnerFogWarpVolume>();

        RoomToOFWV[DBRoom.Pioneer] = pioneerInteractables.transform.Find("OuterWarp_Pioneer").GetComponent<OuterFogWarpVolume>();
        RoomToOFWV[DBRoom.Vessel] = vesselInteractables.transform.Find("OuterWarp_Vessel").GetComponent<OuterFogWarpVolume>();
        RoomToOFWV[DBRoom.SmallNest] = smallNestInteractables.transform.Find("OuterWarp_SmallNest").GetComponent<OuterFogWarpVolume>();
        RoomToOFWV[DBRoom.AnglerNest] = anglerNestInteractables.transform.Find("OuterWarp_AnglerNest").GetComponent<OuterFogWarpVolume>();
        RoomToOFWV[DBRoom.Cluster] = clusterInteractables.transform.Find("OuterWarp_Cluster").GetComponent<OuterFogWarpVolume>();
        RoomToOFWV[DBRoom.ExitOnly] = exitOnlyInteractables.transform.Find("OuterWarp_ExitOnly").GetComponent<OuterFogWarpVolume>();
        RoomToOFWV[DBRoom.Hub] = hubInteractables.transform.Find("OuterWarp_Hub").GetComponent<OuterFogWarpVolume>();
        RoomToOFWV[DBRoom.EscapePod] = escapePodInteractables.transform.Find("OuterWarp_EscapePod").GetComponent<OuterFogWarpVolume>();

        var hubIFVWs = hubInteractables.transform.GetComponentsInChildren<InnerFogWarpVolume>();
        WarpToIFWVs[DBWarp.Hub1] = hubIFVWs.Where(ifvw => ifvw.name == "InnerWarp_ToCluster").ToList();
        WarpToIFWVs[DBWarp.Hub2] = hubIFVWs.Where(ifvw => ifvw.name == "InnerWarp_ToAnglerNest").ToList();
        WarpToIFWVs[DBWarp.Hub3] = hubIFVWs.Where(ifvw => ifvw.name == "InnerWarp_ToSmallNest").ToList();
        WarpToIFWVs[DBWarp.Hub4] = hubIFVWs.Where(ifvw => ifvw.name == "InnerWarp_ToEscapePod").ToList();

        var clusterIFVWs = clusterInteractables.transform.GetComponentsInChildren<InnerFogWarpVolume>();
        WarpToIFWVs[DBWarp.Cluster1] = clusterIFVWs.Where(ifvw => ifvw.name == "InnerWarp_ToPioneer" || ifvw.name == "SeedWarp_ToPioneer").ToList();
        WarpToIFWVs[DBWarp.Cluster2] = clusterIFVWs.Where(ifvw => ifvw.name == "InnerWarp_ToExitOnly").ToList();

        WarpToIFWVs[DBWarp.EscapePod1] = new List<InnerFogWarpVolume> {
            escapePodInteractables.transform.Find("InnerWarp_ToAnglerNest").GetComponent<InnerFogWarpVolume>()
        };

        var anglerNestIFVWs = anglerNestInteractables.transform.GetComponentsInChildren<InnerFogWarpVolume>();
        WarpToIFWVs[DBWarp.AnglerNest1] = anglerNestIFVWs.Where(ifvw => ifvw.name == "InnerWarp_ToExitOnly").ToList();
        WarpToIFWVs[DBWarp.AnglerNest2] = anglerNestIFVWs.Where(ifvw => ifvw.name == "InnerWarp_ToVessel").ToList();

        ApplyDBLayout();
    }

    private static void ApplyDBLayout()
    {
        if (CurrentDBLayout == null)
        {
            APRandomizer.OWMLModConsole.WriteLine($"ApplyDBLayout() returning early because DB has not been randomized");
            return;
        }

        APRandomizer.OWMLModConsole.WriteLine($"applying randomized Dark Bramble layout for seed={Seed}:\n" +
            $"space -> {CurrentDBLayout.entrance}\n{string.Join("\n", CurrentDBLayout.warps.Select(wr => $"{wr.Key}->{wr.Value}"))}");

        EntranceIFVW._linkedOuterWarpVolume = RoomToOFWV[CurrentDBLayout.entrance];
        EntranceIFVW._linkedOuterWarpVolume._linkedInnerWarpVolume = EntranceIFVW; // backing out of DB's first room should of course exit DB

        foreach (var (warp, room) in CurrentDBLayout.warps)
        {
            foreach (var ifwv in WarpToIFWVs[warp])
            {
                // This is the kind of warp we usually care about:
                // When you go "into" a spherical portal in one room, which room do you end up in?
                var newOFWVLink = RoomToOFWV[room];
                ifwv._linkedOuterWarpVolume = newOFWVLink;

                // Slightly more complex is "exiting" a DB room. When you pass through the room's OFWV, you emerge from its _linkedInnerWarpVolume.

                // Most commonly, the _linkedInnerWarpVolume is one of DB's exits to space.
                // We don't need to change these, so there's no code for them.

                // Three OFWVs link to the "previous" room, so we need to change those to match our new layout.
                if (newOFWVLink == RoomToOFWV[DBRoom.SmallNest] || newOFWVLink == RoomToOFWV[DBRoom.EscapePod] || newOFWVLink == RoomToOFWV[DBRoom.Cluster])
                {
                    // Unlike the vanilla layout, it's possible for there to be multiple entrances to one of these rooms,
                    // but an OFWV can only have one "exit" / linked IFWV. We simply let the last entrance "win".
                    ifwv._linkedOuterWarpVolume._linkedInnerWarpVolume = ifwv;
                }

                // The one unique case is Pioneer's OFWV linking "two zones back" to Hub.
                // I don't think it's worth figuring out what "two zones back" should mean in all possible randomized DB layouts,
                // so let's just edit this one to also go directly outside.
                if (newOFWVLink == RoomToOFWV[DBRoom.Pioneer])
                {
                    RoomToOFWV[DBRoom.Pioneer]._linkedInnerWarpVolume = EntranceIFVW;
                }
            }
        }

        // keeping this test since it crashed???
        /*
        entranceIFVW._linkedOuterWarpVolume = clusterOFWV;
        foreach (var ifvw in clusterToPioneerIFVWs) ifvw._linkedOuterWarpVolume = vesselOFWV;// hubOFWV; <- stackless crash???
        foreach (var ifvw in clusterToExitOnlyIFVWs) ifvw._linkedOuterWarpVolume = escapePodOFWV;
        escapePodToAnglerNestIFVW._linkedOuterWarpVolume = pioneerOFWV;
        foreach (var ifvw in hubToClusterIFVWs) ifvw._linkedOuterWarpVolume = pioneerOFWV;
        foreach (var ifvw in hubToEscapePodIFVWs) ifvw._linkedOuterWarpVolume = smallNestOFWV;
        */

        // Next, deal with the Signalscope signals.

        var signals = GameObject.FindObjectsOfType<AudioSignal>();

        // We can't directly "move" a signal. Instead we have to delete the vanilla signals
        // we've made incorrect, and create new ones in the correct places.

        // In the vanilla layout, Hub and Cluster are the only two DB rooms with "transitive" signals,
        // which are the ones we need to clean up.
        // Fortunately, these rooms do not contain any "root" signals that we want to leave alone,
        // so we can simply delete every signal in these two rooms.
        var signalsToDelete = signals.Where(s => {
            var ofwv = s._outerFogWarpVolume;
            return ofwv == RoomToOFWV[DBRoom.Hub] || ofwv == RoomToOFWV[DBRoom.Cluster];
        });
        APRandomizer.OWMLModConsole.WriteLine($"signalsToDelete {signalsToDelete.Count()} - {string.Join("|", signalsToDelete)}");

        // While deleting these signals, take references to their audio sources to help create the replacement signals.
        OWAudioSource harmonicaSource = null;
        OWAudioSource pod3Source = null;

        foreach (var s in signalsToDelete)
        {
            if (s.name == "Signal_Harmonica" && harmonicaSource == null)
                harmonicaSource = s._owAudioSource;
            if (s.name == "Signal_EscapePod" && pod3Source == null)
                pod3Source = s._owAudioSource;

            APRandomizer.OWMLModConsole.WriteLine($"deleting signals on {s.gameObject.transform.parent}/{s.gameObject}");
            s.gameObject.DestroyAllComponents<AudioSignal>();
        }

        HashSet<DBRoom> harmonicaRooms = new HashSet<DBRoom> { DBRoom.Pioneer };
        HashSet<DBRoom> ep3Rooms = new HashSet<DBRoom> { DBRoom.EscapePod };
        HashSet<DBWarp> harmonicaWarps = new();
        HashSet<DBWarp> ep3Warps = new();

        APRandomizer.OWMLModConsole.WriteLine($"start adding signals");
        bool roomSetsChanged = true;
        while (roomSetsChanged)
        {
            roomSetsChanged = false;

            APRandomizer.OWMLModConsole.WriteLine($"checking if more signals need to be added after {string.Join("|", harmonicaWarps)} and {string.Join("|", ep3Warps)}");
            foreach (var (warp, endRoom) in CurrentDBLayout.warps)
            {
                if (harmonicaRooms.Contains(endRoom) && !harmonicaWarps.Contains(warp))
                {
                    harmonicaWarps.Add(warp);

                    var startRoom = WarpsInRoom.First(roomAndWarps => roomAndWarps.Value.Contains(warp)).Key;
                    if (!harmonicaRooms.Contains(startRoom))
                    {
                        roomSetsChanged |= true;
                        harmonicaRooms.Add(startRoom);
                    }

                    foreach (var ifwv in WarpToIFWVs[warp])
                    {
                        var signal = ifwv.gameObject.AddComponent<AudioSignal>();
                        signal._onlyAudibleToScope = true;
                        signal._outerFogWarpVolume = RoomToOFWV[startRoom];

                        APRandomizer.OWMLModConsole.WriteLine($"adding harmonica signal to {warp}");
                        signal._frequency = SignalFrequency.Traveler;
                        signal._name = SignalName.Traveler_Feldspar;
                        signal._owAudioSource = harmonicaSource;
                    }
                }

                if (ep3Rooms.Contains(endRoom) && !ep3Warps.Contains(warp))
                {
                    ep3Warps.Add(warp);

                    var startRoom = WarpsInRoom.First(roomAndWarps => roomAndWarps.Value.Contains(warp)).Key;
                    if (!ep3Rooms.Contains(startRoom))
                    {
                        roomSetsChanged |= true;
                        ep3Rooms.Add(startRoom);
                    }

                    foreach (var ifwv in WarpToIFWVs[warp])
                    {
                        var signal = ifwv.gameObject.AddComponent<AudioSignal>();
                        signal._onlyAudibleToScope = true;
                        signal._outerFogWarpVolume = RoomToOFWV[startRoom];

                        APRandomizer.OWMLModConsole.WriteLine($"adding EP3 signal to {warp}");
                        signal._frequency = SignalFrequency.EscapePod;
                        signal._name = SignalName.EscapePod_DB;
                        signal._owAudioSource = pod3Source;
                    }
                }
            }
        }

        APRandomizer.OWMLModConsole.WriteLine($"finished adding signals, final warp sets were: {string.Join("|", harmonicaWarps)} and {string.Join("|", ep3Warps)}");
    }
}
