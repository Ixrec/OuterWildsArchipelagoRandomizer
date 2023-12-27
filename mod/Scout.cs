using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace ArchipelagoRandomizer;

[HarmonyPatch]
internal class Scout
{
    public static bool hasScout = false;

    public static void SetHasScout(bool hasScout)
    {
        if (Scout.hasScout != hasScout)
        {
            Scout.hasScout = hasScout;
            ApplyHasScoutFlag(hasScout);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ToolModeSwapper), nameof(ToolModeSwapper.EquipToolMode))]
    public static void ToolModeSwapper_EquipToolMode_Prefix(ToolMode mode)
    {
        if (mode == ToolMode.Probe && !hasScout)
        {
            Randomizer.OWMLModConsole.WriteLine($"deactivating Scout model in player launcher since they don't have the Scout item yet");
            getScoutInPlayerLauncher()?.SetActive(false);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ProbeLauncher), nameof(ProbeLauncher.LaunchProbe))]
    public static bool ProbeLauncher_LaunchProbe_Prefix()
    {
        if (!hasScout) {
            Randomizer.OWMLModConsole.WriteLine($"blocked attempt to launch the scout");
            return false;
        }
        return true;
    }

    static ScreenPrompt launchScoutPrompt = null;

    // doing this earlier in Awake causes other methods to throw exceptions when the prompt unexpectedly has 0 buttons instead of 1
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ProbePromptController), nameof(ProbePromptController.LateInitialize))]
    public static void ProbePromptController_LateInitialize_Postfix(ProbePromptController __instance)
    {
        Randomizer.OWMLModConsole.WriteLine($"ProbePromptController_LateInitialize_Postfix fetching references to scout models and scout prompt");
        launchScoutPrompt = __instance._launchPrompt;

        ApplyHasScoutFlag(hasScout);
    }

    public static void ApplyHasScoutFlag(bool hasScout)
    {
        if (launchScoutPrompt is null) return;

        // I usually try to fetch references like this only once during startup, but there are so many ways the
        // Scout models can get invalidated or revalidated later on that we have to fetch them here on the fly.
        GameObject scoutInsideShip = null;
        GameObject scoutInShipLauncher = null;
        var ship = Locator.GetShipBody()?.gameObject?.transform;
        if (ship is not null)
        {
            scoutInsideShip = ship.Find("Module_Supplies/Systems_Supplies/ExpeditionGear/EquipmentGeo/Props_HEA_Probe_STATIC")?.gameObject;
            scoutInShipLauncher = ship.Find("Module_Cockpit/Systems_Cockpit/ProbeLauncher/Props_HEA_Probe_Prelaunch")?.gameObject;
        }
        GameObject scoutInPlayerLauncher = getScoutInPlayerLauncher();

        if (hasScout)
        {
            launchScoutPrompt._commandIdList = new List<InputConsts.InputCommandType> { InputLibrary.toolActionPrimary.CommandType };
            // copy-pasted from the body of ProbePromptController.Awake()
            launchScoutPrompt.SetText(UITextLibrary.GetString(UITextType.ProbeLaunchPrompt) + "   <CMD>");
            scoutInsideShip?.SetActive(!Locator.GetPlayerSuit()?.IsWearingSuit() ?? false);
            scoutInShipLauncher?.SetActive(true);
            scoutInPlayerLauncher?.SetActive(true);
        }
        else
        {
            launchScoutPrompt._commandIdList = new();
            launchScoutPrompt.SetText("Scout Not Available");
            scoutInsideShip?.SetActive(false);
            scoutInShipLauncher?.SetActive(false);
            scoutInPlayerLauncher?.SetActive(false);
        }
    }

    private static GameObject getScoutInPlayerLauncher()
    {
        var player = Locator.GetPlayerBody()?.gameObject?.transform;
        if (player is not null)
            return player.Find("PlayerCamera/ProbeLauncher/Props_HEA_ProbeLauncher/Props_HEA_Probe_Prelaunch")?.gameObject;
        return null;
    }

    // todo:
    // taking off the suit re-activates all of the ship models for its parts,
    // including the scout model, even if we don't have the item yet
    // have not figured out where the code is to stagger these visual activations
    // re-disabling it in PlayerSpacesuit.RemoveSuit does not work
}
