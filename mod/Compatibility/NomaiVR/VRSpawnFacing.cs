using System;
using System.Reflection;
using HarmonyLib;

namespace ArchipelagoRandomizer.Compatibility.NomaiVR;

/// <summary>
/// NomaiVR has a feature where both the player and Timber Hearth itself are rotated to make sure the player
/// wakes up (standing in VR), facing Giant's Deep and witnessing the probe launch.
///
/// This set of patches disables this when either the orbits are randomized or the player doesn't spawn on TH,
/// since in both cases the player won't be facing Giant's Deep anyway and the patches NomaiVR apply cause
/// more problems than they solve.
/// </summary>
internal static class VRSpawnFacing
{
    private static readonly Harmony harmony = new("Ixrec.ArchipelagoRandomizer.NomaiVRSpawnFacing");

    private static bool installed;
    private static bool installFailed;

    /// <summary>
    /// Installs the NomaiVR spawn-facing suppression patches once NomaiVR is loaded.
    /// </summary>
    public static void EnsurePatches()
    {
        if (installed || installFailed) return;
        if (!VRCompatibility.IsNomaiVRLoaded) return; // NomaiVR not up yet; try again on a later scene load.

        var patchType = Type.GetType("NomaiVR.EffectFixes.FixProbeCannonVisibility+Behaviour+Patch, NomaiVR");
        var rotatePlayer = patchType?.GetMethod("RotatePlayer", BindingFlags.NonPublic | BindingFlags.Static);
        var rotateTimberHearth = patchType?.GetMethod("RotateTimberHearth", BindingFlags.NonPublic | BindingFlags.Static);

        if (patchType == null || rotatePlayer == null || rotateTimberHearth == null)
        {
            installFailed = true;
            APRandomizer.OWMLModConsole.WriteLine(
                "Could not resolve NomaiVR's FixProbeCannonVisibility methods for the spawn-facing fix; " +
                "NomaiVR's 'face Giant's Deep' logic will not be suppressed on non-vanilla spawns or randomized orbits.",
                OWML.Common.MessageType.Warning);
            return;
        }

        var skipPrefix = new HarmonyMethod(typeof(VRSpawnFacing), nameof(SuppressGiantsDeepFacing_Prefix));
        harmony.Patch(rotatePlayer, prefix: skipPrefix);
        harmony.Patch(rotateTimberHearth, prefix: skipPrefix);
        installed = true;
    }

    /// <summary>
    /// Harmony prefix shared by both NomaiVR methods. Returning false skips the original to suppress rotation patches.
    /// </summary>
    private static bool SuppressGiantsDeepFacing_Prefix() => !ShouldSuppressGiantsDeepFacing();

    private static bool ShouldSuppressGiantsDeepFacing() =>
        Spawn.spawnChoice is not (Spawn.SpawnChoice.Vanilla or Spawn.SpawnChoice.TimberHearth)
        || Orbits.AreOrbitsRandomized;
}
