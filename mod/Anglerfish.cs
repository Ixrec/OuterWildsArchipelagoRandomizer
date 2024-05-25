using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
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

    private static HashSet<OuterFogWarpVolume.Name> VisitedOFWVs = new();

    [HarmonyPrefix, HarmonyPatch(typeof(OuterFogWarpVolume), nameof(OuterFogWarpVolume.PropagateCanvasMarkerOutwards))]
    public static bool OuterFogWarpVolume_PropagateCanvasMarkerOutwards(OuterFogWarpVolume __instance, CanvasMarker marker, bool addMarker, float warpDist = 0f)
    {
        var ofwvName = marker.GetOuterFogWarpVolume().GetName();
        if (VisitedOFWVs.Contains(ofwvName))
        {
            //APRandomizer.OWMLModConsole.WriteLine($"detected duplicate OFWV.PropagateCanvasMarkerOutwards() calls on {ofwvName}, skipping base game code to prevent infinite loop");
            if (marker.GetSecondaryLabelType() == CanvasMarker.SecondaryLabelType.NONE)
            {
                //APRandomizer.OWMLModConsole.WriteLine($"adding custom duplicate warning to {marker._label} marker since it has none of the vanilla warnings");
                marker._dangerIndicatorRootObj.SetActive(true);
                marker._markerWarningImageObj.SetActive(false);
                marker._secondaryTextField.text = "WARNING: DUPLICATE SIGNAL(S) DETECTED, UNABLE TO PINPOINT DIRECTION";
                marker._secondaryTextField.SetAllDirty();
            }
            return false;
        }
        else
        {
            VisitedOFWVs.Add(ofwvName);
            return true;
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(CanvasMarker), nameof(CanvasMarker.OnTrackFogWarpVolume))]
    public static void CanvasMarker_OnTrackFogWarpVolume(CanvasMarker __instance, FogWarpVolume warpVolume) => VisitedOFWVs = new();
    [HarmonyPrefix, HarmonyPatch(typeof(CanvasMarker), nameof(CanvasMarker.OnUntrackFogWarpVolume))]
    public static void CanvasMarker_OnUntrackFogWarpVolume(CanvasMarker __instance, FogWarpVolume warpVolume) => VisitedOFWVs = new();
    [HarmonyPrefix, HarmonyPatch(typeof(CanvasMarkerManager), nameof(CanvasMarkerManager.UpdateAllFogMarkers))]
    public static void CanvasMarkerManager_UpdateAllFogMarkers(CanvasMarkerManager __instance) => VisitedOFWVs = new();
}
