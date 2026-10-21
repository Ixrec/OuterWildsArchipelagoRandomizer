using ArchipelagoRandomizer.ItemImpls.FCProgression;
using HarmonyLib;
using OWML.ModHelper;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ArchipelagoRandomizer;

// Here we don't use the usual [HarmonyPatch] annotations that are applied by CreateAndPatchAll() in the mod's Awake() method.
// Since NH isn't a dependency, we can't guarantee NH will be loaded before us, so we have to wait until we are sure to manually apply this patch.

internal class WarpDrive
{
    // List of systems that we want to remove the entry requirements for
    private static string[] SkipEntrySystems = {
        "GameWyrm.HearthsNeighbor",
        "DeepBramble",
    };

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
                {
                    var nd = new NotificationData(NotificationTarget.Player, "ADVANCED WARP CORE INSTALLED ONTO SHIP\nINTERSTELLAR TRAVEL NOW POSSIBLE", 10);
                    NotificationManager.SharedInstance.PostNotification(nd, false);
                    if (WarpDriveVisuals != null)
                    {
                        WarpDriveVisuals.SetActive(true);
                    }
                    if (nhm != null)
                    {
                        // Force-enable the warp drive immediately
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
        // CanWarp Patch
        var scm = Type.GetType("NewHorizons.Handlers.StarChartHandler, NewHorizons"); // since this is from another assembly we need the explicit ", AssemblyName"
        if (scm == null)
        {
            APRandomizer.OWMLModConsole.WriteLine($"failed to apply CanWarpPatch, SCM was null", OWML.Common.MessageType.Warning);
            return false;
        }

        var canWarpMethod = scm.GetMethod("CanWarp", BindingFlags.Public | BindingFlags.Static);
        if (canWarpMethod == null)
        {
            APRandomizer.OWMLModConsole.WriteLine($"failed to apply CanWarpPatch, canWarpMethod was null", OWML.Common.MessageType.Warning);
            return false;
        }

        var canWarpPostfixPatch = typeof(WarpDrive).GetMethod("CanWarp_PrefixPatch", BindingFlags.NonPublic | BindingFlags.Static);
        if (canWarpPostfixPatch == null)
        {
            APRandomizer.OWMLModConsole.WriteLine($"failed to apply CanWarpPatch, CanWarp_PrefixPatch was null", OWML.Common.MessageType.Warning);
            return false;
        }

        var harmony = new Harmony("Ixrec.ArchipelagoRandomizer.DelayedPatches");
        harmony.Patch(canWarpMethod, prefix: new HarmonyMethod(canWarpPostfixPatch));

        // HasUnlockedSystem Patch
        var hasUnlockedSystemMethod = scm.GetMethod("HasUnlockedSystem", BindingFlags.Public | BindingFlags.Static);
        if (hasUnlockedSystemMethod == null)
        {
            APRandomizer.OWMLModConsole.WriteLine($"failed to apply HasUnlockedSystemPatch, hasUnlockedSystemMethod was null", OWML.Common.MessageType.Warning);
            return false;
        }

        var hasUnlockedSystemPostfixPatch = typeof(WarpDrive).GetMethod("HasUnlockedSystem_PostfixPatch", BindingFlags.NonPublic | BindingFlags.Static);
        if (hasUnlockedSystemPostfixPatch == null)
        {
            APRandomizer.OWMLModConsole.WriteLine($"failed to apply HasUnlockedSystemPatch, HasUnlockedSystem_PostfixPatch was null", OWML.Common.MessageType.Warning);
            return false;
        }

        harmony.Patch(hasUnlockedSystemMethod, postfix: new HarmonyMethod(hasUnlockedSystemPostfixPatch));

        // WarpDriveVisuals Patch
        nhm = Type.GetType("NewHorizons.Main, NewHorizons");
        if (nhm == null)
        {
            APRandomizer.OWMLModConsole.WriteLine($"failed to apply WarpDriveVisualsPatch, NHM was null", OWML.Common.MessageType.Warning);
            return false;
        }

        var enableWarpDriveVisualsMethod = nhm.GetMethod("EnableWarpDriveVisuals");
        if (enableWarpDriveVisualsMethod == null)
        {
            APRandomizer.OWMLModConsole.WriteLine($"failed to apply WarpDriveVisualsPatch, enableWarpDriveVisualsMethod was null", OWML.Common.MessageType.Warning);
            return false;
        }

        var hideWarpDrivePostfixPatch = typeof(WarpDrive).GetMethod("HideWarpDrive_PostfixPatch", BindingFlags.NonPublic | BindingFlags.Static);
        if (hideWarpDrivePostfixPatch == null)
        {
            APRandomizer.OWMLModConsole.WriteLine($"failed to apply WarpDriveVisualsPatch, HideWarpDrive_PostfixPatch was null", OWML.Common.MessageType.Warning);
            return false;
        }

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
        // Preventing the warp drive from being added makes it hard to add ourselves. Instead we just disable it immediately afterwards
        if (!HasWarpDrive && APRandomizer.APWorldVersion >= Version.Parse("1.4.1"))
        {
            APRandomizer.OWMLModConsole.WriteLine("HideWarpDrive_PostfixPatch() hiding warp drive visuals", OWML.Common.MessageType.Info);
            WarpDriveVisuals = GameObject.Find("Ship_Body/WarpDrive");
            WarpDriveVisuals.SetActive(false);
        }
    }

    private static void HasUnlockedSystem_PostfixPatch(ref bool __result, string system)
    { // The default entrance is interesting, but the associated ship log requires the translator. Here we use a free location on the other side of the warp.
        if (system == "Samster68.BanjoGalaxy")
        {
            // In case a user has FQ installed, but not enabled in yaml
            if (!LocationNames.locationToArchipelagoId.ContainsKey(Location.FQ_NEW_GALAXY)) return;

            __result = APRandomizer.APSession.Locations.AllLocationsChecked.Contains(LocationNames.locationToArchipelagoId[Location.FQ_NEW_GALAXY]);
        }

        if (SkipEntrySystems.Contains(system))
            __result = true;
    }
}
