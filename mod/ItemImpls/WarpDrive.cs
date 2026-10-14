using ArchipelagoRandomizer.ItemImpls.FCProgression;
using HarmonyLib;
using OWML.ModHelper;
using System;
using System.Reflection;

namespace ArchipelagoRandomizer;

// Here we don't use the usual [HarmonyPatch] annotations that are applied by CreateAndPatchAll() in the mod's Awake() method.
// Since FC isn't a dependency, we can't guarantee FC will be loaded before us, so we have to wait until we are sure to manually apply this patch.

internal class WarpDrivePatch
{
    private static bool _hasWarpDrive = false;

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
                    var nd = new NotificationData(NotificationTarget.Player, "ADVANCED WARP CORE NOW INSTALLED", 10);
                    NotificationManager.SharedInstance.PostNotification(nd, false);
                }
            }
        }
    }

    private static bool patchApplied = false;
    public static void EnsurePatchApplied()
    {
        if (!patchApplied)
            patchApplied = ApplyPatch();
    }

    private static bool ApplyPatch()
    {
        var scm = Type.GetType("NewHorizons.Handlers.StarChartHandler, NewHorizons"); // since this is from another assembly we need the explicit ", AssemblyName"
        if (scm == null)
        {
            APRandomizer.OWMLModConsole.WriteLine($"failed to apply WarpDrivePatch, SCM was null", OWML.Common.MessageType.Warning);
            return false;
        }

        var canWarpMethod = scm.GetMethod("CanWarp", BindingFlags.Public | BindingFlags.Static);
        if (canWarpMethod == null)
        {
            APRandomizer.OWMLModConsole.WriteLine($"failed to apply WarpDrivePatch, canWarpMethod was null", OWML.Common.MessageType.Warning);
            return false;
        }

        var canShowWarpDriveMethod = scm.GetMethod("CanShowWarpDriveModel", BindingFlags.Public | BindingFlags.Static);
        if (canShowWarpDriveMethod == null)
        {
            APRandomizer.OWMLModConsole.WriteLine($"failed to apply WarpDrivePatch, canShowWarpDriveMethod was null", OWML.Common.MessageType.Warning);
            return false;
        }

        var canWarpPostfixPatch = typeof(WarpDrivePatch).GetMethod("CanWarp_PrefixPatch", BindingFlags.NonPublic | BindingFlags.Static);
        if (canWarpPostfixPatch == null)
        {
            APRandomizer.OWMLModConsole.WriteLine($"failed to apply WarpDrivePatch, CanWarp_PrefixPatch was null", OWML.Common.MessageType.Warning);
            return false;
        }

        var harmony = new Harmony("Ixrec.ArchipelagoRandomizer.DelayedPatches");
        harmony.Patch(canWarpMethod, prefix: new HarmonyMethod(canWarpPostfixPatch));
        harmony.Patch(canShowWarpDriveMethod, prefix: new HarmonyMethod(canWarpPostfixPatch));
        APRandomizer.OWMLModConsole.WriteLine($"successfully applied WarpDrivePatch", OWML.Common.MessageType.Success);
        return true;
    }

    private static bool CanWarp_PrefixPatch(ref bool __result)
    {
        if (!HasWarpDrive) // TODO: Has item condition
        {
            __result = false;
            return false;
        }
        return true;
    }
}
