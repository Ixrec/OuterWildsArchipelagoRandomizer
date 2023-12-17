using HarmonyLib;
using System.Collections.Generic;
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

            // we don't want to retain these references beyond a scene transition / loop reset, or else
            // they become invalid and lead to NullReferenceExceptions when we try using them later
            relevantQuantumObjects.Clear();

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

            if (hasImagingKnowledge)
            {
                var nd = new NotificationData(NotificationTarget.Player, "RECONFIGURING CAMERA TO CAPTURE QUANTUM WAVELENGTH", 10);
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
}
