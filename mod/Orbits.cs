using HarmonyLib;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
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
        /*if (planetOrderSlotData is string coordsString && coordsString == "vanilla")
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
        }*/
        PlanetOrder = ["GD", "DB", "HGT", "BH", "TH"];
        //foreach (JToken planetId in planetOrderArray)
        //    PlanetOrder.Add((string)planetId);

        OrbitAngles = new Dictionary<string, long> {
            { "GD", 330 },
            { "DB", 240 },
            { "HGT", 210 },
            { "BH", 210 },
            { "TH", 330 },
            { "SS", 60 },
            { "AR", 180 },
            {  "HL", 60 },
            { "OPC", 330 }
        };
        //foreach (var (objectId, angleToken) in orbitAnglesObject)
        //    OrbitAngles[objectId] = (long)angleToken;

        RotationAxes = new Dictionary<string, Vector3> {
            { "ET", Vector3.left },
            { "AT", Vector3.zero },
            { "TH", Vector3.right },
            { "BH", Vector3.left },
        };
        /*foreach (var (objectId, direction) in rotationAxesObject)
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
            }*/
    }

    [HarmonyPostfix, HarmonyPatch(typeof(OWPhysics), nameof(OWPhysics.CalculateOrbitVelocity))]
    public static void OWPhysics_CalculateOrbitVelocity(Vector3 __result, OWRigidbody primaryBody, OWRigidbody satelliteBody, float orbitAngle)
    {
        if (satelliteBody.name == "TimberHearth_Body")
        {
            APRandomizer.OWMLModConsole.WriteLine($"OWPhysics_CalculateOrbitVelocity {primaryBody} / {satelliteBody} / {orbitAngle} -> {__result}");

            GravityVolume attachedGravityVolume = primaryBody.GetAttachedGravityVolume();
            if (attachedGravityVolume == null)
                return;

            APRandomizer.OWMLModConsole.WriteLine($"OWPhysics_CalculateOrbitVelocity({primaryBody.name},{satelliteBody.name}) GetWorldCenterOfMass() = {satelliteBody.GetWorldCenterOfMass()}");//, IsKinematic() = {satelliteBody.IsKinematic()}, IsSimulatedKinematic() = {satelliteBody.IsSimulatedKinematic()}");
            APRandomizer.OWMLModConsole.WriteLine($"OWPhysics_CalculateOrbitVelocity({primaryBody.name},{satelliteBody.name}) transform.position = {satelliteBody.transform.position}, RigidBody.position = {satelliteBody.GetRigidbody().position}");
            var s = satelliteBody.GetWorldCenterOfMass(); // this is the first thing that changes between wakeup and statue
            var p = primaryBody.GetWorldCenterOfMass(); // sun is always 0,0,0
            Vector3 vector = s - p; // this is the same
            Vector3 vector2 = Vector3.Cross(vector, Vector3.up).normalized;
            vector2 = Quaternion.AngleAxis(orbitAngle, vector) * vector2;
            float num = Mathf.Sqrt(attachedGravityVolume.CalculateForceAccelerationOnBody(satelliteBody).magnitude * vector.magnitude); // 2nd change
            var r = vector2 * num;
            APRandomizer.OWMLModConsole.WriteLine($"OWPhysics_CalculateOrbitVelocity({primaryBody},{satelliteBody}) patch result = {r}");
        }
    }
    [HarmonyPostfix, HarmonyPatch(typeof(InitialMotion), nameof(InitialMotion.GetInitVelocity))]
    public static void InitialMotion_GetInitVelocity(InitialMotion __instance, Vector3 __result)
    {
        if (__instance.name == "Sun_Body" || __instance.name == "TimberHearth_Body")
        {
            APRandomizer.OWMLModConsole.WriteLine($"InitialMotion_GetInitVelocity {__instance} -> {__result}");
        }
    }
    [HarmonyPrefix, HarmonyPatch(typeof(InitialMotion), nameof(InitialMotion.Start))]
    public static void InitialMotion_Start_Prefix(InitialMotion __instance)
    {
        if (__instance.name == "TimberHearth_Body")
        {
            APRandomizer.OWMLModConsole.WriteLine($"InitialMotion_Start_Prefix {__instance}'s initial v is {__instance._isInitVelocityDirty} / {__instance._cachedInitVelocity}");
            APRandomizer.OWMLModConsole.WriteLine($"InitialMotion_Start_Prefix {__instance}: {__instance._initLinearDirection.normalized} / {__instance._initLinearSpeed} / {__instance._primaryBody} / {__instance._satelliteBody} / {__instance._orbitAngle} / {__instance._orbitImpulseScalar}");
        }
    }
    [HarmonyPostfix, HarmonyPatch(typeof(InitialMotion), nameof(InitialMotion.Start))]
    public static void InitialMotion_Start_Postfix(InitialMotion __instance)
    {
        if (__instance.name == "TimberHearth_Body")
        {
            APRandomizer.OWMLModConsole.WriteLine($"InitialMotion_Start_Postfix {__instance}'s initial v is {__instance._isInitVelocityDirty} / {__instance._cachedInitVelocity}");
            APRandomizer.OWMLModConsole.WriteLine($"InitialMotion_Start_Prefix {__instance}: {__instance._initLinearDirection.normalized} / {__instance._initLinearSpeed} / {__instance._primaryBody} / {__instance._satelliteBody} / {__instance._orbitAngle} / {__instance._orbitImpulseScalar}");
        }
    }

    /*[HarmonyPrefix, HarmonyPatch(typeof(OWRigidbody), nameof(OWRigidbody.RunningKinematicSimulation))]
    public static void OWRigidbody_RunningKinematicSimulation_Prefix(OWRigidbody __instance, ref bool __result)
    {
        if (__instance.name == "TimberHearth_Body")
        {
            APRandomizer.OWMLModConsole.WriteLine($"OWRigidbody_RunningKinematicSimulation_Prefix {__instance}");
        }
    }
    [HarmonyPostfix, HarmonyPatch(typeof(OWRigidbody), nameof(OWRigidbody.RunningKinematicSimulation))]
    public static void OWRigidbody_RunningKinematicSimulation_Postfix(OWRigidbody __instance, ref bool __result)
    {
        if (__instance.name == "TimberHearth_Body")
        {
            APRandomizer.OWMLModConsole.WriteLine($"OWRigidbody_RunningKinematicSimulation_Postfix {__instance} -> {__result}");
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(OWRigidbody), nameof(OWRigidbody.GetWorldCenterOfMass))]
    public static void OWRigidbody_GetWorldCenterOfMass_Prefix(OWRigidbody __instance, ref Vector3 __result)
    {
        if (__instance.name == "TimberHearth_Body")
        {
            APRandomizer.OWMLModConsole.WriteLine($"OWRigidbody_GetWorldCenterOfMass_Prefix {__instance}");
        }
    }
    [HarmonyPostfix, HarmonyPatch(typeof(OWRigidbody), nameof(OWRigidbody.GetWorldCenterOfMass))]
    public static void OWRigidbody_GetWorldCenterOfMass_Postfix(OWRigidbody __instance, ref Vector3 __result)
    {
        if (__instance.name == "TimberHearth_Body")
        {
            APRandomizer.OWMLModConsole.WriteLine($"OWRigidbody_GetWorldCenterOfMass_Postfix {__instance} -> {__result}");
        }
    }*/

    [HarmonyPostfix, HarmonyPatch(typeof(InitialMotion), nameof(InitialMotion.Awake))]
    public static void InitialMotion_Awake_Postfix(InitialMotion __instance)
    {
        if (__instance.name == "TimberHearth_Body")
            APRandomizer.OWMLModConsole.WriteLine($"InitialMotion_Awake_Postfix {__instance}'s initial v is {__instance._isInitVelocityDirty} / {__instance._cachedInitVelocity}");

        PlanetOrder = ["TH", "HGT", "DB", "BH", "GD"];
        OrbitAngles = new Dictionary<string, long> {
            { "GD", 30 },
            { "DB", 0},
            { "HGT", 0 },
            { "BH", 0 },
            { "TH", 30 },
            { "SS", 330 },
            { "AR", 0 },
            {  "HL", 240 },
            { "OPC", 90 }
        };
        RotationAxes = new Dictionary<string, Vector3> {
            { "ET", Vector3.back },
            { "AT", Vector3.forward },
            { "TH", Vector3.right },
            { "BH", Vector3.forward },
        };

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
                if (__instance.name == "TimberHearth_Body")
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
                if (__instance.name == "TimberHearth_Body")
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
            APRandomizer.OWMLModConsole.WriteLine($"sunPosition = {sunPosition}");

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
                APRandomizer.OWMLModConsole.WriteLine($"moving {planetId} / {goToMove} into lane {i}");

                var oldSunDistance = goToMove.transform.position - sunPosition;

                var targetSunDistance = originalPositions[i] - sunPosition;
                APRandomizer.OWMLModConsole.WriteLine($"changing {planetId} / {goToMove}'s sun distance from {oldSunDistance} to {targetSunDistance}");
                var distanceMultiplier = (targetSunDistance.magnitude / oldSunDistance.magnitude);

                var newPosition = sunPosition + (oldSunDistance * distanceMultiplier);
                var positionChange = newPosition - goToMove.transform.position;

                // Actually move the planet
                APRandomizer.OWMLModConsole.WriteLine($"{planetId} / {goToMove} position before = {goToMove.transform.position}, {goToMove.GetComponent<Rigidbody>().position}");
                goToMove.transform.position += positionChange;
                goToMove.GetComponent<Rigidbody>().position = goToMove.transform.position;
                APRandomizer.OWMLModConsole.WriteLine($"{planetId} / {goToMove} position after = {goToMove.transform.position}, {goToMove.GetComponent<Rigidbody>().position}");
                // Also move the satellites orbiting that planet (this is why we need the position *change*, not just the new position)
                foreach (var satellite in satellites[goToMove])
                {
                    satellite.transform.position += positionChange;
                    satellite.GetComponent<Rigidbody>().position = satellite.transform.position;
                }
            }
        }
    }
}
