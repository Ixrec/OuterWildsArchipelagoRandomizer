using HarmonyLib;
using OWML.Common;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class GhostMatter
{
    static List<ParticleSystemRenderer> ghostMatterParticleRenderers = new();

    public static void OnCompleteSceneLoad(OWScene scene, OWScene loadScene)
    {
        if (loadScene != OWScene.SolarSystem) return;

        var all_psrs = GameObject.FindObjectsOfType<ParticleSystemRenderer>();
        var wisp_psrs = all_psrs.Where(psr => psr.mesh?.name == "Effects_GM_WillOWisp");

        ghostMatterParticleRenderers = wisp_psrs.ToList();

        if (RandomizeGhostMatter)
            RandomlyEditGhostMatterPlacement();
    }

    private static bool RandomizeGhostMatter = false;
    private static System.Random prng = new System.Random();

    public static void ModSettingsChanged(IModConfig config)
    {
        RandomizeGhostMatter = config.GetSettingsValue<bool>("Randomize Ghost Matter");
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
            __instance._darkMatterEntryEffect = null;
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
            return false;

        return true;
    }

    private static void RandomlyEditGhostMatterPlacement()
    {
        APRandomizer.OWMLModConsole.WriteLine($"RandomlyEditGhostMatterPlacement() called");

        var changeCometTunnelGM = prng.Next(0, 2);
        if (changeCometTunnelGM == 1)
        {
            APRandomizer.OWMLModConsole.WriteLine($"RandomlyEditGhostMatterPlacement() changing comet tunnel");

            var cometLeftTunnelPSR = GameObject.Find("Comet_Body/Sector_CO/Sector_CometInterior/Effects_CometInterior/Effects_GM_AuroraWisps"); // no number
            var cometLeftTunnelDMV = GameObject.Find("Comet_Body/Sector_CO/Sector_CometInterior/Interactables_CometInterior/DarkMatterVolume"); // no number

            cometLeftTunnelPSR.transform.localPosition = new Vector3(0, -55, -10);
            cometLeftTunnelDMV.transform.localPosition = new Vector3(0, -55, -10);
        }

        var changeCometSlalomFirstGateGM = prng.Next(0, 2);
        if (changeCometSlalomFirstGateGM == 1)
        {
            APRandomizer.OWMLModConsole.WriteLine($"RandomlyEditGhostMatterPlacement() changing comet slalom first gate");

            var cometSlalomFirstLeftPSR = GameObject.Find("Comet_Body/Sector_CO/Sector_CometInterior/Effects_CometInterior/Effects_GM_AuroraWisps (6)");
            var cometSlalomFirstLeftDMV = GameObject.Find("Comet_Body/Sector_CO/Sector_CometInterior/Interactables_CometInterior/DarkMatter_KillVolumes/DarkMatterVolume (3)");

            var lp = cometSlalomFirstLeftPSR.transform.localPosition; lp.z = 7; cometSlalomFirstLeftPSR.transform.localPosition = lp;

            lp = cometSlalomFirstLeftDMV.transform.localPosition; lp.z = 6; cometSlalomFirstLeftDMV.transform.localPosition = lp;
        }

        var changeCometSlalomSecondGateGM = prng.Next(0, 2);
        if (changeCometSlalomSecondGateGM == 1)
        {
            APRandomizer.OWMLModConsole.WriteLine($"RandomlyEditGhostMatterPlacement() changing comet slalom second gate");

            var cometSlalomSecondRightPSR = GameObject.Find("Comet_Body/Sector_CO/Sector_CometInterior/Effects_CometInterior/Effects_GM_AuroraWisps (5)");
            var cometSlalomSecondRightDMV = GameObject.Find("Comet_Body/Sector_CO/Sector_CometInterior/Interactables_CometInterior/DarkMatter_KillVolumes/DarkMatterVolume (5)");

            var lp = cometSlalomSecondRightPSR.transform.localPosition; lp.z = -3; cometSlalomSecondRightPSR.transform.localPosition = lp;

            lp = cometSlalomSecondRightDMV.transform.localPosition; lp.z = -3; cometSlalomSecondRightDMV.transform.localPosition = lp;
        }

        var changeBrambleIslandRootGM = prng.Next(0, 2);
        if (changeBrambleIslandRootGM == 1)
        {
            APRandomizer.OWMLModConsole.WriteLine($"RandomlyEditGhostMatterPlacement() changing bramble island roots");

            var brambleIslandRightRootPSR = GameObject.Find("BrambleIsland_Body/Sector_BrambleIsland/Interactables_BrambleIsland").transform.GetChild(3).Find("Effects_GM_AuroraWisps");
            var brambleIslandRightRootDMV1 = GameObject.Find("BrambleIsland_Body/Sector_BrambleIsland/Interactables_BrambleIsland").transform.GetChild(3).GetChild(2);
            var brambleIslandRightRootDMV2 = GameObject.Find("BrambleIsland_Body/Sector_BrambleIsland/Interactables_BrambleIsland").transform.GetChild(3).GetChild(3);

            brambleIslandRightRootPSR.transform.localPosition = new Vector3(5, 2, -30);
            brambleIslandRightRootDMV1.transform.localPosition = new Vector3(5, 2, -30);
            brambleIslandRightRootDMV2.gameObject.SetActive(false);
        }

        var changeBrambleIslandIceGM = prng.Next(0, 3);
        if (changeBrambleIslandIceGM == 1)
        {
            APRandomizer.OWMLModConsole.WriteLine($"RandomlyEditGhostMatterPlacement() changing bramble island ice so the middle path is safe");

            var brambleIslandMiddlePSR = GameObject.Find("BrambleIsland_Body/Sector_BrambleIsland/Interactables_BrambleIsland").transform.GetChild(4).Find("Effects_GM_AuroraWisps (2)");
            var brambleIslandMiddleDMV = GameObject.Find("BrambleIsland_Body/Sector_BrambleIsland/Interactables_BrambleIsland").transform.GetChild(4).GetChild(0);

            var lp = brambleIslandMiddlePSR.transform.localPosition; lp.x = 10; brambleIslandMiddlePSR.transform.localPosition = lp;

            lp = brambleIslandMiddleDMV.transform.localPosition; lp.x = 10; brambleIslandMiddleDMV.transform.localPosition = lp;
        }
        else if (changeBrambleIslandIceGM == 2)
        {
            APRandomizer.OWMLModConsole.WriteLine($"RandomlyEditGhostMatterPlacement() changing bramble island ice so the right path is safe");

            var brambleIslandRightPSR = GameObject.Find("BrambleIsland_Body/Sector_BrambleIsland/Interactables_BrambleIsland").transform.GetChild(4).Find("Effects_GM_AuroraWisps (1)");
            var brambleIslandRightDMV = GameObject.Find("BrambleIsland_Body/Sector_BrambleIsland/Interactables_BrambleIsland").transform.GetChild(4).GetChild(1);

            var lp = brambleIslandRightPSR.transform.localPosition; lp.x = 10; brambleIslandRightPSR.transform.localPosition = lp;

            lp = brambleIslandRightDMV.transform.localPosition; lp.x = 10; brambleIslandRightDMV.transform.localPosition = lp;
        }

        var eyeShrineGMParent = GameObject.Find("CaveTwin_Body/Sector_CaveTwin/Sector_SouthHemisphere/Sector_SouthUnderground/Sector_City/Sector_EyeDistrict/Interactables_EyeDistrict/DarkMatter");

        var changeEyeShrineFloorGM = prng.Next(0, 2);
        if (changeEyeShrineFloorGM == 1)
        {
            APRandomizer.OWMLModConsole.WriteLine($"RandomlyEditGhostMatterPlacement() moving eye shrine floor GM up to the ceiling");

            var eyeShrineFloorPSR = eyeShrineGMParent.transform.Find("Effects_GM_AuroraWisps (1)");
            var eyeShrineFloorDMV = eyeShrineGMParent.transform.Find("DarkMatterVolume");

            var lp = eyeShrineFloorPSR.transform.localPosition; lp.y = -11; lp.z = 7; eyeShrineFloorPSR.transform.localPosition = lp;
            var ls = eyeShrineFloorPSR.transform.localScale; ls.x = 0.5f; ls.z = 0.5f; eyeShrineFloorPSR.transform.localScale = ls;
            lp = eyeShrineFloorDMV.transform.localPosition; lp.y = -11; lp.z = 7; eyeShrineFloorDMV.transform.localPosition = lp;
            ls = eyeShrineFloorDMV.transform.localScale; ls.x = 0.5f; ls.z = 0.5f; eyeShrineFloorDMV.transform.localScale = ls;
        }
        var changeEyeShrineEntranceGM = prng.Next(0, 2);
        if (changeEyeShrineEntranceGM == 1)
        {
            APRandomizer.OWMLModConsole.WriteLine($"RandomlyEditGhostMatterPlacement() moving eye shrine entrance GM onto one of the ledges");

            var eyeShrineEntrancePSR = eyeShrineGMParent.transform.Find("Effects_GM_AuroraWisps (3)");
            var eyeShrineEntranceDMV = eyeShrineGMParent.transform.Find("DarkMatterVolume (2)");

            var lp = eyeShrineEntrancePSR.transform.localPosition; lp.y = -16; lp.z = 13; eyeShrineEntrancePSR.transform.localPosition = lp;
            lp = eyeShrineEntranceDMV.transform.localPosition; lp.y = -15; lp.z = 13; eyeShrineEntranceDMV.transform.localPosition = lp;
        }
        var changeEyeShrineBackCornerGM = prng.Next(0, 2);
        if (changeEyeShrineBackCornerGM == 1)
        {
            APRandomizer.OWMLModConsole.WriteLine($"RandomlyEditGhostMatterPlacement() moving eye shrine back corner GM onto one of the ledges");

            var eyeShrineBackCornerPSR = eyeShrineGMParent.transform.Find("Effects_GM_AuroraWisps (2)");
            var eyeShrineBackCornerDMV = eyeShrineGMParent.transform.Find("DarkMatterVolume (1)");

            eyeShrineBackCornerPSR.transform.localPosition = new Vector3(-8, -16, -6);
            eyeShrineBackCornerDMV.transform.localPosition = new Vector3(-8, -16, -6);
        }

        // don't try to edit DLC ghost matter if the DLC is not installed
        if (EntitlementsManager.IsDlcOwned() != EntitlementsManager.AsyncOwnershipStatus.Owned)
            return;

        var changeRLWorkshopGM = prng.Next(0, 2);
        if (changeRLWorkshopGM == 1)
        {
            APRandomizer.OWMLModConsole.WriteLine($"RandomlyEditGhostMatterPlacement() changing river lowlands workshop");

            var workshopEntranceDMV = GameObject.Find("RingWorld_Body/Sector_RingInterior/Sector_Zone1/Interactables_Zone1/GhostMatter_Submergible/DarkMatterVolume (3)");
            var workshopEntrancePSR = workshopEntranceDMV.transform.Find("Effects_GM_AuroraWisps (1)");

            workshopEntranceDMV.transform.localPosition = new Vector3(-108, 2.75f, -55);
            // the PSR is a child of the DMV, so we don't need to move it separately

            // I can't seem to scale the DMV along only one axis, so to keep it from leaking up through the floor, I can't widen it either.
            // I'm guessing this is because it uses a SphereShape for collision detection, and that's strictly spheres, not ellipsoids?
            // Fortunately we can still widen the PSR. This does mean the danger zone is smaller than the visual effect, but that's okay.
            workshopEntrancePSR.transform.localScale = new Vector3(2, 1, 2);
        }

        var changeTemplePathFirstTreeGM = prng.Next(0, 2);
        if (changeTemplePathFirstTreeGM == 1)
        {
            APRandomizer.OWMLModConsole.WriteLine($"RandomlyEditGhostMatterPlacement() changing first tree on path to Abandoned Temple");

            var firstTreeDMV = GameObject.Find("RingWorld_Body/Sector_RingInterior/Sector_Zone3/Interactables_Zone3/GhostMatter_CanyonPath/DarkMatterVolume"); // no number
            var firstTreePSR = GameObject.Find("RingWorld_Body/Sector_RingInterior/Sector_Zone3/Interactables_Zone3/GhostMatter_CanyonPath/Effects_GM_AuroraWisps (1)");

            firstTreeDMV.transform.localPosition = new Vector3(-6, -1, 6);
            firstTreeDMV.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f); // because the path curves, a sphere fits more naturally on the "outside",
                                                                                  // and needs to shrink when we move it to the "inside" path
            firstTreePSR.transform.localPosition = new Vector3(-6, -1, 6);
        }
        var changeTemplePathSecondTreeGM = prng.Next(0, 2);
        if (changeTemplePathSecondTreeGM == 1)
        {
            APRandomizer.OWMLModConsole.WriteLine($"RandomlyEditGhostMatterPlacement() changing second tree on path to Abandoned Temple");

            var secondTreeDMV = GameObject.Find("RingWorld_Body/Sector_RingInterior/Sector_Zone3/Interactables_Zone3/GhostMatter_CanyonPath/DarkMatterVolume (1)");
            var secondTreePSR = GameObject.Find("RingWorld_Body/Sector_RingInterior/Sector_Zone3/Interactables_Zone3/GhostMatter_CanyonPath/Effects_GM_AuroraWisps (2)");

            secondTreeDMV.transform.localPosition = new Vector3(2, -1, -5);
            secondTreePSR.transform.localPosition = new Vector3(2, -1, -4);
        }
    }
}
