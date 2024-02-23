using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class GhostMatter
{
    static List<ParticleSystemRenderer> ghostMatterParticleRenderers = new();

    public static void Setup()
    {
        LoadManager.OnCompleteSceneLoad += (scene, loadScene) =>
        {
            if (loadScene != OWScene.SolarSystem) return;

            APRandomizer.OWMLModConsole.WriteLine($"GhostMatter.Setup fetching references to ghost matter particle renderers", OWML.Common.MessageType.Debug);

            var all_psrs = GameObject.FindObjectsOfType<ParticleSystemRenderer>();
            var wisp_psrs = all_psrs.Where(psr => psr.mesh?.name == "Effects_GM_WillOWisp");

            ghostMatterParticleRenderers = wisp_psrs.ToList();
        };
    }

    private static bool _hasGhostMatterKnowledge = false;

    public static bool hasGhostMatterKnowledge
    {
        get => _hasGhostMatterKnowledge;
        set
        {
            if (_hasGhostMatterKnowledge != value)
            {
                _hasGhostMatterKnowledge = value;

                if (_hasGhostMatterKnowledge)
                {
                    // todo: tweak notification if we also have scout already
                    var nd = new NotificationData(NotificationTarget.Player, "RECONFIGURING CAMERA TO CAPTURE GHOST MATTER WAVELENGTH", 10);
                    NotificationManager.SharedInstance.PostNotification(nd, false);
                }
            }
        }
    }

    // These two patches prevent camera photos from showing ghost matter until you have the item.

    // The game enables and disables some of these particle renderers on its own at various times,
    // so to ensure sure this works at all and avoid any weird side effects, we need to wait until
    // the moment the camera is being used to actually disable the relevant renderers, *and* we
    // need to re-enable the ones that were enabled before.
    static List<ParticleSystemRenderer> disabledParticleRenderers = new();

    [HarmonyPrefix, HarmonyPatch(typeof(ProbeLauncher), nameof(ProbeLauncher.TakeSnapshotWithCamera))]
    public static void ProbeLauncher_TakeSnapshotWithCamera_Prefix(ProbeCamera camera)
    {
        if (!hasGhostMatterKnowledge)
        {
            foreach (var psr in ghostMatterParticleRenderers)
            {
                psr.enabled = false;
                disabledParticleRenderers.Add(psr);
            }
        }
    }
    [HarmonyPostfix, HarmonyPatch(typeof(ProbeLauncher), nameof(ProbeLauncher.TakeSnapshotWithCamera))]
    public static void ProbeLauncher_TakeSnapshotWithCamera_Postfix(ProbeCamera camera)
    {
        if (!hasGhostMatterKnowledge)
        {
            foreach (var psr in disabledParticleRenderers)
            {
                psr.enabled = true;
            }
            disabledParticleRenderers.Clear();
        }
    }

    // This patch prevents the scout from showing "! Hazard" when it's inside ghost matter.
    [HarmonyPostfix, HarmonyPatch(typeof(HazardDetector), nameof(HazardDetector.GetDisplayDangerMarker))]
    public static void HazardDetector_GetDisplayDangerMarker_Postfix(HazardDetector __instance, ref bool __result)
    {
        if (__instance._activeVolumes.All(av => av is HazardVolume && (av as HazardVolume).GetHazardType() == HazardVolume.HazardType.DARKMATTER))
            __result = false;
    }

    // This patch prevents the scout from making a big green splash when it enters ghost matter.
    [HarmonyPrefix, HarmonyPatch(typeof(HazardDetector), nameof(HazardDetector.OnVolumeAdded))]
    public static void HazardDetector_OnVolumeAdded_Prefix(HazardDetector __instance, EffectVolume eVolume)
    {
        HazardVolume hazardVolume = eVolume as HazardVolume;
        HazardVolume.HazardType hazardType = hazardVolume.GetHazardType();
        if (__instance.GetName() == Detector.Name.Probe && hazardType == HazardVolume.HazardType.DARKMATTER)
        {
            APRandomizer.OWMLModConsole.WriteLine($"HazardDetector_OnVolumeAdded_Prefix blocking the scout's darkMatterEntryEffect");
            __instance._darkMatterEntryEffect = null;
        }
    }

    // This patch prevents the scout from leaving a green trail as it passes through ghost matter.
    [HarmonyPrefix, HarmonyPatch(typeof(DarkMatterVolume), nameof(DarkMatterVolume.OnEffectVolumeEnter))]
    public static bool DarkMatterVolume_OnEffectVolumeEnter_Prefix(DarkMatterVolume __instance, GameObject hitObj)
    {
        HazardDetector detector = hitObj.GetComponent<HazardDetector>();
        if (detector == null) return false; // no need to make the base game repeat this null check

        // This prevents the scout from being added to the DMV's _trackedDetectors list,
        // which is what it uses to emit the trail of WillOWisp particles as an object
        // moves through ghost matter.
        if (detector.GetName() == Detector.Name.Probe)
        {
            APRandomizer.OWMLModConsole.WriteLine($"DarkMatterVolume_OnEffectVolumeEnter_Prefix blocking the scout's ghost matter particleTrail");
            return false;
        }
        return true;
    }
}
