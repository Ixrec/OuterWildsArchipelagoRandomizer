using HarmonyLib;
using System;
using System.Reflection;

namespace ArchipelagoRandomizer;

// Here we don't use the usual [HarmonyPatch] annotations that are applied by CreateAndPatchAll() in the mod's Awake() method.
// Since FC isn't a dependency, we can't guarantee FC will be loaded before us, so we have to wait until we are sure to manually apply this patch.

internal class SadDitylumPatch
{
    private static bool patchApplied = false;
    public static void EnsurePatchApplied()
    {
        if (!patchApplied)
            patchApplied = ApplyPatch();
    }

    private static bool ApplyPatch()
    {
        var sdm = Type.GetType("DeepBramble.Ditylum.SadDitylumManager, DeepBramble"); // since this is from another assembly we need the explicit ", AssemblyName"
        if (sdm == null)
        {
            APRandomizer.OWMLModConsole.WriteLine($"failed to apply SadDitylumPatch, SDM was null", OWML.Common.MessageType.Warning);
            return false;
        }

        var originalMethod = sdm.GetMethod("Sit", BindingFlags.NonPublic | BindingFlags.Instance);
        if (originalMethod == null)
        {
            APRandomizer.OWMLModConsole.WriteLine($"failed to apply SadDitylumPatch, originalMethod was null", OWML.Common.MessageType.Warning);
            return false;
        }

        var sitPostfixPatch = typeof(SadDitylumPatch).GetMethod("Sit_PostfixPatch");
        if (sitPostfixPatch == null)
        {
            APRandomizer.OWMLModConsole.WriteLine($"failed to apply SadDitylumPatch, sitPostfixPatch was null", OWML.Common.MessageType.Warning);
            return false;
        }

        var harmony = new Harmony("Ixrec.ArchipelagoRandomizer.DelayedPatches");
        harmony.Patch(originalMethod, postfix: new HarmonyMethod(sitPostfixPatch));
        APRandomizer.OWMLModConsole.WriteLine($"successfully applied SadDitylumPatch", OWML.Common.MessageType.Success);
        return true;
    }

    private static void Sit_PostfixPatch()
    {
        Spawn.ResetSpawnSystem();
    }
}
