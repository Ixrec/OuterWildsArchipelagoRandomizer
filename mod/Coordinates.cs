using HarmonyLib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;

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
                ApplyHasCoordinatesFlag(value);
            }
        }
    }

    private static List<List<CoordinateDrawing.CoordinatePoint>> correctCoordinates = CoordinateDrawing.vanillaEOTUCoordinates;

    // thestrangepie and I have both experienced a rare, unreproducible native crash leaving no OWML logs when acquiring the Coordinates item.
    // When I experienced it, the native stack pointed to the Texture2D constructor. Since I was unable to reproduce it (even with mod code that
    // spammed Texture2D constructions), my only guess for how to mitigate this is to construct the Texture2Ds we need as early as possible
    // instead of waiting for them to become needed in game.
    private static Texture2D promptCoordsTexture = new Texture2D(600, 200, TextureFormat.ARGB32, false);
    // some ship log views will stretch their sprites into a square, so we need to draw squares (600 x 600) to avoid distortion
    private static Texture2D shipLogCoordsTexture = new Texture2D(600, 600, TextureFormat.ARGB32, false);
    private static Texture2D shipLogBlankTexture = new Texture2D(600, 600, TextureFormat.ARGB32, false);

    // Although I have no evidence implicating the Sprite constructors, we might as well create those eagerly and avoid wasteful recreations later.
    private static Sprite promptCoordsSprite = null;
    private static Sprite shipLogCoordsSprite = null;
    private static Sprite shipLogBlankSprite = null;

    public static void SetCorrectCoordinatesFromSlotData(object coordsSlotData)
    {
        if (coordsSlotData is string coordsString && coordsString == "vanilla")
        {
            APRandomizer.OWMLModConsole.WriteLine($"slot_data['eotu_coordinates'] was 'vanilla'. Leaving vanilla coordinates unchanged");
        }
        else if (coordsSlotData is JArray coords)
        {
            correctCoordinates = coords.Select(coord => (coord as JArray).Select(num => (CoordinateDrawing.CoordinatePoint)(long)num).ToList()).ToList();
            APRandomizer.OWMLModConsole.WriteLine($"Set coordinates to the values found in slot_data['eotu_coordinates']: " +
                $"{string.Join(", ", correctCoordinates.Select(coord => string.Join("|", coord)))}" +
                $"\nand generating appropriate sprites.");

            // These need to be regenerated whenever we switch profiles and change the correct coordinates
            shipLogCoordsSprite = CoordinateDrawing.CreateCoordinatesSprite(
                shipLogCoordsTexture,
                correctCoordinates,
                UnityEngine.Color.black,
                doKerning: false
            );
            promptCoordsSprite = CoordinateDrawing.CreateCoordinatesSprite(
                promptCoordsTexture,
                correctCoordinates,
                UnityEngine.Color.clear,
                doKerning: true
            );

            // This only needs to be generated once, since it doesn't depend on the coordinates
            if (shipLogBlankSprite == null)
            {
                var tex = shipLogBlankTexture;
                foreach (var x in Enumerable.Range(0, tex.width))
                    foreach (var y in Enumerable.Range(0, tex.height))
                        tex.SetPixel(x, y, UnityEngine.Color.black);
                tex.Apply();
                shipLogBlankSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
        }
        else
        {
            APRandomizer.OWMLModConsole.WriteLine($"Leaving vanilla coordinates unchanged because slot_data['eotu_coordinates'] was invalid: {coordsSlotData}", OWML.Common.MessageType.Error);
        }
    }

    private static GameObject hologramMeshGO = null;

    /* The hologram names in the Control Module and Probe Tracking Module are:
     * Hologram_HourglassOrders
     * Hologram_CannonDestruction
     * Hologram_DamageReport
     * Hologram_LatestProbeTrajectory
     * Hologram_AllProbeTrajectories
     * Hologram_EyeCoordinates
     */
    [HarmonyPrefix, HarmonyPatch(typeof(OrbitalCannonHologramProjector), nameof(OrbitalCannonHologramProjector.OnSlotActivated))]
    public static void OrbitalCannonHologramProjector_OnSlotActivated_Prefix(OrbitalCannonHologramProjector __instance, NomaiInterfaceSlot slot)
    {
        var activeIndex = __instance.GetSlotIndex(slot);
        var hologram = __instance._holograms[activeIndex];
        if (hologram.name == "Hologram_EyeCoordinates")
        {
            APRandomizer.OWMLModConsole.WriteLine($"OrbitalCannonHologramProjector_OnSlotActivated_Prefix for {hologram.name} " +
                "marking GD_COORDINATES checked and editing hologram to show this multiworld's coordinates");

            LocationTriggers.CheckLocation(Location.GD_COORDINATES);

            hologramMeshGO = hologram.GetComponentInChildren<MeshRenderer>().gameObject;

            ApplyHasCoordinatesFlag(_hasCoordinates);
        }
    }

    private static ShipLogManager logManager = null;

    [HarmonyPrefix, HarmonyPatch(typeof(ShipLogManager), nameof(ShipLogManager.Awake))]
    public static void ShipLogManager_Awake_Prefix(ShipLogManager __instance)
    {
        logManager = __instance;

        APRandomizer.OWMLModConsole.WriteLine($"ShipLogManager_Awake_Prefix editing ship log entry for EotU coordinates", OWML.Common.MessageType.Debug);

        ApplyHasCoordinatesFlag(_hasCoordinates);
    }

    public static void ApplyHasCoordinatesFlag(bool hasCoordinates)
    {
        APRandomizer.OWMLModConsole.WriteLine($"ApplyHasCoordinatesFlag({hasCoordinates}) updating PTM hologram model and ship log entry sprite for EotU coordinates");

        if (hologramMeshGO != null)
        {
            hologramMeshGO.DestroyAllComponentsImmediate<MeshFilter>();
            var filter = hologramMeshGO.AddComponent<MeshFilter>();

            CoordinateDrawing.Shapes3D model;
            if (hasCoordinates)
                model = CoordinateDrawing.getCoordinatesModel(correctCoordinates);
            else
                model = CoordinateDrawing.getQuestionMarksModel();

            var mesh = new Mesh();
            mesh.vertices = model.vertices;
            mesh.triangles = model.triangles;
            mesh.Optimize();
            mesh.RecalculateNormals();

            filter.mesh = mesh;
        }

        if (logManager != null)
        {
            // Sadly to edit the ship log reliably we have to edit two different data structures,
            // one of which is generated from the other during wakeup. Otherwise we end up with
            // issues like the edits applying only on wakeup or only after wakeup.

            ShipLogEntry ptmGeneratedEntry = null;
            var generatedEntryList = logManager.GetEntryList();
            if (generatedEntryList != null)
                ptmGeneratedEntry = generatedEntryList.Find(entry => entry.GetID() == "OPC_SUNKEN_MODULE");

            var libraryEntryData = logManager._shipLogLibrary.entryData;
            var ptmLibraryIndex = libraryEntryData.IndexOf(entry => entry.id == "OPC_SUNKEN_MODULE");
            var ptmLibraryEntry = libraryEntryData[ptmLibraryIndex];

            if (hasCoordinates)
            {
                if (shipLogCoordsSprite == null)
                    APRandomizer.OWMLModConsole.WriteLine($"ApplyHasCoordinatesFlag({hasCoordinates}) skipping shipLogCoordsSprite since it's null (which should mean this world didn't randomize coordinates)");
                else
                {
                    ptmLibraryEntry.altSprite = shipLogCoordsSprite;
                    ptmGeneratedEntry?.SetAltSprite(shipLogCoordsSprite);
                }
            }
            else
            {
                if (shipLogBlankSprite == null)
                    APRandomizer.OWMLModConsole.WriteLine($"ApplyHasCoordinatesFlag({hasCoordinates}) skipping shipLogBlankSprite since it's null (which should mean this world didn't randomize coordinates)");
                else
                {
                    ptmLibraryEntry.altSprite = shipLogBlankSprite;
                    ptmGeneratedEntry?.SetAltSprite(shipLogBlankSprite);
                }
            }

            // Because libraryEntryData is an array, not a list, we have to assign our edited object into the array afterward
            libraryEntryData[ptmLibraryIndex] = ptmLibraryEntry;
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(EyeCoordinatePromptTrigger), nameof(EyeCoordinatePromptTrigger.Update))]
    public static bool EyeCoordinatePromptTrigger_Update_Prefix(EyeCoordinatePromptTrigger __instance)
    {
        __instance._promptController.SetEyeCoordinatesVisibility(
            _hasCoordinates &&
            OWInput.IsInputMode(InputMode.Character) // the vanilla implementation doesn't have this check, but I think it should,
                                                     // and it's more annoying for this mod because of the in-game pause console
        );
        return false; // skip vanilla implementation
    }

    [HarmonyPrefix, HarmonyPatch(typeof(KeyInfoPromptController), nameof(KeyInfoPromptController.Awake))]
    public static void KeyInfoPromptController_Awake_Prefix(KeyInfoPromptController __instance)
    {
        // the prompt accepts rectangular sprites without issue, so use our default 600 x 200 size
        if (promptCoordsSprite != null)
        {
            APRandomizer.OWMLModConsole.WriteLine($"KeyInfoPromptController_Awake_Prefix updating _eyeCoordinatesSprite");
            __instance._eyeCoordinatesSprite = promptCoordsSprite;
        }
        else
            APRandomizer.OWMLModConsole.WriteLine($"KeyInfoPromptController_Awake_Prefix doing nothing since promptCoordsSprite is still null (which should mean this world didn't randomize coordinates)");
    }

    [HarmonyPrefix, HarmonyPatch(typeof(NomaiCoordinateInterface), nameof(NomaiCoordinateInterface.CheckEyeCoordinates))]
    public static bool NomaiCoordinateInterface_CheckEyeCoordinates_Prefix(NomaiCoordinateInterface __instance, ref bool __result)
    {
        CoordinateDrawing.CoordinatePoint NodeToCoordPoint(int node)
        {
            switch (node)
            {
                case 0: return CoordinateDrawing.CoordinatePoint.UpperLeft;
                case 1: return CoordinateDrawing.CoordinatePoint.UpperRight;
                case 2: return CoordinateDrawing.CoordinatePoint.Right;
                case 3: return CoordinateDrawing.CoordinatePoint.LowerRight;
                case 4: return CoordinateDrawing.CoordinatePoint.LowerLeft;
                case 5: return CoordinateDrawing.CoordinatePoint.Left;
                default: throw new InvalidCastException($"{node} is not a possible value for a Nomai Coordinate Interface node and/or hexagonal CoordinatePoint");
            };
        }

        var ncs = __instance._nodeControllers;

        var inputMatchesVanillaCoords = true;
        __result = true;

        foreach (var i in Enumerable.Range(0, 3))
        {
            var nodes = ncs[i]._activeNodes;
            var inputCoordinate = nodes.Select(NodeToCoordPoint);
            var correctCoordinate = correctCoordinates[i];

            // allow "backwards" inputs, since they're the same shape
            var inputIsCorrect = inputCoordinate.SequenceEqual(correctCoordinate) || inputCoordinate.Reverse().SequenceEqual(correctCoordinate);

            APRandomizer.OWMLModConsole.WriteLine($"NomaiCoordinateInterface_CheckEyeCoordinates_Prefix for coordinate {i} compared " +
                $" [{nodes.Count}]{string.Join("|", nodes)} vs [{correctCoordinate.Count}]{string.Join("|", correctCoordinate)}, " +
                $"inputIsCorrect={inputIsCorrect}", OWML.Common.MessageType.Debug);

            if (!inputIsCorrect)
                __result = false;

            var vanillaCoordinate = CoordinateDrawing.vanillaEOTUCoordinates[i];
            var inputMatchesVanilla = inputCoordinate.SequenceEqual(vanillaCoordinate) || inputCoordinate.Reverse().SequenceEqual(vanillaCoordinate);
            if (!inputMatchesVanilla)
                inputMatchesVanillaCoords = false;
        }

        if (__result == false && inputMatchesVanillaCoords == true)
        {
            if (_hasCoordinates)
                APRandomizer.InGameAPConsole.AddText($"The correct Eye coordinates for this Archipelago world are different from the vanilla game's coordinates. " +
                    $"Please check the prompt in the lower left corner of the screen.");
            else
                APRandomizer.InGameAPConsole.AddText($"The correct Eye coordinates for this Archipelago world are different from the vanilla game's coordinates. " +
                    $"Please come back after the 'Coordinates' item for this world has been found.");
        }

        return false; // skip vanilla implementation
    }
}
