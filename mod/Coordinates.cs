using HarmonyLib;
using System;
using System.Collections.Generic;
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

    private static GameObject hologramMeshGO = null;

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
        if (hologram.name == "Hologram_EyeCoordinates")
        {
            Randomizer.OWMLModConsole.WriteLine($"OrbitalCannonHologramProjector_OnSlotActivated_Prefix for {hologram.name} " +
                "marking GD_COORDINATES checked and editing hologram to show this multiworld's coordinates");

            LocationTriggers.CheckLocation(Location.GD_COORDINATES);

            hologramMeshGO = hologram.GetComponentInChildren<MeshRenderer>().gameObject;

            ApplyHasCoordinatesFlag(_hasCoordinates);
        }
    }

    private static ShipLogManager logManager = null;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ShipLogManager), nameof(ShipLogManager.Awake))]
    public static void ShipLogManager_Awake_Prefix(ShipLogManager __instance)
    {
        logManager = __instance;

        Randomizer.OWMLModConsole.WriteLine($"ShipLogManager_Awake_Prefix editing ship log entry for EotU coordinates");

        ApplyHasCoordinatesFlag(_hasCoordinates);
    }

    public static void ApplyHasCoordinatesFlag(bool hasCoordinates)
    {
        Randomizer.OWMLModConsole.WriteLine($"ApplyHasCoordinatesFlag({hasCoordinates}) updating PTM hologram model and ship log entry sprite for EotU coordinates");

        if (hologramMeshGO is not null)
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

        if (logManager is not null)
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
                // some ship log views will stretch this sprite into a square, so we need to draw a square (600 x 600) to avoid distortion
                var s = CoordinateDrawing.CreateCoordinatesSprite(600, 600, correctCoordinates, Color.black);
                ptmLibraryEntry.altSprite = s;
                ptmGeneratedEntry?.SetAltSprite(s);
            }
            else
            {
                // just show a black square if you don't have the coordinates yet
                var size = 600;
                var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
                foreach (var x in Enumerable.Range(0, size))
                    foreach (var y in Enumerable.Range(0, size))
                        tex.SetPixel(x, y, Color.black);
                tex.Apply();

                var s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
                ptmLibraryEntry.altSprite = s;
                ptmGeneratedEntry?.SetAltSprite(s);
            }

            // Because libraryEntryData is an array, not a list, we have to assign our edited object into the array afterward
            libraryEntryData[ptmLibraryIndex] = ptmLibraryEntry;
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
        __instance._eyeCoordinatesSprite = CoordinateDrawing.CreateCoordinatesSprite(600, 200, correctCoordinates, Color.clear);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NomaiCoordinateInterface), nameof(NomaiCoordinateInterface.CheckEyeCoordinates))]
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

            Randomizer.OWMLModConsole.WriteLine($"NomaiCoordinateInterface_CheckEyeCoordinates_Prefix for coordinate {i} compared " +
                $" [{nodes.Count}]{string.Join("|", nodes)} vs [{correctCoordinate.Count}]{string.Join("|", correctCoordinate)}, " +
                $"inputIsCorrect={inputIsCorrect}");

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
                Randomizer.InGameAPConsole.AddText($"The correct Eye coordinates for this Archipelago world are different from the vanilla game's coordinates. " +
                    $"Please check the prompt in the lower left corner of the screen.");
            else
                Randomizer.InGameAPConsole.AddText($"The correct Eye coordinates for this Archipelago world are different from the vanilla game's coordinates. " +
                    $"Please come back after the 'Coordinates' item for this world has been found.");
        }

        return false; // skip vanilla implementation
    }
}
