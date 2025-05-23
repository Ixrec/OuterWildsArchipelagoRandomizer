using HarmonyLib;
using Newtonsoft.Json.Linq;
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
            }
        }
    }

    private static List<List<CoordinateDrawing.CoordinatePoint>> correctCoordinates = null;

    public static void SetCorrectCoordinatesFromSlotData(object coordsSlotData)
    {
        // Reset most stuff derived from correctCoordinates so we don't display the wrong profile's coordinates
        promptCoordsSprite = null;
        shipLogCoordsTexture = new Texture2D(600, 600, TextureFormat.ARGB32, false);
        shipLogCoordsSprite = null;

        if (coordsSlotData is string coordsString && coordsString == "vanilla")
        {
            // leaving vanilla coordinates unchanged
            correctCoordinates = null;
        }
        else if (coordsSlotData is JArray coords)
        {
            correctCoordinates = coords.Select(coord => (coord as JArray).Select(num => (CoordinateDrawing.CoordinatePoint)(long)num).ToList()).ToList();
        }
        else
        {
            APRandomizer.OWMLModConsole.WriteLine($"Leaving vanilla coordinates unchanged because slot_data['eotu_coordinates'] was invalid: {coordsSlotData}", OWML.Common.MessageType.Error);
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
    [HarmonyPrefix, HarmonyPatch(typeof(OrbitalCannonHologramProjector), nameof(OrbitalCannonHologramProjector.OnSlotActivated))]
    public static void OrbitalCannonHologramProjector_OnSlotActivated_Prefix(OrbitalCannonHologramProjector __instance, NomaiInterfaceSlot slot)
    {
        var activeIndex = __instance.GetSlotIndex(slot);
        var hologram = __instance._holograms[activeIndex];
        if (hologram.name == "Hologram_EyeCoordinates")
        {
            LocationTriggers.CheckLocation(Location.GD_COORDINATES);

            // If coordinates aren't randomized, then checking the location is all we want to do
            if (correctCoordinates == null) return;

            var hologramMeshGO = hologram.GetComponentInChildren<MeshRenderer>().gameObject;

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
    }

    private static ShipLogManager logManager = null;

    [HarmonyPrefix, HarmonyPatch(typeof(ShipLogManager), nameof(ShipLogManager.Awake))]
    public static void ShipLogManager_Awake_Prefix(ShipLogManager __instance)
    {
        logManager = __instance;

        //APRandomizer.OWMLModConsole.WriteLine($"ShipLogManager_Awake_Prefix editing ship log entry for EotU coordinates");
    }

    // thestrangepie and I have both experienced a rare, unreproducible native crash leaving no OWML logs when acquiring the Coordinates item.
    // When I experienced it, the native stack pointed to the Texture2D constructor. Since I was unable to reproduce it (even with mod code that
    // spammed Texture2D constructions), my only guess for how to mitigate this is to construct the Texture2Ds we need as early as possible
    // instead of waiting for them to become needed in game.
    // some ship log views will stretch their sprites into a square, so we need to draw squares (600 x 600) to avoid distortion
    private static Texture2D shipLogCoordsTexture = new Texture2D(600, 600, TextureFormat.ARGB32, false);
    private static Texture2D shipLogBlankTexture = new Texture2D(600, 600, TextureFormat.ARGB32, false);

    // public so this can be used by the tracker for the "Mission" Checklist entry and the "Eye of the Universe Coordinates" Inventory entry
    public static Sprite shipLogCoordsSprite = null;

    public static void EnsureShipLogCoordsSpriteIsUpToDate()
    {
        // just recreate/reset it every time we enter ship or suit log to be safe
        if (correctCoordinates == null)
            shipLogCoordsSprite = null;
        else
            // some ship log views will stretch this sprite into a square, so we need to draw a square (600 x 600) to avoid distortion
            shipLogCoordsSprite = CoordinateDrawing.CreateCoordinatesSprite(shipLogCoordsTexture, correctCoordinates, UnityEngine.Color.black, doKerning: false);
    }

    public static void EnsureShipLogPTMEntrySpriteEdited()
    {
        if (logManager == null)
            return;

        // vanilla / unrandomized coords, so nothing to do here
        if (correctCoordinates == null)
            return;

        ShipLogEntry ptmGeneratedEntry = null;
        var generatedEntryList = logManager.GetEntryList();
        if (generatedEntryList != null)
            ptmGeneratedEntry = generatedEntryList.Find(entry => entry.GetID() == "OPC_SUNKEN_MODULE");

        if (_hasCoordinates)
        {
            EnsureShipLogCoordsSpriteIsUpToDate();
            ptmGeneratedEntry?.SetAltSprite(shipLogCoordsSprite);
        }
        else
        {
            // just show a black square if you don't have the coordinates yet
            var size = 600;
            var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
            foreach (var x in Enumerable.Range(0, size))
                foreach (var y in Enumerable.Range(0, size))
                    tex.SetPixel(x, y, UnityEngine.Color.black);
            tex.Apply();

            var s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            ptmGeneratedEntry?.SetAltSprite(s);
        }
    }

    // wait until the player accesses the ship log to update its sprites
    [HarmonyPrefix, HarmonyPatch(typeof(ShipLogController), nameof(ShipLogController.EnterShipComputer))]
    public static void ShipLogController_EnterShipComputer_Prefix(ShipLogController __instance)
    {
        if (correctCoordinates == null) return;

        //APRandomizer.OWMLModConsole.WriteLine($"ShipLogController_EnterShipComputer_Prefix({_hasCoordinates}) updating ship log entry sprite for EotU coordinates");

        EnsureShipLogPTMEntrySpriteEdited();
    }

    // the prompt accepts rectangular sprites without issue, so use our default 600 x 200 size
    private static Texture2D promptCoordsTexture = new Texture2D(600, 200, TextureFormat.ARGB32, false);
    private static Sprite promptCoordsSprite = null;

    [HarmonyPostfix, HarmonyPatch(typeof(EyeCoordinatePromptTrigger), nameof(EyeCoordinatePromptTrigger.Start))]
    public static void EyeCoordinatePromptTrigger_Start_Postfix(EyeCoordinatePromptTrigger __instance)
    {
        // vanilla / unrandomized coords, so nothing to do here
        if (correctCoordinates == null)
            return;

        if (promptCoordsSprite == null)
        {
            APRandomizer.OWMLModConsole.WriteLine($"EyeCoordinatePromptTrigger_Start_Postfix drawing prompt coordinates sprite");
            promptCoordsSprite = CoordinateDrawing.CreateCoordinatesSprite(
                promptCoordsTexture,
                correctCoordinates,
                UnityEngine.Color.clear,
                doKerning: true
            );
        }

        // No need to change _eyeCoordinatesSprite because it's only used in KIPC.Awake() to
        // construct _eyeCoordinatesPrompt, and we have to reset this every loop anyway.
        __instance._promptController._eyeCoordinatesPrompt._customSprite = promptCoordsSprite;
    }

    private static ScreenPrompt coordinatesNotAvailablePrompt = null;

    [HarmonyPostfix, HarmonyPatch(typeof(EyeCoordinatePromptTrigger), nameof(EyeCoordinatePromptTrigger.OnEntry))]
    public static void EyeCoordinatePromptTrigger_OnEntry_Postfix(EyeCoordinatePromptTrigger __instance)
    {
        if (coordinatesNotAvailablePrompt == null)
        {
            coordinatesNotAvailablePrompt = new ScreenPrompt("Coordinates Not Available", 0);
            Locator.GetPromptManager().AddScreenPrompt(coordinatesNotAvailablePrompt, PromptPosition.LowerLeft, false);
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(EyeCoordinatePromptTrigger), nameof(EyeCoordinatePromptTrigger.Update))]
    public static bool EyeCoordinatePromptTrigger_Update_Prefix(EyeCoordinatePromptTrigger __instance)
    {
        coordinatesNotAvailablePrompt.SetVisibility(
            !_hasCoordinates &&
            OWInput.IsInputMode(InputMode.Character)
        );
        __instance._promptController.SetEyeCoordinatesVisibility(
            _hasCoordinates &&
            OWInput.IsInputMode(InputMode.Character) // the vanilla implementation doesn't have this check, but I think it should,
                                                     // and it's more annoying for this mod because of the in-game pause console
        );
        return false; // skip vanilla implementation
    }

    [HarmonyPostfix, HarmonyPatch(typeof(EyeCoordinatePromptTrigger), nameof(EyeCoordinatePromptTrigger.OnExit))]
    public static void EyeCoordinatePromptTrigger_OnExit_Postfix(EyeCoordinatePromptTrigger __instance)
    {
        coordinatesNotAvailablePrompt.SetVisibility(false);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(NomaiCoordinateInterface), nameof(NomaiCoordinateInterface.CheckEyeCoordinates))]
    public static bool NomaiCoordinateInterface_CheckEyeCoordinates_Prefix(NomaiCoordinateInterface __instance, ref bool __result)
    {
        if (correctCoordinates == null) return true; // let vanilla implementation handle it

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
