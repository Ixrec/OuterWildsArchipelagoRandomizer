using System;
using System.Reflection;
using HarmonyLib;
using Object = UnityEngine.Object;

namespace ArchipelagoRandomizer.Compatibility.NomaiVR;

/// <summary>
/// Fixes pre-suit tool-grab softlock under NomaiVR.
///
/// NomaiVR does not handle tools not entering tool mode (tools get "stuck in hand"), this breaks tool-equips until
/// restart when suitless.
/// </summary>
[HarmonyPatch]
internal static class VRToolHolster
{
    private static bool reflectionResolved;
    private static bool reflectionFailed;
    private static Type holsterToolType;
    private static FieldInfo modeField;
    private static FieldInfo handField;
    private static MethodInfo unequipMethod;

    [HarmonyPostfix, HarmonyPatch(typeof(ToolModeSwapper), nameof(ToolModeSwapper.EquipToolMode))]
    public static void ToolModeSwapper_EquipToolMode_Postfix(ToolMode mode)
    {
        if (!VRCompatibility.IsNomaiVRLoaded) return;

        if (mode != ToolMode.SignalScope && mode != ToolMode.Translator && mode != ToolMode.Probe) return;

        // If the equip succeeded the tool is actually in that mode now -> nothing latched, nothing to do.
        if (Locator.GetToolModeSwapper()?.IsInToolMode(mode, ToolGroup.Suit) == true) return;

        // The equip failed (vetoed by AP, a suit requirement, or anything else). Reset the holster later
        // when all this has been processed.
        MainThreadDispatcher.Enqueue(() => ResetStuckHolster(mode));
    }

    private static void ResetStuckHolster(ToolMode mode)
    {
        if (!VRCompatibility.IsNomaiVRLoaded) return;
        if (!EnsureReflection()) return;

        // If the tool (somehow) ended up equipped after all, nothing is stuck.
        if (Locator.GetToolModeSwapper()?.IsInToolMode(mode, ToolGroup.Suit) == true) return;

        foreach (var obj in Object.FindObjectsOfType(holsterToolType))
        {
            // Match the holster for this tool mode.
            if (!(modeField.GetValue(obj) is ToolMode holsterMode) || holsterMode != mode) continue;

            // A non-null `hand` means the holster still thinks it's holding the tool — the latched/stuck state.
            if (!(handField.GetValue(obj) is Object hand) || hand == null) continue;

            // Call NomaiVR's own Unequip()
            unequipMethod.Invoke(obj, null);
            APRandomizer.OWMLModConsole.WriteLine($"reset stuck {mode} holster (NomaiVR softlock prevention)");
        }
    }

    /// <summary>Resolves (once) the NomaiVR HolsterTool members we reflect against. Returns false if unavailable.</summary>
    private static bool EnsureReflection()
    {
        if (reflectionResolved) return !reflectionFailed;
        reflectionResolved = true;

        holsterToolType = Type.GetType("NomaiVR.Tools.HolsterTool, NomaiVR");
        modeField = holsterToolType?.GetField("mode", BindingFlags.Public | BindingFlags.Instance);
        handField = holsterToolType?.GetField("hand", BindingFlags.NonPublic | BindingFlags.Instance);
        unequipMethod = holsterToolType?.GetMethod("Unequip", BindingFlags.NonPublic | BindingFlags.Instance);

        if (holsterToolType == null || modeField == null || handField == null || unequipMethod == null)
        {
            reflectionFailed = true;
            APRandomizer.OWMLModConsole.WriteLine(
                "Could not resolve NomaiVR HolsterTool members for the tool-holster softlock fix; " +
                "the pre-suit holster softlock will not be auto-corrected.",
                OWML.Common.MessageType.Warning);
        }

        return !reflectionFailed;
    }
}