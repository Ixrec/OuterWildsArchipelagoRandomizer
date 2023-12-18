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
    [HarmonyPatch(typeof(ProbeLauncher), nameof(ProbeLauncher.LaunchProbe))]
    public static bool ProbeLauncher_LaunchProbe_Prefix()
    {
        if (!hasScout) {
            Randomizer.Instance.ModHelper.Console.WriteLine($"blocked attempt to launch the scout");
            return false;
        }
        return true;
    }

    static ScreenPrompt launchScoutPrompt = null;
    static GameObject scoutInsideShip = null;
    static GameObject scoutInShipLauncher = null;

    // doing this earlier in Awake causes other methods to throw exceptions when the prompt unexpectedly has 0 buttons instead of 1
    [HarmonyPostfix]
    [HarmonyPatch(typeof(ProbePromptController), nameof(ProbePromptController.LateInitialize))]
    public static void ProbePromptController_LateInitialize_Postfix(ProbePromptController __instance)
    {
        Randomizer.Instance.ModHelper.Console.WriteLine($"ProbePromptController_LateInitialize_Postfix fetching references to scout models and scout prompt");
        launchScoutPrompt = __instance._launchPrompt;

        var ship = Locator.GetShipBody()?.gameObject?.transform;
        if (ship is null)
        {
            scoutInsideShip = null;
            scoutInShipLauncher = null;
        }
        else
        {
            scoutInsideShip = ship.Find("Module_Supplies/Systems_Supplies/ExpeditionGear/EquipmentGeo/Props_HEA_Probe_STATIC").gameObject;
            scoutInShipLauncher = ship.Find("Module_Cockpit/Systems_Cockpit/ProbeLauncher/Props_HEA_Probe_Prelaunch").gameObject;
        }

        ApplyHasScoutFlag(hasScout);
    }

    public static void ApplyHasScoutFlag(bool hasScout)
    {
        if (launchScoutPrompt is null) return;

        if (hasScout)
        {
            launchScoutPrompt._commandIdList = new List<InputConsts.InputCommandType> { InputLibrary.toolActionPrimary.CommandType };
            // copy-pasted from the body of ProbePromptController.Awake()
            launchScoutPrompt.SetText(UITextLibrary.GetString(UITextType.ProbeLaunchPrompt) + "   <CMD>");
            scoutInsideShip?.SetActive(!Locator.GetPlayerSuit()?.IsWearingSuit() ?? false);
            scoutInShipLauncher?.SetActive(true);
        }
        else
        {
            launchScoutPrompt._commandIdList = new();
            launchScoutPrompt.SetText("Scout Not Available");
            scoutInsideShip?.SetActive(false);
            scoutInShipLauncher?.SetActive(false);
        }
    }

    // todo:
    // taking off the suit re-activates all of the ship models for its parts,
    // including the scout model, even if we don't have the item yet
    // have not figured out where the code is to stagger these visual activations
    // re-disabling it in PlayerSpacesuit.RemoveSuit does not work
}
