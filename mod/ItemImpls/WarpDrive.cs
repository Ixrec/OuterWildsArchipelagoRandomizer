using ArchipelagoRandomizer.ItemImpls.FCProgression;
using HarmonyLib;
using OWML.ModHelper;
using System;
using System.Reflection;
using UnityEngine;

namespace ArchipelagoRandomizer;

// Here we don't use the usual [HarmonyPatch] annotations that are applied by CreateAndPatchAll() in the mod's Awake() method.
// Since NH isn't a dependency, we can't guarantee NH will be loaded before us, so we have to wait until we are sure to manually apply this patch.

internal class WarpDrive
{
    public static void RemoveEntryRequirements()
    {
        APRandomizer.OWMLModConsole.WriteLine("RemoveEntryRequirements() changing system warp requirements");
        APRandomizer.NewHorizonsAPI.DefineStarSystem("GameWyrm.HearthsNeighbor", "{ \"factRequiredForWarp\": \"TH_VILLAGE_X1\"}", APRandomizer.Instance);
        APRandomizer.NewHorizonsAPI.DefineStarSystem("DeepBramble", "{ \"factRequiredForWarp\": \"TH_VILLAGE_X1\"}", APRandomizer.Instance);
    }

    private static GameObject WarpDriveVisuals = null;
    private static bool _hasWarpDrive = false;
    private static Type nhm;

    public static bool HasWarpDrive
    {
        get => _hasWarpDrive;
        set
        {
            if (_hasWarpDrive != value)
            {
                _hasWarpDrive = value;

                if (_hasWarpDrive)
                { // Initializing the warp drive visuals doesn't seem to work mid-loop, or is at least hacky
                    var nd = new NotificationData(NotificationTarget.Player, "ADVANCED WARP CORE INSTALLED IN SHIP\nINTERSTELLAR TRAVEL NOW POSSIBLE", 10);
                    NotificationManager.SharedInstance.PostNotification(nd, false);
                    if (WarpDriveVisuals != null)
                    {
                        WarpDriveVisuals.SetActive(true);
                    }
                    if (nhm != null)
                    {
                        var enableWarpDriveFunctionalityMethod = nhm.GetMethod("EnableWarpDriveFunctionality");
                        if (enableWarpDriveFunctionalityMethod == null)
                        {
                            APRandomizer.OWMLModConsole.WriteLine($"failed to enable warp drive, enableWarpDriveFunctionalityMethod was null", OWML.Common.MessageType.Warning);
                            return;
                        }
                        var newHorizons = APRandomizer.Instance.ModHelper.Interaction.TryGetMod("xen.NewHorizons");
                        enableWarpDriveFunctionalityMethod.Invoke(newHorizons, new object[] { });
                    }
                }
            }
        }
    }

    private static bool patchApplied = false;
    public static void EnsurePatchApplied()
    {
        if (!patchApplied)
            patchApplied = ApplyPatches();
    }

    private static bool ApplyPatches()
    {
        var scm = Type.GetType("NewHorizons.Handlers.StarChartHandler, NewHorizons"); // since this is from another assembly we need the explicit ", AssemblyName"
        if (scm == null)
        {
            APRandomizer.OWMLModConsole.WriteLine($"failed to apply WarpDrivePatch, SCM was null", OWML.Common.MessageType.Warning);
            return false;
        }

        nhm = Type.GetType("NewHorizons.Main, NewHorizons");
        if (nhm == null)
        {
            APRandomizer.OWMLModConsole.WriteLine($"failed to apply WarpDrivePatch, NHM was null", OWML.Common.MessageType.Warning);
            return false;
        }

        var canWarpMethod = scm.GetMethod("CanWarp", BindingFlags.Public | BindingFlags.Static);
        if (canWarpMethod == null)
        {
            APRandomizer.OWMLModConsole.WriteLine($"failed to apply WarpDrivePatch, canWarpMethod was null", OWML.Common.MessageType.Warning);
            return false;
        }

        var enableWarpDriveVisualsMethod = nhm.GetMethod("EnableWarpDriveVisuals");
        if (enableWarpDriveVisualsMethod == null)
        {
            APRandomizer.OWMLModConsole.WriteLine($"failed to apply WarpDrivePatch, enableWarpDriveVisualsMethod was null", OWML.Common.MessageType.Warning);
            return false;
        }

        var canWarpPostfixPatch = typeof(WarpDrive).GetMethod("CanWarp_PrefixPatch", BindingFlags.NonPublic | BindingFlags.Static);
        if (canWarpPostfixPatch == null)
        {
            APRandomizer.OWMLModConsole.WriteLine($"failed to apply WarpDrivePatch, CanWarp_PrefixPatch was null", OWML.Common.MessageType.Warning);
            return false;
        }

        var hideWarpDrivePostfixPatch = typeof(WarpDrive).GetMethod("HideWarpDrive_PostfixPatch", BindingFlags.NonPublic | BindingFlags.Static);
        if (hideWarpDrivePostfixPatch == null)
        {
            APRandomizer.OWMLModConsole.WriteLine($"failed to apply WarpDrivePatch, HideWarpDrive_PostfixPatch was null", OWML.Common.MessageType.Warning);
            return false;
        }

        var harmony = new Harmony("Ixrec.ArchipelagoRandomizer.DelayedPatches");
        harmony.Patch(canWarpMethod, prefix: new HarmonyMethod(canWarpPostfixPatch));
        harmony.Patch(enableWarpDriveVisualsMethod, postfix: new HarmonyMethod(hideWarpDrivePostfixPatch));
        APRandomizer.OWMLModConsole.WriteLine($"successfully applied WarpDrivePatch", OWML.Common.MessageType.Success);
        return true;
    }

    private static bool CanWarp_PrefixPatch(ref bool __result)
    {
        if (!HasWarpDrive)
        {
            __result = false;
            return false;
        }
        return true;
    }

    private static void HideWarpDrive_PostfixPatch()
    {
        if (!HasWarpDrive)
        {
            APRandomizer.OWMLModConsole.WriteLine("HideWarpDrive_PostfixPatch() hiding warp drive visuals", OWML.Common.MessageType.Info);
            WarpDriveVisuals = GameObject.Find("Ship_Body/WarpDrive");
            WarpDriveVisuals.SetActive(false);
        }
    }
}
