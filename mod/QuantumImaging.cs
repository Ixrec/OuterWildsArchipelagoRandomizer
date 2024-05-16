using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class QuantumImaging
{
    static List<QuantumObject> relevantQuantumObjects = new();

    public static void OnCompleteSceneLoad(OWScene _scene, OWScene loadScene)
    {
        // we don't want to retain these references beyond a scene transition / loop reset, or else
        // they become invalid and lead to NullReferenceExceptions when we try using them later
        relevantQuantumObjects.Clear();

        if (loadScene != OWScene.SolarSystem) return;

        // For this class, we care about any quantum object that can move around,
        // and that the player can "lock" in place by taking a photo of it.

        // I believe all of these fall into two categories:
        // - SocketedQuantumObjects, with a fixed set of "socket" simpleLocations they rotate between
        // - The Quantum Moon, which can't use sockets because it orbits around each planet

        relevantQuantumObjects.AddRange(GameObject.FindObjectsOfType<SocketedQuantumObject>());
        relevantQuantumObjects.RemoveAll(qo => qo.CompareTag("Ship")); // why is our spaceship a quantum object? I have no idea
        relevantQuantumObjects.Add(Locator.GetQuantumMoon());
    }

    private static bool _hasImagingKnowledge = false;

    public static bool hasImagingKnowledge
    {
        get => _hasImagingKnowledge;
        set
        {
            if (_hasImagingKnowledge != value)
            {
                _hasImagingKnowledge = value;

                if (_hasImagingKnowledge)
                {
                    var nd = new NotificationData(NotificationTarget.Player, "RECONFIGURING CAMERA TO CAPTURE QUANTUM WAVELENGTH", 10);
                    NotificationManager.SharedInstance.PostNotification(nd, false);
                }
            }
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(ProbeLauncher), nameof(ProbeLauncher.TakeSnapshotWithCamera))]
    public static bool ProbeLauncher_TakeSnapshotWithCamera_Prefix(ProbeCamera camera)
    {
        var __instance = camera;
        if (hasImagingKnowledge) return true;

        foreach (var qo in relevantQuantumObjects)
        {
            var distance = Vector3.Distance(qo.transform.position, __instance.transform.position);

            if (
                qo != null &&
                qo.gameObject != null && // no idea why CompareTag() NREs inside Unity code without this
                qo.CheckVisibilityFromProbe(__instance.GetOWCamera()) &&
                (distance < qo._maxSnapshotLockRange)
            ) {
                var player = Locator.GetPlayerBody();
                var isOccluded = Physics.Raycast(
                    player.transform.position,
                    camera.GetOWCamera().transform.forward,
                    distance * 0.9f, // arbitrary constant to avoid the quantum object "occluding itself"
                    OWLayerMask.physicalMask
                );
                if (isOccluded)
                {
                    APRandomizer.OWMLModConsole.WriteLine($"allowing ProbeCamera.TakeSnapshot because '{qo.name}' is occluded by something matching OWLayerMask.physicalMask");
                    return true;
                }

                APRandomizer.OWMLModConsole.WriteLine($"ProbeCamera.TakeSnapshot blocked because '{qo.name}' is visible " +
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
