using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
public static class Coordinates
{
    private static bool _hasCoordinates = false;

    public static bool hasCoordinates
    {
        get => _hasCoordinates;
        set
        {
            if (_hasCoordinates != value)
            {
                _hasCoordinates = value;
            }
        }
    }

    /* The hologram names in the Control Module and Probe Tracking Module are:
     * Hologram_HourglassOrders
     * Hologram_CannonDestruction
     * Hologram_DamageReport
     * Hologram_LatestProbeTrajectory
     * Hologram_AllProbeTrajectories
     * Hologram_EyeCoordinates
     */
    [HarmonyPrefix]
    [HarmonyPatch(typeof(OrbitalCannonHologramProjector), nameof(OrbitalCannonHologramProjector.OnSlotActivated))]
    public static void OrbitalCannonHologramProjector_OnSlotActivated_Prefix(OrbitalCannonHologramProjector __instance, NomaiInterfaceSlot slot)
    {
        var activeIndex = __instance.GetSlotIndex(slot);
        var hologram = __instance._holograms[activeIndex];
        Randomizer.OWMLModConsole.WriteLine($"OrbitalCannonHologramProjector_OnSlotActivated_Prefix {__instance.name} {activeIndex} {hologram.name}");
        if (hologram.name == "Hologram_EyeCoordinates")
        {
            LocationTriggers.CheckLocation(Location.GD_COORDINATES);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(EyeCoordinatePromptTrigger), nameof(EyeCoordinatePromptTrigger.Update))]
    public static bool EyeCoordinatePromptTrigger_Update_Prefix(EyeCoordinatePromptTrigger __instance)
    {
        __instance._promptController.SetEyeCoordinatesVisibility(
            _hasCoordinates &&
            OWInput.IsInputMode(InputMode.Character) // the vanilla implementation doesn't have this check, but I think it should,
                                                     // and it's more annoying for this mod because of the in-game pause console
        );
        return false; // skip vanilla implementation
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(KeyInfoPromptController), nameof(KeyInfoPromptController.Awake))]
    public static void KeyInfoPromptController_Awake_Prefix(KeyInfoPromptController __instance)
    {
        // the prompt accepts rectangular sprites without issue, so use our default 600 x 200 size
        __instance._eyeCoordinatesSprite = CoordinateDrawing.CreateCoordinatesSprite(600, 200, CoordinateDrawing.vanillaEOTUCoordinates, Color.clear);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipLogManager), nameof(ShipLogManager.Awake))]
    public static void ShipLogManager_Awake_Prefix(ShipLogManager __instance)
    {
        Randomizer.OWMLModConsole.WriteLine($"ShipLogManager_Awake_Prefix ediing OPC_SUNKEN_MODULE entry sprite to show this multiworld's EotU coordinates");

        var entryData = __instance._shipLogLibrary.entryData;
        var i = entryData.IndexOf(entry => entry.id == "OPC_SUNKEN_MODULE");

        var ptmEntry = entryData[i];
        // some ship log views will stretch this sprite into a square, so we need to draw a square (600 x 600) to avoid distortion
        ptmEntry.altSprite = CoordinateDrawing.CreateCoordinatesSprite(600, 600, CoordinateDrawing.vanillaEOTUCoordinates, Color.black);
        entryData[i] = ptmEntry;
    }

}
