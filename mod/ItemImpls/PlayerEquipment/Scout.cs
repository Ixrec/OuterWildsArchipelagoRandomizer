using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class Scout
{
    private static bool _hasScout = false;

    public static bool hasScout
    {
        get => _hasScout;
        set
        {
            if (_hasScout != value)
            {
                _hasScout = value;
                ApplyHasScoutFlag(_hasScout);
            }
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(ToolModeSwapper), nameof(ToolModeSwapper.EquipToolMode))]
    public static void ToolModeSwapper_EquipToolMode_Prefix(ToolMode mode)
    {
        if (mode == ToolMode.Probe && !hasScout)
        {
            //APRandomizer.OWMLModConsole.WriteLine($"deactivating Scout model in player launcher since they don't have the Scout item yet");
            getScoutInPlayerLauncher()?.SetActive(false);
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(ProbeLauncher), nameof(ProbeLauncher.LaunchProbe))]
    public static bool ProbeLauncher_LaunchProbe_Prefix()
    {
        if (!hasScout) {
            APRandomizer.OWMLModConsole.WriteLine($"blocked attempt to launch the scout");
            return false;
        }
        return true;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(ProbeLauncher), nameof(ProbeLauncher.Start))]
    public static void ProbeLauncher_Start_Postfix(ProbeLauncher __instance)
    {
        if (!hasScout)
        {
            //APRandomizer.OWMLModConsole.WriteLine($"putting the Scout Launcher in photo mode since we don't have the Scout yet");
            __instance._photoMode = true;
        }
    }

    static ScreenPrompt launchScoutPrompt = null;
    static ScreenPrompt cannotLaunchScoutPrompt = null;

    // doing this earlier in Awake causes other methods to throw exceptions when the prompt unexpectedly has 0 buttons instead of 1
    [HarmonyPostfix, HarmonyPatch(typeof(ProbePromptController), nameof(ProbePromptController.LateInitialize))]
    public static void ProbePromptController_LateInitialize_Postfix(ProbePromptController __instance)
    {
        launchScoutPrompt = __instance._launchPrompt;

        cannotLaunchScoutPrompt = new ScreenPrompt("Scout Not Available", 0);
        Locator.GetPromptManager().AddScreenPrompt(cannotLaunchScoutPrompt, PromptPosition.UpperRight, false);

        ApplyHasScoutFlag(hasScout);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(ProbePromptController), nameof(ProbePromptController.Update))]
    public static void ProbePromptController_Update_Postfix(ProbePromptController __instance)
    {
        cannotLaunchScoutPrompt.SetVisibility(false);
        if (launchScoutPrompt.IsVisible() && !_hasScout)
        {
            launchScoutPrompt.SetVisibility(false);
            cannotLaunchScoutPrompt.SetVisibility(true);
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(ProbePromptController), nameof(ProbePromptController.OnProbeLauncherUnequipped))]
    public static void ProbePromptController_OnProbeLauncherUnequipped_Postfix(ProbePromptController __instance)
    {
        cannotLaunchScoutPrompt.SetVisibility(false);
    }

    public static void ApplyHasScoutFlag(bool hasScout)
    {
        // I usually try to fetch references like this only once during startup, but there are so many ways the
        // Scout models can get invalidated or revalidated later on that we have to fetch them here on the fly.
        GameObject scoutInsideShip = null;
        GameObject scoutInShipLauncher = null;
        var ship = Locator.GetShipBody()?.gameObject?.transform;
        if (ship != null)
        {
            scoutInsideShip = ship.Find("Module_Supplies/Systems_Supplies/ExpeditionGear/EquipmentGeo/Props_HEA_Probe_STATIC")?.gameObject;
            scoutInShipLauncher = ship.Find("Module_Cockpit/Systems_Cockpit/ProbeLauncher/Props_HEA_Probe_Prelaunch")?.gameObject;
        }
        GameObject scoutInPlayerLauncher = getScoutInPlayerLauncher();

        scoutInShipLauncher?.SetActive(hasScout);
        scoutInPlayerLauncher?.SetActive(hasScout);

        if (hasScout)
            scoutInsideShip?.SetActive(!Locator.GetPlayerSuit()?.IsWearingSuit() ?? false);
        else
            scoutInsideShip?.SetActive(false);
    }

    private static GameObject getScoutInPlayerLauncher()
    {
        var player = Locator.GetPlayerBody()?.gameObject?.transform;
        if (player != null)
            return player.Find("PlayerCamera/ProbeLauncher/Props_HEA_ProbeLauncher/Props_HEA_Probe_Prelaunch")?.gameObject;
        return null;
    }

    // todo:
    // taking off the suit re-activates all of the ship models for its parts,
    // including the scout model, even if we don't have the item yet
    // have not figured out where the code is to stagger these visual activations
    // re-disabling it in PlayerSpacesuit.RemoveSuit does not work
}
