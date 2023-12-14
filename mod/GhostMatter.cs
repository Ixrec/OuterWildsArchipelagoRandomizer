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

            Randomizer.Instance.ModHelper.Console.WriteLine($"GhostMatter.Setup fetching references to ghost matter particle renderers");

            var all_psrs = GameObject.FindObjectsOfType<ParticleSystemRenderer>();
            var wisp_psrs = all_psrs.Where(psr => psr.mesh?.name == "Effects_GM_WillOWisp");

            // There is one WillOWisp PSR that's visible to the naked eye without taking a photo:
            // In the Timber Hearth village's ghost matter tutorial area, every so often there's
            // a visible green flash, and it leaves a little trail of smaller flashes behind it.
            // That trail is a PSR named "ObjectTrail", and it's the only visible WillOWisp in the game.
            // Anyway, we're not interested in editing that one, so that's why we do an isVisible filter here.
            var invisible_wisp_psrs = wisp_psrs.Where(psr => !psr.isVisible);

            ghostMatterParticleRenderers = invisible_wisp_psrs.ToList();
        };
    }

    public static bool hasGhostMatterKnowledge = false;

    public static void SetHasGhostMatterKnowledge(bool hasGhostMatterKnowledge)
    {
        if (GhostMatter.hasGhostMatterKnowledge != hasGhostMatterKnowledge)
        {
            GhostMatter.hasGhostMatterKnowledge = hasGhostMatterKnowledge;

            if (hasGhostMatterKnowledge)
            {
                var nd = new NotificationData(NotificationTarget.Player, "RECONFIGURING CAMERA TO CAPTURE GHOST MATTER WAVELENGTH", 10);
                NotificationManager.SharedInstance.PostNotification(nd, false);
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ProbeLauncher), nameof(ProbeLauncher.TakeSnapshotWithCamera))]
    public static void ProbeLauncher_TakeSnapshotWithCamera_Prefix(ProbeCamera camera)
    {
        foreach (var psr in ghostMatterParticleRenderers)
            psr.enabled = hasGhostMatterKnowledge;
    }
}
