using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ArchipelagoRandomizer.Compatibility.NomaiVR;

/// <summary>
/// NomaiVR tool-holster fixes.
///
/// This first generally prevents soft-locks when trying to equip tools suitless: NomaiVR does not handle a tool
/// failing to enter tool mode (the tool gets "stuck in hand"), which breaks all tool-equips until restart.
/// We detect the failed equip and reset the stuck holster.
///
/// Secondly this hides un-usable holsters in the first place: if the randomizer hasn't granted the Signalscope or
/// any Translator, they just aren't shown. If any translator is unlocked, it's shown everywhere, but this class
/// also provides a mechanism to inform the player of unusable translators when in different sectors.
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

    // the holsters we hide/show by AP ownership, keyed by tool mode.
    private static readonly Dictionary<ToolMode, GameObject> managedHolsters = new();

    // How many frames to keep retrying the scene-load sweep while waiting for NomaiVR to create the holsters.
    private const int MaxSweepAttempts = 10;

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

    private static bool IsHidableToolMode(ToolMode mode) =>
        mode is ToolMode.SignalScope or ToolMode.Translator;

    private static bool IsToolOwned(ToolMode mode) => mode switch
    {
        ToolMode.SignalScope => SignalscopeManager.hasSignalscope,
        ToolMode.Translator => Translator.hasAnyTranslator,
        _ => true, // we never try to hide anything else
    };

    /// <summary>
    /// Called from APRandomizer's OnCompleteSceneLoad dispatcher. NomaiVR (re)creates the holsters on every playable
    /// scene load, so we re-find them and apply their visibility here.
    /// </summary>
    public static void OnCompleteSceneLoad(OWScene scene, OWScene loadScene)
    {
        if (!VRCompatibility.IsNomaiVRLoaded) return;
        // NomaiVR only creates the holsters in its PlayableScenes (SolarSystem + EyeOfTheUniverse).
        if (loadScene != OWScene.SolarSystem && loadScene != OWScene.EyeOfTheUniverse) return;

        // NomaiVR creates the holsters in its module Start(), which runs after OnCompleteSceneLoad, so we continously
        // retry until they exist.
        MainThreadDispatcher.Enqueue(() => TrySetHolsterVisibilityAndRegister(0));
    }

    private static void TrySetHolsterVisibilityAndRegister(int attempt)
    {
        if (!VRCompatibility.IsNomaiVRLoaded || !EnsureReflection()) return;

        // Register and apply each holster as we find it. Signalscope and Translator are separate NomaiVR modules
        // whose holsters may be created on different frames.
        foreach (var obj in Object.FindObjectsOfType(holsterToolType))
        {
            if (!(modeField.GetValue(obj) is ToolMode mode) || !IsHidableToolMode(mode)) continue;
            managedHolsters[mode] = ((Component)obj).gameObject;
            ApplyHolsterAvailability(mode);
        }

        // Keep retrying until BOTH managed holsters are registered.
        var haveBoth =
            managedHolsters.TryGetValue(ToolMode.SignalScope, out var sig) && sig != null &&
            managedHolsters.TryGetValue(ToolMode.Translator, out var trn) && trn != null;
        if (!haveBoth && attempt < MaxSweepAttempts)
            MainThreadDispatcher.Enqueue(() => TrySetHolsterVisibilityAndRegister(attempt + 1));
    }

    public static void ApplyHolsterAvailability(ToolMode mode)
    {
        if (!managedHolsters.TryGetValue(mode, out var go) || go == null) return;
        var owned = IsToolOwned(mode);
        if (go.activeSelf != owned) go.SetActive(owned);
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

    /// <summary>
    /// Show a notification telling the player they can't equip the translator.
    /// 
    /// In VR the only way to equip the translator is a deliberate holster grab and there's no built-in feedback for
    /// a rejected grab, so show a HUD notification. This will only show if the suit is equipped, so suitless
    /// there's still no notification, but that's fine.
    /// </summary>
    public static void ShowTranslatorNotAvailableNotification(string cannotTranslatePromptText)
    {
        var nd = new NotificationData(NotificationTarget.Player, cannotTranslatePromptText.ToUpper(), 3f, false);
        NotificationManager.SharedInstance.PostNotification(nd, false);
    }
}