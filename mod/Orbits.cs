using HarmonyLib;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class Orbits
{
    // for testing: lets you use the map screen from inside QM/Stranger/Dreamworld/etc
    /*[HarmonyPrefix, HarmonyPatch(typeof(MapController), nameof(MapController.MapInoperable))]
    public static bool MapController_MapInoperable(MapController __instance, ref bool __result)
    {
        __result = false;
        return false;
    }*/

    private static List<string> PlanetOrder = null;
    private static Dictionary<string, long> OrbitAngles = null;
    private static Dictionary<string, Vector3> RotationAxes = null;

    public static void ApplySlotData(object planetOrderSlotData, object orbitAnglesSlotData, object rotationAxesSlotData)
    {
        if (planetOrderSlotData is string coordsString && coordsString == "vanilla")
            // leaving vanilla orbits unchanged
            return;

        if (planetOrderSlotData is not JArray planetOrderArray)
        {
            APRandomizer.OWMLModConsole.WriteLine($"Leaving vanilla orbits unchanged because slot_data['planet_order'] was invalid: {planetOrderSlotData}", OWML.Common.MessageType.Error);
            return;
        }
        if (orbitAnglesSlotData is not JObject orbitAnglesObject)
        {
            APRandomizer.OWMLModConsole.WriteLine($"Leaving vanilla orbits unchanged because slot_data['orbit_angles'] was invalid: {orbitAnglesSlotData}", OWML.Common.MessageType.Error);
            return;
        }
        if (rotationAxesSlotData is not JObject rotationAxesObject)
        {
            APRandomizer.OWMLModConsole.WriteLine($"Leaving vanilla orbits unchanged because slot_data['rotation_axes'] was invalid: {rotationAxesSlotData}", OWML.Common.MessageType.Error);
            return;
        }

        PlanetOrder = new();
        foreach (JToken planetId in planetOrderArray)
            PlanetOrder.Add((string)planetId);

        OrbitAngles = new();
        foreach (var (objectId, angleToken) in orbitAnglesObject)
            OrbitAngles[objectId] = (long)angleToken;

        RotationAxes = new();
        foreach (var (objectId, direction) in rotationAxesObject)
            switch ((string)direction)
            {
                case "up": RotationAxes[objectId] = Vector3.up; break;
                case "down": RotationAxes[objectId] = Vector3.down; break;
                case "left": RotationAxes[objectId] = Vector3.left; break;
                case "right": RotationAxes[objectId] = Vector3.right; break;
                case "forward": RotationAxes[objectId] = Vector3.forward; break;
                case "back": RotationAxes[objectId] = Vector3.back; break;
                case "zero": RotationAxes[objectId] = Vector3.zero; break;
                default: APRandomizer.OWMLModConsole.WriteLine($"Unsupported direction '{direction}' in slot_data['rotation_axes']: {rotationAxesSlotData}", OWML.Common.MessageType.Error); break;
            }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(InitialMotion), nameof(InitialMotion.Awake))]
    public static void InitialMotion_Awake_Postfix(InitialMotion __instance)
    {
        // orbit randomization is off, do nothing
        if (PlanetOrder == null)
            return;

        var orbitingGONameToSlotDataId = new Dictionary<string, string> {
            { "FocalBody", "HGT" },
            { "TimberHearth_Body", "TH" },
            { "BrittleHollow_Body", "BH" },
            { "GiantsDeep_Body", "GD" },
            { "DarkBramble_Body", "DB" },
            { "SunStation_Body", "SS" },
            { "Moon_Body", "AR" },
            { "VolcanicMoon_Body", "HL" },
            { "OrbitalProbeCannon_Body", "OPC" },
        };

        if (orbitingGONameToSlotDataId.TryGetValue(__instance.name, out var orbitingId))
            if (OrbitAngles.TryGetValue(orbitingId, out var angle))
            {
                APRandomizer.OWMLModConsole.WriteLine($"setting {__instance}'s InitialMotion._orbitAngle to {angle}");
                __instance._orbitAngle = angle;
            }

        var rotatingGONameToSlotDataId = new Dictionary<string, string> {
            { "CaveTwin_Body", "ET" },
            { "TowerTwin_Body", "AT" },
            { "TimberHearth_Body", "TH" },
            { "BrittleHollow_Body", "BH" },
        };

        if (rotatingGONameToSlotDataId.TryGetValue(__instance.name, out var rotatingId))
            if (RotationAxes.TryGetValue(rotatingId, out var axis))
            {
                APRandomizer.OWMLModConsole.WriteLine($"setting {__instance}'s InitialMotion._rotationAxis to {axis}");
                __instance._rotationAxis = axis;
            }

        // arbitrarily choose one Awake() call to do planet order changes in
        if (__instance.name == "TimberHearth_Body")
        {
            var hgt = GameObject.Find("FocalBody");
            var th = GameObject.Find("TimberHearth_Body");
            var bh = GameObject.Find("BrittleHollow_Body");
            var gd = GameObject.Find("GiantsDeep_Body");
            var db = GameObject.Find("DarkBramble_Body");

            // have to save these values before we start moving the planets around
            List<Vector3> originalPositions = [
                hgt.transform.position,
                th.transform.position,
                bh.transform.position,
                gd.transform.position,
                db.transform.position
            ];

            var satellites = new Dictionary<GameObject, List<GameObject>>
            {
                { hgt, new() },
                { th, new List<GameObject>{ GameObject.Find("Moon_Body"), GameObject.Find("Satellite_Body") } },
                { bh, new List<GameObject>{ GameObject.Find("VolcanicMoon_Body") } },
                { gd, new List<GameObject>{ GameObject.Find("OrbitalProbeCannon_Body") } },
                { db, new() },
            };

            var sunPosition = GameObject.Find("Sun_Body").transform.position;

            var reorderableSlotDataIdToGOName = new Dictionary<string, string> {
                { "HGT", "FocalBody" },
                { "TH", "TimberHearth_Body" },
                { "BH", "BrittleHollow_Body" },
                { "GD", "GiantsDeep_Body" },
                { "DB", "DarkBramble_Body" },
            };
            for (int i = 0; i < PlanetOrder.Count; i++)
            {
                var planetId = PlanetOrder[i];
                var goName = reorderableSlotDataIdToGOName[planetId];
                var goToMove = GameObject.Find(goName);
                APRandomizer.OWMLModConsole.WriteLine($"moving {planetId} / {__instance} into lane {i}");

                var oldSunDistance = goToMove.transform.position - sunPosition;

                var targetSunDistance = originalPositions[i] - sunPosition;
                var distanceMultiplier = (targetSunDistance.magnitude / oldSunDistance.magnitude);

                var newPosition = sunPosition + (oldSunDistance * distanceMultiplier);
                var positionChange = newPosition - goToMove.transform.position;

                // Actually move the planet
                goToMove.transform.position += positionChange;
                // Also move the satellites orbiting that planet (this is why we need the position *change*, not just the new position)
                foreach (var satellite in satellites[goToMove])
                    satellite.transform.position += positionChange;
            }
        }
    }
}
