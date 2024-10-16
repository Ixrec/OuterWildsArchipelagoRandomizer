using HarmonyLib;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArchipelagoRandomizer;

// TODO: During/after EotE integration, decide if we want to randomize orbits of:
// - HearthianMapSatellite_Body
// - RingWorld_Body (including StaticRing_Body)

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
        PlanetOrder = null;
        OrbitAngles = null;
        RotationAxes = null;

        ApplyOrbitLanesAndAngles(planetOrderSlotData, orbitAnglesSlotData);
        ApplyPlanetRotationAxes(rotationAxesSlotData);
    }

    public static void ApplyOrbitLanesAndAngles(object planetOrderSlotData, object orbitAnglesSlotData)
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

        PlanetOrder = new();
        foreach (JToken planetId in planetOrderArray)
            PlanetOrder.Add((string)planetId);

        OrbitAngles = new();
        foreach (var (objectId, angleToken) in orbitAnglesObject)
            OrbitAngles[objectId] = (long)angleToken;
    }

    public static void ApplyPlanetRotationAxes(object rotationAxesSlotData)
    {
        if (rotationAxesSlotData is string rotationString && rotationString == "vanilla")
        {
            // leaving vanilla rotations unchanged
            return;
        }

        if (rotationAxesSlotData is not JObject rotationAxesObject)
        {
            APRandomizer.OWMLModConsole.WriteLine($"Leaving vanilla planet rotations unchanged because slot_data['rotation_axes'] was invalid: {rotationAxesSlotData}", OWML.Common.MessageType.Error);
            return;
        }

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
            { "SS_Debris_Body", "SS" },
            { "Moon_Body", "AR" },
            { "VolcanicMoon_Body", "HL" },
            { "OrbitalProbeCannon_Body", "OPC" },
            { "CannonMuzzle_Body", "OPC" },
            { "CannonBarrel_Body", "OPC" },
        };

        if (orbitingGONameToSlotDataId.TryGetValue(__instance.name, out var orbitingId))
            if (OrbitAngles.TryGetValue(orbitingId, out var angle))
            {
                //APRandomizer.OWMLModConsole.WriteLine($"setting {__instance}'s InitialMotion._orbitAngle to {angle}");
                __instance._orbitAngle = angle;

                // the OPC debris objects need to be rotated around the main OPC object, because simply giving all three
                // a 90-degree orbit causes them to overlap (and squish the player) at the north and south poles of GD
                if (orbitingId == "OPC" && __instance.name != "OrbitalProbeCannon_Body")
                {
                    var opcPos = GameObject.Find("OrbitalProbeCannon_Body").transform.position;
                    var gdPos = GameObject.Find("GiantsDeep_Body").transform.position;
                    __instance.transform.RotateAround(opcPos, opcPos - gdPos, angle);
                }

                // the sun station needs to be perpendicular to its own orbit to make the spacewalk possible
                if (orbitingId == "SS")
                    __instance.transform.Rotate(new Vector3(0, angle, 0));
            }

        var rotatingGONameToSlotDataId = new Dictionary<string, string> {
            { "CaveTwin_Body", "ET" },
            { "TowerTwin_Body", "AT" },
            { "TimberHearth_Body", "TH" },
            { "BrittleHollow_Body", "BH" },
        };

        if (RotationAxes != null)
            if (rotatingGONameToSlotDataId.TryGetValue(__instance.name, out var rotatingId))
                if (RotationAxes.TryGetValue(rotatingId, out var axis))
                {
                    //APRandomizer.OWMLModConsole.WriteLine($"setting {__instance}'s InitialMotion._rotationAxis to {axis}");
                    __instance._rotationAxis = axis;

                    // Inside the ATP, the cables to the warp core are part of AT, but the ring you walk on is a separate TimeLoopRing object.
                    // If only AT's rotation is changed, this allows the cables to crush the player to death anywhere in the ATP, including
                    // on the warp platform as they arrive. So we want them to remain at least a little in sync, to keep the danger reasonable.
                    if (rotatingId == "AT")
                        GameObject.Find("TimeLoopRing_Body").GetComponent<InitialMotion>()._rotationAxis = -axis;
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
                { gd, new List<GameObject>{ GameObject.Find("OrbitalProbeCannon_Body"), GameObject.Find("CannonMuzzle_Body"), GameObject.Find("CannonBarrel_Body") } },
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
                //APRandomizer.OWMLModConsole.WriteLine($"moving {planetId} / {__instance} into lane {i}");

                var oldSunDistance = goToMove.transform.position - sunPosition;

                var targetSunDistance = originalPositions[i] - sunPosition;
                var distanceMultiplier = (targetSunDistance.magnitude / oldSunDistance.magnitude);

                var newPosition = sunPosition + (oldSunDistance * distanceMultiplier);
                var positionChange = newPosition - goToMove.transform.position;

                // Actually move the planet
                goToMove.transform.position += positionChange;
                // Rigidbody caches its GO's position, so if we don't manually invalidate its cache,
                // planets may compute a different orbit after the statue reset than they do on wakeup.
                goToMove.GetComponent<Rigidbody>().position = goToMove.transform.position;

                // Also move the satellites orbiting that planet (this is why we need the position *change*, not just the new position)
                foreach (var satellite in satellites[goToMove])
                {
                    satellite.transform.position += positionChange;
                    satellite.GetComponent<Rigidbody>().position = satellite.transform.position;
                }
            }
        }
    }

    // The following code is all about "unbreaking" the OPC's fake-to-real debris swap.
    // The vanilla code decides when to do the swap based on the angles between TH and GD, which
    // of course falls apart when we're doing randomized orbits, so we have to do it ourselves.
    // In the vanilla game the swap happens at 20 seconds, so we hardcode that number here.

    // TODO: make this work on "loop 0", when TimeLoop elapsed() methods always return 0?
    private static bool oplcDebrisSwitched = false;

    public static void OnCompleteSceneLoad(OWScene _scene, OWScene loadScene)
    {
        if (loadScene != OWScene.SolarSystem) return;

        //APRandomizer.OWMLModConsole.WriteLine($"resetting oplcDebrisSwitched to false, was {oplcDebrisSwitched}");
        oplcDebrisSwitched = false;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(OrbitalProbeLaunchController), nameof(OrbitalProbeLaunchController.FixedUpdate))]
    public static void OrbitalProbeLaunchController_FixedUpdate_Postfix(OrbitalProbeLaunchController __instance)
    {
        if (!oplcDebrisSwitched)
        {
            if (__instance._fakeCount > 0 && __instance._realCount > 0)
            {
                oplcDebrisSwitched = true;
                //APRandomizer.OWMLModConsole.WriteLine($"OrbitalProbeLaunchController_FixedUpdate_Postfix switched on its own at {TimeLoop.GetMinutesElapsed()} / {TimeLoop.GetSecondsElapsed()}");
            }
            else if (TimeLoop.GetSecondsElapsed() > 20)
            {
                // copy-pasted from OPLC::FixedUpdate(), with the if()s removed
                for (int i = 0; i < __instance._fakeDebrisBodies.Length; i++)
                {
                    if (__instance._fakeDebrisBodies[i] != null)
                    {
                        Object.Destroy(__instance._fakeDebrisBodies[i]?.gameObject);
                        __instance._fakeCount++;
                    }
                }
                for (int j = 0; j < __instance._realDebrisSectorProxies.Length; j++)
                {
                    __instance._realDebrisSectorProxies[j].gameObject.SetActive(true);
                    __instance._realCount++;
                }

                oplcDebrisSwitched = true;
                //APRandomizer.OWMLModConsole.WriteLine($"OrbitalProbeLaunchController_FixedUpdate_Postfix switched from fake OPC debris to real OPC debris because the vanilla code did not");
            }
        }
    }

    // useful for testing orbit combinations

    /*private static int TestAngle = 90;

    [HarmonyPrefix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Update))]
    public static void ToolModeUI_Update_Prefix()
    {
        if (OWInput.SharedInputManager.IsNewlyPressed(InputLibrary.up2))
        {
            TestAngle += 30;
            APRandomizer.OWMLModConsole.WriteLine($"TestAngle changed to {TestAngle}");
        }
        if (OWInput.SharedInputManager.IsNewlyPressed(InputLibrary.down2))
        {
            TestAngle -= 30;
            APRandomizer.OWMLModConsole.WriteLine($"TestAngle changed to {TestAngle}");
        }

        if (OWInput.SharedInputManager.IsNewlyPressed(InputLibrary.left2))
        {
            var p = PlanetOrder[0];
            PlanetOrder.RemoveAt(0);
            PlanetOrder.Add(p);
            APRandomizer.OWMLModConsole.WriteLine($"PlanetOrder changed to {string.Join(", ", PlanetOrder)}");
        }
        if (OWInput.SharedInputManager.IsNewlyPressed(InputLibrary.right2))
        {
            var p = PlanetOrder.Last();
            PlanetOrder.RemoveAt(PlanetOrder.Count - 1);
            PlanetOrder.Insert(0, p);
            APRandomizer.OWMLModConsole.WriteLine($"PlanetOrder changed to {string.Join(", ", PlanetOrder)}");
        }
    }*/
}
