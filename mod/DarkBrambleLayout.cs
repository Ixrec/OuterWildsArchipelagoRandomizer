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

    private static int seed = 0;

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

        seed++;
        var prng = new System.Random(seed);

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
            var dbl = GenerateDBLayout();
            APRandomizer.OWMLModConsole.WriteLine($"seed={seed}: space -> {dbl.entrance}\n{string.Join("\n", dbl.warps.Select(wr => $"{wr.Key}->{wr.Value}"))}");
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
        APRandomizer.OWMLModConsole.WriteLine($"{pioneerInteractables} - {vesselInteractables} - {smallNestInteractables} - {anglerNestInteractables} - {clusterInteractables} - {exitOnlyInteractables} - {hubInteractables} - {escapePodInteractables}");

        var pioneerOFWV = pioneerInteractables.transform.Find("OuterWarp_Pioneer").GetComponent<OuterFogWarpVolume>();
        var vesselOFWV = vesselInteractables.transform.Find("OuterWarp_Vessel").GetComponent<OuterFogWarpVolume>();
        var smallNestOFWV = smallNestInteractables.transform.Find("OuterWarp_SmallNest").GetComponent<OuterFogWarpVolume>();
        var anglerNestOFWV = anglerNestInteractables.transform.Find("OuterWarp_AnglerNest").GetComponent<OuterFogWarpVolume>();
        var clusterOFWV = clusterInteractables.transform.Find("OuterWarp_Cluster").GetComponent<OuterFogWarpVolume>();
        var exitOnlyOFWV = exitOnlyInteractables.transform.Find("OuterWarp_ExitOnly").GetComponent<OuterFogWarpVolume>();
        var hubOFWV = hubInteractables.transform.Find("OuterWarp_Hub").GetComponent<OuterFogWarpVolume>();
        var escapePodOFWV = escapePodInteractables.transform.Find("OuterWarp_EscapePod").GetComponent<OuterFogWarpVolume>();
        APRandomizer.OWMLModConsole.WriteLine($"{pioneerOFWV} - {vesselOFWV} - {smallNestOFWV} - {anglerNestOFWV} - {clusterOFWV} - {exitOnlyOFWV} - {hubOFWV} - {escapePodOFWV}");

        var entranceIFVW = GameObject.Find("DarkBramble_Body/Sector_DB/Interactables_DB/EntranceWarp_ToHub").GetComponent<InnerFogWarpVolume>();

        var clusterIFVWs = clusterInteractables.transform.GetComponentsInChildren<InnerFogWarpVolume>();
        var clusterToPioneerIFVWs = clusterIFVWs.Where(ifvw => ifvw.name == "InnerWarp_ToPioneer");
        var clusterToExitOnlyIFVWs = clusterIFVWs.Where(ifvw => ifvw.name == "InnerWarp_ToExitOnly");

        var escapePodToAnglerNestIFVW = escapePodInteractables.transform.Find("InnerWarp_ToAnglerNest").GetComponent<InnerFogWarpVolume>();

        var anglerNestIFVWs = anglerNestInteractables.transform.GetComponentsInChildren<InnerFogWarpVolume>();
        var anglerNestToExitOnlyIFVWs = clusterIFVWs.Where(ifvw => ifvw.name == "InnerWarp_ToExitOnly");
        var anglerNestToVesselIFVWs = clusterIFVWs.Where(ifvw => ifvw.name == "InnerWarp_ToVessel");

        var hubIFVWs = hubInteractables.transform.GetComponentsInChildren<InnerFogWarpVolume>();
        var hubToClusterIFVWs = clusterIFVWs.Where(ifvw => ifvw.name == "InnerWarp_ToCluster");
        var hubToAnglerNestIFVWs = clusterIFVWs.Where(ifvw => ifvw.name == "InnerWarp_ToAnglerNest");
        var hubToSmallNestIFVWs = clusterIFVWs.Where(ifvw => ifvw.name == "InnerWarp_ToSmallNest");
        var hubToEscapePodIFVWs = clusterIFVWs.Where(ifvw => ifvw.name == "InnerWarp_ToEscapePod");

        foreach (var ifvw in GameObject.FindObjectsOfType<InnerFogWarpVolume>())
            if (ifvw?._linkedOuterWarpVolume?._linkedInnerWarpVolume != ifvw)
                APRandomizer.OWMLModConsole.WriteLine($"mismatch: {ifvw?.transform?.parent?.name}/{ifvw?.name} -> {ifvw?._linkedOuterWarpVolume?.transform?.parent?.name}/{ifvw?._linkedOuterWarpVolume?.name} -> {ifvw?._linkedOuterWarpVolume?._linkedInnerWarpVolume?.transform?.parent?.name}/{ifvw?._linkedOuterWarpVolume?.name}");

        // actually edit some warps
        /*
        entranceIFVW._linkedOuterWarpVolume = clusterOFWV;
        foreach (var ifvw in clusterToPioneerIFVWs) ifvw._linkedOuterWarpVolume = vesselOFWV;// hubOFWV; <- stackless crash???
        foreach (var ifvw in clusterToExitOnlyIFVWs) ifvw._linkedOuterWarpVolume = escapePodOFWV;
        escapePodToAnglerNestIFVW._linkedOuterWarpVolume = pioneerOFWV;
        foreach (var ifvw in hubToClusterIFVWs) ifvw._linkedOuterWarpVolume = pioneerOFWV;
        foreach (var ifvw in hubToEscapePodIFVWs) ifvw._linkedOuterWarpVolume = smallNestOFWV;
        */

        /*
                space-> Hub
        Hub1->EscapePod
        EscapePod1->AnglerNest
        Hub4->Cluster
        Cluster2->Pioneer
        AnglerNest2->ExitOnly
        Hub2->Vessel
        Cluster1->SmallNest
        AnglerNest1->AnglerNest
        Hub3->Vessel
                    */
        APRandomizer.OWMLModConsole.WriteLine($"changing warps");
        /*entranceIFVW._linkedOuterWarpVolume = hubOFWV; // all edits to hub IFVWs ignored???
        foreach (var ifvw in hubToClusterIFVWs) ifvw._linkedOuterWarpVolume = escapePodOFWV;
        escapePodToAnglerNestIFVW._linkedOuterWarpVolume = anglerNestOFWV;
        foreach (var ifvw in hubToEscapePodIFVWs) ifvw._linkedOuterWarpVolume = clusterOFWV;
        foreach (var ifvw in clusterToExitOnlyIFVWs) ifvw._linkedOuterWarpVolume = pioneerOFWV;
        foreach (var ifvw in anglerNestToVesselIFVWs) ifvw._linkedOuterWarpVolume = exitOnlyOFWV;
        foreach (var ifvw in hubToAnglerNestIFVWs) ifvw._linkedOuterWarpVolume = vesselOFWV;
        foreach (var ifvw in clusterToPioneerIFVWs) ifvw._linkedOuterWarpVolume = smallNestOFWV;
        foreach (var ifvw in anglerNestToExitOnlyIFVWs) ifvw._linkedOuterWarpVolume = anglerNestOFWV;
        foreach (var ifvw in hubToSmallNestIFVWs) ifvw._linkedOuterWarpVolume = vesselOFWV;*/

        /*
        space -> AnglerNest
        AnglerNest1->Hub
        Hub4->Cluster
        Hub1->EscapePod
        Hub3->Pioneer
        AnglerNest2->SmallNest
        Hub2->ExitOnly
        Cluster2->Vessel
        Cluster1->Vessel
        EscapePod1->Vessel
            */
        entranceIFVW._linkedOuterWarpVolume = anglerNestOFWV;
        foreach (var ifvw in anglerNestToExitOnlyIFVWs) ifvw._linkedOuterWarpVolume = hubOFWV; // ignored!
        foreach (var ifvw in hubToEscapePodIFVWs) ifvw._linkedOuterWarpVolume = clusterOFWV;
        foreach (var ifvw in hubToClusterIFVWs) ifvw._linkedOuterWarpVolume = escapePodOFWV;
        foreach (var ifvw in hubToSmallNestIFVWs) ifvw._linkedOuterWarpVolume = pioneerOFWV;
        foreach (var ifvw in anglerNestToVesselIFVWs) ifvw._linkedOuterWarpVolume = smallNestOFWV; // ignored?
        foreach (var ifvw in hubToAnglerNestIFVWs) ifvw._linkedOuterWarpVolume = exitOnlyOFWV;
        foreach (var ifvw in clusterToExitOnlyIFVWs) ifvw._linkedOuterWarpVolume = vesselOFWV;
        foreach (var ifvw in clusterToPioneerIFVWs) ifvw._linkedOuterWarpVolume = vesselOFWV;
        escapePodToAnglerNestIFVW._linkedOuterWarpVolume = vesselOFWV;

        /*var ofwvs = GameObject.FindObjectsOfType<OuterFogWarpVolume>();
        APRandomizer.OWMLModConsole.WriteLine($"ofwvs: {ofwvs.Length}\n{string.Join("\n", ofwvs.Select(ofwv => {
            return $"{ofwv.transform.parent.name}/{ofwv.name} ({ofwv._name}) - {ofwv._linkedInnerWarpVolume.transform.parent.name}/{ofwv._linkedInnerWarpVolume.name}";
        }))}");

        var ifwvs = GameObject.FindObjectsOfType<InnerFogWarpVolume>();
        APRandomizer.OWMLModConsole.WriteLine($"ifwvs: {ifwvs.Length}\n{string.Join("\n", ifwvs.Select(ifwv => {
            return $"{ifwv.transform?.parent?.name}/{ifwv.name} - {ifwv._linkedOuterWarpVolume?.transform?.parent?.name}/{ifwv._linkedOuterWarpVolume?.name} ({ifwv._linkedOuterWarpName})";
        }))}");*/

        var signals = GameObject.FindObjectsOfType<AudioSignal>();
        /*APRandomizer.OWMLModConsole.WriteLine($"signals: {signals.Length}\n{string.Join("\n", signals.Select(s => {
            return $"{s.transform?.parent?.name}/{s.name} - {s._outerFogWarpVolume?.transform?.parent?.name}/{s._outerFogWarpVolume?.name}";
        }))}");*/

        OWAudioSource harmonicaSource = null;
        OWAudioSource pod3Source = null;

        // actually delete all the vanilla signal on nodes inside DB (the signals on the DB exterior/entrance are untouched)
        var dbInteriorSignals = signals.Where(s => s._outerFogWarpVolume != null);
        foreach (var s in dbInteriorSignals)
        {
            if (s.name == "Signal_Harmonica" && harmonicaSource == null)
                harmonicaSource = s._owAudioSource;
            if (s.name == "Signal_EscapePod" && pod3Source == null)
                pod3Source = s._owAudioSource;
            s.gameObject.DestroyAllComponents<AudioSignal>();
        }

        var signal = escapePodToAnglerNestIFVW.gameObject.AddComponent<AudioSignal>();
        signal._frequency = SignalFrequency.Traveler;
        signal._name = SignalName.Traveler_Feldspar;
        signal._onlyAudibleToScope = true;
        signal._outerFogWarpVolume = escapePodOFWV;
        signal._owAudioSource = harmonicaSource;
    }
}
