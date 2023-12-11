using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class QuantumImaging
{
    static List<QuantumObject> relevantQuantumObjects = new();

    public static void Setup()
    {
        LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
        {
            if (loadScene != OWScene.SolarSystem) return;

            Randomizer.Instance.ModHelper.Console.WriteLine($"QuantumImaging.Setup fetching references to relevant quantum objects");

            // For this class, we care about any quantum object that can move around,
            // and that the player can "lock" in place by taking a photo of it.

            // I believe all of these fall into two categories:
            // - SocketedQuantumObjects, with a fixed set of "socket" simpleLocations they rotate between
            // - The Quantum Moon, which can't use sockets because it orbits around each planet

            relevantQuantumObjects.AddRange(GameObject.FindObjectsOfType<SocketedQuantumObject>());
            relevantQuantumObjects.Add(Locator.GetQuantumMoon());
        };
    }

    public static bool hasImagingKnowledge = false;

    public static void SetHasImagingKnowledge(bool hasImagingKnowledge)
    {
        if (QuantumImaging.hasImagingKnowledge != hasImagingKnowledge)
        {
            QuantumImaging.hasImagingKnowledge = hasImagingKnowledge;

            if (hasImagingKnowledge) // TODO: && has camera
            {
                var nd = new NotificationData(NotificationTarget.Player, "RECONFIGURING CAMERA TO CAPTURE QUANTUM EM FREQUENCY", 10);
                NotificationManager.SharedInstance.PostNotification(nd, false);
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ProbeLauncher), nameof(ProbeLauncher.TakeSnapshotWithCamera))]
    public static bool ProbeLauncher_TakeSnapshotWithCamera_Prefix(ProbeCamera camera)
    {
        var __instance = camera;
        if (hasImagingKnowledge) return true;

        foreach (var qo in relevantQuantumObjects)
        {
            var distance = Vector3.Distance(qo.transform.position, __instance.transform.position);
            if (
                qo is not null &&
                qo.gameObject is not null && // no idea why CompareTag() NREs inside Unity code without this
                !qo.CompareTag("Ship") &&
                qo.CheckVisibilityFromProbe(__instance.GetOWCamera()) &&
                (distance < qo._maxSnapshotLockRange)
            ) {
                Randomizer.Instance.ModHelper.Console.WriteLine($"ProbeCamera.TakeSnapshot blocked because '{qo.name}' is visible " +
                    $"and is {distance} distance units away (within the object's 'max snapshot lock range' of {qo._maxSnapshotLockRange})");
                NotificationManager.SharedInstance.PostNotification(new NotificationData(
                    OWInput.IsInputMode(InputMode.ShipCockpit) ? NotificationTarget.Ship : NotificationTarget.Player,
                    "UNEXPECTED CAMERA ERROR"
                ), false);
                return false;
            }
        }

        return true;
    }

    static ScreenPrompt cameraEMRangePrompt = new("Camera EM Range: Visible & Quantum", 0);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.LateInitialize))]
    public static void ToolModeUI_LateInitialize_Postfix()
    {
        Locator.GetPromptManager().AddScreenPrompt(cameraEMRangePrompt, PromptPosition.UpperRight, false);
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ToolModeUI), nameof(ToolModeUI.Update))]
    public static void ToolModeUI_Update_Postfix()
    {
        cameraEMRangePrompt.SetVisibility(
            hasImagingKnowledge && // TODO: && has camera
            (OWInput.IsInputMode(InputMode.Character) || OWInput.IsInputMode(InputMode.ShipCockpit)) &&
            Locator.GetToolModeSwapper().IsInToolMode(ToolMode.Probe)
        );
    }
}
