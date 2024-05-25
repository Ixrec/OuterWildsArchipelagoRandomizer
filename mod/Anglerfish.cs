using HarmonyLib;
using System;
using UnityEngine;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class Anglerfish
{
    private static bool _hasAnglerfishKnowledge = false;

    public static bool hasAnglerfishKnowledge
    {
        get => _hasAnglerfishKnowledge;
        set
        {
            if (_hasAnglerfishKnowledge != value)
            {
                _hasAnglerfishKnowledge = value;

                if (_hasAnglerfishKnowledge)
                {
                    var nd = new NotificationData(NotificationTarget.All, "RECONFIGURING SPACESHIP FOR SILENT RUNNING MODE", 10);
                    NotificationManager.SharedInstance.PostNotification(nd, false);
                }
            }
        }
    }

    static bool shipMakingNoise = false;
    static bool playerMakingNoise = false;

    // In the vanilla game, _noiseRadius is always between 0 and 400.
    // In testing, 200 seemed like an ideal distance for this because it allows you to think you
    // might make it, but it's still too high to survive the 3 fish at the start of fog zone 2.
    [HarmonyPostfix, HarmonyPatch(typeof(ShipNoiseMaker), nameof(ShipNoiseMaker.Update))]
    public static void ShipNoiseMaker_Update_Postfix(ref ShipNoiseMaker __instance)
    {
        if (!hasAnglerfishKnowledge)
            __instance._noiseRadius = Math.Max(__instance._noiseRadius, 200);

        __instance._noiseRadius = 0;
        var newShipNoise = __instance._noiseRadius > 0;
        if (newShipNoise != shipMakingNoise)
        {
            shipMakingNoise = newShipNoise;
            UpdatePromptText();
        }
    }
    [HarmonyPostfix, HarmonyPatch(typeof(PlayerNoiseMaker), nameof(PlayerNoiseMaker.Update))]
    public static void PlayerNoiseMaker_Update_Postfix(ref PlayerNoiseMaker __instance)
    {
        if (!hasAnglerfishKnowledge)
            __instance._noiseRadius = Math.Max(__instance._noiseRadius, 200);

        __instance._noiseRadius = 0;
        var newPlayerNoise = __instance._noiseRadius > 0;
        if (newPlayerNoise != playerMakingNoise)
        {
            playerMakingNoise = newPlayerNoise;
            UpdatePromptText();
        }
    }

    static string activeText = "Silent Running Mode: <color=green>Active</color>";
    static string inactiveText = "Silent Running Mode: <color=red>Inactive</color>";
    static ScreenPrompt silentRunningPrompt = new(activeText, 0);

    public static void UpdatePromptText()
    {
        if (shipMakingNoise || playerMakingNoise)
            silentRunningPrompt.SetText(inactiveText);
        else
            silentRunningPrompt.SetText(activeText);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.LateInitialize))]
    public static void ToolModeUI_LateInitialize_Postfix()
    {
        Locator.GetPromptManager().AddScreenPrompt(silentRunningPrompt, PromptPosition.UpperRight, false);
    }
    [HarmonyPostfix, HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Update))]
    public static void ToolModeUI_Update_Postfix()
    {
        silentRunningPrompt.SetVisibility(
            hasAnglerfishKnowledge &&
            (OWInput.IsInputMode(InputMode.Character) || OWInput.IsInputMode(InputMode.ShipCockpit)) &&
            (
                Locator.GetPlayerSectorDetector().IsWithinSector(Sector.Name.DarkBramble) ||
                Locator.GetPlayerSectorDetector().IsWithinSector(Sector.Name.BrambleDimension)
            )
        );
    }

    // the DB crash happens if ReceiveWarpedDetector runs either RepositionWarpedBody or AddObjectToVolume(detector.gameObject)
    [HarmonyPrefix, HarmonyPatch(typeof(FogWarpVolume), nameof(FogWarpVolume.ReceiveWarpedDetector))]
    public static bool FogWarpVolume_ReceiveWarpedDetector(FogWarpVolume __instance, FogWarpDetector detector, Vector3 localRelVelocity, Vector3 localPos, Quaternion localRot)
    {
        APRandomizer.OWMLModConsole.WriteLine($"FogWarpVolume_ReceiveWarpedDetector {__instance} {detector}");

        OWRigidbody owrigidbody = detector.GetOWRigidbody();
        APRandomizer.OWMLModConsole.WriteLine($"FogWarpVolume_ReceiveWarpedDetector A");
        bool flag = detector.CompareName(FogWarpDetector.Name.Player) && PlayerState.IsAttached() && !PlayerState.IsInsideShip();
        APRandomizer.OWMLModConsole.WriteLine($"FogWarpVolume_ReceiveWarpedDetector B");
        if (flag)
        {
            APRandomizer.OWMLModConsole.WriteLine($"FogWarpVolume_ReceiveWarpedDetector B1");
            owrigidbody = detector.GetOWRigidbody().transform.parent.GetComponentInParent<OWRigidbody>();
            MonoBehaviour.print("body to reposition: " + owrigidbody.name);
        }
        APRandomizer.OWMLModConsole.WriteLine($"FogWarpVolume_ReceiveWarpedDetector C");
        //__instance.RepositionWarpedBody(owrigidbody, localRelVelocity, localPos, localRot); // causes delayed crash
        APRandomizer.OWMLModConsole.WriteLine($"FogWarpVolume_ReceiveWarpedDetector D");
        if (flag)
        {
            APRandomizer.OWMLModConsole.WriteLine($"FogWarpVolume_ReceiveWarpedDetector D1");
            GlobalMessenger.FireEvent("PlayerRepositioned");
        }
        APRandomizer.OWMLModConsole.WriteLine($"FogWarpVolume_ReceiveWarpedDetector E");
        if (detector.CompareName(FogWarpDetector.Name.Ship) && PlayerState.IsInsideShip())
        {
            APRandomizer.OWMLModConsole.WriteLine($"FogWarpVolume_ReceiveWarpedDetector E1");
            __instance._sector.GetTriggerVolume().AddObjectToVolume(Locator.GetPlayerDetector());
            __instance._sector.GetTriggerVolume().AddObjectToVolume(Locator.GetPlayerCameraDetector());
        }
        APRandomizer.OWMLModConsole.WriteLine($"FogWarpVolume_ReceiveWarpedDetector F");
        __instance._sector.GetTriggerVolume().AddObjectToVolume(detector.gameObject); // crashes immediately
        APRandomizer.OWMLModConsole.WriteLine($"FogWarpVolume_ReceiveWarpedDetector G");
        detector.OnFogWarp();
        APRandomizer.OWMLModConsole.WriteLine($"FogWarpVolume_ReceiveWarpedDetector H");


        return false;
    }
    [HarmonyPrefix, HarmonyPatch(typeof(OWTriggerVolume), nameof(OWTriggerVolume.AddObjectToVolume))]
    public static bool OWTriggerVolume_AddObjectToVolume(OWTriggerVolume __instance, GameObject hitObj)
    {
        APRandomizer.OWMLModConsole.WriteLine($"OWTriggerVolume_AddObjectToVolume {__instance} {hitObj}");

        if (!__instance._active)
        {
            return false;
        }
        if (__instance.name == "Sector_HubDimension") APRandomizer.OWMLModConsole.WriteLine($"OWTriggerVolume_AddObjectToVolume A");
        if (__instance._trackedObjects.SafeAdd(hitObj))
        {
            if (__instance.name == "Sector_HubDimension") APRandomizer.OWMLModConsole.WriteLine($"OWTriggerVolume_AddObjectToVolume A1");
            __instance.AddHitObjectListeners(hitObj);
            if (__instance.name == "Sector_HubDimension") APRandomizer.OWMLModConsole.WriteLine($"OWTriggerVolume_AddObjectToVolume A1.5");
            __instance.FireEntryEvent(hitObj);
            if (__instance.name == "Sector_HubDimension") APRandomizer.OWMLModConsole.WriteLine($"OWTriggerVolume_AddObjectToVolume A2");
            return false;
        }
        if (__instance.name == "Sector_HubDimension") APRandomizer.OWMLModConsole.WriteLine($"OWTriggerVolume_AddObjectToVolume B");
        Debug.LogWarning("OWTriggerVolume " + __instance.gameObject.name + " already contains " + hitObj.name, __instance);
        if (__instance.name == "Sector_HubDimension") APRandomizer.OWMLModConsole.WriteLine($"OWTriggerVolume_AddObjectToVolume C");
        if (!__instance._ignoreDuplicateOccupantWarning && __instance._childEntryways.Count == 0 && __instance._sharedEntryways.Length == 0)
        {
            if (__instance.name == "Sector_HubDimension") APRandomizer.OWMLModConsole.WriteLine($"OWTriggerVolume_AddObjectToVolume C1");
            Debug.Break();
        }
        if (__instance.name == "Sector_HubDimension") APRandomizer.OWMLModConsole.WriteLine($"OWTriggerVolume_AddObjectToVolume D");

        return false;
    }
}
